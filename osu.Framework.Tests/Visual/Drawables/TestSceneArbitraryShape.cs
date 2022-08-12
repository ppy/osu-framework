// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneArbitraryShape : FrameworkTestScene
    {
        private ArbitraryShape shape;
        private Dropdown<FillRule> dropdown;
        private List<DragHandle> handles = new List<DragHandle>();
        private List<SpriteIcon> directionGuides = new List<SpriteIcon>();
        private bool isShapeValid;
        private Box boundngBox;

        public TestSceneArbitraryShape()
        {
            Add(new CircularContainer()
            {
                Masking = true,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box() { Colour = Colour4.Gray, RelativeSizeAxes = Axes.Both },
                    boundngBox = new Box()
                    {
                        Anchor = Anchor.Centre,
                        Colour = Colour4.HotPink.Opacity(0.5f)
                    },
                    shape = new HoverableShape()
                    {
                        Anchor = Anchor.Centre
                    }
                }
            });
            Add(dropdown = new BasicDropdown<FillRule>()
            {
                Width = 300,
                Items = Enum.GetValues<FillRule>()
            });

            dropdown.Current.ValueChanged += v =>
            {
                shape.FillRule = v.NewValue;
            };
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            shape.Texture = store.Get(@"sample-texture");
        }

        protected override bool OnClick(ClickEvent e)
        {
            DragHandle handle = new DragHandle() { Position = Content.ToLocalSpace(e.ScreenSpaceMousePosition) };
            handle.Dragged += onHandleDragged;
            Add(handle);
            handles.Add(handle);
            isShapeValid = false;
            return true;
        }

        private void onHandleDragged(DragHandle handle, DragEvent e)
        {
            handle.Position = Content.ToLocalSpace(e.ScreenSpaceMousePosition);
            isShapeValid = false;
        }

        protected override void Update()
        {
            base.Update();
            if (!isShapeValid)
            {
                int guideIndex = -1;
                Vector2 lastPoint = Vector2.Zero;

                void addGuide(Vector2 from, Vector2 to)
                {
                    if (directionGuides.Count <= guideIndex)
                    {
                        SpriteIcon guide = new SpriteIcon()
                        {
                            Icon = FontAwesome.Solid.ChevronRight,
                            Size = new Vector2(20),
                            Colour = Colour4.HotPink,
                            Origin = Anchor.Centre
                        };
                        directionGuides.Add(guide);
                        Add(guide);

                    }
                    directionGuides[guideIndex].Position = (from + to) / 2;
                    directionGuides[guideIndex].Rotation = MathF.Atan2((to - from).Y, (to - from).X) / MathF.PI * 180;
                    guideIndex++;
                }

                shape.ClearVertices();
                foreach (var i in handles)
                {
                    var pos = i.Position - Content.DrawSize / 2;
                    shape.AddVertex(pos);
                    if (guideIndex >= 0)
                    {
                        addGuide(lastPoint, i.Position);
                    }
                    else guideIndex++;
                    lastPoint = i.Position;
                }
                if (handles.Count >= 3)
                {
                    addGuide(lastPoint, handles[0].Position);
                }
                isShapeValid = true;

            }
            if (handles.Any())
                shape.Position = handles[0].Position - Content.DrawSize / 2 - shape.PositionInBoundingBox(handles[0].Position - Content.DrawSize / 2);

            boundngBox.Position = shape.Position;
            boundngBox.Size = shape.Size;
            shape.Colour = shape.IsHovered ? Colour4.Red : Colour4.White;
        }

        private class DragHandle : Circle
        {
            public DragHandle()
            {
                Size = new Vector2(10);
                Origin = Anchor.Centre;
                Colour = Colour4.Yellow;
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                Dragged?.Invoke(this, e);
            }

            public event Action<DragHandle, DragEvent>? Dragged;
        }

        private class HoverableShape : ArbitraryShape
        {
            protected override bool OnHover(HoverEvent e)
            {
                return true;
            }
        }
    }
}
