using System;

namespace PlayMyLanguage.Processors.Support
{
    public class TranslationCompleteEventArgs : EventArgs
    {
        public string Message { get; set; }
        public CompletionResult CompletionResult { get; set; }
    }
}