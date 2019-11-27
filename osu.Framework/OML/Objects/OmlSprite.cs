using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.OML.Attributes;
using osuTK;

namespace osu.Framework.OML.Objects
{
    [OmlObject(Aliases = new[] { "img", "image", "sprite" })]
    public class OmlSprite : OmlObject
    {
        public string Src { get; set; }

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            var tex = store.Get(Src);

            var sprite = new Sprite
            {
                Texture = tex,

                Size = new Vector2(Width / Height), // Apply Aspect Ratio
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                FillMode = FillMode.Stretch,
            };

            Child = sprite;
        }
    }
}
