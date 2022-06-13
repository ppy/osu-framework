// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;

namespace osu.Framework.Testing
{
    /// <summary>
    /// Denotes a method which adds <see cref="TestScene"/> steps.
    /// Invoked via <see cref="TestScene.RunSetUpSteps"/> (which is called from nUnit's [SetUp] or <see cref="TestBrowser.LoadTest"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class SetUpStepsAttribute : Attribute
    {
    }
}
