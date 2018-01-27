using System.Collections.Generic;

namespace PlayMyLanguage.Translation.Support
{
    public interface ILanguageSwitcher
    {
        /// <summary>
        /// Directory location for the game.
        /// </summary>
        string GamePath { get; set; }

        /// <summary>
        /// Retrieves texts to be translated.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetTranslatables();

        /// <summary>
        /// Writes translations for the game.
        /// </summary>
        /// <param name="source">Source Language.</param>
        /// <param name="target">Target Language.</param>
        /// <param name="translations"></param>
        void WriteTranslations(Language source, Language target, IDictionary<string, string> translations);

        /// <summary>
        /// Switches to the <param name="target"></param> lanaguage.
        /// </summary>
        /// <param name="target">Target language to switch to.</param>
        void SwitchLanguages(Language target);

        /// <summary>
        /// determines if a translation exists for the supplied <param name="language"></param>.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        bool IsLanguageAvailable(Language language);

        /// <summary>
        /// Determines it the language switcher is compatible with the current game directory.
        /// </summary>
        /// <returns></returns>
        bool IsCompatible();
    }
}