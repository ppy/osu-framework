// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    internal static class GLUtils
    {
        public static PrimitiveType ToPrimitiveType(PrimitiveTopology topology)
        {
            switch (topology)
            {
                case PrimitiveTopology.Points:
                    return PrimitiveType.Points;

                case PrimitiveTopology.Lines:
                    return PrimitiveType.Lines;

                case PrimitiveTopology.LineStrip:
                    return PrimitiveType.LineStrip;

                case PrimitiveTopology.Triangles:
                    return PrimitiveType.Triangles;

                case PrimitiveTopology.TriangleStrip:
                    return PrimitiveType.TriangleStrip;

                default:
                    throw new ArgumentException($"Unsupported vertex topology: {topology}.", nameof(topology));
            }
        }

        public static DepthFunction ToDepthFunction(DepthStencilFunction testFunction)
        {
            switch (testFunction)
            {
                case DepthStencilFunction.Never:
                    return DepthFunction.Never;

                case DepthStencilFunction.LessThan:
                    return DepthFunction.Less;

                case DepthStencilFunction.LessThanOrEqual:
                    return DepthFunction.Lequal;

                case DepthStencilFunction.Equal:
                    return DepthFunction.Equal;

                case DepthStencilFunction.GreaterThanOrEqual:
                    return DepthFunction.Gequal;

                case DepthStencilFunction.GreaterThan:
                    return DepthFunction.Greater;

                case DepthStencilFunction.NotEqual:
                    return DepthFunction.Notequal;

                case DepthStencilFunction.Always:
                    return DepthFunction.Always;

                default:
                    throw new ArgumentException($"Unsupported depth test function: {testFunction}.", nameof(testFunction));
            }
        }

        public static StencilFunction ToStencilFunction(DepthStencilFunction testFunction)
        {
            switch (testFunction)
            {
                case DepthStencilFunction.Never:
                    return StencilFunction.Never;

                case DepthStencilFunction.LessThan:
                    return StencilFunction.Less;

                case DepthStencilFunction.LessThanOrEqual:
                    return StencilFunction.Lequal;

                case DepthStencilFunction.Equal:
                    return StencilFunction.Equal;

                case DepthStencilFunction.GreaterThanOrEqual:
                    return StencilFunction.Gequal;

                case DepthStencilFunction.GreaterThan:
                    return StencilFunction.Greater;

                case DepthStencilFunction.NotEqual:
                    return StencilFunction.Notequal;

                case DepthStencilFunction.Always:
                    return StencilFunction.Always;

                default:
                    throw new ArgumentException($"Unsupported stencil test function: {testFunction}.", nameof(testFunction));
            }
        }

        public static StencilOp ToStencilOperation(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Zero:
                    return StencilOp.Zero;

                case StencilOperation.Invert:
                    return StencilOp.Invert;

                case StencilOperation.Replace:
                    return StencilOp.Replace;

                case StencilOperation.Keep:
                    return StencilOp.Keep;

                case StencilOperation.Increase:
                    return StencilOp.Incr;

                case StencilOperation.Decrease:
                    return StencilOp.Decr;

                case StencilOperation.IncreaseWrap:
                    return StencilOp.IncrWrap;

                case StencilOperation.DecreaseWrap:
                    return StencilOp.DecrWrap;

                default:
                    throw new ArgumentException($"Unsupported stencil operation: {operation}.", nameof(operation));
            }
        }
    }
}
