using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using PlayMyLanguage.Extensions;
using PlayMyLanguage.IO;
using PlayMyLanguage.Translation.Support;

namespace PlayMyLanguage.Translation.LanguageSwitchers
{
    public class RpgMakerLanguageSwitcher : ILanguageSwitcher
    {
        private string _dataPath;
        private const string DataFileSearch = "*.json";

        private string _gamePath;
        /// <inheritdoc />
        public string GamePath
        {
            get => _gamePath;
            set
            {
                _gamePath = value;
                _dataPath = GetDataPath(value);
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetTranslatables()
        {
            return Directory.EnumerateFiles(_dataPath, DataFileSearch, SearchOption.TopDirectoryOnly)
                            .SelectMany(file => GetTranslatables(JToken.Parse(File.ReadAllText(file))))
                            .Distinct();
        }

        private static IEnumerable<string> GetTranslatables(JToken token)
        {
            var translatables = new List<string>();
            switch (token.Type)
            {
                case JTokenType.Array:
                    foreach (var arrItem in (JArray)token)
                    {
                        translatables.AddRange(GetTranslatables(arrItem));
                    }
                    break;
                case JTokenType.Object:
                    foreach (var prop in (JObject)token)
                    {
                        translatables.AddRange(GetTranslatables(prop.Value));
                    }
                    break;
                case JTokenType.String:
                    var value = (string)((JValue)token).Value;
                    if (value != string.Empty && value.Any(c => c > 127))
                    {
                        value = value.Trim();
                        if (value != string.Empty)
                        {
                            translatables.Add(value.Trim());
                        }
                    }
                    break;
            }
            return translatables;
        }

        /// <inheritdoc />
        public void WriteTranslations(Language source, Language target, IDictionary<string, string> translations)
        {
            var sourceLanguagePath = Path.Combine(_dataPath, source.Code);
            var targetLanguagePath = Path.Combine(_dataPath, target.Code);
            DirectoryHelper.EnsureDirectoryExists(sourceLanguagePath);
            DirectoryHelper.EnsureDirectoryExists(targetLanguagePath);

            //TODO: determine screen size of game? calculate charactsr that can be displayed normally in a message? and subtract the character wisth of the actor image??
            var maxLength = 43;
            foreach (var file in Directory.EnumerateFiles(_dataPath, DataFileSearch, SearchOption.TopDirectoryOnly))
            {
                var filename = Path.GetFileName(file);
                var sourceFilename = Path.Combine(sourceLanguagePath, filename);
                if (!File.Exists(sourceFilename))
                {
                    File.Copy(file, sourceFilename, false);
                }

                var token = JToken.Parse(File.ReadAllText(file));
                WriteTranslations(token, translations, maxLength);
                File.WriteAllText(Path.Combine(targetLanguagePath, filename), token.ToString());
            }
        }

        private static void WriteTranslations(JToken token, IDictionary<string, string> translations, int maxLength)
        {
            switch (token.Type)
            {
                case JTokenType.Array:
                    var arr = (JArray)token;
                    if (arr.Count != 0)
                    {
                        for (var arrIndex = arr.Count - 1; arrIndex >= 0; arrIndex--)
                        {
                            WriteTranslations(arr[arrIndex], translations, maxLength);
                        }
                    }
                    break;
                case JTokenType.Object:
                    var code = (RpgCode?)token.GetValueSafely<int?>("code");
                    if (code == RpgCode.Message)
                    {
                        WriteRpgTranslation(token, translations, maxLength, code.Value);
                    }
                    else
                    {
                        foreach (var prop in (JObject)token)
                        {
                            WriteTranslations(prop.Value, translations, maxLength);
                        }
                    }
                    break;
                case JTokenType.String:
                    //fallback for non-coded
                    var jValue = (JValue)token;
                    var value = (string)jValue.Value;
                    if (value != string.Empty && value.Any(c => c > 127) && translations.TryGetValue(value.Trim(), out var translated))
                    {
                        jValue.Value = translated;
                    }
                    break;
            }
        }

        private static void WriteRpgTranslation(JToken token, IDictionary<string, string> translations, int maxLength, RpgCode rpgCode)
        {
            switch (rpgCode)
            {
                case RpgCode.Message:
                    if (token.SelectToken("parameters") is JArray parameters &&
                        parameters.Any() &&
                        parameters.First is JValue value &&
                        value.Type == JTokenType.String)
                    {
                        var untranslated = (string)value.Value;
                        if (untranslated != string.Empty && translations.TryGetValue(untranslated.Trim(), out var translated))
                        {
                            var remainingTranslations = SplitTranslation(translated, maxLength);
                            SetRpgMessage(token, remainingTranslations.Dequeue());

                            while (remainingTranslations.Count != 0)
                            {
                                var wrappedToken = token.DeepClone();
                                SetRpgMessage(wrappedToken, remainingTranslations.Dequeue());
                                token.AddAfterSelf(wrappedToken);
                            }
                        }
                    }

                    break;
            }
        }

        private static void SetRpgMessage(JToken token, string message)
        {
            var arr = (JArray)token["parameters"];
            arr.Clear();
            arr.Add(new JValue(message));
        }

        private static Queue<string> SplitTranslation(string value, int maxLength)
        {
            return SplitTranslation(value, maxLength, new Queue<string>());
        }

        private static Queue<string> SplitTranslation(string value, int maxLength, Queue<string> translations)
        {
            if (value.Length > maxLength)
            {
                var lastSpace = value.LastIndexOf(" ", maxLength, StringComparison.OrdinalIgnoreCase);
                if (lastSpace > -1 || (lastSpace = value.IndexOf(" ", maxLength, StringComparison.OrdinalIgnoreCase)) > -1)
                {
                    translations.Enqueue(value.Substring(0, lastSpace));
                    var remaining = value.Substring(lastSpace + 1);
                    if (remaining.Length > maxLength)
                    {
                        return SplitTranslation(remaining, maxLength, translations);
                    }
                    translations.Enqueue(remaining);
                    return translations;
                }
            }
            translations.Enqueue(value);
            return translations;
        }

        /// <inheritdoc />
        public void SwitchLanguages(Language target)
        {
            var backupPath = Path.Combine(_dataPath, "Backup");
            DirectoryHelper.EnsureDirectoryExists(backupPath);
            foreach (var file in Directory.EnumerateFiles(_dataPath, DataFileSearch, SearchOption.TopDirectoryOnly))
            {
                File.Copy(file, Path.Combine(backupPath, Path.GetFileName(file)), true);
            }

            var targetLanguagePath = Path.Combine(_dataPath, target.Code);
            DirectoryHelper.EnsureDirectoryExists(targetLanguagePath);
            foreach (var file in Directory.EnumerateFiles(targetLanguagePath, DataFileSearch, SearchOption.TopDirectoryOnly))
            {
                File.Copy(file, Path.Combine(_dataPath, Path.GetFileName(file)), true);
            }
        }

        /// <inheritdoc />
        public bool IsLanguageAvailable(Language language)
        {
            var languageDirectory = Path.Combine(_dataPath, language.Code);
            return Directory.Exists(languageDirectory) && Directory.EnumerateFiles(languageDirectory, DataFileSearch).Any();
        }

        /// <inheritdoc />
        public bool IsCompatible()
        {
            return Directory.Exists(_dataPath) && Directory.EnumerateFiles(_dataPath, DataFileSearch).Any();
        }

        private static string GetDataPath(string gamePath)
        {
            return Path.Combine(gamePath, "www", "data");
        }

        private enum RpgCode
        {
            Message = 401
        }
    }
}