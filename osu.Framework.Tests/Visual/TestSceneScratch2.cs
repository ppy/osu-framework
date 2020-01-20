// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestSceneScratch2 : ManualInputManagerTestScene
    {
        [Test]
        public void DoTest()
        {
            InputReceptor receptorBelow = null;
            InputReceptor receptorAbove = null;

            AddStep("add first box", () =>
            {
                Child = receptorBelow = new InputReceptor { Size = new Vector2(50) };
            });

            AddStep("press key", () => InputManager.PressKey(Key.A));

            AddStep("add second box", () =>
            {
                Add(receptorAbove = new InputReceptor { Size = new Vector2(50) });
            });

            AddStep("release key", () => InputManager.ReleaseKey(Key.A));

            AddAssert("below received key down", () => receptorBelow.DownReceived); // true (correct)
            AddAssert("below received key up", () => receptorBelow.UpReceived); // false (wrong)

            AddAssert("above has not received key down", () => !receptorAbove.DownReceived); // false (correct)
            AddAssert("above has not received key up", () => !receptorAbove.UpReceived); // true (wrong)
        }

        public class InputReceptor : Box
        {
            public bool DownReceived;

            public bool UpReceived;

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (e.Repeat)
                    return false;

                DownReceived = true;
                return true;
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
                UpReceived = true;
            }
        }
    }
}
