// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Collections.Generic;
using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Textures
{
    public sealed class TextureSheet
    {
        public Texture Raw { get; }

        public IReadOnlyList<TextureCropSchema> Crops => crops;

        private List<TextureCropSchema> crops;

        public TextureSheet(Texture texture)
        {
            Raw = texture;
            crops = new List<TextureCropSchema>();
        }

        public void Append(TextureCropSchema crop)
        {
            // Validate Size
            if (crop.Size.X > Raw.Size.X || crop.Size.Y > Raw.Size.Y)
                throw new InvalidOperationException("The crop size cannot be larger than the original texture size.");

            // Validate Offset
            if (crop.Offset.X > Raw.Size.X || crop.Offset.Y > Raw.Size.Y)
                throw new InvalidOperationException("The crop offset cannot be larger than the original texture size.");

            crops.Add(crop);
        }

        public void Append(TextureCropSchema[] crops)
        {
            foreach (var crop in crops)
                Append(crop);
        }

        public IReadOnlyList<Texture> Build()
        {
            return (from crop in crops
                    select Raw.Crop(new RectangleF(crop.Offset, crop.Size))).ToList();
        }

        public static TextureSheet Auto(
            Texture texture,
            int columns,
            int rows,
            Vector2 size,
            Vector2 spacing,
            Axes relativeSizeAxes = Axes.None,
            WrapMode wrapModeS = WrapMode.None,
            WrapMode wrapModeT = WrapMode.None)
        {
            var sheet = new TextureSheet(texture);

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    sheet.Append(new TextureCropSchema
                    {
                        Size = size,
                        Offset = new Vector2((size.X + spacing.X) * x, (size.Y + spacing.Y) * y),
                        RelativeSizeAxes = relativeSizeAxes,
                        WrapModeS = wrapModeS,
                        WrapModeT = wrapModeT
                    });
                }

            return sheet;
        }
    }

    public class TextureCropSchema
    {
        public Vector2 Size = Vector2.Zero;
        public Vector2 Offset = Vector2.Zero;

        public Axes RelativeSizeAxes = Axes.None;

        public WrapMode WrapModeS = WrapMode.None;
        public WrapMode WrapModeT = WrapMode.None;
    }
}
