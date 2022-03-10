// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgress : Sprite, IHasCurrentValue<double>
    {
        private readonly BindableWithCurrent<double> current = new BindableWithCurrent<double>();

        public Bindable<double> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        public CircularProgress()
        {
            Current.ValueChanged += newValue => Invalidate(Invalidation.DrawNode);
            Texture = Texture.WhitePixel;
        }

        protected override DrawNode CreateDrawNode() => new CircularProgressDrawNode(this);

        public TransformSequence<CircularProgress> FillTo(double newValue, double duration = 0, Easing easing = Easing.None)
            => FillTo(newValue, duration, new DefaultEasingFunction(easing));

        public TransformSequence<CircularProgress> FillTo<TEasing>(double newValue, double duration, in TEasing easing)
            where TEasing : IEasingFunction
            => this.TransformBindableTo(Current, newValue, duration, easing);

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "CircularProgress");
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "CircularProgress");
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
                innerRadius = Math.Clamp(value, 0, 1);
                Invalidate(Invalidation.DrawNode);
            }
        }

        private class CircularProgressDrawNode : SpriteDrawNode
        {
            public new CircularProgress Source => (CircularProgress)base.Source;

            public CircularProgressDrawNode(CircularProgress source)
                : base(source)
            {
            }

            private float innerRadius;
            private float progress;

            public override void ApplyState()
            {
                base.ApplyState();

                innerRadius = Source.innerRadius;
                progress = Math.Abs((float)Source.current.Value);
            }

            protected override void Blit(Action<TexturedVertex2D> vertexAction)
            {
                Shader.GetUniform<float>("innerRadius").UpdateValue(ref innerRadius);
                Shader.GetUniform<float>("progress").UpdateValue(ref progress);

                base.Blit(vertexAction);
            }
        }
    }

    public static class CircularProgressTransformSequenceExtensions
    {
        public static TransformSequence<CircularProgress> FillTo(this TransformSequence<CircularProgress> t, double newValue, double duration = 0, Easing easing = Easing.None)
            => t.FillTo(newValue, duration, new DefaultEasingFunction(easing));

        public static TransformSequence<CircularProgress> FillTo<TEasing>(this TransformSequence<CircularProgress> t, double newValue, double duration, TEasing easing)
            where TEasing : IEasingFunction
            => t.Append(cp => cp.FillTo(newValue, duration, easing));
    }
}
