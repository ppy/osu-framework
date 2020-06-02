// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A sprite that displays its texture.
    /// </summary>
    public class Sprite : Drawable, ITexturedShaderDrawable
    {
        public IShader TextureShader { get; protected set; }

        public IShader RoundedTextureShader { get; protected set; }

        [Obsolete("Has no effect. Use TextureRectangle instead.")] // can be removed 20201201
        public bool WrapTexture { get; set; }

        private RectangleF textureRectangle = new RectangleF(0, 0, 1, 1);

        /// <summary>
        /// Sub-rectangle of the sprite in which the texture is positioned.
        /// Can be either relative coordinates (0 to 1) or absolute coordinates,
        /// depending on <see cref="TextureRelativeSizeAxes"/>.
        /// </summary>
        public RectangleF TextureRectangle
        {
            get => textureRectangle;
            set
            {
                if (textureRectangle == value)
                    return;

                textureRectangle = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Axes textureRelativeSizeAxes = Axes.Both;

        /// <summary>
        /// Whether or not the <see cref="TextureRectangle"/> is in relative coordinates
        /// (0 to 1) or in absolute coordinates.
        /// </summary>
        public Axes TextureRelativeSizeAxes
        {
            get => textureRelativeSizeAxes;
            set
            {
                if (textureRelativeSizeAxes == value)
                    return;

                textureRelativeSizeAxes = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        /// <summary>
        /// Absolutely sized sub-rectangle in which the texture is positioned in the coordinate space of this <see cref="Sprite"/>.
        /// Based on <see cref="TextureRectangle"/>.
        /// </summary>
        public RectangleF DrawTextureRectangle
        {
            get
            {
                RectangleF result = TextureRectangle;

                if (TextureRelativeSizeAxes != Axes.None)
                {
                    var drawSize = DrawSize;

                    if ((TextureRelativeSizeAxes & Axes.X) > 0)
                    {
                        result.X *= drawSize.X;
                        result.Width *= drawSize.X;
                    }

                    if ((TextureRelativeSizeAxes & Axes.Y) > 0)
                    {
                        result.Y *= drawSize.Y;
                        result.Height *= drawSize.Y;
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Maximum value that can be set for <see cref="EdgeSmoothness"/> on either axis.
        /// </summary>
        public const int MAX_EDGE_SMOOTHNESS = 3; // See https://github.com/ppy/osu-framework/pull/3511#discussion_r421665156 for relevant discussion.

        /// <summary>
        /// Determines over how many pixels of width the border of the sprite is smoothed
        /// in X and Y direction respectively.
        /// IMPORTANT: When masking an edge-smoothed sprite some of the smooth transition
        /// may be masked away. This should be counteracted by setting the MaskingSmoothness
        /// of the masking container to a slightly larger value than EdgeSmoothness.
        /// </summary>
        public Vector2 EdgeSmoothness = Vector2.Zero;

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            texture?.Dispose();
            texture = null;

            base.Dispose(isDisposing);
        }

        #endregion

        protected override DrawNode CreateDrawNode() => new SpriteDrawNode(this);

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
        }

        private Texture texture;

        /// <summary>
        /// The texture that this sprite should draw. Any previous texture will be disposed.
        /// If this sprite's <see cref="Drawable.Size"/> is <see cref="Vector2.Zero"/> (eg if it has not been set previously), the <see cref="Drawable.Size"/>
        /// of this sprite will be set to the size of the texture.
        /// <see cref="Drawable.FillAspectRatio"/> is automatically set to the aspect ratio of the given texture or 1 if the texture is null.
        /// </summary>
        public virtual Texture Texture
        {
            get => texture;
            set
            {
                if (value == texture)
                    return;

                texture?.Dispose();
                texture = value;

                float width;
                float height;

                if ((TextureRelativeSizeAxes & Axes.X) > 0)
                    width = (texture?.Width ?? 1) / TextureRectangle.Width;
                else
                    width = TextureRectangle.Width;

                if ((TextureRelativeSizeAxes & Axes.Y) > 0)
                    height = (texture?.Height ?? 1) / TextureRectangle.Height;
                else
                    height = TextureRectangle.Height;

                FillAspectRatio = width / height;
                Invalidate(Invalidation.DrawNode);

                if (Size == Vector2.Zero)
                    Size = new Vector2(texture?.DisplayWidth ?? 0, texture?.DisplayHeight ?? 0);
            }
        }

        public Vector2 InflationAmount { get; private set; }

        protected override Quad ComputeScreenSpaceDrawQuad()
        {
            if (EdgeSmoothness == Vector2.Zero)
            {
                InflationAmount = Vector2.Zero;
                return base.ComputeScreenSpaceDrawQuad();
            }

            if (EdgeSmoothness.X > MAX_EDGE_SMOOTHNESS || EdgeSmoothness.Y > MAX_EDGE_SMOOTHNESS)
            {
                throw new InvalidOperationException(
                    $"May not smooth more than {MAX_EDGE_SMOOTHNESS} or will leak neighboring textures in atlas. Tried to smooth by ({EdgeSmoothness.X}, {EdgeSmoothness.Y}).");
            }

            Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();

            InflationAmount = new Vector2(scale.X * EdgeSmoothness.X, scale.Y * EdgeSmoothness.Y);
            return ToScreenSpace(DrawRectangle.Inflate(InflationAmount));
        }

        public override string ToString()
        {
            string result = base.ToString();
            if (!string.IsNullOrEmpty(texture?.AssetName))
                result += $" tex: {texture.AssetName}";
            return result;
        }
    }
}
