// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace osu.Framework.Extensions
{
    public static class OSPlatformExtensions
    {
        /// <summary>
        /// Checks whether this <see cref="MemberInfo"/> is supported on the current runtime platform as specified by
        /// <see cref="SupportedOSPlatformAttribute"/> and <see cref="UnsupportedOSPlatformAttribute"/>.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to check the attributes of.</param>
        /// <returns><c>true</c> if this <paramref name="member"/> is supported, false otherwise.</returns>
        /// <remarks>
        /// This is only a naive check of attributes defined directly on this member, and doesn't account for the (un)supported platforms of the containing class or assembly.
        /// </remarks>
        public static bool IsSupportedOnCurrentOSPlatform(this MemberInfo member)
        {
            var supported = member.GetCustomAttributes<SupportedOSPlatformAttribute>().ToArray();
            var unsupported = member.GetCustomAttributes<UnsupportedOSPlatformAttribute>();

            if (unsupported.Any(matchesCurrentPlatform))
                return false;

            if (supported.Length == 0) // no explicit SupportedOSPlatformAttribute means that it's supported on all platforms.
                return true;

            return supported.Any(matchesCurrentPlatform);
        }

        /// <summary>
        /// Returns whether the provided <see cref="OSPlatformAttribute"/> matches the current (runtime) platform.
        /// </summary>
        /// <remarks>This is currently a naive check which doesn't support specific OS versions.</remarks>
        private static bool matchesCurrentPlatform(OSPlatformAttribute attribute)
        {
            if (attribute.PlatformName.Contains('.'))
                throw new NotImplementedException($"{nameof(OSPlatformExtensions)} doesn't currently support version identifiers in {nameof(OSPlatformAttribute)}.");

            return OperatingSystem.IsOSPlatform(attribute.PlatformName);
        }
    }
}
