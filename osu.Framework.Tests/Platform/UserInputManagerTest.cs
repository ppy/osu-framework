// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class UserInputManagerTest : TestCase
    {
        [Test]
        public void IsAliveTest()
        {
            AddAssert("UserInputManager is alive", () => new UserInputManager().IsAlive);
        }
    }
}
