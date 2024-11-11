// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using NUnit.Framework;

namespace osu.Framework.Testing.Drawables.Steps
{
    internal class TracedException : AssertionException
    {
        private readonly StackTrace trace;

        public TracedException(string description, StackTrace trace)
            : base(description)
        {
            this.trace = trace;
        }

        public override string StackTrace => trace.ToString();
    }
}
