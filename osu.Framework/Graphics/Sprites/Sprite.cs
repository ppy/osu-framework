// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A sprite that displays its texture.
    /// </summary>
    public class Sprite : Drawable
    {
        private Shader textureShader;
        private Shader roundedTextureShader;

        /// <summary>
        /// True if the texture should be tiled. If you had a 16x16 texture and scaled the sprite to be 64x64 the texture would be repeated in a 4x4 grid along the size of the sprite.
        /// </summary>
        public bool WrapTexture;

        /// <summary>
        /// Maximum value that can be set for <see cref="EdgeSmoothness"/> on either axis.
        /// </summary>
        public const int MAX_EDGE_SMOOTHNESS = 2;

        /// <summary>
        /// Determines over how many pixels of width the border of the sprite is smoothed
        /// in X and Y direction respectively.
        /// IMPORTANT: When masking an edge-smoothed sprite some of the smooth transition
        /// may be masked away. This should be counteracted by setting the MaskingSmoothness
        /// of the masking container to a slightly larger value than EdgeSmoothness.
        /// </summary>
        public Vector2 EdgeSmoothness = Vector2.Zero;

        /// <summary>
        /// True if the <see cref="Texture"/> should be disposed when this sprite gets disposed.
        /// </summary>
        public bool CanDisposeTexture { get; protected set; }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            if (CanDisposeTexture && texture != null)
            {
                if (!(texture is TextureWhitePixel))
                    texture.Dispose();
                texture = null;
            }

            base.Dispose(isDisposing);
        }

        #endregion

        protected override DrawNode CreateDrawNode() => new SpriteDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            SpriteDrawNode n = (SpriteDrawNode)node;

            n.ScreenSpaceDrawQuad = ScreenSpaceDrawQuad;
            n.DrawRectangle = DrawRectangle;
            n.Texture = Texture;
            n.WrapTexture = WrapTexture;

            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;
            n.InflationAmount = inflationAmount;

            base.ApplyDrawNode(node);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            textureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
        }

        private Texture texture;

        /// <summary>
        /// The texture that this sprite should draw. If <see cref="CanDisposeTexture"/> is true and the texture gets replaced, the old texture will be disposed.
        /// If this sprite's <see cref="Drawable.Size"/> is <see cref="Vector2.Zero"/> (eg if it has not been set previously), the <see cref="Drawable.Size"/>
        /// of this sprite will be set to the size of the texture.
        /// <see cref="Drawable.FillAspectRatio"/> is automatically set to the aspect ratio of the given texture or 1 if the texture is null.
        /// </summary>
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
                FillAspectRatio = (float)(texture?.Width ?? 1) / (texture?.Height ?? 1);
                Invalidate(Invalidation.DrawNode);

                if (Size == Vector2.Zero)
                    Size = new Vector2(texture?.DisplayWidth ?? 0, texture?.DisplayHeight ?? 0);
            }
        }

        private Vector2 inflationAmount;

        protected override Quad ComputeScreenSpaceDrawQuad()
        {
            if (EdgeSmoothness == Vector2.Zero)
            {
                inflationAmount = Vector2.Zero;
                return base.ComputeScreenSpaceDrawQuad();
            }

            if (EdgeSmoothness.X > MAX_EDGE_SMOOTHNESS || EdgeSmoothness.Y > MAX_EDGE_SMOOTHNESS)
                throw new InvalidOperationException(
                    $"May not smooth more than {MAX_EDGE_SMOOTHNESS} or will leak neighboring textures in atlas. Tried to smooth by ({EdgeSmoothness.X}, {EdgeSmoothness.Y}).");

            Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();

            inflationAmount = new Vector2(scale.X * EdgeSmoothness.X, scale.Y * EdgeSmoothness.Y);
            return ToScreenSpace(DrawRectangle.Inflate(inflationAmount));
        }

        public override string ToString()
        {
            string result = base.ToString();
            if (!string.IsNullOrEmpty(texture?.AssetName))
                result += $" tex: {texture?.AssetName}";
            return result;
        }
    }
}
