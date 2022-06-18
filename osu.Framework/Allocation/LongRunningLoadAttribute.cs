// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Denotes a component which performs long-running tasks in its <see cref="BackgroundDependencyLoaderAttribute"/> method that are not CPU intensive.
    /// Long-running tasks are scheduled into a lower priority thread pool.
    /// </summary>
    /// <remarks>
    /// This forces immediate consumers to use <see cref="CompositeDrawable.LoadComponentAsync{TLoadable}"/> when loading the component.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class LongRunningLoadAttribute : Attribute
    {
    }
}
