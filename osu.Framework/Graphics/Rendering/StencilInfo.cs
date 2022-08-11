// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering
{
    public struct StencilInfo : IEquatable<StencilInfo>
    {
        /// <summary>
        /// Whether stencil testing should occur.
        /// If this is false, no <see cref="StencilOp"/> will occur (use <see cref="StencilFunction.Always"/> instead).
        /// </summary>
        public readonly bool StencilTest;

        /// <summary>
        /// The stencil test function.
        /// </summary>
        public readonly StencilFunction TestFunction;

        /// <summary>
        /// The stencil test value to compare against.
        /// </summary>
        public readonly int TestValue;

        /// <summary>
        /// The stencil mask.
        /// </summary>
        public readonly int Mask;

        /// <summary>
        /// The operation to perform on the stencil buffer in case the stencil test failed.
        /// </summary>
        public readonly StencilOp StencilTestFailOperation;

        /// <summary>
        /// The operation to perform on the stencil buffer in case the depth test failed.
        /// </summary>
        public readonly StencilOp DepthTestFailOperation;

        /// <summary>
        /// The operation to perform on the stencil buffer in case the stencil test passed.
        /// </summary>
        public readonly StencilOp TestPassedOperation;

        public StencilInfo(bool stencilTest = false, StencilFunction testFunction = StencilFunction.Always, int testValue = 1, int mask = 0xff,
            StencilOp stencilFailed = StencilOp.Keep, StencilOp depthFailed = StencilOp.Keep, StencilOp passed = StencilOp.Replace)
        {
            StencilTest = stencilTest;
            TestFunction = testFunction;
            TestValue = testValue;
            Mask = mask;
            StencilTestFailOperation = stencilFailed;
            DepthTestFailOperation = depthFailed;
            TestPassedOperation = passed;
        }

        public bool Equals (StencilInfo other) =>
            other.StencilTest == StencilTest &&
            other.TestFunction == TestFunction &&
            other.TestValue == TestValue &&
            other.Mask == Mask &&
            other.StencilTestFailOperation == StencilTestFailOperation &&
            other.DepthTestFailOperation == DepthTestFailOperation &&
            other.TestPassedOperation == TestPassedOperation;
    }
}
