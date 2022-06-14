// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Exceptions
{
    [HeadlessTest]
    public class TestSceneDependencyInjectionExceptions : FrameworkTestScene
    {
        [Test]
        public void TestImmediateException()
        {
            Exception thrownException = null;

            AddStep("add thrower", () =>
            {
                try
                {
                    Child = new Thrower(typeof(Exception));
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }
            });

            assertCorrectStack(() => thrownException);
        }

        [Test]
        public void TestImmediateAggregateException()
        {
            Exception thrownException = null;

            AddStep("add thrower", () =>
            {
                try
                {
                    Child = new Thrower(typeof(Exception), true);
                }
                catch (Exception ex)
                {
                    thrownException = ex;
                }
            });

            assertCorrectStack(() => thrownException);
        }

        [Test]
        public void TestAsyncException()
        {
            AsyncThrower thrower = null;

            AddStep("add thrower", () => Child = thrower = new AsyncThrower(typeof(Exception)));
            AddUntilStep("wait for exception", () => thrower.ThrownException != null);

            assertCorrectStack(() => thrower.ThrownException);
        }

        private void assertCorrectStack(Func<Exception> exception) => AddAssert("exception has correct callstack", () =>
        {
            string stackTrace = exception().StackTrace;
            Debug.Assert(stackTrace != null);

            return stackTrace.Contains($"{nameof(TestSceneDependencyInjectionExceptions)}.{nameof(Thrower)}");
        });

        private class AsyncThrower : CompositeDrawable
        {
            public Exception ThrownException { get; private set; }

            private readonly Type exceptionType;

            public AsyncThrower(Type exceptionType)
            {
                this.exceptionType = exceptionType;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                LoadComponentAsync(new Thrower(exceptionType), AddInternal);
            }

            public override bool UpdateSubTree()
            {
                try
                {
                    return base.UpdateSubTree();
                }
                catch (Exception ex)
                {
                    ThrownException = ex;
                }

                return true;
            }
        }

        private class Thrower : Drawable
        {
            private readonly Type exceptionType;
            private readonly bool aggregate;

            public Thrower(Type exceptionType, bool aggregate = false)
            {
                this.exceptionType = exceptionType;
                this.aggregate = aggregate;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (aggregate)
                {
                    try
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        throw (Exception)Activator.CreateInstance(exceptionType);
                    }
                    catch (Exception ex)
                    {
                        throw new AggregateException(ex);
                    }
                }

                // ReSharper disable once PossibleNullReferenceException
                throw (Exception)Activator.CreateInstance(exceptionType);
            }
        }
    }
}
