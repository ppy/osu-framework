using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="TextFlowContainer"/> that supports adding icons into its text. Inherit from this class to define reusable custom placeholders for icons.
    /// </summary>
    public class CustomizableTextContainer : TextFlowContainer
    {
        /// <summary>
        /// Sets the placeholders that should be used to replace the numeric placeholders, in the order given.
        /// </summary>
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

        /// <summary>
        /// Adds the given drawable as a placeholder that can be used when adding text. The drawable must not have a parent. Returns the index that can be used to reference the added placeholder.
        /// </summary>
        /// <param name="drawable">The drawable to use as a placeholder. This drawable must not have a parent.</param>
        /// <returns>The index that can be used to reference the added placeholder.</returns>
        public int AddPlaceholder(Drawable drawable)
        {
            placeholders.Add(drawable);
            return placeholders.Count - 1;
        }

        // I dislike these overloads as much as you, but if we only had the general overload taking a Delegate, AddIconFactory("test", someInstanceMethod) would not compile (because we would need to cast someInstanceMethod to a delegate type first).
        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [name] is encountered in the text. The factory method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [name(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Delegate factory) => iconFactories.Add(name, factory);
        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [name] is encountered in the text. The factory method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [name(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Func<Drawable> factory) => iconFactories.Add(name, factory);
        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [name] is encountered in the text. The factory method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [name(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Func<int, Drawable> factory) => iconFactories.Add(name, factory);
        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [name] is encountered in the text. The factory method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [name(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Func<int, int, Drawable> factory) => iconFactories.Add(name, factory);

        // Example usage of the syntax this class intends to support:
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
