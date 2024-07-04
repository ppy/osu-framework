// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using NUnit.Framework;

namespace osu.Framework.Logging
{
    /// <summary>
    /// A <see cref="TraceListener"/> that throws exceptions when a trace is hit.
    /// This allows consistent behaviour across runtimes (ie. under Mono where no winforms dialog is displayed on encountering an exception).
    /// </summary>
    public class ThrowingTraceListener : TraceListener
    {
        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void Fail(string message) => throw new AssertionException(message);

        public override void Fail(string message1, string message2) => throw new AssertionException($"{message1}: {message2}");
    }
}
