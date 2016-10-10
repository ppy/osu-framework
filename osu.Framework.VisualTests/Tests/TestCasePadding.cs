using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Input;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.VisualTests.Tests
{
    class TestCasePadding : TestCase
    {
        public override string Name => @"Padding";

        public override string Description => @"Add fixed padding via a PaddingContainer";

        public override void Reset()
        {
            base.Reset();

            Add(new AutoSizeContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                    },
                    new PaddedBox(Color4.Blue)
                    {
                        Padding = new Padding(20),
                        Size = new Vector2(200),
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Masking = true,
                        Children = new Drawable[]
                        {
                            new PaddedBox(Color4.DarkSeaGreen)
                            {
                                Padding = new Padding(40),
                                RelativeSizeAxes = Axes.Both,
                                Origin = Anchor.Centre,
                                Anchor = Anchor.Centre
                            }
                        }
                    }
                }
            });
        }


        class PaddedBox : Container
        {
            private SpriteText t1, t2, t3, t4;

            Container content;

            protected override Container Content => content;

            public PaddedBox(Color4 colour)
            {
                AddInternal(new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour,
                    },
                    content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    t1 = new SpriteText
                    {
                        Text = Padding.Top.ToString(),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre
                    },
                    t2 = new SpriteText
                    {
                        Text = Padding.Right.ToString(),
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight
                    },
                    t3 = new SpriteText
                    {
                        Text = Padding.Bottom.ToString(),
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    },
                    t4 = new SpriteText
                    {
                        Text = Padding.Left.ToString(),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft
                    }
                });

                Masking = true;
            }

            public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
            {
                t1.Text = Padding.Top.ToString();
                t2.Text = Padding.Right.ToString();
                t3.Text = Padding.Bottom.ToString();
                t4.Text = Padding.Left.ToString();

                return base.Invalidate(invalidation, source, shallPropagate);
            }

            public bool AllowDrag = true;

            protected override bool OnDrag(InputState state)
            {
                if (!AllowDrag) return false;

                Position += state.Mouse.Delta;
                return true;
            }

            protected override bool OnDragEnd(InputState state)
            {
                return true;
            }

            protected override bool OnDragStart(InputState state) => AllowDrag;
        }
    }
}
