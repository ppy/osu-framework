// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.OML.Attributes;
using osuTK;

namespace osu.Framework.OML.Objects
{
    [OmlObject("bufferedContainer")]
    public class BufferedOmlObject : OmlObject
    {
        private readonly BufferedContainer bufferedContainer;

        public Vector2 BlurSigma
        {
            get => bufferedContainer.BlurSigma;
            set => bufferedContainer.BlurSigma = value;
        }

        public float BlurRotation
        {
            get => bufferedContainer.BlurRotation;
            set => bufferedContainer.BlurRotation = value;
        }

        public ColourInfo EffectColour
        {
            get => bufferedContainer.EffectColour;
            set => bufferedContainer.EffectColour = value;
        }

        public BufferedOmlObject()
        {
            bufferedContainer = new BufferedContainer
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                FillMode = FillMode.Fill,
            };

            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.TopLeft;
            Origin = Anchor.TopLeft;
            FillMode = FillMode.Fill;

            base.Add(bufferedContainer);
        }

        public override void Add(Drawable drawable) => bufferedContainer.Add(drawable);
        public override bool Remove(Drawable drawable) => bufferedContainer.Remove(drawable);
    }
}
