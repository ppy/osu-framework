// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneIgnore : FrameworkTestScene
    {
        [Test]
        [Ignore("test")]
        public void IgnoredTest()
        {
            AddStep($"Throw {typeof(InvalidOperationException)}", () => throw new InvalidOperationException());
        }
    }
}
