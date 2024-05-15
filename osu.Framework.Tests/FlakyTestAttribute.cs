// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace osu.Framework.Tests
{
    /// <summary>
    /// An attribute to mark any flaky tests.
    /// Will add a retry count unless environment variable `FAIL_FLAKY_TESTS` is set to `1`.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class FlakyTestAttribute : NUnitAttribute, IRepeatTest
    {
        private readonly int tryCount;

        public FlakyTestAttribute()
            : this(10)
        {
        }

        public FlakyTestAttribute(int tryCount)
        {
            this.tryCount = tryCount;
        }

        public TestCommand Wrap(TestCommand command) => new FlakyTestCommand(command, tryCount);

        // Adapted from https://github.com/nunit/nunit/blob/4eaab2eef3713907ca37bfb2f7f47e3fc2785214/src/NUnitFramework/framework/Attributes/RetryAttribute.cs
        // Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt
        public class FlakyTestCommand : DelegatingTestCommand
        {
            private readonly int tryCount;

            public FlakyTestCommand(TestCommand innerCommand, int tryCount)
                : base(innerCommand)
            {
                this.tryCount = tryCount;
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                int count = FrameworkEnvironment.FailFlakyTests ? 1 : tryCount;

                while (count-- > 0)
                {
                    try
                    {
                        context.CurrentResult = innerCommand.Execute(context);
                    }
                    catch (Exception ex)
                    {
                        context.CurrentResult ??= context.CurrentTest.MakeTestResult();
                        context.CurrentResult.RecordException(ex);
                    }

                    if (context.CurrentResult.ResultState != ResultState.Failure)
                        break;

                    if (count > 0)
                    {
                        context.CurrentResult = context.CurrentTest.MakeTestResult();
                        context.CurrentRepeatCount++;
                    }
                }

                return context.CurrentResult;
            }
        }
    }
}
