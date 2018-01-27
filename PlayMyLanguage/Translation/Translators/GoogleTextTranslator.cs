using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using PlayMyLanguage.Exceptions;
using PlayMyLanguage.Translation.Support;
using RestSharp;
using RestSharp.Extensions.MonoHttp;

namespace PlayMyLanguage.Translation.Translators
{
    public class GoogleTextTranslator : TextTranslator
    {
        /// <inheritdoc />
        public override Support.Translation Translate(IEnumerable<string> translatables, Language source, Language target)
        {
            var translation = new Support.Translation
            {
                Source = source,
                Target = target,
            };

            translatables = translatables.ToList();
            var count = 0;
            var total = translatables.Count();
            const int batchMax = 50;
            using (var enumerator = translatables.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var batch = new List<string>();
                    do
                    {
                        batch.Add(enumerator.Current);
                    }
                    while (batch.Count < batchMax && enumerator.MoveNext());

                    var translatedRaw = Translate(batch, source, target);
                    if (!translatedRaw.StartsWith("["))
                    {
                        throw new PaywallReachedException("Paywall reached.");
                    }
                    var json = JArray.Parse(translatedRaw);
                    ProcessTranslationBatch(batch, json, translation);

                    count += batch.Count;
                    var progress = count * 100 / total;
                    OnStatusChanged($"Translating ({count} of {total})...", progress);
                }
            }

            return translation;
        }

        private static void ProcessTranslationBatch(IList<string> batch, JToken token, Support.Translation translation)
        {
            var jsonTranslations = (JArray)token.First;
            var translationIndex = 0;
            foreach (var translatable in batch)
            {
                var accumulatedTranslation = "";
                var accumulatedTranslatable = "";
                var found = false;
                while (!found && translationIndex < jsonTranslations.Count && jsonTranslations[translationIndex].Type == JTokenType.Array)
                {
                    var jsonTranslationTemp = jsonTranslations[translationIndex];
                    var jsonTranslation = (JArray)jsonTranslationTemp;
                    accumulatedTranslation += jsonTranslation[0].Value<string>();
                    accumulatedTranslatable += jsonTranslation[1].Value<string>();

                    found = Regex.IsMatch(accumulatedTranslatable, "(\r\n\\s*)\\z");
                    translationIndex++;
                }

                if (found || translationIndex == jsonTranslations.Count)
                {
                    accumulatedTranslation = Regex.Replace(accumulatedTranslation, "(\r\n\\s*)\\z", "");
                    translation.Translations.Add(translatable, accumulatedTranslation);
                }
                else
                {
                    File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "log.log"),
                                       $"[{DateTime.Now}] Exception: Failed to locate match for {translatable}.\r\n" +
                                       "Request: " + string.Join("\r\n", batch) + "\r\n" +
                                       "Response: " + token + "\r\n" +
                                       "\r\n");
                }
            }

            if (translation.Source.Code == "auto")
            {
                var detectedLanguage = Languages.FirstOrDefault(l => l.Code == token[2].Value<string>());
                if (detectedLanguage != null)
                {
                    translation.Source = detectedLanguage;
                }
            }
        }

        private static string Translate(IList<string> translatables, Language source, Language target)
        {
            var client = new RestClient("http://translate.google.com/translate_a/")
            {
                UserAgent = "PlayMyLanguage"
            };
            var request = new RestRequest("single?client=gtx&dt=t&ie=UTF-8&oe=UTF-8", Method.POST);
            request.AddHeader("Accept-Charset", "ISO-8859-1,utf-8;q=0.7,*;q=0.7");
            request.AddQueryParameter("sl", source.Code);
            request.AddQueryParameter("tl", target.Code);
            request.AddParameter("application/x-www-form-urlencoded", "q=" + HttpUtility.UrlEncode(string.Join("\r\n", translatables) + "\r\n"), ParameterType.RequestBody);

            return client.Execute(request).Content;
        }
    }
}