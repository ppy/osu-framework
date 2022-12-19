// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Allocation;

namespace osu.Framework.Localisation
{
    public static class CultureInfoHelper
    {
        // there is a possibility that something changes the current culture before this class is used, but the framework currently doesn't do that.

        /// <summary>
        /// The system-default <see cref="CultureInfo"/> used for number, date, time and other string formatting.
        /// </summary>
        public static CultureInfo SystemCulture { get; private set; } = CultureInfo.CurrentCulture;

        /// <summary>
        /// The system-default <see cref="CultureInfo"/> used for app languages/translations.
        /// </summary>
        public static CultureInfo SystemUICulture { get; private set; } = CultureInfo.CurrentUICulture;

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
                culture = CultureInfo.GetCultureInfo(name, predefinedOnly: true);

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

        /// <summary>
        /// For use in tests only.
        /// Temporarily changes <see cref="SystemCulture"/> and <see cref="SystemUICulture"/>.
        /// </summary>
        internal static IDisposable ChangeSystemCulture(string culture, string uiCulture)
        {
            var previousCulture = SystemCulture;
            var previousUICulture = SystemUICulture;

            SystemCulture = new CultureInfo(culture);
            SystemUICulture = new CultureInfo(uiCulture);

            return new InvokeOnDisposal(() =>
            {
                SystemCulture = previousCulture;
                SystemUICulture = previousUICulture;
            });
        }

        /// <summary>
        /// For use in tests only.
        /// Temporarily changes <see cref="SystemCulture"/> and <see cref="SystemUICulture"/>.
        /// </summary>
        internal static IDisposable ChangeSystemCulture(string allCultures) => ChangeSystemCulture(allCultures, allCultures);
    }
}
