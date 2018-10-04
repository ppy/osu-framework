// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestLogging
    {
        [Test]
        public void TestExceptionLogging()
        {
            TestException resolvedException = null;

            void logTest(LogEntry entry)
            {
                if (entry.Exception is TestException ex)
                {
                    Assert.IsNull(resolvedException, "exception was forwarded more than once");
                    resolvedException = ex;
                }
            }

            Logger.NewEntry += logTest;
            Logger.Error(new TestException(), "message");
            Logger.NewEntry -= logTest;

            Assert.IsNotNull(resolvedException, "exception wasn't forwarded by logger");
        }

        [Test]
        public void TestUnhandledExceptionLogging()
        {
            TestException resolvedException = null;

            void logTest(LogEntry entry)
            {
                if (entry.Exception is TestException ex)
                {
                    Assert.IsNull(resolvedException, "exception was forwarded more than once");
                    resolvedException = ex;
                }
            }

            Logger.NewEntry += logTest;

            try
            {
                using (var host = new HeadlessGameHost())
                {
                    var game = new TestGame();
                    game.Schedule(() => throw new TestException());
                    host.Run(game);
                }
            }
            catch
            {
                // catch crashing exception
            }

            Assert.IsNotNull(resolvedException, "exception wasn't forwarded by logger");
            Logger.NewEntry -= logTest;
        }

        [Test]
        public void TestUnhandledIgnoredException()
        {
            Assert.DoesNotThrow(() => runWithIgnoreCount(2, 2));
        }

        [Test]
        public void TestUnhandledIgnoredOnceException()
        {
            Assert.Throws<TestException>(() => runWithIgnoreCount(1, 2));
        }

        /// <summary>
        /// Ignore unhandled exceptions for the provided count.
        /// </summary>
        /// <param name="ignoreCount">Number of exceptions to ignore.</param>
        /// <param name="fireCount">How many exceptions to fire.</param>
        private void runWithIgnoreCount(int ignoreCount, int fireCount)
        {
            using (var host = new HeadlessGameHost())
            {
                host.ExceptionThrown += ex => ignoreCount-- > 0;

                var game = new TestGame();

                for (int i = 0; i < fireCount; i++)
                    game.Schedule(() => throw new TestException());
                game.Schedule(() => game.Exit());

                host.Run(game);
            }
        }

        [Test]
        public void TestGameUpdateExceptionNoLogging()
        {
            Assert.Throws<TestException>(() =>
            {
                using (var host = new HeadlessGameHost())
                    host.Run(new CrashTestGame());
            });
        }

        private class CrashTestGame : Game
        {
            protected override void Update()
            {
                base.Update();
                throw new TestException();
            }
        }

        [Test]
        public void TestTaskExceptionLogging()
        {
            Exception resolvedException = null;

            void logTest(LogEntry entry)
            {
                if (entry.Exception is AggregateException ex)
                {
                    Assert.IsNull(resolvedException, "exception was forwarded more than once");
                    resolvedException = ex;
                }
            }

            Logger.NewEntry += logTest;

            using (new HeadlessGameHost())
            {
                // see https://tpodolak.com/blog/2015/08/10/tpl-exception-handling-and-unobservedtaskexception-issue/
                // needs to be in a separate method so the Task gets GC'd.
                performTaskException();

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.IsNotNull(resolvedException, "exception wasn't forwarded by logger");
            Logger.NewEntry -= logTest;
        }

        private void performTaskException()
        {
            var task = Task.Run(() => throw new TestException());
            while (!task.IsCompleted)
                Thread.Sleep(1);
        }

        [Test]
        public void TestRecursiveExceptionLogging()
        {
            TestExceptionWithInnerException resolvedException = null;
            TestInnerException resolvedInnerException = null;

            void logTest(LogEntry entry)
            {
                if (entry.Exception is TestExceptionWithInnerException ex)
                {
                    Assert.IsNull(resolvedException, "exception was forwarded more than once");
                    resolvedException = ex;
                }

                if (entry.Exception is TestInnerException inner)
                {
                    Assert.IsNull(resolvedInnerException, "exception was forwarded more than once");
                    resolvedInnerException = inner;
                }
            }

            Logger.NewEntry += logTest;
            Logger.Error(new TestExceptionWithInnerException(), "message", recursive: true);
            Logger.NewEntry -= logTest;

            Assert.IsNotNull(resolvedException, "exception wasn't forwarded by logger");
            Assert.IsNotNull(resolvedInnerException, "inner exception wasn't forwarded by logger");
        }

        private class TestException : Exception
        {
        }

        public class TestExceptionWithInnerException : Exception
        {
            public TestExceptionWithInnerException()
                : base("", new TestInnerException())
            {
            }
        }

        private class TestInnerException : Exception
        {
        }
    }
}
