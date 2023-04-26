// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Batches;
using osu.Framework.Graphics.OpenGL.Shaders;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics.ES30;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid.SPIRV;
using FramebufferAttachment = osuTK.Graphics.ES30.FramebufferAttachment;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = osuTK.Graphics.ES30.PixelFormat;
using PrimitiveTopology = osu.Framework.Graphics.Rendering.PrimitiveTopology;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace osu.Framework.Graphics.OpenGL
{
    internal class GLRenderer : Renderer
    {
        private IOpenGLGraphicsSurface openGLSurface = null!;

        protected internal override bool VerticalSync
        {
            get => openGLSurface.VerticalSync;
            set => openGLSurface.VerticalSync = value;
        }

        protected internal override bool AllowTearing { get; set; }

        public override bool IsDepthRangeZeroToOne => false;
        public override bool IsUvOriginTopLeft => false;
        public override bool IsClipSpaceYInverted => false;

        /// <summary>
        /// The maximum allowed render buffer size.
        /// </summary>
        public int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.

        /// <summary>
        /// Whether the current platform is embedded.
        /// </summary>
        public bool IsEmbedded { get; private set; }

        private int backbufferFramebuffer;

        private int? computeMipmapShader;
        private GLUniformBuffer<ComputeMipmapGenerationParameters>? computeMipmapParametersBuffer;
        private GLUniformBlock? computeMipmapParametersBlock;

        private bool supportsComputeMipmapGeneration;

        private GLShader renderMipmapShader = null!;

        private readonly int[] lastBoundBuffers = new int[2];

        private bool? lastBlendingEnabledState;
        private int lastBoundVertexArray;

        protected override void InitialiseDevice(IGraphicsSurface graphicsSurface)
        {
            if (graphicsSurface.Type != GraphicsSurfaceType.OpenGL)
                throw new InvalidOperationException($"{nameof(GLRenderer)} only supports OpenGL graphics surfaces.");

            openGLSurface = (IOpenGLGraphicsSurface)graphicsSurface;
            openGLSurface.MakeCurrent(openGLSurface.WindowContext);

            backbufferFramebuffer = openGLSurface.BackbufferFramebuffer ?? 0;

            string version = GL.GetString(StringName.Version);
            IsEmbedded = version.Contains("OpenGL ES"); // As defined by https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glGetString.xml

            MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
            MaxRenderBufferSize = GL.GetInteger(GetPName.MaxRenderbufferSize);

            supportsComputeMipmapGeneration = GetExtensions().Contains("GL_ARB_compute_shader");

            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);

            Logger.Log($@"GL Initialized
                        GL Version:                 {GL.GetString(StringName.Version)}
                        GL Renderer:                {GL.GetString(StringName.Renderer)}
                        GL Shader Language version: {GL.GetString(StringName.ShadingLanguageVersion)}
                        GL Vendor:                  {GL.GetString(StringName.Vendor)}
                        GL Extensions:              {GetExtensions()}");

            openGLSurface.ClearCurrent();
        }

        protected override void SetupResources()
        {
            MakeCurrent();

            base.SetupResources();

            using (var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources/Shaders"))
            {
                if (supportsComputeMipmapGeneration)
                {
                    int shader = GL.CreateShader(ShaderType.ComputeShader);
                    var spirvResults = SpirvCompilation.CompileCompute(store.Get("sh_mipmap.comp"), IsEmbedded ? CrossCompileTarget.ESSL : CrossCompileTarget.GLSL);
                    GL.ShaderSource(shader, spirvResults.ComputeShader);
                    GL.CompileShader(shader);
                    GL.GetShader(shader, ShaderParameter.CompileStatus, out int compileResult);

                    if (compileResult != 1)
                        throw new InvalidOperationException($"Failed to compile compute mipmap shader: {GL.GetShaderInfoLog(shader)}");

                    computeMipmapShader = GL.CreateProgram();
                    GL.AttachShader(computeMipmapShader.Value, shader);
                    GL.LinkProgram(computeMipmapShader.Value);
                    GL.GetProgram(computeMipmapShader.Value, GetProgramParameterName.LinkStatus, out int linkResult);

                    if (linkResult != 1)
                        throw new GLShader.PartCompilationFailedException("Failed to link compute mipmap shader", GL.GetShaderInfoLog(shader));

                    computeMipmapParametersBlock = new GLUniformBlock(this, computeMipmapShader.Value, 0, 0);
                    computeMipmapParametersBlock.Assign(computeMipmapParametersBuffer = new GLUniformBuffer<ComputeMipmapGenerationParameters>(this));
                }
                else
                {
                    var shaderStore = new PassthroughShaderStore(store);

                    renderMipmapShader = new GLShader(this, "mipmap/mipmap", new[]
                    {
                        new GLShaderPart(this, "mipmap", store.Get("sh_mipmap.vs"), ShaderType.VertexShader, shaderStore),
                        new GLShaderPart(this, "mipmap", store.Get("sh_mipmap.fs"), ShaderType.FragmentShader, shaderStore),
                    }, GlobalUniformBuffer!);
                }
            }
        }

        protected virtual string GetExtensions()
        {
#pragma warning disable CS0618
            GL.GetInteger(All.NumExtensions, out int numExtensions);
#pragma warning restore CS0618

            var extensionsBuilder = new StringBuilder();

            for (int i = 0; i < numExtensions; i++)
                extensionsBuilder.Append($"{GL.GetString(StringNameIndexed.Extensions, i)} ");

            return extensionsBuilder.ToString().TrimEnd();
        }

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            lastBlendingEnabledState = null;
            lastBoundBuffers.AsSpan().Clear();
            lastBoundVertexArray = 0;

            // Seems to be required on some drivers as the context is lost from the draw thread.
            MakeCurrent();

            GL.UseProgram(0);

            base.BeginFrame(windowSize);
        }

        protected internal override void WaitUntilNextFrameReady()
        {
        }

        protected internal override void MakeCurrent() => openGLSurface.MakeCurrent(openGLSurface.WindowContext);
        protected internal override void ClearCurrent() => openGLSurface.ClearCurrent();
        protected internal override void SwapBuffers() => openGLSurface.SwapBuffers();
        protected internal override void WaitUntilIdle() => GL.Finish();

        public bool BindVertexArray(int vaoId)
        {
            if (lastBoundVertexArray == vaoId)
                return false;

            lastBoundVertexArray = vaoId;
            GL.BindVertexArray(vaoId);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);
            return true;
        }

        public bool BindBuffer(BufferTarget target, int buffer)
        {
            int bufferIndex = target - BufferTarget.ArrayBuffer;
            if (lastBoundBuffers[bufferIndex] == buffer)
                return false;

            lastBoundBuffers[bufferIndex] = buffer;
            GL.BindBuffer(target, buffer);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);
            return true;
        }

        protected override void SetShaderImplementation(IShader shader) => GL.UseProgram((GLShader)shader);

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
            switch (uniform)
            {
                case IUniformWithValue<bool> b:
                    GL.Uniform1(uniform.Location, b.GetValue() ? 1 : 0);
                    break;

                case IUniformWithValue<int> i:
                    GL.Uniform1(uniform.Location, i.GetValue());
                    break;

                case IUniformWithValue<float> f:
                    GL.Uniform1(uniform.Location, f.GetValue());
                    break;

                case IUniformWithValue<Vector2> v2:
                    GL.Uniform2(uniform.Location, ref v2.GetValueByRef());
                    break;

                case IUniformWithValue<Vector3> v3:
                    GL.Uniform3(uniform.Location, ref v3.GetValueByRef());
                    break;

                case IUniformWithValue<Vector4> v4:
                    GL.Uniform4(uniform.Location, ref v4.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix2> m2:
                    GL.UniformMatrix2(uniform.Location, false, ref m2.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix3> m3:
                    GL.UniformMatrix3(uniform.Location, false, ref m3.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix4> m4:
                    GL.UniformMatrix4(uniform.Location, false, ref m4.GetValueByRef());
                    break;
            }
        }

        public override void GenerateMipmaps(INativeTexture texture, List<RectangleI> regions)
        {
            var glTexture = (GLTexture)texture;

            if (supportsComputeMipmapGeneration)
                generateMipmapsViaComputeShader(glTexture, regions);
            else
                generateMipmapsViaFramebuffer(glTexture, regions);
        }

        /// <summary>
        /// The number of threads in a thread group for the mipmap compute shader.
        /// </summary>
        /// <remarks>
        /// This is specified in the compute shader as well for OpenGL backends.
        /// </remarks>
        private static readonly Vector2I compute_mipmap_threads = new Vector2I(32, 32);

        private void generateMipmapsViaComputeShader(GLTexture texture, List<RectangleI> regions)
        {
            int width = texture.Width;
            int height = texture.Height;

            GL.UseProgram(computeMipmapShader!.Value);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture.TextureId);

            for (int level = 1; level < IRenderer.MAX_MIPMAP_LEVELS + 1 && (width > 1 || height > 1); level++)
            {
                width = MathUtils.DivideRoundUp(width, 2);
                height = MathUtils.DivideRoundUp(height, 2);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, level - 1);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, level - 1);

                if (IsEmbedded)
                    osuTK.Graphics.ES31.GL.BindImageTexture(2, texture.TextureId, level, false, 0, osuTK.Graphics.ES31.BufferAccessArb.WriteOnly, osuTK.Graphics.ES31.InternalFormat.Rgba8);
                else
                    osuTK.Graphics.OpenGL.GL.BindImageTexture(2, texture.TextureId, level, false, 0, osuTK.Graphics.OpenGL.TextureAccess.WriteOnly, osuTK.Graphics.OpenGL.SizedInternalFormat.Rgba8);

                computeMipmapParametersBlock!.Bind();

                for (int i = 0; i < regions.Count; i++)
                {
                    computeMipmapParametersBuffer!.Data = new ComputeMipmapGenerationParameters
                    {
                        Region = new Vector4(regions[i].X, regions[i].Y, regions[i].Width, regions[i].Height),
                        OutputWidth = width
                    };

                    if (IsEmbedded)
                    {
                        osuTK.Graphics.ES31.GL.DispatchCompute((uint)MathUtils.DivideRoundUp(width, compute_mipmap_threads.X), (uint)MathUtils.DivideRoundUp(height, compute_mipmap_threads.Y), 1);
                        osuTK.Graphics.ES31.GL.MemoryBarrier(osuTK.Graphics.ES31.MemoryBarrierMask.TextureFetchBarrierBit | osuTK.Graphics.ES31.MemoryBarrierMask.TextureUpdateBarrierBit | osuTK.Graphics.ES31.MemoryBarrierMask.ShaderImageAccessBarrierBit);
                    }
                    else
                    {
                        osuTK.Graphics.OpenGL.GL.DispatchCompute((uint)MathUtils.DivideRoundUp(width, compute_mipmap_threads.X), (uint)MathUtils.DivideRoundUp(height, compute_mipmap_threads.Y), 1);
                        osuTK.Graphics.OpenGL.GL.MemoryBarrier(osuTK.Graphics.OpenGL.MemoryBarrierFlags.TextureFetchBarrierBit | osuTK.Graphics.OpenGL.MemoryBarrierFlags.TextureUpdateBarrierBit | osuTK.Graphics.OpenGL.MemoryBarrierFlags.ShaderImageAccessBarrierBit);
                    }
                }
            }

            // restore previous shader if there's one bound currently.
            if (Shader != null)
                GL.UseProgram((GLShader)Shader);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, IRenderer.MAX_MIPMAP_LEVELS);
        }

        private void generateMipmapsViaFramebuffer(GLTexture texture, List<RectangleI> regions)
        {
            using var frameBuffer = new GLFrameBuffer(this, texture);

            BlendingParameters previousBlendingParameters = CurrentBlendingParameters;

            // Use a simple render state (no blending, masking, scissoring, stenciling, etc.)
            SetBlend(BlendingParameters.None);
            PushDepthInfo(new DepthInfo(false, false));
            PushStencilInfo(new StencilInfo(false));
            PushScissorState(false);

            BindFrameBuffer(frameBuffer);

            // Create render state for mipmap generation
            BindTexture(texture);
            renderMipmapShader.Bind();

            int width = texture.Width;
            int height = texture.Height;

            // Generate quad buffer that will hold all the updated regions
            var quadBuffer = new GLQuadBuffer<UncolouredVertex2D>(this, regions.Count, BufferUsageHint.StreamDraw);

            // Compute mipmap by iteratively blitting coarser and coarser versions of the updated regions
            for (int level = 1; level < IRenderer.MAX_MIPMAP_LEVELS + 1 && (width > 1 || height > 1); ++level)
            {
                width = MathUtils.DivideRoundUp(width, 2);
                height = MathUtils.DivideRoundUp(height, 2);

                // Fill quad buffer with downscaled (and conservatively rounded) draw rectangles
                for (int i = 0; i < regions.Count; ++i)
                {
                    // Conservatively round the draw rectangles. Rounding to integer coords is required
                    // in order to ensure all the texels affected by linear interpolation are touched.
                    // We could skip the rounding & use a single vertex buffer for all levels if we had
                    // conservative raster, but alas, that's only supported on NV and Intel.
                    Vector2I topLeft = regions[i].TopLeft;
                    topLeft = new Vector2I(topLeft.X / 2, topLeft.Y / 2);
                    Vector2I bottomRight = regions[i].BottomRight;
                    bottomRight = new Vector2I(MathUtils.DivideRoundUp(bottomRight.X, 2), MathUtils.DivideRoundUp(bottomRight.Y, 2));
                    regions[i] = new RectangleI(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

                    // Normalize the draw rectangle into the unit square, which doubles as texture sampler coordinates.
                    RectangleF r = (RectangleF)regions[i] / new Vector2(width, height);

                    quadBuffer.SetVertex(i * 4 + 0, new UncolouredVertex2D { Position = r.BottomLeft });
                    quadBuffer.SetVertex(i * 4 + 1, new UncolouredVertex2D { Position = r.BottomRight });
                    quadBuffer.SetVertex(i * 4 + 2, new UncolouredVertex2D { Position = r.TopRight });
                    quadBuffer.SetVertex(i * 4 + 3, new UncolouredVertex2D { Position = r.TopLeft });
                }

                // Read the texture from 1 mip level higher...
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, level - 1);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, level - 1);

                // ...than the one we're writing to via frame buffer.
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget2d.Texture2D, texture.TextureId, level);

                // Perform the actual mip level draw
                PushViewport(new RectangleI(0, 0, width, height));

                quadBuffer.Update();
                quadBuffer.Draw();

                PopViewport();
            }

            // Restore previous render state
            renderMipmapShader.Unbind();

            PopScissorState();
            PopStencilInfo();
            PopDepthInfo();

            SetBlend(previousBlendingParameters);

            UnbindFrameBuffer(frameBuffer);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, IRenderer.MAX_MIPMAP_LEVELS);
        }

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            if (texture == null)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + unit);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                return true;
            }

            switch (texture)
            {
                case GLVideoTexture glVideo:
                    if (glVideo.TextureIds == null)
                        return false;

                    for (int i = 0; i < glVideo.TextureIds.Length; i++)
                    {
                        GL.ActiveTexture(TextureUnit.Texture0 + unit + i);
                        GL.BindTexture(TextureTarget.Texture2D, glVideo.TextureIds[i]);
                    }

                    break;

                case GLTexture glTexture:
                    if (glTexture.TextureId <= 0)
                        return false;

                    GL.ActiveTexture(TextureUnit.Texture0 + unit);
                    GL.BindTexture(TextureTarget.Texture2D, glTexture.TextureId);
                    break;
            }

            return true;
        }

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer) =>
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ((GLFrameBuffer?)frameBuffer)?.FrameBuffer ?? backbufferFramebuffer);

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        public void DeleteFrameBuffer(IFrameBuffer frameBuffer)
        {
            while (FrameBuffer == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            ScheduleDisposal(GL.DeleteFramebuffer, ((GLFrameBuffer)frameBuffer).FrameBuffer);
        }

        protected override void ClearImplementation(ClearInfo clearInfo)
        {
            if (clearInfo.Colour != CurrentClearInfo.Colour)
                GL.ClearColor(clearInfo.Colour);

            if (clearInfo.Depth != CurrentClearInfo.Depth)
            {
                if (IsEmbedded)
                {
                    // GL ES only supports glClearDepthf
                    // See: https://www.khronos.org/registry/OpenGL-Refpages/es3.0/html/glClearDepthf.xhtml
                    GL.ClearDepth((float)clearInfo.Depth);
                }
                else
                {
                    // Older desktop platforms don't support glClearDepthf, so standard GL's double version is used instead
                    // See: https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glClearDepth.xhtml
                    osuTK.Graphics.OpenGL.GL.ClearDepth(clearInfo.Depth);
                }
            }

            if (clearInfo.Stencil != CurrentClearInfo.Stencil)
                GL.ClearStencil(clearInfo.Stencil);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        protected override void SetScissorStateImplementation(bool enabled)
        {
            if (enabled)
                GL.Enable(EnableCap.ScissorTest);
            else
                GL.Disable(EnableCap.ScissorTest);
        }

        protected override void SetBlendImplementation(BlendingParameters blendingParameters)
        {
            if (blendingParameters.IsDisabled)
            {
                if (!lastBlendingEnabledState.HasValue || lastBlendingEnabledState.Value)
                    GL.Disable(EnableCap.Blend);

                lastBlendingEnabledState = false;
            }
            else
            {
                if (!lastBlendingEnabledState.HasValue || !lastBlendingEnabledState.Value)
                    GL.Enable(EnableCap.Blend);

                lastBlendingEnabledState = true;

                GL.BlendEquationSeparate(blendingParameters.RGBEquationMode, blendingParameters.AlphaEquationMode);
                GL.BlendFuncSeparate(blendingParameters.SourceBlendingFactor, blendingParameters.DestinationBlendingFactor,
                    blendingParameters.SourceAlphaBlendingFactor, blendingParameters.DestinationAlphaBlendingFactor);
            }
        }

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask)
        {
            GL.ColorMask(blendingMask.HasFlagFast(BlendingMask.Red),
                blendingMask.HasFlagFast(BlendingMask.Green),
                blendingMask.HasFlagFast(BlendingMask.Blue),
                blendingMask.HasFlagFast(BlendingMask.Alpha));
        }

        protected override void SetViewportImplementation(RectangleI viewport) => GL.Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height);

        protected override void SetScissorImplementation(RectangleI scissor) => GL.Scissor(scissor.X, Viewport.Height - scissor.Bottom, scissor.Width, scissor.Height);

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo)
        {
            if (depthInfo.DepthTest)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(GLUtils.ToDepthFunction(depthInfo.Function));
            }
            else
                GL.Disable(EnableCap.DepthTest);

            GL.DepthMask(depthInfo.WriteDepth);
        }

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo)
        {
            if (stencilInfo.StencilTest)
            {
                GL.Enable(EnableCap.StencilTest);
                GL.StencilFunc(GLUtils.ToStencilFunction(stencilInfo.TestFunction), stencilInfo.TestValue, stencilInfo.Mask);
                GL.StencilOp(
                    GLUtils.ToStencilOperation(stencilInfo.StencilTestFailOperation),
                    GLUtils.ToStencilOperation(stencilInfo.DepthTestFailOperation),
                    GLUtils.ToStencilOperation(stencilInfo.TestPassedOperation));
            }
            else
                GL.Disable(EnableCap.StencilTest);
        }

        protected internal override Image<Rgba32> TakeScreenshot()
        {
            var size = ((IGraphicsSurface)openGLSurface).GetDrawableSize();
            var data = MemoryAllocator.Default.Allocate<Rgba32>(size.Width * size.Height);

            GL.ReadPixels(0, 0, size.Width, size.Height, PixelFormat.Rgba, PixelType.UnsignedByte, ref MemoryMarshal.GetReference(data.Memory.Span));

            var image = Image.LoadPixelData<Rgba32>(data.Memory.Span, size.Width, size.Height);
            image.Mutate(i => i.Flip(FlipMode.Vertical));
            return image;
        }

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
        {
            ShaderType glType;

            switch (partType)
            {
                case ShaderPartType.Fragment:
                    glType = ShaderType.FragmentShader;
                    break;

                case ShaderPartType.Vertex:
                    glType = ShaderType.VertexShader;
                    break;

                default:
                    throw new ArgumentException($"Unsupported shader part type: {partType}", nameof(partType));
            }

            return new GLShaderPart(this, name, rawData, glType, store);
        }

        protected override IShader CreateShader(string name, IShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer)
            => new GLShader(this, name, parts.Cast<GLShaderPart>().ToArray(), globalUniformBuffer);

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
        {
            All glFilteringMode;
            RenderbufferInternalFormat[]? glFormats = null;

            switch (filteringMode)
            {
                case TextureFilteringMode.Linear:
                    glFilteringMode = All.Linear;
                    break;

                case TextureFilteringMode.Nearest:
                    glFilteringMode = All.Nearest;
                    break;

                default:
                    throw new ArgumentException($"Unsupported filtering mode: {filteringMode}", nameof(filteringMode));
            }

            if (renderBufferFormats != null)
            {
                glFormats = new RenderbufferInternalFormat[renderBufferFormats.Length];

                for (int i = 0; i < renderBufferFormats.Length; i++)
                {
                    switch (renderBufferFormats[i])
                    {
                        case RenderBufferFormat.D16:
                            glFormats[i] = RenderbufferInternalFormat.DepthComponent16;
                            break;

                        case RenderBufferFormat.D32:
                            glFormats[i] = RenderbufferInternalFormat.DepthComponent32f;
                            break;

                        case RenderBufferFormat.D24S8:
                            glFormats[i] = RenderbufferInternalFormat.Depth24Stencil8;
                            break;

                        case RenderBufferFormat.D32S8:
                            glFormats[i] = RenderbufferInternalFormat.Depth32fStencil8;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported render buffer format: {renderBufferFormats[i]}", nameof(renderBufferFormats));
                    }
                }
            }

            return new GLFrameBuffer(this, glFormats, glFilteringMode);
        }

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>() => new GLUniformBuffer<TData>(this);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4 initialisationColour = default)
        {
            All glFilteringMode;

            switch (filteringMode)
            {
                case TextureFilteringMode.Linear:
                    glFilteringMode = All.Linear;
                    break;

                case TextureFilteringMode.Nearest:
                    glFilteringMode = All.Nearest;
                    break;

                default:
                    throw new ArgumentException($"Unsupported filtering mode: {filteringMode}", nameof(filteringMode));
            }

            return new GLTexture(this, width, height, manualMipmaps, glFilteringMode, initialisationColour);
        }

        protected override INativeTexture CreateNativeVideoTexture(int width, int height) => new GLVideoTexture(this, width, height);

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            => new GLLinearBatch<TVertex>(this, size, maxBuffers, GLUtils.ToPrimitiveType(topology));

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) => new GLQuadBatch<TVertex>(this, size, maxBuffers);
    }
}
