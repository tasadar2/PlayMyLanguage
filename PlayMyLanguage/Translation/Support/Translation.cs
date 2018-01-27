using System.Collections.Generic;

namespace PlayMyLanguage.Translation.Support
{
    public class Translation
    {
        public Language Source { get; set; }
        public Language Target { get; set; }
        public IDictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();
    }
}