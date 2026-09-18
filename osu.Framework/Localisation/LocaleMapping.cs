// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Localisation
{
    /// <summary>
    /// Maps a localisation store to a lookup string.
    /// Used by <see cref="LocalisationManager"/>.
    /// </summary>
    public class LocaleMapping
    {
        public readonly string Name;
        public readonly ILocalisationStore Storage;

        /// <summary>
        /// Create a locale mapping from a localisation store.
        /// </summary>
        /// <param name="store">The store to be used.</param>
        public LocaleMapping(ILocalisationStore store)
        {
            Name = store.UICulture.Name;
            Storage = store;
        }

        /// <summary>
        /// Create a locale mapping with a custom lookup name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="store">The store to be used.</param>
        public LocaleMapping(string name, ILocalisationStore store)
        {
            Name = name;
            Storage = store;
        }
    }
}
