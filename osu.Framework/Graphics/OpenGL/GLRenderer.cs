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
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics.ES30;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using GL4 = osuTK.Graphics.OpenGL;

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

        public bool UseStructuredBuffers { get; private set; }

        /// <summary>
        /// The maximum allowed render buffer size.
        /// </summary>
        public int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.

        /// <summary>
        /// Whether the current platform is embedded.
        /// </summary>
        public bool IsEmbedded { get; private set; }

        private int backbufferFramebuffer;

        private readonly Dictionary<string, IGLUniformBuffer> boundUniformBuffers = new Dictionary<string, IGLUniformBuffer>();
        private bool? lastBlendingEnabledState;
        private int lastBoundVertexArray;

        protected override void Initialise(IGraphicsSurface graphicsSurface)
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

            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);

            string extensions = GetExtensions();

            Logger.Log($@"GL Initialized
                        GL Version:                 {GL.GetString(StringName.Version)}
                        GL Renderer:                {GL.GetString(StringName.Renderer)}
                        GL Shader Language version: {GL.GetString(StringName.ShadingLanguageVersion)}
                        GL Vendor:                  {GL.GetString(StringName.Vendor)}
                        GL Extensions:              {extensions}");

            UseStructuredBuffers = extensions.Contains(@"GL_ARB_shader_storage_buffer_object") && !FrameworkEnvironment.NoStructuredBuffers;

            Logger.Log($"{nameof(UseStructuredBuffers)}: {UseStructuredBuffers}");

            openGLSurface.ClearCurrent();
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
            lastBoundVertexArray = 0;
            boundUniformBuffers.Clear();

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

        public void BindUniformBuffer(string blockName, IGLUniformBuffer glBuffer)
        {
            if (boundUniformBuffers.TryGetValue(blockName, out IGLUniformBuffer? current) && current == glBuffer)
                return;

            FlushCurrentBatch(FlushBatchSource.BindBuffer);
            boundUniformBuffers[blockName] = glBuffer;
        }

        public void DrawVertices(PrimitiveType type, int vertexStart, int verticesCount)
        {
            var glShader = (GLShader)Shader!;

            glShader.BindUniformBlock("g_GlobalUniforms", GlobalUniformBuffer!);

            int currentUniformBinding = 0;
            int currentStorageBinding = 0;

            foreach ((string name, IGLUniformBuffer buffer) in boundUniformBuffers)
            {
                if (glShader.GetUniformBlockIndex(name) is not int index)
                    continue;

                buffer.Flush();

                if (buffer is IGLShaderStorageBufferObject && UseStructuredBuffers)
                {
                    GL4.GL.ShaderStorageBlockBinding(glShader, index, currentStorageBinding);
                    GL4.GL.BindBufferBase(GL4.BufferRangeTarget.ShaderStorageBuffer, currentStorageBinding, buffer.Id);
                    currentStorageBinding++;
                }
                else
                {
                    GL.UniformBlockBinding(glShader, index, currentUniformBinding);
                    GL.BindBufferBase(BufferRangeTarget.UniformBuffer, currentUniformBinding, buffer.Id);
                    currentUniformBinding++;
                }
            }

            GL.DrawElements(type, verticesCount, DrawElementsType.UnsignedShort, vertexStart * sizeof(ushort));
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

        protected override IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore)
            => new GLShader(this, name, parts.Cast<GLShaderPart>().ToArray(), compilationStore);

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

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
            => new GLUniformBuffer<TData>(this);

        protected override IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
            => new GLShaderStorageBufferObject<TData>(this, uboSize, ssboSize);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
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
