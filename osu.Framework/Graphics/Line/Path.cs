// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Sprites
{
    public class Path : Drawable
    {
        public List<Vector2> Positions = new List<Vector2>();
        public float PathWidth = 10f;

        private Shader roundedTextureShader;
        private Shader textureShader;

        private PathDrawNodeSharedData pathDrawNodeSharedData = new PathDrawNodeSharedData();

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

        protected override DrawNode CreateDrawNode() => new PathDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            PathDrawNode n = node as PathDrawNode;
            
            n.Texture = Texture;
            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;
            n.Width = PathWidth;

            n.Shared = pathDrawNodeSharedData;

            n.Segments.Clear();

            if (Positions.Count > 1)
            {
                for (int i = 0; i < Positions.Count - 1; ++i)
                {
                    Line line = new Line(Positions[i], Positions[i + 1]);
                    n.Segments.Add(new Line(line.StartPoint * DrawInfo.Matrix, line.EndPoint * DrawInfo.Matrix));
                }
            }

            base.ApplyDrawNode(node);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.Texture3D, FragmentShaderDescriptor.TextureRounded);
            textureShader = shaders?.Load(VertexShaderDescriptor.Texture3D, FragmentShaderDescriptor.Texture);
        }

        private Texture texture = Texture.WhitePixel;

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

        public override Drawable Clone()
        {
            Path clone = (Path)base.Clone();
            clone.texture = texture;

            return clone;
        }
    }
}
