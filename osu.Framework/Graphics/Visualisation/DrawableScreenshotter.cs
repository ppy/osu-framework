// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Visualisation
{
    /// <summary>
    /// Takes an image of a drawable.
    /// </summary>
    internal partial class DrawableScreenshotter : Drawable, IBufferedDrawable
    {
        public readonly Drawable Target;

        private readonly Action<Image<Rgba32>?> onImageReceived;
        private bool didRender;

        public DrawableScreenshotter(Drawable target, Action<Image<Rgba32>?> onImageReceived)
        {
            this.onImageReceived = onImageReceived;

            Target = target;
        }

        public override Quad ScreenSpaceDrawQuad => Target.ScreenSpaceDrawQuad;

        private IShader textureShader = null!;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            textureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        public override bool IsPresent => true;

        IShader ITexturedShaderDrawable.TextureShader => textureShader;
        Color4 IBufferedDrawable.BackgroundColour => new Color4(0, 0, 0, 0);
        DrawColourInfo? IBufferedDrawable.FrameBufferDrawColour => new DrawColourInfo(Color4.White);
        Vector2 IBufferedDrawable.FrameBufferScale => Vector2.One;

        public override DrawColourInfo DrawColourInfo => new DrawColourInfo(Color4.White);

        public override DrawInfo DrawInfo => Target.DrawInfo;

        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(new[] { RenderBufferFormat.D16 }, pixelSnapping: true, clipToRootNode: true);

        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private void onRendered(IFrameBuffer frameBuffer)
        {
            if (didRender)
                return;

            didRender = true;

            host.DrawThread.Scheduler.Add(() =>
            {
                var image = renderer.ExtractFrameBufferData(frameBuffer);

                Schedule(() =>
                {
                    onImageReceived(image);

                    Expire();
                });
            });
        }

        internal override DrawNode? GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            if (didRender)
                return null;

            var targetDrawNode = Target.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

            if (targetDrawNode == null)
            {
                onImageReceived(null);

                Expire();
                return null;
            }

            // This looks a bit odd, but we essentially want a drawNode that we can safely dispose once we've rendered it.
            // This call will force the target drawable to recreate its drawNode subtree so the one we got should be completely detached.
            Target.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode: true);

            var drawNode = new DrawableScreenshotterDrawNode(this, targetDrawNode, sharedData, onRendered);

            drawNode.ApplyState();

            return drawNode;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            sharedData.Dispose();
        }

        private class DrawableScreenshotterDrawNode : BufferedDrawNode
        {
            private readonly Action<IFrameBuffer> onRendered;

            public DrawableScreenshotterDrawNode(IBufferedDrawable source, DrawNode child, BufferedDrawNodeSharedData sharedData, Action<IFrameBuffer> onRendered)
                : base(source, child, sharedData)
            {
                this.onRendered = onRendered;
            }

            protected override void DrawContents(IRenderer renderer) => onRendered(SharedData.MainBuffer);
        }
    }
}
