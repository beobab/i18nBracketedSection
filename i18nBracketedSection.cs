using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace i18n.Helpers
{
    /// <summary>
    /// This class stores an individual bracketed section from a larger string. 
    /// It knows about sub-bracketed sections (it's children), and has a static method
    /// to call a function of your choice on all bracketed sections in a string
    /// and return the resultant string (i18nBracketedSection.FindAndTranslateBrackets).
    /// </summary>
    public class i18nBracketedSection
    {
        #region TOKENS...
        // Token are stored here in weird forms so that the nugget extractor doesn't find them in source code.
        private const string BEGIN_TOKEN = "[" + "[[";
        private const string END_TOKEN = "]" + "]]";
        private const string DELIMITER_TOKEN = "|" + "||";
        private const string COMMENT_TOKEN = "/" + "//";
        private const string PARAMETER_BEGIN_TOKEN = "(" + "((";
        private const string PARAMETER_END_TOKEN = ")" + "))";

        // All tokens must be three characters long. Or you can refactor this class.
        #endregion

        #region Instance members...

        #region Stored fields (Text and Start)...
        public string Text = "";
        public int Start = 0;
        #endregion

        #region Calculated properties (End, Found, IsComplete, Children and TranslatedText)...
        public int End { get { return Start + Text.Length; } }

        public bool Found { get { return Text.Length > 0; } }
        /// <summary>
        /// If this is a "complete" bracketed section, then the number of opening triple square brackets [ match the number or closing triple brackets ].
        /// This also applies to triple ( and triple ) for translatable parameters.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return Text.Replace(BEGIN_TOKEN, "").Length == Text.Replace(END_TOKEN, "").Length
                     && Text.Replace(PARAMETER_BEGIN_TOKEN, "").Length == Text.Replace(PARAMETER_END_TOKEN, "").Length;
            }
        }

        /// <summary>
        /// This is so you can see what the enumeration returns from the code editor.
        /// </summary>
        public List<i18nBracketedSection> ChildList { get { return Children.ToList(); } }

        /// <summary>
        /// List all the children of a Bracketed Section.
        /// </summary>
        public IEnumerable<i18nBracketedSection> Children
        {
            get
            {
                // Don't even try if this isn't a complete section.
                if (!this.Found) yield break;
                if (!this.IsComplete) yield break;

                var text = this.Text;
                // Strip off the outermost brackets.
                var b = FindBracketComponent(text, 3);

                // No children at all.
                if (!b.Found) yield break;

                // Some children. Iterate til none left.
                while (b.Found)
                {
                    var previousEnd = b.End;
                    yield return b;
                    b = FindBracketComponent(text, previousEnd);
                }
            }
        }

        /// <summary>
        /// Recursively applies the doTranslate function 
        /// </summary>
        /// <param name="doTranslate">This function must remove outermost brackets from the Text</param>
        /// <returns>A string recursively translated with the doTranslate function</returns>
        public string TranslatedText(Func<string, string> doTranslate)
        {
            var sb = new StringBuilder();
            var previousEnd = 0;
            foreach (i18nBracketedSection child in Children)
            {
                // Get the text between the previous child and this one.
                sb.Append(this.Text.Substring(previousEnd, child.Start - previousEnd));
                sb.Append(child.TranslatedText(doTranslate));
                previousEnd = child.End;
            }
            sb.Append(this.Text.Substring(previousEnd, this.Text.Length - previousEnd));
            return doTranslate(sb.ToString());
        }

        public override string ToString()
        {
            return !Found ? "<not found>" : (!IsComplete ? "<incomplete>" + Text : Text);
        }

        #endregion

        #endregion

        #region Static members (FindAndTranslateBrackets, FindBracketComponent, ReplaceSingleBracketedComponent)...

        /// <summary>
        /// This is the most important method in the class. All the rest is for sub-sections. It will translate a section of text, 
        /// using the gettext function on all bracketed sub-sections. It can handle nested brackets.
        /// </summary>
        /// <param name="inputLargeText">This is a large block of text, which may or may not contain triple square open bracket markers.</param>
        /// <param name="getTextWithContext">This is normally HttpContext.Current.GetText from i18n extensions. Any gettext variant will work here.</param>
        /// <returns>A translated piece of text, with all bracketted text translated.</returns>
        /// <remarks>
        /// Usage: 
        ///     var text = "Big long string with [["+"[StuffToTranslate///WithContext|||(((Parameters///AlsoWithContext)))]"+"]] and other text which doesn't get translated";
        ///     FindAndTranslateBrackets(text, (str, ctx) => HttpContext.Current.GetText(str, ctx));
        /// </remarks>
        public static string FindAndTranslateBrackets(string inputLargeText, Func<string, string, string> getTextWithContext)
        {
            Func<string, string> doTranslate = (s) =>
            {
                return ReplaceSingleBracketedComponent(s, (t) =>
                {
                    var contextMarker = t.IndexOf(COMMENT_TOKEN);
                    if (contextMarker < 0)
                        return getTextWithContext(t, "");
                    else
                        return getTextWithContext(t.Substring(0, contextMarker), t.Substring(contextMarker + 3));
                });
            };

            var text = inputLargeText;
            var b = FindBracketComponent(text, 0);
            // Best case where we don't have to modify the text.
            if (!b.Found) return text;

            var previousIndex = 0;
            var sb = new StringBuilder();

            while (b.Found)
            {
                sb.Append(text.Substring(previousIndex, b.Start - previousIndex));
                sb.Append(b.TranslatedText(doTranslate));
                previousIndex = b.End;
                b = FindBracketComponent(text, previousIndex);
            }

            // Add the final chunk of text after the last bracketed component.
            sb.Append(text.Substring(previousIndex, text.Length - previousIndex));

            return sb.ToString();
        }

        /// <summary>
        /// This will find the next bracket section in an inputString. It returns an empty one if it
        /// cannot find a complete valid one.
        /// </summary>
        /// <param name="inputText">String containing unbracketed text and bracketed text.</param>
        /// <param name="searchStartLocation">Zero based location where to start looking for a single bracketed term.</param>
        /// <returns></returns>
        private static i18nBracketedSection FindBracketComponent(string inputText, int searchStartLocation)
        {
            const int bracketLength = 3;

            var firstStart = inputText.IndexOf(BEGIN_TOKEN, searchStartLocation);
            // We cannot find any more bracketed sections. Exit.
            if (firstStart < 0) return new i18nBracketedSection() { Start = searchStartLocation };

            var firstEnding = inputText.IndexOf(END_TOKEN, searchStartLocation);
            // We cannot find any more bracketed sections (because they aren't closed properly). Exit.
            if (firstEnding < 0) return new i18nBracketedSection() { Start = searchStartLocation };

            // Grab the first bracketed section naively.
            var firstTryBracket = new i18nBracketedSection()
            {
                Start = firstStart,
                Text = inputText.Substring(firstStart, (firstEnding + bracketLength) - firstStart)
            };
            // If the first try is a complete one, then we're good. Return that.
            if (firstTryBracket.IsComplete)
            {
                return firstTryBracket;
            }


            // 99% of code will not go through this route. This is for nested brackets.
            // Find the first set of close brackets which successfully close all previous opens.
            // Note that this can skip over (and include) new open square bracket triples.
            var previousEnding = firstEnding;
            var nextTryBracket = new i18nBracketedSection();
            do
            {
                var nextEnding = inputText.IndexOf(END_TOKEN, previousEnding + bracketLength);
                if (nextEnding < 0)
                {
                    // Failed to find a closing bracket in the entire string. Exit.
                    return new i18nBracketedSection() { Start = searchStartLocation };
                }

                // Found another END_TOKEN. Grab up to that and see if it's a complete section.
                nextTryBracket = new i18nBracketedSection()
                {
                    Start = firstStart,
                    Text = inputText.Substring(firstStart, (nextEnding + bracketLength) - firstStart)
                };

                // Loop iterator. Very important to avoid infinite loops.
                previousEnding = nextEnding;
            } while (!nextTryBracket.IsComplete);

            return nextTryBracket;
        }


        /// <summary>
        /// This parses a parameter list in a bracketed section and applies translation to any that exist.
        /// </summary>
        private static string ReplaceSingleBracketedComponent(string inputText, Func<string, string> gettext)
        {
            // If no function present, pass through an identity function which doesn't change the string.
            // Note that this function will receive a string which might contain COMMENT_TOKEN (triple /).
            if (gettext == null) { gettext = (s) => s; }

            var text = inputText;
            if (text.StartsWith(BEGIN_TOKEN) && text.EndsWith(END_TOKEN))
            {
                // If the bracketed text doesn't have any parameters, translate it and exit.
                if (!text.Contains(DELIMITER_TOKEN))
                {
                    text = text.Replace(BEGIN_TOKEN, "").Replace(END_TOKEN, "");
                    return gettext(text);
                }

                text = text.Replace(BEGIN_TOKEN, "").Replace(END_TOKEN, "");
                // Assume zero based %0 because that's what our i18n implementation for web assumes.
                var options = text.Split(new string[] { DELIMITER_TOKEN }, StringSplitOptions.None);
                var nrOptions = options.Length;

                text = gettext(options[0]);
                // Note: We start at 1 because 0 is the main text. 1 onwards are the replacements.
                for (var i = 1; i < nrOptions; i++)
                {
                    // Note: We distinguish between translated and untranslated parameters.
                    var untranslatedOption = options[i];
                    if (untranslatedOption.Contains(PARAMETER_BEGIN_TOKEN))
                    {
                        var translatedOption = gettext(options[i].Replace(PARAMETER_BEGIN_TOKEN, "").Replace(PARAMETER_END_TOKEN, ""));
                        text = text.Replace("%" + (i - 1).ToString(), translatedOption);
                    }
                    else
                    {
                        text = text.Replace("%" + (i - 1).ToString(), untranslatedOption);
                    }
                }

            }
            return text;
        }

        #endregion
    }

}