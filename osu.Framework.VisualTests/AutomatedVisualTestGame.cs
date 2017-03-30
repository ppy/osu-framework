using osu.Framework.Testing;

namespace osu.Framework.VisualTests
{
    public class AutomatedVisualTestGame : Game
    {
        public AutomatedVisualTestGame()
        {
            Add(new TestRunner(new TestBrowser()));
        }
    }
}