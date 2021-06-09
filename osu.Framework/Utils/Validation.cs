// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using osuTK;
using osu.Framework.Graphics;

#nullable enable

namespace osu.Framework.Utils
{
    public static class Validation
    {
        /// <summary>
        /// Returns whether the two coordinates of a <see cref="Vector2"/> are not infinite or NaN.
        /// <para>For further information, see <seealso cref="float.IsFinite(float)"/>.</para>
        /// </summary>
        /// <param name="toCheck">The <see cref="Vector2"/> to check.</param>
        /// <returns>False if X or Y are Infinity or NaN, true otherwise. </returns>
        public static bool IsFinite(Vector2 toCheck) => float.IsFinite(toCheck.X) && float.IsFinite(toCheck.Y);

        /// <summary>
        /// Returns whether the components of a <see cref="MarginPadding"/> are not infinite or NaN.
        /// <para>For further information, see <seealso cref="float.IsFinite(float)"/>.</para>
        /// </summary>
        /// <param name="toCheck">The <see cref="MarginPadding"/> to check.</param>
        /// <returns>False if either component of <paramref name="toCheck"/> are Infinity or NaN, true otherwise. </returns>
        public static bool IsFinite(MarginPadding toCheck) => float.IsFinite(toCheck.Top) && float.IsFinite(toCheck.Bottom) && float.IsFinite(toCheck.Left) && float.IsFinite(toCheck.Right);

        /// <summary>
        /// Attempts to parse <paramref name="uriString"/> as an absolute or relative <see cref="Uri"/> in a platform-agnostic manner.
        /// </summary>
        /// <remarks>
        /// This method is a workaround for inconsistencies across .NET and mono runtimes;
        /// on mono runtimes paths starting with <c>/</c> are considered absolute as per POSIX,
        /// and on .NET such paths are considered to be relative.
        /// This method uses the .NET behaviour.
        /// For more info, see <a href="https://www.mono-project.com/docs/faq/known-issues/urikind-relativeorabsolute/">Mono documentation</a>.
        /// </remarks>
        /// <param name="uriString">The string representation of the URI to parse.</param>
        /// <param name="result">The resultant parsed URI, if parsing succeeded.</param>
        /// <returns><see langword="true"/> if parsing succeeded; <see langword="false"/> otherwise.</returns>
        public static bool TryParseUri(string uriString, [NotNullWhen(true)] out Uri? result)
        {
#pragma warning disable RS0030 // Bypassing banned API check, as it'll actually be used properly here
            UriKind kind = uriString.StartsWith('/') ? UriKind.Relative : UriKind.RelativeOrAbsolute;
#pragma warning restore RS0030
            return Uri.TryCreate(uriString, kind, out result);
        }
    }
}
