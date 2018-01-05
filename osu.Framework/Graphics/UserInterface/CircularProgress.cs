// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgress : Drawable, IHasCurrentValue<double>
    {
        public Bindable<double> Current { get; } = new Bindable<double>();

        public CircularProgress()
        {
            Current.ValueChanged += newValue => Invalidate(Invalidation.DrawNode);
        }

        private Shader roundedTextureShader;
        private Shader textureShader;

        private readonly CircularProgressDrawNodeSharedData pathDrawNodeSharedData = new CircularProgressDrawNodeSharedData();

        public bool CanDisposeTexture { get; protected set; }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            if (CanDisposeTexture)
            {
                texture?.Dispose();
                texture = null;
            }

            base.Dispose(isDisposing);
        }

        #endregion

        protected override DrawNode CreateDrawNode() => new CircularProgressDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            CircularProgressDrawNode n = (CircularProgressDrawNode)node;

            n.Texture = Texture;
            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;
            n.DrawSize = DrawSize;

            n.Shared = pathDrawNodeSharedData;

            n.Angle = (float)Current.Value * MathHelper.TwoPi;

            base.ApplyDrawNode(node);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            textureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        private Texture texture = Texture.WhitePixel;

        public Texture Texture
        {
            get { return texture; }
            set
            {
                if (value == texture)
                    return;

                if (texture != null && CanDisposeTexture)
                    texture.Dispose();

                texture = value;
                Invalidate(Invalidation.DrawNode);
            }
        }
    }
}
