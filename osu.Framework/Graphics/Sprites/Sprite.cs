// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using osu.Framework.Layout;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A sprite that displays its texture.
    /// </summary>
    public class Sprite : Drawable, ITexturedShaderDrawable
    {
        public Sprite()
        {
            AddLayout(conservativeScreenSpaceDrawQuadBacking);
            AddLayout(inflationAmountBacking);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
        }

        public IShader TextureShader { get; protected set; }

        public IShader RoundedTextureShader { get; protected set; }

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

        private Vector2 edgeSmoothness;

        /// <summary>
        /// Determines over how many pixels of width the border of the sprite is smoothed
        /// in X and Y direction respectively.
        /// IMPORTANT: When masking an edge-smoothed sprite some of the smooth transition
        /// may be masked away. This should be counteracted by setting the MaskingSmoothness
        /// of the masking container to a slightly larger value than EdgeSmoothness.
        /// </summary>
        public Vector2 EdgeSmoothness
        {
            get => edgeSmoothness;
            set
            {
                if (edgeSmoothness == value)
                    return;

                if (value.X > MAX_EDGE_SMOOTHNESS || value.Y > MAX_EDGE_SMOOTHNESS)
                {
                    throw new InvalidOperationException(
                        $"May not smooth more than {MAX_EDGE_SMOOTHNESS} or will leak neighboring textures in atlas. Tried to smooth by ({value.X}, {value.Y}).");
                }

                edgeSmoothness = value;

                Invalidate(Invalidation.DrawInfo);
            }
        }

        protected override DrawNode CreateDrawNode() => new SpriteDrawNode(this);

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
                conservativeScreenSpaceDrawQuadBacking.Invalidate();

                if (Size == Vector2.Zero)
                    Size = new Vector2(texture?.DisplayWidth ?? 0, texture?.DisplayHeight ?? 0);
            }
        }

        public Vector2 InflationAmount => inflationAmountBacking.IsValid ? inflationAmountBacking.Value : (inflationAmountBacking.Value = computeInflationAmount());

        private readonly LayoutValue<Vector2> inflationAmountBacking = new LayoutValue<Vector2>(Invalidation.DrawInfo);

        private Vector2 computeInflationAmount()
        {
            if (EdgeSmoothness == Vector2.Zero)
                return Vector2.Zero;

            return DrawInfo.MatrixInverse.ExtractScale().Xy * EdgeSmoothness;
        }

        protected override Quad ComputeScreenSpaceDrawQuad()
        {
            if (EdgeSmoothness == Vector2.Zero)
                return base.ComputeScreenSpaceDrawQuad();

            return ToScreenSpace(DrawRectangle.Inflate(InflationAmount));
        }

        // Matches the invalidation types of Drawable.screenSpaceDrawQuadBacking
        private readonly LayoutValue<Quad> conservativeScreenSpaceDrawQuadBacking = new LayoutValue<Quad>(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence);

        public Quad ConservativeScreenSpaceDrawQuad => conservativeScreenSpaceDrawQuadBacking.IsValid
            ? conservativeScreenSpaceDrawQuadBacking
            : conservativeScreenSpaceDrawQuadBacking.Value = ComputeConservativeScreenSpaceDrawQuad();

        protected virtual Quad ComputeConservativeScreenSpaceDrawQuad()
        {
            if (Texture == null || Texture is TextureWhitePixel)
            {
                if (EdgeSmoothness == Vector2.Zero)
                    return ScreenSpaceDrawQuad;

                return ToScreenSpace(DrawRectangle);
            }

            // ======================================================================================================================
            // The following commented-out code shrinks the texture by the maximum mip level and is thereby conservative.
            // Alternatively, which is the un-commented code, one can assume a certain worst-case LOD bias (in this case -1) and shrink
            // the rectangle in screen space by 0.5 * 2*(LOD_bias) pixels.
            // ======================================================================================================================

            // RectangleF texRect = RelativeDrawTextureRectangle;
            // Vector2 shrinkageAmount = Vector2.Divide(texRect.Size * (1 << IRenderer.MAX_MIPMAP_LEVELS) / 2, Texture.Size);
            // shrinkageAmount = Vector2.ComponentMin(shrinkageAmount, texRect.Size / 2);
            // texRect = texRect.Inflate(-shrinkageAmount);
            //
            // return ToScreenSpace(texRect * DrawSize);

            Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();
            RectangleF rectangle = DrawTextureRectangle;

            // If the texture wraps or is clamped to its edge in some direction, then the entire
            // sprite is opaque in that direction, hence the texture's opaque rectangle can be
            // expanded to the full draw dimension of the sprite.
            if (Texture.WrapModeS == WrapMode.ClampToEdge || Texture.WrapModeS == WrapMode.Repeat)
            {
                rectangle.X = 0;
                rectangle.Width = DrawWidth;
            }

            if (Texture.WrapModeT == WrapMode.ClampToEdge || Texture.WrapModeT == WrapMode.Repeat)
            {
                rectangle.Y = 0;
                rectangle.Height = DrawHeight;
            }

            Vector2 shrinkageAmount = Vector2.ComponentMin(scale.Xy, rectangle.Size / 2);

            return ToScreenSpace(rectangle.Inflate(-shrinkageAmount));
        }

        public override string ToString()
        {
            string result = base.ToString();
            if (!string.IsNullOrEmpty(texture?.AssetName))
                result += $" tex: {texture.AssetName}";
            return result;
        }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            texture?.Dispose();
            texture = null;

            base.Dispose(isDisposing);
        }

        #endregion
    }
}
