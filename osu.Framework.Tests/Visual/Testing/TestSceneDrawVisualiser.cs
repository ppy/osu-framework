// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Input;
using osu.Framework.Logging;
using osu.Framework.Tests.Visual.Bindables;
using osu.Framework.Tests.Visual.Containers;
using osuTK;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneDrawVisualiser : FrameworkTestScene
    {
        private FrameworkTestScene[] subscenes;
        private DrawVisualiser drawVisualiser;
        private ObjectTreeContainer objTreeContainer;
        private Drawable sceneTarget;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Avoid stack-overflow scenarios by isolating the hovered drawables through a new input manager
            Child = new PassThroughInputManager
            {
                Children = new[]
                {
                    sceneTarget = new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = new FillFlowContainer<FrameworkTestScene>
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Children = subscenes = new[]
                            {
                                createSubScene<TestSceneDynamicDepth>(),
                                createSubScene<TestSceneBindableNumbers>(),
                            },
                        }
                    },
                    drawVisualiser = new DrawVisualiser(),
                }
            };

            drawVisualiser.Show();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            foreach (var ss in subscenes)
                ss.Height = DrawHeight / 2;
        }

        private FrameworkTestScene createSubScene<TScene>() where TScene : FrameworkTestScene, new()
            => new TScene
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                RelativeSizeAxes = Axes.X,
            };

        [Test]
        public void SceneTest()
        {
            AddStep("Init", () => drawVisualiser.Target = sceneTarget);
            AddStep("Hide", () => drawVisualiser.Hide());
            AddStep("Show", () => drawVisualiser.Show());
        }

        internal void AssertColours(ReadOnlySpan<Colour4> colours)
        {
            Colour4[] inColours = colours.ToArray();
            AddAssert("Check content", () =>
            {
                if (!(objTreeContainer.TargetVisualiser is VisualisedList visList))
                {
                    Logger.Log("Not a list", level: LogLevel.Debug);
                    return false;
                }

                if (visList.Kind != VisualisedList.ViewKind.Observable)
                {
                    Logger.Log("List is not observable", level: LogLevel.Debug);
                    return false;
                }

                int idx = 0;
                foreach (VisualiserTreeNode item in visList)
                {
                    if (idx == inColours.Length)
                    {
                        Logger.Log("List is overpopulated", level: LogLevel.Debug);
                        return false;
                    }

                    if (!(item is VisualisedDrawable obj) || obj.TargetDrawable.GetType() != typeof(Box))
                    {
                        Logger.Log("List item type does not match", level: LogLevel.Debug);
                        return false;
                    }

                    if (obj.TargetDrawable.Colour != inColours[idx])
                    {
                        Logger.Log($"List item does not match at index {idx}, should be {inColours[idx]}, but is {obj.TargetDrawable.Colour}", level: LogLevel.Debug);
                        return false;
                    }
                    ++idx;
                }

                return idx == inColours.Length;
            });
        }

        [Test]
        public void TestListVisualiser()
        {
            BindableList<Box> list = null!;

            AddStep("Init test", () =>
            {
                list = new BindableList<Box>
                {
                    new Box { Colour = Colour4.Red },
                    new Box { Colour = Colour4.Green },
                    new Box { Colour = Colour4.Blue },
                };
                objTreeContainer = drawVisualiser.SpawnVisualiser<ObjectTreeContainer, VisualisedObject, object>(list);
            });

            AssertColours(stackalloc[]
            {
                Colour4.Red,
                Colour4.Green,
                Colour4.Blue,
            });

            AddStep("Populate list", () =>
            {
                list.Clear();
                list.Add(new Box { Colour = Colour4.Red });
                list.Add(new Box { Colour = Colour4.Yellow });
            });

            AssertColours(stackalloc[]
            {
                Colour4.Red,
                Colour4.Yellow,
            });

            AddStep("Add range", () =>
            {
                list.AddRange(new[]
                {
                    new Box { Colour = Colour4.Cyan },
                    new Box { Colour = Colour4.Sienna },
                    new Box { Colour = Colour4.Gold },
                });
            });

            AssertColours(stackalloc[]
            {
                Colour4.Red,
                Colour4.Yellow,
                Colour4.Cyan,
                Colour4.Sienna,
                Colour4.Gold,
            });

            AddStep("Insert", () =>
            {
                list.Insert(2, new Box { Colour = Colour4.Snow });
            });

            AssertColours(stackalloc[]
            {
                Colour4.Red,
                Colour4.Yellow,
                Colour4.Snow,
                Colour4.Cyan,
                Colour4.Sienna,
                Colour4.Gold,
            });

            AddStep("Remove item", () =>
            {
                list.RemoveAt(1);
                list.Remove(list[3]);
            });

            AssertColours(stackalloc[]
            {
                Colour4.Red,
                Colour4.Snow,
                Colour4.Cyan,
                Colour4.Gold,
            });

            AddStep("Move item", () =>
            {
                list.Move(1, 3);
            });

            AssertColours(stackalloc[]
            {
                Colour4.Red,
                Colour4.Cyan,
                Colour4.Gold,
                Colour4.Snow,
            });

            AddStep("Replace item", () =>
            {
                list[0] = new Box { Colour = Colour4.RosyBrown };
            });

            AssertColours(stackalloc[]
            {
                Colour4.RosyBrown,
                Colour4.Cyan,
                Colour4.Gold,
                Colour4.Snow,
            });
        }
    }
}
