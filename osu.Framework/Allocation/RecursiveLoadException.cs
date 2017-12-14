// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// The exception that is re-thrown by <see cref="DependencyContainer"/> when a loader invocation fails.
    /// This exception type builds a readablestacktrace message since loader invocations tend to be long recursive reflection calls.
    /// </summary>
    public class RecursiveLoadException : Exception
    {
        /// <summary>
        /// Types that are ignored for the custom stack traces. The initializers for these typically invoke
        /// initializers in user code where the problem actually lies.
        /// </summary>
        private static readonly Type[] blacklist =
        {
            typeof(Container),
            typeof(Container<>),
            typeof(CompositeDrawable)
        };

        private readonly StringBuilder traceBuilder;

        public RecursiveLoadException(Exception inner, MethodInfo loaderMethod)
            : base(string.Empty, (inner as RecursiveLoadException)?.InnerException ?? inner)
        {
            traceBuilder = inner is RecursiveLoadException recursiveException ? recursiveException.traceBuilder : new StringBuilder();

            if (!blacklist.Contains(loaderMethod.DeclaringType))
                traceBuilder.AppendLine($"  at {loaderMethod.DeclaringType}.{loaderMethod.Name} ()");
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append(InnerException?.ToString() ?? string.Empty);
            builder.AppendLine(traceBuilder.ToString());

            return builder.ToString();
        }
    }
}
