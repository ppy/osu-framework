using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace FlappyDon.Game.Testing
{
    public partial class FlappyDonTestBrowser : FlappyDonGameBase
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddRange(new Drawable[]
            {
                new DrawSizePreservingFillContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new TestBrowser("FlappyDon"),
                        new CursorContainer()
                    }
                }
            });
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);
            host.Window.CursorState |= CursorState.Hidden;
        }
    }
}
