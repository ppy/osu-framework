// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneDrawNodeDisposal : FrameworkTestScene
    {
        /// <summary>
        /// Tests that all references are lost after a drawable is disposed.
        /// </summary>
        [Test]
        public void TestBasicDrawNodeReferencesRemovedAfterDisposal() => performTest(new Box { RelativeSizeAxes = Axes.Both });

        /// <summary>
        /// Tests that all references are lost after a composite is disposed.
        /// </summary>
        [Test]
        public void TestCompositeDrawNodeReferencesRemovedAfterDisposal() => performTest(new NonFlattenedContainer
        {
            RelativeSizeAxes = Axes.Both,
            Children = new[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Colour = Color4.Blue
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    X = 0.5f,
                    Width = 0.5f,
                    Colour = Color4.Blue
                },
            }
        });

        /// <summary>
        /// Tests that all references are lost after a buffered container is disposed.
        /// </summary>
        [Test]
        public void TestBufferedDrawNodeReferencesRemovedAfterDisposal() => performTest(new BufferedContainer
        {
            RelativeSizeAxes = Axes.Both,
            Children = new[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Colour = Color4.Blue
                },
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    X = 0.5f,
                    Width = 0.5f,
                    Colour = Color4.Blue
                },
            }
        });

        private void performTest(Drawable child)
        {
            Container parentContainer = null;

            var drawableRefs = new List<WeakReference>();

            // Add the children to the hierarchy, and build weak-reference wrappers around them
            AddStep("create hierarchy", () =>
            {
                drawableRefs.Clear();
                buildReferencesRecursive(child);

                Child = parentContainer = new NonFlattenedContainer
                {
                    Size = new Vector2(200),
                    Child = child
                };

                void buildReferencesRecursive(Drawable target)
                {
                    drawableRefs.Add(new WeakReference(target));

                    if (target is CompositeDrawable compositeTarget)
                    {
                        foreach (var c in compositeTarget.InternalChildren)
                            buildReferencesRecursive(c);
                    }
                }
            });

            AddWaitStep("wait for some draw nodes", IRenderer.MAX_DRAW_NODES);

            // Clear the parent to ensure no references are held via drawables themselves,
            // and remove the parent to ensure that the parent maintains references to the child draw nodes
            AddStep("clear + remove parent container", () =>
            {
                parentContainer.Clear();
                Remove(parentContainer);

                // Lose last hard-reference to the child
                child = null;
            });

            // Wait for all drawables to get disposed
            DisposalMarker disposalMarker = null;
            AddStep("add disposal marker", () => AsyncDisposalQueue.Enqueue(disposalMarker = new DisposalMarker()));
            AddUntilStep("wait for drawables to dispose", () => disposalMarker.Disposed);

            // Induce the collection of drawables
            AddStep("invoke GC", () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            });

            AddUntilStep("all drawable references lost", () => !drawableRefs.Any(r => r.IsAlive));
        }

        private class NonFlattenedContainer : Container
        {
            protected override bool CanBeFlattened => false;
        }

        private class DisposalMarker : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
