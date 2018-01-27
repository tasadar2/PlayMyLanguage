using System;

namespace PlayMyLanguage.Exceptions
{
    public class PaywallReachedException : Exception
    {
        public PaywallReachedException(string message)
            : base(message)
        { }

        public PaywallReachedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}