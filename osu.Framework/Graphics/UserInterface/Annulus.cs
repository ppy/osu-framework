// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class Annulus : Drawable
    {
        private readonly Bindable<double> startAngle = new Bindable<double>();
        private readonly Bindable<double> endAngle = new Bindable<double>();

        public Bindable<double> StartAngle
        {
            get => startAngle;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                startAngle.UnbindBindings();
                startAngle.BindTo(value);
            }
        }

        public Bindable<double> EndAngle
        {
            get => endAngle;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                endAngle.UnbindBindings();
                endAngle.BindTo(value);
            }
        }

        public Annulus()
        {
            StartAngle.ValueChanged += _ => Invalidate(Invalidation.DrawNode);
            EndAngle.ValueChanged += _ => Invalidate(Invalidation.DrawNode);
        }

        private IShader roundedTextureShader;
        private IShader textureShader;

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            texture?.Dispose();
            texture = null;

            base.Dispose(isDisposing);
        }

        #endregion

        protected override DrawNode CreateDrawNode() => new AnnularDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            AnnularDrawNode n = (AnnularDrawNode)node;

            n.Texture = Texture;
            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;
            n.DrawSize = DrawSize;
            n.StartAngle = (float)StartAngle.Value;
            n.EndAngle = (float)EndAngle.Value;
            n.InnerRadius = innerRadius;

            base.ApplyDrawNode(node);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            roundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            textureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        private Texture texture = Texture.WhitePixel;

        public Texture Texture
        {
            get => texture;
            set
            {
                if (value == texture)
                    return;

                texture?.Dispose();
                texture = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private float innerRadius = 1;

        /// <summary>
        /// The inner fill radius, relative to the <see cref="Drawable.DrawSize"/> of the <see cref="CircularProgress"/>.
        /// The value range is 0 to 1 where 0 is invisible and 1 is completely filled.
        /// The entire texture still fills the disk without cropping it.
        /// </summary>
        public float InnerRadius
        {
            get => innerRadius;
            set
            {
                innerRadius = MathHelper.Clamp(value, 0, 1);
                Invalidate(Invalidation.DrawNode);
            }
        }
    }
}
