// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Denotes a method which adds <see cref="TestCase"/> steps.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetUpStepsAttribute : Attribute
    {
    }
}