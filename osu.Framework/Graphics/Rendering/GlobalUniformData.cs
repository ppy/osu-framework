// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering.ShaderTypes;
using osuTK;

namespace osu.Framework.Graphics.Rendering
{
    // sh_GlobalUniforms.h
    [StructLayout(LayoutKind.Explicit)]
    public record struct GlobalUniformData
    {
        [FieldOffset(0)] // Align: N
        public bool GammaCorrection;

        [FieldOffset(4)] // Align: N
        public bool BackbufferDraw;

        [FieldOffset(16)] // Align: 4N
        public Matrix4 ProjMatrix;

        [FieldOffset(80)] // Align: 4N
        public PackedMatrix3 ToMaskingSpace;

        [FieldOffset(128)] // Align: N
        public bool IsMasking;

        [FieldOffset(132)] // Align: N
        public float CornerRadius;

        [FieldOffset(136)] // Align: N
        public float CornerExponent;

        [FieldOffset(144)] // Align: 4N
        public Vector4 MaskingRect;

        [FieldOffset(160)] // Align: N
        public float BorderThickness;

        [FieldOffset(176)] // Align: 4N
        public Matrix4 BorderColour;

        [FieldOffset(240)] // Align: N
        public float MaskingBlendRange;

        [FieldOffset(244)] // Align: N
        public float AlphaExponent;

        [FieldOffset(248)] // Align: 2N
        public Vector2 EdgeOffset;

        [FieldOffset(256)] // Align: N
        public bool DiscardInner;

        [FieldOffset(260)] // Align: N
        public float InnerCornerRadius;

        [FieldOffset(264)] // Align: N
        public int WrapModeS;

        [FieldOffset(268)] // Align: N
        public int WrapModeT;
    }
}
