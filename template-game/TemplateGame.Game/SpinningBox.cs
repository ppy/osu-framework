using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace TemplateGame.Game
{
    public class SpinningBox : CompositeDrawable
    {
        private Box box;

        public SpinningBox()
        {
            Size = new Vector2(100);
            Origin = Anchor.Centre;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(box = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            box.Loop(b => b.RotateTo(0).RotateTo(360, 2500));
        }
    }
}
