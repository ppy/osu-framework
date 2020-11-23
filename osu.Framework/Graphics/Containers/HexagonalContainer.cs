using System.Collections.Generic;
using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;
using osuTK.Graphics;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    public class HexagonalContainer : HexagonalContainer<Drawable> { }

    public class HexagonalContainer<T> : BufferedContainer<T>
        where T : Drawable
    {
        private static readonly float sin_pi_over_3 = (float)Math.Sin(Math.PI / 3);
        private static readonly float tan_pi_over_3 = (float)Math.Tan(Math.PI / 3);

        public static readonly float HEXAGON_INRADIUS = sin_pi_over_3;

        public IShader Shader { get; private set; }

        private readonly HexagonalContainerDrawNodeSharedData sharedData;

        public HexagonalContainer(RenderbufferInternalFormat[] formats = null, bool pixelSnapping = false) =>
            sharedData = new HexagonalContainerDrawNodeSharedData(formats, pixelSnapping);

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders) => Shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "TextureHexagon");

        protected override DrawNode CreateDrawNode() => new HexagonalContainerDrawNode(this, sharedData);

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            Vector2 norm = screenSpacePos - ScreenSpaceDrawQuad.TopLeft;
            norm = new Vector2(norm.X / ScreenSpaceDrawQuad.Width, norm.Y / ScreenSpaceDrawQuad.Height); // apparently we can't divide Vector2s?
            norm = (norm - new Vector2(0.5f)) * 2;

            // hexagons are horizontally and vertically symmetrical, so we only have to test one quadrant :)
            norm = new Vector2(Math.Abs(norm.X), Math.Abs(norm.Y));

            if (norm.Y > sin_pi_over_3)
            {
                return false; // top bound
            }

            if (norm.Y > -tan_pi_over_3 * (norm.X - 1))
            {
                return false; // right bound
            }

            return true;
        }

        private class HexagonalContainerDrawNode : BufferedDrawNode, ICompositeDrawNode
        {
            protected new HexagonalContainer<T> Source => (HexagonalContainer<T>)base.Source;

            protected new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

            private IShader hexagonShader;
            private bool drawOriginal;
            private ColourInfo effectColour;
            private BlendingParameters effectBlending;
            private EffectPlacement effectPlacement;

            public HexagonalContainerDrawNode(BufferedContainer<T> source, HexagonalContainerDrawNodeSharedData sharedData)
                : base(source, new CompositeDrawableDrawNode(source), sharedData) { }

            public override void ApplyState()
            {
                base.ApplyState();

                hexagonShader = Source.Shader;

                effectColour = Source.EffectColour;
                effectBlending = Source.DrawEffectBlending;
                effectPlacement = Source.EffectPlacement;

                drawOriginal = Source.DrawOriginal;
            }

            protected override void PopulateContents()
            {
                base.PopulateContents();

                GLWrapper.PushScissorState(false);

                FrameBuffer current = SharedData.CurrentEffectBuffer;
                FrameBuffer target = SharedData.GetNextEffectBuffer();

                GLWrapper.SetBlend(BlendingParameters.None);

                using (BindFrameBuffer(target))
                {
                    float resolution = Math.Max(Source.ScreenSpaceDrawQuad.Width, Source.ScreenSpaceDrawQuad.Height); // shh
                    hexagonShader.GetUniform<float>("g_Resolution").UpdateValue(ref resolution);
                    hexagonShader.Bind();
                    DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height), ColourInfo.SingleColour(Color4.White));
                    hexagonShader.Unbind();
                }

                GLWrapper.PopScissorState();
            }

            protected override void DrawContents()
            {
                if (drawOriginal && effectPlacement == EffectPlacement.InFront)
                    base.DrawContents();

                GLWrapper.SetBlend(effectBlending);

                ColourInfo finalEffectColour = DrawColourInfo.Colour;
                finalEffectColour.ApplyChild(effectColour);

                DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, finalEffectColour);

                if (drawOriginal && effectPlacement == EffectPlacement.Behind)
                    base.DrawContents();
            }

            public List<DrawNode> Children
            {
                get => Child.Children;
                set => Child.Children = value;
            }

            public bool AddChildDrawNodes => RequiresRedraw;
        }

        private class HexagonalContainerDrawNodeSharedData : BufferedDrawNodeSharedData
        {
            public HexagonalContainerDrawNodeSharedData(RenderbufferInternalFormat[] formats, bool pixelSnapping)
                : base(2, formats, pixelSnapping) { }
        }
    }
}
