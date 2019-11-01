// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Denotes a component which performs long-running tasks in its <see cref="BackgroundDependencyLoaderAttribute"/> method that are not CPU intensive.
    /// This will force a consumer to use <see cref="CompositeDrawable.LoadComponentAsync{TLoadable}"/> when loading the components, and also schedule them in a lower priority pool.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LongRunningLoadAttribute : Attribute
    {
    }
}
