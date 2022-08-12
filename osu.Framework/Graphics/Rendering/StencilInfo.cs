// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    public readonly struct StencilInfo : IEquatable<StencilInfo>
    {
        /// <summary>
        /// The default stencil properties.
        /// </summary>
        public static StencilInfo Default => new StencilInfo(false);

        /// <summary>
        /// Whether stencil testing should occur.
        /// If this is false, no <see cref="StencilOperation"/> will occur (use <see cref="DepthStencilFunction.Always"/> instead).
        /// </summary>
        public readonly bool StencilTest;

        /// <summary>
        /// The stencil test function.
        /// </summary>
        public readonly DepthStencilFunction TestFunction;

        /// <summary>
        /// The stencil test value to compare against.
        /// </summary>
        public readonly int TestValue;

        /// <summary>
        /// The stencil mask.
        /// </summary>
        public readonly int Mask;

        /// <summary>
        /// The operation to perform on the stencil buffer when the stencil test fails.
        /// </summary>
        public readonly StencilOperation StencilTestFailOperation;

        /// <summary>
        /// The operation to perform on the stencil buffer when the depth test fails.
        /// </summary>
        public readonly StencilOperation DepthTestFailOperation;

        /// <summary>
        /// The operation to perform on the stencil buffer when both the stencil and depth tests pass.
        /// </summary>
        public readonly StencilOperation TestPassedOperation;

        public StencilInfo(bool stencilTest = true, DepthStencilFunction testFunction = DepthStencilFunction.Always, int testValue = 1, int mask = 0xff,
                           StencilOperation stencilFailed = StencilOperation.Keep, StencilOperation depthFailed = StencilOperation.Keep, StencilOperation passed = StencilOperation.Replace)
        {
            StencilTest = stencilTest;
            TestFunction = testFunction;
            TestValue = testValue;
            Mask = mask;
            StencilTestFailOperation = stencilFailed;
            DepthTestFailOperation = depthFailed;
            TestPassedOperation = passed;
        }

        public bool Equals(StencilInfo other) =>
            other.StencilTest == StencilTest &&
            other.TestFunction == TestFunction &&
            other.TestValue == TestValue &&
            other.Mask == Mask &&
            other.StencilTestFailOperation == StencilTestFailOperation &&
            other.DepthTestFailOperation == DepthTestFailOperation &&
            other.TestPassedOperation == TestPassedOperation;
    }
}
