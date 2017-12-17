// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics
{
    public class UpdateSubTreeException : Exception
    {
        /// <summary>
        /// Types that are ignored for the custom stack traces.
        /// </summary>
        private static readonly Type[] blacklist =
        {
            typeof(Container),
            typeof(Container<>),
            typeof(CompositeDrawable)
        };

        private readonly Exception original;

        public UpdateSubTreeException(Exception original, CompositeDrawable root)
            : base(original.Message)
        {
            this.original = original;

            var originType = original.TargetSite.DeclaringType;

            var recursiveTypes = new List<Type>();
            Drawable current = root;
            while (current != null)
            {
                var currentType = current.GetType();
                if (currentType == originType || !blacklist.Contains(currentType))
                    recursiveTypes.Add(current.GetType());

                var composite = current as CompositeDrawable;
                if (composite == null || composite.CurrentUpdateIndex == -1)
                    break;
                current = composite.AliveInternalChildren[composite.CurrentUpdateIndex];
            }

            var traceBuilder = new StringBuilder();

            // Write all lines from the original exception until the first UpdateSubTree is hit
            var lines = original.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var o in lines)
            {
                traceBuilder.AppendLine(o);
                if (o.Contains(nameof(Drawable.UpdateSubTree)))
                    break;
            }

            // Skip the first UpdateSubTree, since that's going to be handled by the foreach above with more complete details
            for (int i = recursiveTypes.Count - 2; i >= 0; i--)
                traceBuilder.AppendLine($"  at {recursiveTypes[i]}.{nameof(Drawable.UpdateSubTree)} ()");
            stackTrace = traceBuilder.ToString();
        }

        private readonly string stackTrace;
        public override string StackTrace => stackTrace;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{original.GetType()}: {original.Message}");
            builder.AppendLine(stackTrace);

            return builder.ToString();
        }
    }
}
