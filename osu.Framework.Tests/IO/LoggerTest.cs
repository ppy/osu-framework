// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using NUnit.Framework;
using osu.Framework.Logging;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class LoggerTest
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
