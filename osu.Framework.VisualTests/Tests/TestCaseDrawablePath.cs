﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Input;
using System.Collections.Generic;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseDrawablePath : TestCase
    {
        public override string Name => @"Drawable Paths";
        public override string Description => @"Various cases of drawable paths.";

        public override void Reset()
        {
            base.Reset();

            int width = 20;
            Texture gradientTexture = new Texture(width, 1, true);
            byte[] data = new byte[width * 4];
            for (int i = 0; i < width; ++i)
            {
                float brightness = (float)i / (width - 1);
                int index = i * 4;
                data[index + 0] = (byte)(brightness * 255);
                data[index + 1] = (byte)(brightness * 255);
                data[index + 2] = (byte)(brightness * 255);
                data[index + 3] = 255;
            }
            gradientTexture.SetData(new TextureUpload(data));

            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Simple path",
                                        TextSize = 20,
                                        Colour = Color4.White,
                                    },
                                    new Path
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Positions = new List<Vector2> { Vector2.One * 50, Vector2.One * 100 },
                                        Texture = gradientTexture,
                                        Colour = Color4.Green,
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Curved path",
                                        TextSize = 20,
                                        Colour = Color4.White,
                                    },
                                    new Path
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Positions = new List<Vector2>
                                        {
                                            new Vector2(50, 50),
                                            new Vector2(50, 250),
                                            new Vector2(250, 250),
                                            new Vector2(250, 50),
                                            new Vector2(50, 50),
                                        },
                                        Texture = gradientTexture,
                                        Colour = Color4.Blue,
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Self-overlapping path",
                                        TextSize = 20,
                                        Colour = Color4.White,
                                    },
                                    new Path
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Positions = new List<Vector2>
                                        {
                                            new Vector2(50, 50),
                                            new Vector2(50, 250),
                                            new Vector2(250, 250),
                                            new Vector2(250, 150),
                                            new Vector2(20, 150),
                                        },
                                        Texture = gradientTexture,
                                        Colour = Color4.Red,
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Draw something ;)",
                                        TextSize = 20,
                                        Colour = Color4.White,
                                    },
                                    new DrawablePath
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Texture = gradientTexture,
                                        Colour = Color4.White,
                                    },
                                }
                            },
                        }
                    }
                }
            });
        }

        class DrawablePath : Path
        {
            public override bool HandleInput => true;

            private Vector2 oldPos;

            protected override bool OnDragStart(InputState state)
            {
                AddVertex(state.Mouse.Position);
                oldPos = state.Mouse.Position;
                return true;
            }

            protected override bool OnDrag(InputState state)
            {
                Vector2 pos = state.Mouse.Position;
                if ((pos - oldPos).Length > 10)
                {
                    AddVertex(pos);
                    oldPos = pos;
                }

                return base.OnDrag(state);
            }
        }
    }
}
