using System;

namespace PlayMyLanguage.Processors.Support
{
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int Progress { get; set; }
    }
}