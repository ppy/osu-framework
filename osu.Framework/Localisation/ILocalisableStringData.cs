// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

#nullable enable

namespace osu.Framework.Localisation
{
    /// <summary>
    /// An interface for <see cref="LocalisableString"/>'s data.
    /// </summary>
    public interface ILocalisableStringData : IEquatable<ILocalisableStringData>
    {
        /// <summary>
        /// Gets a localised <see cref="string"/> using the given localisation store and other required data.
        /// </summary>
        /// <param name="store">The localisation store.</param>
        /// <param name="preferUnicode">Whether unicode is preferred if available.</param>
        string GetLocalised(ILocalisationStore? store, bool preferUnicode);
    }
}
