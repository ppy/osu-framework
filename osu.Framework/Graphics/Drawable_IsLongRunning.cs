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
                if (this is ISourceGeneratedLongRunningLoadCache sgCache && sgCache.KnownType == GetType())
                    return sgCache.IsLongRunning;

                return GetType().GetCustomAttribute<LongRunningLoadAttribute>() != null;
            }
        }
    }
}
