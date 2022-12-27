// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Layout;
using osuTK;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneRawBuffers : FrameworkTestScene
    {
        public TestSceneRawBuffers()
        {
            Add(new Box
            {
                Size = new Vector2(200),
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Colour = Colour4.DarkGray
            });
            Add(new CachedStarDrawable
            {
                Size = new Vector2(200),
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre
            });
        }

        private partial class CachedStarDrawable : Drawable
        {
            private CachedStarDrawableSharedData sharedData = new();

            private Vector2 lastSize;
            protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
            {
                if (lastSize != DrawSize)
                {
                    lastSize = DrawSize;
                    sharedData.InvalidationId++;
                }
                return base.OnInvalidate(invalidation, source);
            }

            private IShader shader;
            [BackgroundDependencyLoader]
            private void load(ShaderManager shaders)
            {
                shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            }

            protected override DrawNode CreateDrawNode()
                => new CachedStarDrawableDrawNode(sharedData, this);

            private class CachedStarDrawableSharedData
            {
                public ulong InvalidationId = 1;
                public ulong UploadedId;

                public IRawVertexBuffer<DepthWrappingVertex<TexturedVertex2D>> VBO;
                public IRawElementBuffer<ushort> EBO;
                public IRawVertexArray VAO;

                public int VerticeCount;
            }

            private class CachedStarDrawableDrawNode : DrawNode
            {
                new protected CachedStarDrawable Source => (CachedStarDrawable)base.Source;
                private CachedStarDrawableSharedData sharedData;

                public CachedStarDrawableDrawNode(CachedStarDrawableSharedData sharedData, CachedStarDrawable source) : base(source)
                {
                    this.sharedData = sharedData;
                }

                private Vector2 size;
                private IShader shader;
                private ulong invalidationId;
                public override void ApplyState()
                {
                    base.ApplyState();
                    size = Source.DrawSize;
                    shader = Source.shader;
                    invalidationId = sharedData.InvalidationId;
                }

                private void bindBuffers(IRenderer renderer)
                {
                    if (sharedData.VAO == null)
                    {
                        sharedData.VBO = renderer.CreateRawVertexBuffer<DepthWrappingVertex<TexturedVertex2D>>();
                        sharedData.EBO = renderer.CreateRawElementBuffer<ushort>();
                        sharedData.VAO = renderer.CreateRawVertexArray();

                        sharedData.VAO.Bind();
                        sharedData.EBO.Bind();
                        sharedData.VBO.Bind();
                        sharedData.VBO.SetLayout();
                    }
                    else
                    {
                        sharedData.VAO.Bind();
                    }
                }

                private Vector2 toCircular(float angle, float distance)
                {
                    return size / 2 + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) / 2 * distance * size;
                }

                private void buildBuffers(IRenderer renderer)
                {
                    Span<DepthWrappingVertex<TexturedVertex2D>> vertices = stackalloc DepthWrappingVertex<TexturedVertex2D>[11];
                    var delta = MathF.Tau / 5;
                    for (int i = 0; i < 5; i++)
                    {
                        var thetaA = i * delta - MathF.PI / 2;
                        var thetaB = (i + 0.5f) * delta - MathF.PI / 2;
                        vertices[i * 2] = new DepthWrappingVertex<TexturedVertex2D>
                        {
                            BackbufferDrawDepth = renderer.BackbufferDrawDepth,
                            Vertex = new TexturedVertex2D
                            {
                                Colour = Colour4.White,
                                Position = toCircular(thetaA, 1)
                            }
                        };
                        vertices[i * 2 + 1] = new DepthWrappingVertex<TexturedVertex2D>
                        {
                            BackbufferDrawDepth = renderer.BackbufferDrawDepth,
                            Vertex = new TexturedVertex2D
                            {
                                Colour = Colour4.Blue,
                                Position = toCircular(thetaB, 0.5f)
                            }
                        };
                    }
                    vertices[10] = new DepthWrappingVertex<TexturedVertex2D>
                    {
                        BackbufferDrawDepth = renderer.BackbufferDrawDepth,
                        Vertex = new TexturedVertex2D
                        {
                            Colour = Colour4.Red,
                            Position = toCircular(0, 0)
                        }
                    };
                    sharedData.VBO.Bind();
                    sharedData.VBO.BufferData(vertices, BufferUsageHint.StaticDraw);

                    Span<ushort> indices = stackalloc ushort[30];
                    for (ushort i = 0; i < 10; i++)
                    {
                        indices[i * 3] = 10;
                        indices[i * 3 + 1] = i;
                        indices[i * 3 + 2] = (ushort)((i + 1) % 10);
                    }
                    sharedData.EBO.Bind();
                    sharedData.EBO.BufferData(indices, BufferUsageHint.StaticDraw);
                    sharedData.VerticeCount = 30;
                }

                protected internal override bool CanDrawOpaqueInterior => true;

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);

                    shader.Bind();
                    renderer.PushLocalMatrix(DrawInfo.Matrix);
                    renderer.WhitePixel.Bind();
                    bindBuffers(renderer);
                    if (invalidationId != sharedData.UploadedId)
                    {
                        buildBuffers(renderer);
                        sharedData.UploadedId = invalidationId;
                    }
                    sharedData.EBO.Draw(PrimitiveTopology.Triangles, sharedData.VerticeCount);
                    sharedData.VAO.Unbind();
                    renderer.PopLocalMatrix();
                    shader.Unbind();
                }

                protected override void Dispose(bool isDisposing)
                {
                    base.Dispose(isDisposing);

                    if (sharedData.VAO != null)
                    {
                        sharedData.VAO.Dispose();
                        sharedData.EBO.Dispose();
                        sharedData.VAO.Dispose();
                        sharedData.VAO = null;
                        sharedData.EBO = null;
                        sharedData.VAO = null;
                    }
                }
            }
        }
    }
}
