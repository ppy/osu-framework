using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Containers
{
    public class CustomizableTextContainer : TextFlowContainer
    {
        public IEnumerable<Drawable> Placeholders
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                placeholders.Clear();
                placeholders.AddRange(value);
            }
        }

        private List<Drawable> placeholders = new List<Drawable>();
        private Dictionary<string, Delegate> iconFactories = new Dictionary<string, Delegate>();

        public void AddPlaceholder(Drawable drawable)
        {
            placeholders.Add(drawable);
        }
        protected void AddIconCallback(string name, Delegate factory)
        {
            iconFactories.Add(name, factory);
        }
        protected void AddIconCallback(string name, Func<Drawable> factory)
        {
            iconFactories.Add(name, factory);
        }
        protected void AddIconCallback(string name, Func<int, Drawable> factory)
        {
            iconFactories.Add(name, factory);
        }
        protected void AddIconCallback(string name, Func<int, int, Drawable> factory)
        {
            iconFactories.Add(name, factory);
        }

        /*
         * text.Text = "This [HeartIcon] is a permanent health upgrade";
         * text.Text = "Press [KeyboardKeyIcon(65)] to advance text."; // <- 65 = keycode for "A"
         * text.Text = "I can escape [[0]] too!";

         * text.AddPlaceholder(0, someComplexDrawable);
         * text.Text = "Something [0] very special!"
         */

        internal override IEnumerable<SpriteText> addLine(TextLine line, bool newLineIsParagraph)
        {
            if (!newLineIsParagraph)
                AddInternal(new NewLineContainer(true));

            List<SpriteText> sprites = new List<SpriteText>();
            int index = 0;
            var str = line.Text;
            while (index < str.Length)
            {
                Drawable placeholderDrawable = null;
                var nextPlaceholderIndex = str.IndexOf('[', index);
                // make sure we skip ahead to the next [ as long as the current [ is escaped
                while (nextPlaceholderIndex != -1 && str.IndexOf("[[", nextPlaceholderIndex) == 0)
                    nextPlaceholderIndex = str.IndexOf('[', nextPlaceholderIndex + 2);

                string strPiece = null;
                if (nextPlaceholderIndex != -1)
                {
                    var placeholderEnd = str.IndexOf(']', nextPlaceholderIndex);
                    // make sure we skip ahead to the next ] as long as the current ] is escaped
                    while (placeholderEnd != -1 && str.IndexOf("]]", placeholderEnd) == 0)
                        placeholderEnd = str.IndexOf(']', placeholderEnd + 2);

                    if (placeholderEnd != -1)
                    {
                        strPiece = str.Substring(index, nextPlaceholderIndex - index);
                        var placeholderStr = str.Substring(nextPlaceholderIndex + 1, placeholderEnd - nextPlaceholderIndex - 1).Trim();
                        var placeholderName = placeholderStr;
                        var paramStr = "";
                        var parensOpen = placeholderStr.IndexOf('(');
                        if (parensOpen != -1)
                        {
                            placeholderName = placeholderStr.Substring(0, parensOpen).Trim();
                            var parensClose = placeholderStr.IndexOf(')', parensOpen);
                            if (parensClose != -1)
                                paramStr = placeholderStr.Substring(parensOpen + 1, parensClose - parensOpen - 1).Trim();
                            else
                                throw new ArgumentException($"Missing ) in placeholder {placeholderStr}.");
                        }

                        int placeholderIndex;
                        if (int.TryParse(placeholderStr, out placeholderIndex))
                        {
                            if (placeholderIndex >= placeholders.Count)
                                throw new ArgumentException($"This text has {placeholders.Count} placeholders. But placeholder with index {placeholderIndex} was used.");
                            else if (placeholderIndex < 0)
                                throw new ArgumentException($"Negative placeholder indices are invalid. Index {placeholderIndex} was used.");

                            placeholderDrawable = placeholders[placeholderIndex];
                        }
                        else
                        {
                            object[] args;
                            if (string.IsNullOrWhiteSpace(paramStr))
                            {
                                args = new object[0];
                            }
                            else
                            {
                                var argStrs = paramStr.Split(',');
                                args = new object[argStrs.Length];
                                for (int i = 0; i < argStrs.Length; ++i)
                                {
                                    int argVal;
                                    if (!int.TryParse(argStrs[i], out argVal))
                                        throw new ArgumentException($"The argument {argStrs[i]} in placeholder {placeholderStr} is not an integer.");

                                    args[i] = argVal;
                                }
                            }
                            Delegate cb;
                            if (!iconFactories.TryGetValue(placeholderName, out cb))
                                throw new ArgumentException($"There is no placeholder named {placeholderName}.");

                            placeholderDrawable = (Drawable)cb.DynamicInvoke(args);
                        }
                        index = placeholderEnd + 1;
                    }
                }
                if (strPiece == null)
                {
                    strPiece = str.Substring(index);
                    index = str.Length;
                }
                // unescape stuff
                strPiece.Replace("[[", "[");
                strPiece.Replace("]]", "]");
                bool first = true;
                foreach (string l in strPiece.Split('\n'))
                {
                    if (!first)
                    {
                        var lastChild = Children.LastOrDefault();
                        if (lastChild != null)
                            AddInternal(new NewLineContainer(newLineIsParagraph));
                    }

                    foreach (string word in splitWords(l))
                    {
                        if (string.IsNullOrEmpty(word)) continue;

                        var textSprite = createSpriteTextWithLine(line);
                        textSprite.Text = word;
                        sprites.Add(textSprite);
                        AddInternal(textSprite);
                    }

                    first = false;
                }
                if (placeholderDrawable != null)
                {
                    if (placeholderDrawable.Parent != null)
                        throw new ArgumentException($"All icons used by a customizable text container must not have a parent. If you get this error message it means one of your icon factories created a drawable that was already added to another parent, or you used a drawable as a placeholder that already has another parent or you used an index-based placeholder (like [2]) more than once.");
                    AddInternal(placeholderDrawable);
                }
            }

            return sprites;
        }
    }
}
