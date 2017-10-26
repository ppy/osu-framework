// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
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
        /// Rethrows the <see cref="AggregateException.InnerException"/> of an <see cref="AggregateException"/> if it exists,
        /// otherwise, rethrows <paramref name="aggregateException"/>.
        /// This preserves the stack trace of the exception that is rethrown, and will not include the point of rethrow.
        /// </summary>
        /// <param name="aggregateException">The captured exception.</param>
        public static void RethrowIfSingular(this AggregateException aggregateException)
        {
            if (aggregateException.InnerExceptions.Count == 1)
                aggregateException.InnerException.Rethrow();
            aggregateException.Rethrow();
        }
    }
}
