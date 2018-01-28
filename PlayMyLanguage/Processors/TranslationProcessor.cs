using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PlayMyLanguage.Exceptions;
using PlayMyLanguage.Processors.Support;
using PlayMyLanguage.Translation.Support;

namespace PlayMyLanguage.Processors
{
    public class TranslationProcessor
    {
        private readonly ILanguageSwitcher _languageSwitcher;
        private readonly ITextTranslator _textTranslator;

        public event EventHandler<StatusEventArgs> StatusChanged;
        public event EventHandler<TranslationCompleteEventArgs> TranslationComplete;

        public TranslationProcessor(ILanguageSwitcher languageSwitcher, ITextTranslator textTranslator)
        {
            _languageSwitcher = languageSwitcher;
            _textTranslator = textTranslator;
            _textTranslator.ProgressChanged += Translator_ProgressChanged;
        }

        ~TranslationProcessor()
        {
            _textTranslator.ProgressChanged -= Translator_ProgressChanged;
        }

        public void Translate(string gamePath, Language sourceLanguage, Language targetLanguage)
        {
            try
            {
                _languageSwitcher.GamePath = gamePath;
                if (_languageSwitcher.IsCompatible())
                {
                    var paywallReached = false;
                    if (!_languageSwitcher.IsLanguageAvailable(targetLanguage))
                    {
                        OnStatusChanged("Translating...", 0);

                        var translatables = _languageSwitcher.GetTranslatables();

                        try
                        {
                            var translation = Task.Run(() => _textTranslator.Translate(translatables, sourceLanguage, targetLanguage)).Result;
                            _languageSwitcher.WriteTranslations(translation.Source, translation.Target, translation.Translations);
                        }
                        catch (PaywallReachedException)
                        {
                            paywallReached = true;
                        }
                    }

                    _languageSwitcher.SwitchLanguages(targetLanguage);

                    var gameExecutablePath = Path.Combine(gamePath, "game.exe");
                    if (paywallReached)
                    {
                        OnTranslationComplete("Paywall reached; Partial transalation.", CompletionResult.Warn);
                    }
                    else
                    {
                        if (File.Exists(gameExecutablePath))
                        {
                            Process.Start(gameExecutablePath);
                            OnTranslationComplete("Translation complete.", CompletionResult.Success);
                        }
                        else
                        {
                            OnTranslationComplete("Translation complete. Game executable could not be found.", CompletionResult.Warn);
                        }
                    }
                }
                else
                {
                    OnTranslationComplete("Data directory could not be found.", CompletionResult.Error);
                }
            }
            catch (Exception ex)
            {
                OnTranslationComplete("An exception occurred during translation. Message: " + ex.Message, CompletionResult.Error);
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, "log.log"), "Exception:\r\nMessage: " + ex.Message + "\r\nStackTrace: " + ex.StackTrace + "\r\n");
                throw;
            }
        }

        private void Translator_ProgressChanged(object sender, StatusEventArgs e)
        {
            OnStatusChanged(e.Message, e.Progress);
        }

        protected virtual void OnStatusChanged(string message, int progress)
        {
            StatusChanged?.Invoke(this,
                                  new StatusEventArgs
                                  {
                                      Message = message,
                                      Progress = progress
                                  });
        }

        protected virtual void OnTranslationComplete(string message, CompletionResult completionResult)
        {
            TranslationComplete?.Invoke(this,
                                        new TranslationCompleteEventArgs
                                        {
                                            Message = message,
                                            CompletionResult = completionResult
                                        });
        }
    }
}