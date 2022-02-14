using osu.Framework.Testing;

namespace FlappyDon.Game.Tests.Visual
{
    public class FlappyDonTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new FlappyDonTestSceneTestRunner();

        private class FlappyDonTestSceneTestRunner : FlappyDonGameBase, ITestSceneTestRunner
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
