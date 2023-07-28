// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneShaderStorageBufferObject : FrameworkTestScene
    {
        public TestSceneShaderStorageBufferObject()
        {
            Add(new GridDrawable { RelativeSizeAxes = Axes.Both });
        }

        private partial class GridDrawable : Drawable
        {
            private const int separation = 1;
            private const int size = 32;

            private IShader shader = null!;

            private readonly List<Quad> areas = new List<Quad>();

            [BackgroundDependencyLoader]
            private void load(ShaderManager shaderManager)
            {
                shader = shaderManager.Load("SSBOTest", "SSBOTest");
            }

            protected override void Update()
            {
                base.Update();

                areas.Clear();

                for (float y = 0; y < DrawHeight; y += size + separation)
                {
                    for (float x = 0; x < DrawWidth; x += size + separation)
                        areas.Add(ToScreenSpace(new RectangleF(x, y, size, size)));
                }

                Invalidate(Invalidation.DrawNode);
            }

            protected override DrawNode CreateDrawNode() => new GridDrawNode(this);

            private class GridDrawNode : DrawNode
            {
                private const int min_ssbo_size = 64;
                private const int max_ssbo_size = 8192;

                protected new GridDrawable Source => (GridDrawable)base.Source;

                private IShader shader = null!;
                private readonly List<Quad> areas = new List<Quad>();

                public GridDrawNode(IDrawable source)
                    : base(source)
                {
                }

                public override void ApplyState()
                {
                    base.ApplyState();

                    shader = Source.shader;
                    areas.Clear();
                    areas.AddRange(Source.areas);
                }

                private IShaderStorageBufferObject<ColourData>? colourBuffer;
                private IVertexBatch<ColourIndexedVertex>? vertices;

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);

                    // Create the vertex batch.
                    vertices ??= renderer.CreateQuadBatch<ColourIndexedVertex>(400, 1000);

                    // Create the SSBO. It only needs to be populated once for the demonstration of this test.
                    if (colourBuffer == null)
                    {
                        colourBuffer = renderer.CreateShaderStorageBufferObject<ColourData>(min_ssbo_size, max_ssbo_size);
                        var rng = new Random(1337);

                        for (int i = 0; i < colourBuffer.Size; i++)
                            colourBuffer[i] = new ColourData { Colour = new Vector4(rng.NextSingle(), rng.NextSingle(), rng.NextSingle(), 1) };
                    }

                    // Bind the custom shader and SSBO.
                    shader.Bind();
                    shader.BindUniformBlock("g_ColourBuffer", colourBuffer);

                    // Submit vertices, making sure that we don't submit an index which would overflow the SSBO.
                    for (int i = 0; i < areas.Count; i++)
                    {
                        vertices.Add(new ColourIndexedVertex
                        {
                            Position = areas[i].BottomLeft,
                            ColourIndex = i % colourBuffer.Size
                        });

                        vertices.Add(new ColourIndexedVertex
                        {
                            Position = areas[i].BottomRight,
                            ColourIndex = i % colourBuffer.Size
                        });

                        vertices.Add(new ColourIndexedVertex
                        {
                            Position = areas[i].TopRight,
                            ColourIndex = i % colourBuffer.Size
                        });

                        vertices.Add(new ColourIndexedVertex
                        {
                            Position = areas[i].TopLeft,
                            ColourIndex = i % colourBuffer.Size
                        });
                    }

                    vertices.Draw();
                }

                protected override void Dispose(bool isDisposing)
                {
                    base.Dispose(isDisposing);

                    colourBuffer?.Dispose();
                    vertices?.Dispose();
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct ColourData
        {
            public UniformVector4 Colour;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ColourIndexedVertex : IEquatable<ColourIndexedVertex>, IVertex
        {
            [VertexMember(2, VertexAttribPointerType.Float)]
            public Vector2 Position;

            [VertexMember(1, VertexAttribPointerType.Int)]
            public int ColourIndex;

            public readonly bool Equals(ColourIndexedVertex other) =>
                Position.Equals(other.Position)
                && ColourIndex == other.ColourIndex;
        }
    }
}
