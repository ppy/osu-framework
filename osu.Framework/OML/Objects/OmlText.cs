// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.OML.Attributes;

namespace osu.Framework.OML.Objects
{
    [OmlObject(Aliases = new[] { "text" })]
    public class OmlText : OmlObject
    {
        [UsedImplicitly]
        public float FontSize { get; set; } = 20;

        [BackgroundDependencyLoader]
        private void load()
        {
            var font = new FontUsage(null, FontSize);

            var text = new SpriteText
            {
                Text = BindableValue.Value,
                Font = font,

                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Stretch,
            };

            Child = text;
        }
    }
}
