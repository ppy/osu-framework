﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="TextFlowContainer"/> that supports adding icons into its text. Inherit from this class to define reusable custom placeholders for icons.
    /// </summary>
    public class CustomizableTextContainer : TextFlowContainer
    {
        private const string unescaped_left = "[";
        private const string escaped_left = "[[";

        private const string unescaped_right = "]";
        private const string escaped_right = "]]";

        public static string Escape(string text) => text.Replace(unescaped_left, escaped_left).Replace(unescaped_right, escaped_right);

        public static string Unescape(string text) => text.Replace(escaped_left, unescaped_left).Replace(escaped_right, unescaped_right);

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

        private readonly List<Drawable> placeholders = new List<Drawable>();
        private readonly Dictionary<string, Delegate> iconFactories = new Dictionary<string, Delegate>();

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

        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [<paramref name="name"/>] is encountered in the text. The <paramref name="factory"/> method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [<paramref name="name"/>(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Delegate factory) => iconFactories.Add(name, factory);

        // I dislike the following overloads as much as you, but if we only had the general overload taking a Delegate, AddIconFactory("test", someInstanceMethod) would not compile (because we would need to cast someInstanceMethod to a delegate type first).
        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [<paramref name="name"/>] is encountered in the text. The <paramref name="factory"/> method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [<paramref name="name"/>(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Func<Drawable> factory) => iconFactories.Add(name, factory);

        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [<paramref name="name"/>] is encountered in the text. The <paramref name="factory"/> method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [<paramref name="name"/>(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Func<int, Drawable> factory) => iconFactories.Add(name, factory);

        /// <summary>
        /// Adds the given factory method as a placeholder. It will be used to create a drawable each time [<paramref name="name"/>] is encountered in the text. The <paramref name="factory"/> method must return a <see cref="Drawable"/> and may contain an arbitrary number of integer parameters. If there are, fe, 2 integer parameters on the factory method, the placeholder in the text would need to look like [<paramref name="name"/>(42, 1337)] supplying the values 42 and 1337 to the method as arguments.
        /// </summary>
        /// <param name="name">The name of the placeholder that the factory should create drawables for.</param>
        /// <param name="factory">The factory method creating drawables.</param>
        protected void AddIconFactory(string name, Func<int, int, Drawable> factory) => iconFactories.Add(name, factory);

        internal override IEnumerable<Drawable> AddLine(TextLine line, bool newLineIsParagraph)
        {
            if (!newLineIsParagraph)
                AddInternal(new NewLineContainer(true));

            var sprites = new List<Drawable>();
            int index = 0;
            string str = line.Text;

            while (index < str.Length)
            {
                Drawable placeholderDrawable = null;
                int nextPlaceholderIndex = str.IndexOf(unescaped_left, index, StringComparison.Ordinal);
                // make sure we skip ahead to the next [ as long as the current [ is escaped
                while (nextPlaceholderIndex != -1 && str.IndexOf(escaped_left, nextPlaceholderIndex, StringComparison.Ordinal) == nextPlaceholderIndex)
                    nextPlaceholderIndex = str.IndexOf(unescaped_left, nextPlaceholderIndex + 2, StringComparison.Ordinal);

                string strPiece = null;

                if (nextPlaceholderIndex != -1)
                {
                    int placeholderEnd = str.IndexOf(unescaped_right, nextPlaceholderIndex, StringComparison.Ordinal);
                    // make sure we skip  ahead to the next ] as long as the current ] is escaped
                    while (placeholderEnd != -1 && str.IndexOf(escaped_right, placeholderEnd, StringComparison.InvariantCulture) == placeholderEnd)
                        placeholderEnd = str.IndexOf(unescaped_right, placeholderEnd + 2, StringComparison.Ordinal);

                    if (placeholderEnd != -1)
                    {
                        strPiece = str[index..nextPlaceholderIndex];
                        string placeholderStr = str.Substring(nextPlaceholderIndex + 1, placeholderEnd - nextPlaceholderIndex - 1).Trim();
                        string placeholderName = placeholderStr;
                        string paramStr = "";
                        int parensOpen = placeholderStr.IndexOf('(');

                        if (parensOpen != -1)
                        {
                            placeholderName = placeholderStr.Substring(0, parensOpen).Trim();
                            int parensClose = placeholderStr.IndexOf(')', parensOpen);
                            if (parensClose != -1)
                                paramStr = placeholderStr.Substring(parensOpen + 1, parensClose - parensOpen - 1).Trim();
                            else
                                throw new ArgumentException($"Missing ) in placeholder {placeholderStr}.");
                        }

                        if (int.TryParse(placeholderStr, out int placeholderIndex))
                        {
                            if (placeholderIndex >= placeholders.Count)
                                throw new ArgumentException($"This text has {placeholders.Count} placeholders. But placeholder with index {placeholderIndex} was used.");
                            if (placeholderIndex < 0)
                                throw new ArgumentException($"Negative placeholder indices are invalid. Index {placeholderIndex} was used.");

                            placeholderDrawable = placeholders[placeholderIndex];
                        }
                        else
                        {
                            object[] args;

                            if (string.IsNullOrWhiteSpace(paramStr))
                            {
                                args = Array.Empty<object>();
                            }
                            else
                            {
                                string[] argStrs = paramStr.Split(',');
                                args = new object[argStrs.Length];

                                for (int i = 0; i < argStrs.Length; ++i)
                                {
                                    if (!int.TryParse(argStrs[i], out int argVal))
                                        throw new ArgumentException($"The argument \"{argStrs[i]}\" in placeholder {placeholderStr} is not an integer.");

                                    args[i] = argVal;
                                }
                            }

                            if (!iconFactories.TryGetValue(placeholderName, out Delegate cb))
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
                strPiece = Unescape(strPiece);
                sprites.AddRange(AddString(new TextLine(strPiece, line.CreationParameters), newLineIsParagraph));

                if (placeholderDrawable != null)
                {
                    if (placeholderDrawable.Parent != null)
                        throw new ArgumentException("All icons used by a customizable text container must not have a parent. If you get this error message it means one of your icon factories created a drawable that was already added to another parent, or you used a drawable as a placeholder that already has another parent or you used an index-based placeholder (like [2]) more than once.");

                    AddInternal(placeholderDrawable);
                }
            }

            return sprites;
        }
    }
}
