// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Globalization;

namespace osu.Framework.Localisation
{
    public static class CultureInfoHelper
    {
        // there is a possibility that something changes the current culture before this class is used, but the framework currently doesn't do that.

        /// <summary>
        /// The system-default <see cref="CultureInfo"/> used for number, date, time and other string formatting.
        /// </summary>
        public static CultureInfo SystemCulture { get; } = CultureInfo.CurrentCulture;

        /// <summary>
        /// The system-default <see cref="CultureInfo"/> used for app languages/translations.
        /// </summary>
        public static CultureInfo SystemUICulture { get; } = CultureInfo.CurrentUICulture;

        /// <summary>
        /// Wrapper around <see cref="CultureInfo.GetCultureInfo(string)"/> providing common behaviour and exception handling.
        /// </summary>
        /// <param name="name">Name of the culture.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> with the specified <paramref name="name"/>.</param>
        /// <returns>Whether the culture was successfully retrieved and a is .NET/OS predefined culture.</returns>
        public static bool TryGetCultureInfo(string name, out CultureInfo culture)
        {
            if (string.IsNullOrEmpty(name))
            {
                culture = SystemCulture;
                return true;
            }

            try
            {
#if NET6_0_OR_GREATER
                culture = CultureInfo.GetCultureInfo(name, predefinedOnly: true);
#else
                culture = CultureInfo.GetCultureInfo(name);
#endif
                // This is best-effort for now to catch cases where dotnet is creating cultures.
                // See https://github.com/dotnet/runtime/blob/5877e8b713742b6d80bd1aa9819094be029e3e1f/src/libraries/System.Private.CoreLib/src/System/Globalization/CultureData.Icu.cs#L341-L345
                if (culture.ThreeLetterWindowsLanguageName == "ZZZ")
                {
                    culture = SystemCulture;
                    return false;
                }

                return true;
            }
            catch (CultureNotFoundException)
            {
                culture = SystemCulture;
                return false;
            }
        }

        /// <summary>
        /// Enumerates all <see cref="CultureInfo.Parent"/> cultures of this <see cref="CultureInfo"/> (including itself, but excluding <see cref="CultureInfo.InvariantCulture"/>).
        /// </summary>
        public static IEnumerable<CultureInfo> EnumerateParentCultures(this CultureInfo cultureInfo)
        {
            for (var c = cultureInfo; !EqualityComparer<CultureInfo>.Default.Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
                yield return c;
        }
    }
}
