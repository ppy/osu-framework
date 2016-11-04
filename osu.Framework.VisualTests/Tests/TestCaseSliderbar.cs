using osu.Framework.Configuration;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseSliderbar : TestCase
    {
        public override string Name => @"Sliderbar";
        public override string Description => @"Sliderbar tests.";
        private Sliderbar positiveAndNegativeSliderbar;
        private BindableDouble positiveAndNegativeBindableDouble;
        private SpriteText positiveAndNegativeSliderbarText;

        public override void Reset()
        {
            base.Reset();

            positiveAndNegativeBindableDouble=new BindableDouble(8);
            positiveAndNegativeBindableDouble.ValueChanged += PositiveAndNegativeBindableDouble_ValueChanged;

            positiveAndNegativeSliderbarText = new SpriteText
            {
                Text = $"Selected value: {positiveAndNegativeBindableDouble.Value}",
                Position = new Vector2(25,0)
            };
            positiveAndNegativeSliderbar = new Sliderbar(-10, 10, positiveAndNegativeBindableDouble, Color4.White, Color4.Pink)
            {
                Size = new Vector2(200, 10),
                Position = new Vector2(25, 25)
            };

            Add(positiveAndNegativeSliderbar);
            Add(positiveAndNegativeSliderbarText);
        }

        private void PositiveAndNegativeBindableDouble_ValueChanged(object sender, System.EventArgs e)
        {
            positiveAndNegativeSliderbarText.Text = $"Selected value: {positiveAndNegativeBindableDouble.Value:N}";
        }
    }
}
