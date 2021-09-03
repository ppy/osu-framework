using osu.Framework.Testing;

namespace Template.Game.Tests.Visual
{
    public class ApplicationTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new ApplicationTestRunner();

        private class ApplicationTestRunner : ApplicationBase, ITestSceneTestRunner
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
