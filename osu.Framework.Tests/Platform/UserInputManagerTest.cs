// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class UserInputManagerTest : TestCase
    {
        [Test]
        public void IsAliveTest()
        {
            AddAssert("UserInputManager is alive", () =>
            {
                using (var client = new TestHeadlessGameHost(@"client", true))
                {
                    return client.CurrentRoot.IsAlive;
                }
            });
        }

        private class TestHeadlessGameHost : HeadlessGameHost
        {
            public Drawable CurrentRoot => Root;

            public TestHeadlessGameHost(string hostname, bool bindIPC)
                : base(hostname, bindIPC)
            {
                using (var game = new TestGame())
                {
                    Root = game.CreateUserInputManager();
                }
            }
        }
    }
}
