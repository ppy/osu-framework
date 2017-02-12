// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using System.Diagnostics;
using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Sprites
{
    public class Sprite : Drawable
    {
        private Shader textureShader;
        private Shader roundedTextureShader;

        public bool WrapTexture = false;

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
            SpriteDrawNode n = node as SpriteDrawNode;

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
            textureShader = shaders?.Load(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.Texture);
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.TextureRounded);
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
            else
            {
                Debug.Assert(
                    EdgeSmoothness.X <= MAX_EDGE_SMOOTHNESS &&
                    EdgeSmoothness.Y <= MAX_EDGE_SMOOTHNESS,
                    $@"May not smooth more than {MAX_EDGE_SMOOTHNESS} or will leak neighboring textures in atlas.");

                Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();

                inflationAmount = new Vector2(scale.X * EdgeSmoothness.X, scale.Y * EdgeSmoothness.Y);
                return ToScreenSpace(DrawRectangle.Inflate(inflationAmount));
            }
        }

        public override string ToString()
        {
            return base.ToString() + $" tex: {texture?.AssetName}";
        }

        public FillMode FillMode { get; set; }

        protected override Vector2 DrawScale
        {
            get
            {
                Vector2 modifier = Vector2.One;

                switch (FillMode)
                {
                    case FillMode.Fill:
                        modifier = new Vector2(Math.Max(Parent.DrawSize.X / DrawWidth, Parent.DrawSize.Y / DrawHeight));
                        break;
                    case FillMode.Fit:
                        modifier = new Vector2(Math.Min(Parent.DrawSize.X / DrawWidth, Parent.DrawSize.Y / DrawHeight));
                        break;
                    case FillMode.Stretch:
                        modifier = new Vector2(Parent.DrawSize.X / DrawWidth, Parent.DrawSize.Y / DrawHeight);
                        break;
                }

                return base.DrawScale * modifier;
            }
        }
    }

    public enum FillMode
    {
        None,
        Fill,
        Fit,
        Stretch
    }
}
