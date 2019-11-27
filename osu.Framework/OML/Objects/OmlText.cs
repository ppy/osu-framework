using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.OML.Attributes;

namespace osu.Framework.OML.Objects
{
    [OmlObject(Aliases = new[] { "text" })]
    public class OmlText : OmlObject
    {
        private float fontSize { get; } = 20;

        [BackgroundDependencyLoader]
        private void load()
        {
            var font = new FontUsage(null, fontSize);

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
