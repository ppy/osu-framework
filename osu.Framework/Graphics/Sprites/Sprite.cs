// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Sprites
{
    public class Sprite : Drawable
    {
        private Shader textureShader;
        private Shader roundedTextureShader;

        public bool WrapTexture;

        public const int MAX_EDGE_SMOOTHNESS = 2;

        /// <summary>
        /// Determines over how many pixels of width the border of the sprite is smoothed
        /// in X and Y direction respectively.
        /// IMPORTANT: When masking an edge-smoothed sprite some of the smooth transition
        /// may be masked away. This should be counteracted by setting the MaskingSmoothness
        /// of the masking container to a slightly larger value than EdgeSmoothness.
        /// </summary>
        public Vector2 EdgeSmoothness = Vector2.Zero;

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
