// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Layout;
using osuTK;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneRawBuffers : FrameworkTestScene
    {
        private Container starContainer;
        public TestSceneRawBuffers()
        {
            Add(new Box
            {
                Size = new Vector2(200),
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
                Colour = Colour4.DarkGray
            });
            Add(starContainer = new Container
            {
                Size = new Vector2(200),
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre
            });
            BasicDropdown<RenderMode> dropdown;
            Add(dropdown = new BasicDropdown<RenderMode>
            {
                Position = new Vector2(10),
                Width = 300,
                Items = Enum.GetValues<RenderMode>()
            });

            dropdown.Current.BindValueChanged(v =>
            {
                setMode(v.NewValue);
            }, true);
        }

        private void setMode(RenderMode mode)
        {
            starContainer.Clear(disposeChildren: true);
            starContainer.Add(new CachedStarDrawable(mode)
            {
                RelativeSizeAxes = Axes.Both,
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre
            });
        }

        private enum RenderMode
        {
            Batch,
            CacheVertexLayout,
            CacheIndexBufferBind,
            CacheBoth,
        }

        private partial class CachedStarDrawable : Drawable
        {
            private CachedStarDrawableSharedData sharedData;
            public CachedStarDrawable(RenderMode mode)
            {
                sharedData = new CachedStarDrawableSharedData { Mode = mode };
            }

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
                public RenderMode Mode;

                public ulong InvalidationId = 1;
                public ulong UploadedId;

                public IRawVertexBuffer<DepthWrappingVertex<TexturedVertex2D>> Vertices;
                public IRawIndexBuffer<ushort> Indices;
                public IRenderStateArray StateArray;
                public IVertexBatch<TexturedVertex2D> Batch;

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

                private void initBuffers(IRenderer renderer)
                {
                    if (sharedData.StateArray == null)
                    {
                        sharedData.Vertices = renderer.CreateRawVertexBuffer<DepthWrappingVertex<TexturedVertex2D>>();
                        sharedData.Indices = renderer.CreateRawIndexBuffer<ushort>();

                        StateArrayFlags cachedState;
                        if (sharedData.Mode == RenderMode.CacheIndexBufferBind)
                            cachedState = StateArrayFlags.IndexBuffer;
                        else if (sharedData.Mode == RenderMode.CacheVertexLayout)
                            cachedState = StateArrayFlags.VertexLayout;
                        else
                            cachedState = StateArrayFlags.IndexBuffer | StateArrayFlags.VertexLayout;

                        sharedData.StateArray = renderer.CreateRenderStateArray(cachedState);

                        sharedData.StateArray.Bind();
                        if (cachedState.HasFlagFast(StateArrayFlags.IndexBuffer))
                            sharedData.Indices.Bind();
                        if (cachedState.HasFlagFast(StateArrayFlags.VertexLayout))
                        {
                            sharedData.Vertices.Bind();
                            sharedData.Vertices.SetLayout();
                        }
                    }
                    else
                    {
                        sharedData.StateArray.Bind();
                    }
                }

                private Vector2 toCircular(float angle, float distance)
                {
                    return size / 2 + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) / 2 * distance * size;
                }

                private IEnumerable<TexturedVertex2D> buildVertices()
                {
                    var delta = MathF.Tau / 5;
                    for (int i = 0; i < 5; i++)
                    {
                        var thetaA = i * delta - MathF.PI / 2;
                        var thetaB = (i + 0.5f) * delta - MathF.PI / 2;
                        yield return new TexturedVertex2D
                        {
                            Colour = Colour4.White,
                            Position = toCircular(thetaA, 1)
                        };
                        yield return new TexturedVertex2D
                        {
                            Colour = Colour4.Blue,
                            Position = toCircular(thetaB, 0.5f)
                        };
                    }
                    yield return new TexturedVertex2D
                    {
                        Colour = Colour4.Red,
                        Position = toCircular(0, 0)
                    };
                }

                private IEnumerable<(ushort, ushort, ushort)> buildIndices()
                {
                    for (ushort i = 0; i < 10; i++)
                    {
                        yield return (10, i, (ushort)((i + 1) % 10));
                    }
                }

                private TexturedVertex2D[] vertexBuffer;
                private IEnumerable<(TexturedVertex2D, TexturedVertex2D, TexturedVertex2D)> buildTriangles()
                {
                    vertexBuffer ??= new TexturedVertex2D[11];

                    int n = 0;
                    foreach (var i in buildVertices())
                    {
                        vertexBuffer[n++] = i;
                    }

                    Span<ushort> indices = stackalloc ushort[30];
                    n = 0;
                    foreach (var i in buildIndices())
                    {
                        yield return (
                            vertexBuffer[i.Item1],
                            vertexBuffer[i.Item2],
                            vertexBuffer[i.Item3]
                        );
                    }
                }

                private void buildBuffers(IRenderer renderer)
                {
                    Span<DepthWrappingVertex<TexturedVertex2D>> vertices = stackalloc DepthWrappingVertex<TexturedVertex2D>[11];
                    int n = 0;
                    foreach (var i in buildVertices())
                    {
                        vertices[n++] = new DepthWrappingVertex<TexturedVertex2D>
                        {
                            BackbufferDrawDepth = renderer.BackbufferDrawDepth,
                            Vertex = i
                        };
                    }

                    sharedData.Vertices.Bind();
                    sharedData.Vertices.BufferData(vertices, BufferUsageHint.StaticDraw);

                    Span<ushort> indices = stackalloc ushort[30];
                    n = 0;
                    foreach (var i in buildIndices())
                    {
                        indices[n++] = i.Item1;
                        indices[n++] = i.Item2;
                        indices[n++] = i.Item3;
                    }
                    
                    sharedData.Indices.Bind();
                    sharedData.Indices.BufferData(indices, BufferUsageHint.StaticDraw);
                    sharedData.VerticeCount = n;
                }

                // no clue why, but probably related to the batch-centric approach this being false makes it hide behind the background box when the cursor is outside the window
                // I presume this can be fixed with allowing BackbufferDrawDepth to have a uniform offset
                protected internal override bool CanDrawOpaqueInterior => true;

                public override void Draw(IRenderer renderer)
                {
                    base.Draw(renderer);

                    shader.Bind();
                    renderer.PushLocalMatrix(DrawInfo.Matrix);
                    renderer.WhitePixel.Bind();

                    if (sharedData.Mode == RenderMode.Batch)
                    {
                        sharedData.Batch ??= renderer.CreateQuadBatch<TexturedVertex2D>(11, 1);
                        foreach (var (a, b, c) in buildTriangles())
                        {
                            sharedData.Batch.Add(a);
                            sharedData.Batch.Add(b);
                            sharedData.Batch.Add(c);
                            sharedData.Batch.Add(c);
                        }
                        sharedData.Batch.Draw();
                    }
                    else
                    {
                        initBuffers(renderer);
                        if (!sharedData.StateArray.CachedState.HasFlagFast(StateArrayFlags.IndexBuffer))
                            sharedData.Indices.Bind();
                        if (!sharedData.StateArray.CachedState.HasFlagFast(StateArrayFlags.VertexLayout))
                        {
                            sharedData.Vertices.Bind();
                            sharedData.Vertices.SetLayout();
                        }

                        if (invalidationId != sharedData.UploadedId)
                        {
                            buildBuffers(renderer);
                            sharedData.UploadedId = invalidationId;
                        }
                        sharedData.Indices.Draw(PrimitiveTopology.Triangles, sharedData.VerticeCount);
                        sharedData.StateArray.Unbind();
                    }

                    renderer.PopLocalMatrix();
                    shader.Unbind();
                }

                protected override void Dispose(bool isDisposing)
                {
                    base.Dispose(isDisposing);

                    if (sharedData.StateArray != null)
                    {
                        sharedData.StateArray.Dispose();
                        sharedData.Indices.Dispose();
                        sharedData.StateArray.Dispose();
                        sharedData.StateArray = null;
                        sharedData.Indices = null;
                        sharedData.StateArray = null;
                    }
                    if (sharedData.Batch != null)
                    {
                        sharedData.Batch.Dispose();
                        sharedData.Batch = null;
                    }
                }
            }
        }
    }
}
