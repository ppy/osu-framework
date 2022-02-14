using osu.Framework.Testing;

namespace TemplateGame.Game.Tests.Visual
{
    public class TemplateGameTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new TemplateGameTestSceneTestRunner();

        private class TemplateGameTestSceneTestRunner : TemplateGameGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}
