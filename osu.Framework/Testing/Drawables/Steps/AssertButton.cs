// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using NUnit.Framework;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public partial class AssertButton : StepButton
    {
        public required StackTrace CallStack { get; init; }
        public required Func<bool> Assertion { get; init; }
        public Func<string>? GetFailureMessage { get; init; }

        public string? ExtendedDescription { get; init; }

        public AssertButton()
        {
            Action += checkAssert;
            LightColour = Color4.OrangeRed;
        }

        private void checkAssert()
        {
            if (Assertion())
                Success();
            else
            {
                StringBuilder builder = new StringBuilder();

                builder.Append(Text);

                if (!string.IsNullOrEmpty(ExtendedDescription))
                    builder.Append($" {ExtendedDescription}");

                if (GetFailureMessage != null)
                    builder.Append($": {GetFailureMessage()}");

                throw ExceptionDispatchInfo.SetRemoteStackTrace(new AssertionException(builder.ToString()), CallStack.ToString());
            }
        }

        public override string ToString() => "Assert: " + base.ToString();
    }
}
