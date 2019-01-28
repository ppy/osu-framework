﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Transforms;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgress : Drawable, IHasCurrentValue<double>
    {
        private readonly Bindable<double> current = new Bindable<double>();

        public Bindable<double> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                current.UnbindBindings();
                current.BindTo(value);
            }
        }

        public CircularProgress()
        {
            Current.ValueChanged += newValue => Invalidate(Invalidation.DrawNode);
        }

        private Shader roundedTextureShader;
        private Shader textureShader;

        private readonly CircularProgressDrawNodeSharedData pathDrawNodeSharedData = new CircularProgressDrawNodeSharedData();

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            texture?.Dispose();
            texture = null;

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
            n.InnerRadius = innerRadius;

            base.ApplyDrawNode(node);
        }

        public TransformSequence<CircularProgress> FillTo(double newValue, double duration = 0, Easing easing = Easing.None)
            => this.TransformBindableTo(Current, newValue, duration, easing);

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            textureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
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

    public static class CircularProgressExtensions
    {
        public static TransformSequence<CircularProgress> FillTo(this CircularProgress t, double newValue, double duration = 0, Easing easing = Easing.None)
            => t.TransformBindableTo(t.Current, newValue, duration, easing);
    }
}
