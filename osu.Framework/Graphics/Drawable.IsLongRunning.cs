// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics
{
    public partial class Drawable
    {
        internal bool IsLongRunning
        {
            get
            {
                // ReSharper disable once SuspiciousTypeConversion.Global (this is used by source generators, but only in release builds).
                if (this is ISourceGeneratedLongRunningLoadCache sgCache && sgCache.KnownType == GetType())
                    return sgCache.IsLongRunning;

                return GetType().GetCustomAttribute<LongRunningLoadAttribute>() != null;
            }
        }
    }
}
