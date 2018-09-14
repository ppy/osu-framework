// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A class representing text that can be localised and formatted.
    /// </summary>
    public struct LocalisableString : IEquatable<LocalisableString>
    {
        /// <summary>
        /// The text to be used for localisation and/or formatting.
        /// </summary>
        public Bindable<string> Text { get; }

        /// <summary>
        /// Whether <see cref="Text"/> should be localised.
        /// </summary>
        public Bindable<bool> Localised { get; }

        /// <summary>
        /// The arguments to format <see cref="Text"/> with.
        /// </summary>
        public Bindable<object[]> Args { get; }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="localised">Whether the text should be localised.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisableString(string text, bool localised = true, params object[] args)
        {
            Text = new Bindable<string>(text ?? string.Empty);
            Localised = new Bindable<bool>(localised);
            Args = new Bindable<object[]>(args);
        }

        /// <summary>
        /// Creates a new <see cref="LocalisableString"/>. This localises by default.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="args">The arguments to format the text with.</param>
        public LocalisableString(string text, params object[] args)
            : this(text, true, args)
        {
        }

        public static implicit operator string(LocalisableString localisable) => localisable.Text.Value;

        public static implicit operator LocalisableString(string text) => new LocalisableString(text);

        #region IEquatable

        public static bool operator ==(LocalisableString localisable, string other) => localisable.Text.Value == other;

        public static bool operator !=(LocalisableString localisable, string other) => !(localisable == other);

        public static bool operator ==(LocalisableString localisable, LocalisableString other)
            => localisable.Text.Value == other.Text.Value && localisable.Localised.Value == other.Localised.Value && localisable.Args.Value.SequenceEqual(other.Args.Value);

        public static bool operator !=(LocalisableString localisable, LocalisableString other) => !(localisable == other);

        public bool Equals(LocalisableString other) => this == other;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LocalisableString other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Text.Value.GetHashCode();

                hashCode = (hashCode * 397) ^ Localised.Value.GetHashCode();

                if (Args.Value != null)
                    for (int i = 0; i < Args.Value.Length; i++)
                        hashCode = (hashCode * 397) ^ Args.Value[i].GetHashCode();

                return hashCode;
            }
        }

        #endregion
    }
}
