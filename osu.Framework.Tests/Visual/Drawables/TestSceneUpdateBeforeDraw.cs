// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    [System.ComponentModel.Description("Tests whether drawable updates occur before drawing.")]
    public class TestSceneUpdateBeforeDraw : FrameworkTestScene
    {
        /// <summary>
        /// Tests whether a <see cref="Drawable"/> is updated before being drawn when it is added to a parent
        /// late enough into the frame that the <see cref="Drawable"/> shouldn't be drawn for the frame.
        /// </summary>
        [Test]
        public void TestUpdateBeforeDrawFromLateAddition()
        {
            var receiver = new Container
            {
                Size = new Vector2(100),
                Child = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(50),
                    Colour = Color4.Red
                }
            };

            var sender = new HookableContainer();
            var greenBox = new TestBox { Colour = Color4.Green };

            AddStep("add children", () =>
            {
                Children = new Drawable[]
                {
                    new SpriteText { Text = "Red box should be visible, green should not be visible" },
                    // Order is important
                    receiver,
                    sender,
                };
            });

            sender.OnUpdateAfterChildren = () => receiver.Add(greenBox);

            AddStep("wait one frame", () => { });
            AddAssert("green not present", () => !greenBox.IsPresent);
        }

        private class HookableContainer : Container
        {
            /// <summary>
            /// Invoked once.
            /// </summary>
            public Action OnUpdateAfterChildren;

            private bool hasInvoked;

            protected override void UpdateAfterChildren()
            {
                base.UpdateAfterChildren();

                if (hasInvoked)
                    return;

                hasInvoked = true;

                OnUpdateAfterChildren?.Invoke();
            }
        }

        /// <summary>
        /// A box which sets its alpha to 0 in <see cref="Update"/> if it hasn't been drawn yet.
        /// </summary>
        private class TestBox : Box
        {
            private bool hasDrawn;

            public TestBox()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Size = new Vector2(50);
            }

            protected override void Update()
            {
                base.Update();

                if (hasDrawn)
                    return;

                Alpha = 0;
            }

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
            {
                hasDrawn = true;
                return base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);
            }
        }
    }
}
