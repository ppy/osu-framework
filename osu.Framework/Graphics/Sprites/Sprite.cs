// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.OpenGL;
using System.Diagnostics;

namespace osu.Framework.Graphics.Sprites
{
    public class Sprite : Drawable
    {
        private Shader textureShader;
        private Shader roundedTextureShader;

        public bool WrapTexture = false;
        public bool SmoothenEdges = false;

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

        protected override void Load(BaseGame game)
        {
            base.Load(game);

            if (textureShader == null)
                textureShader = game?.Shaders?.Load(new ShaderDescriptor(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.Texture));

            if (roundedTextureShader == null)
                roundedTextureShader = game?.Shaders?.Load(new ShaderDescriptor(VertexShaderDescriptor.Texture2D, FragmentShaderDescriptor.TextureRounded));
        }

        protected override bool CheckForcedPixelSnapping(Quad screenSpaceQuad)
        {
            return
                Rotation == 0
                && Math.Abs(screenSpaceQuad.Width - Math.Round(screenSpaceQuad.Width)) < 0.1f
                && Math.Abs(screenSpaceQuad.Height - Math.Round(screenSpaceQuad.Height)) < 0.1f;
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

        private float inflationAmount;
        protected override Quad ComputeScreenSpaceDrawQuad()
        {
            if (!SmoothenEdges)
            {
                inflationAmount = 0;
                return base.ComputeScreenSpaceDrawQuad();
            }

            Vector3 scale = DrawInfo.MatrixInverse.ExtractScale();
            inflationAmount = (float)Math.Min(scale.X, scale.Y);
            return ToScreenSpace(DrawRectangle.Inflate(inflationAmount));
        }

        public override Drawable Clone()
        {
            Sprite clone = (Sprite)base.Clone();
            clone.texture = texture;

            return clone;
        }

        public override string ToString()
        {
            return base.ToString() + $" tex: {texture?.AssetName}";
        }
    }
}
