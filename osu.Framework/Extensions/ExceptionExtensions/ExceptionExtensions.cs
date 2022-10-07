// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace osu.Framework.Extensions.ExceptionExtensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Rethrows <paramref name="exception"/> as if it was captured in the current context.
        /// This preserves the stack trace of <paramref name="exception"/>, and will not include the point of rethrow.
        /// </summary>
        /// <param name="exception">The captured exception.</param>
        public static void Rethrow(this Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        /// <summary>
        /// Rethrows the <see cref="Exception.InnerException"/> of an <see cref="AggregateException"/> if it exists,
        /// otherwise, rethrows <paramref name="aggregateException"/>.
        /// This preserves the stack trace of the exception that is rethrown, and will not include the point of rethrow.
        /// </summary>
        /// <param name="aggregateException">The captured exception.</param>
        public static void RethrowAsSingular(this AggregateException aggregateException) => aggregateException.AsSingular().Rethrow();

        /// <summary>
        /// Flattens <paramref name="aggregateException"/> into a singular <see cref="Exception"/> if the <paramref name="aggregateException"/>
        /// contains only a single <see cref="Exception"/>. Otherwise, returns <paramref name="aggregateException"/>.
        /// </summary>
        /// <param name="aggregateException">The captured exception.</param>
        /// <returns>The highest level of flattening possible.</returns>
        public static Exception AsSingular(this AggregateException aggregateException)
        {
            if (aggregateException.InnerExceptions.Count != 1)
                return aggregateException;

            while (aggregateException.InnerExceptions.Count == 1)
            {
                if (!(aggregateException.InnerException is AggregateException innerAggregate))
                    return aggregateException.InnerException;

                aggregateException = innerAggregate;
            }

            return aggregateException;
        }

        /// <summary>
        /// Retrieves the last exception from a recursive <see cref="TargetInvocationException"/>.
        /// </summary>
        /// <param name="exception">The exception to retrieve the exception from.</param>
        /// <returns>The exception at the point of invocation.</returns>
        public static Exception GetLastInvocation(this TargetInvocationException exception)
        {
            var inner = exception.InnerException;
            while (inner is TargetInvocationException)
                inner = inner.InnerException;
            return inner;
        }
    }
}
