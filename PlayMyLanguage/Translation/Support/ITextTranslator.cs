using System;
using System.Collections.Generic;
using PlayMyLanguage.Processors.Support;

namespace PlayMyLanguage.Translation.Support
{
    public interface ITextTranslator
    {
        /// <summary>
        /// Reports when the translation status has changed.
        /// </summary>
        event EventHandler<StatusEventArgs> ProgressChanged;

        /// <summary>
        /// Translates texts from the <param name="source"></param> language to the <param name="target"></param> language.
        /// </summary>
        /// <param name="translatables">Texts to be translated.</param>
        /// <param name="source">Source language.</param>
        /// <param name="target">Target language.</param>
        /// <returns></returns>
        Translation Translate(IEnumerable<string> translatables, Language source, Language target);
    }
}