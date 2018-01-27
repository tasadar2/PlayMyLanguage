using Newtonsoft.Json.Linq;

namespace PlayMyLanguage.Extensions
{
    public static class JTokenExtensions
    {
        public static T GetValueSafely<T>(this JToken token, string path)
        {
            return token.SelectToken(path) is JValue value ? value.Value<T>() : default(T);
        }
    }
}