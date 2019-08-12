// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transforms;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgress : Drawable, ITexturedShaderDrawable, IHasCurrentValue<double>
    {
        private readonly Bindable<double> current = new Bindable<double>();

        private Bindable<double> currentBound;

        public Bindable<double> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (currentBound != null) current.UnbindFrom(currentBound);
                current.BindTo(currentBound = value);
            }
        }

        public CircularProgress()
        {
            Current.ValueChanged += newValue => Invalidate(Invalidation.DrawNode);
        }

        public IShader RoundedTextureShader { get; private set; }
        public IShader TextureShader { get; private set; }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            texture?.Dispose();
            texture = null;

            base.Dispose(isDisposing);
        }

        #endregion

        protected override DrawNode CreateDrawNode() => new CircularProgressDrawNode(this);

        public TransformSequence<CircularProgress> FillTo(double newValue, double duration = 0, Easing easing = Easing.None)
            => this.TransformBindableTo(Current, newValue, duration, easing);

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
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

    public static class CircularProgressTransformSequenceExtensions
    {
        public static TransformSequence<CircularProgress> FillTo(this TransformSequence<CircularProgress> t, double newValue, double duration = 0, Easing easing = Easing.None)
            => t.Append(cp => cp.TransformBindableTo(cp.Current, newValue, duration, easing));
    }
}
