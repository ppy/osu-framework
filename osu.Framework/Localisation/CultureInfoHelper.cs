// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Framework.Localisation
{
    public static class CultureInfoHelper
    {
        /// <summary>
        /// Wrapper around <see cref="CultureInfo.GetCultureInfo(string)"/> providing common behaviour and exception handling.
        /// </summary>
        /// <param name="name">Name of the culture.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> with the specified <paramref name="name"/>.</param>
        /// <returns>Whether the culture was successfully retrieved and a is .NET/OS predefined culture.</returns>
        public static bool TryGetCultureInfo(string name, out CultureInfo culture)
        {
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
                    culture = CultureInfo.InvariantCulture;
                    return false;
                }

                return true;
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.InvariantCulture;
                return false;
            }
        }
    }
}
