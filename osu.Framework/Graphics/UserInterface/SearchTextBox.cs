using osu.Framework.Events;
using osu.Framework.Graphics.Sprites;
using OpenTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class SearchTextBox : TextBox
    {
        public SearchTextBox()
        {
            OnChange += SearchTextBox_OnChange;
        }

        #region Disposal

        protected override void Dispose(bool disposing)
        {
            OnChange -= SearchTextBox_OnChange;
            base.Dispose(disposing);
        }

        #endregion

        public override void Load(BaseGame game)
        {
            base.Load(game);
            Add(
                new Sprite
                {
                    Origin = Anchor.CentreRight,
                    Anchor = Anchor.CentreRight,
                    Texture = game.Textures.Get(@"Menu/logo"), // should be changed, but I don't have search icon texture
                    Size = new Vector2((float)(Size.Y * 0.9), (float)(Size.Y * 0.9))
                });
        }

        private void SearchTextBox_OnChange(TextBox sender, bool newText)
        {
            if (newText) OnSearch?.Invoke(this, new OnSearchEventArgs(Text));
        }

        public delegate void OnSearchHandler(object sender, OnSearchEventArgs e);

        public event OnSearchHandler OnSearch;
    }
}
