// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using osu.Framework.Logging;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A localisable string with formatting support.
    /// </summary>
    public class LocalisableFormattableString : IEquatable<LocalisableFormattableString>, ILocalisableStringData
    {
        public readonly string Format;
        public readonly object?[] Args;

        /// <summary>
        /// Creates a <see cref="LocalisableFormattableString"/> with an <see cref="IFormattable"/> value and a format string.
        /// </summary>
        /// <param name="interpolation">The interpolated string containing format and arguments.</param>
        protected internal LocalisableFormattableString(FormattableString interpolation)
            : this(interpolation.Format, interpolation.GetArguments())
        {
        }

        /// <summary>
        /// Creates a <see cref="LocalisableFormattableString"/> with an <see cref="IFormattable"/> value and a format string.
        /// </summary>
        /// <remarks>
        /// Note that the <paramref name="args"/> parameter is intentionally not marked with <c>params</c> to avoid taking priority over the <see cref="LocalisableFormattableString(FormattableString)"/> constructor.
        /// For more information, see https://github.com/dotnet/roslyn/issues/46.
        /// </remarks>
        /// <param name="format">The format string.</param>
        /// <param name="args">The objects to format.</param>
        protected internal LocalisableFormattableString(string format, object?[] args)
        {
            Format = format;
            Args = args;
        }

        public string GetLocalised(LocalisationParameters parameters) => FormatString(Format, Args, parameters);

        protected virtual string FormatString(string format, object?[] args, LocalisationParameters parameters)
        {
            try
            {
                return string.Format(parameters.FormatProvider, format, args.Select(argument =>
                {
                    if (argument is LocalisableString localisableString)
                        argument = localisableString.Data;

                    if (argument is ILocalisableStringData localisableData)
                        return localisableData.GetLocalised(parameters);

                    return argument;
                }).ToArray());
            }
            catch (FormatException e)
            {
                // The formatting has failed
                Logger.Log($"Localised format failed. Format provider: {parameters.FormatProvider}, format string: \"{format}\", base format string: \"{Format}\". Exception: {e}",
                    LoggingTarget.Runtime, LogLevel.Verbose);
            }

            return ToString();
        }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, Format, Args);

        public bool Equals(LocalisableFormattableString? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Format == other.Format
                   && Args.SequenceEqual(other.Args);
        }

        public virtual bool Equals(ILocalisableStringData? other) => other is LocalisableFormattableString formattable && Equals(formattable);
        public override bool Equals(object? obj) => obj is LocalisableFormattableString formattable && Equals(formattable);

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Format);
            foreach (object? arg in Args)
                hashCode.Add(arg);
            return hashCode.ToHashCode();
        }
    }
}
