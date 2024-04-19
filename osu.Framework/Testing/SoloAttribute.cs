// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Denotes a single test method which will be "solo"ed during visual execution.
    /// This implies all other tests will be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class SoloAttribute : Attribute
    {
    }
}
