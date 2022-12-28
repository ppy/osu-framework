// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLStateArray : IRenderStateArray
    {
        // these 4 get set when un/binding, not for dynamic use
        public static int ImplicitAmountEnabledAttributes;
        public static int ImplicitBoundElementArray;

        public int BoundElementArray;
        public int AmountEnabledAttributes;

        public static GLStateArray? BoundArray;

        [MemberNotNullWhen(false, nameof(BoundArray))]
        public static bool IsImplicitArrayBound => BoundArray == null;

        protected int VAOHandle { get; private set; }
        protected readonly GLRenderer Renderer;
        public StateArrayFlags CachedState { get; private set; }
        public bool CachesVertexLayout { get; private set; }
        public bool CachesIndexBuffer { get; private set; }

        public GLStateArray(GLRenderer renderer, StateArrayFlags flags = StateArrayFlags.VertexArray)
        {
            CachesVertexLayout = flags.HasFlagFast(StateArrayFlags.VertexLayout);
            CachesIndexBuffer = flags.HasFlagFast(StateArrayFlags.IndexBuffer);

            if (CachesVertexLayout)
                VAOHandle = GL.GenVertexArray();
            else
                VAOHandle = -1;

            Renderer = renderer;
            CachedState = flags;
        }

        public bool Bind()
        {
            if (BoundArray == this)
                return false;

            Bind(Renderer, VAOHandle, CachesVertexLayout, CachesIndexBuffer, ref AmountEnabledAttributes, ref BoundElementArray);
            BoundArray = this;
            return true;
        }

        public void Unbind()
        {
            if (BoundArray != this)
                return;

            Bind(Renderer, 0, true, true, ref ImplicitAmountEnabledAttributes, ref ImplicitBoundElementArray);
            BoundArray = null;
        }

        static void Bind(GLRenderer renderer, int vao, bool cacheVertexLayout, bool cacheIndexBuffer,
            ref int amountEnabledAttributes, ref int boundElementArray)
        {
            var currentElementBuffer = renderer.GetBoundBuffer(BufferTarget.ElementArrayBuffer);
            if (IsImplicitArrayBound)
            {
                ImplicitAmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                ImplicitBoundElementArray = currentElementBuffer;
            }
            else
            {
                if (BoundArray.CachesVertexLayout)
                    BoundArray.AmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                if (BoundArray.CachesIndexBuffer)
                    BoundArray.BoundElementArray = currentElementBuffer;
            }

            if (cacheVertexLayout)
            {
                FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
                GL.BindVertexArray(vao);

                GLVertexUtils.AmountEnabledAttributes = amountEnabledAttributes;
                renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, boundElementArray);

                if (!cacheIndexBuffer)
                {
                    renderer.BindBuffer(BufferTarget.ElementArrayBuffer, currentElementBuffer);
                }
            }
            else
            {
                if (!IsImplicitArrayBound)
                {
                    FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
                    GL.BindVertexArray(0);

                    GLVertexUtils.AmountEnabledAttributes = ImplicitAmountEnabledAttributes;
                    renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, ImplicitBoundElementArray);
                }

                if (cacheIndexBuffer)
                {
                    renderer.BindBuffer(BufferTarget.ElementArrayBuffer, boundElementArray);
                }
            }
        }

        public void Dispose()
        {
            if (VAOHandle != -1)
            {
                GL.DeleteVertexArray(VAOHandle);
                VAOHandle = -1;
            }

            GC.SuppressFinalize(this);
        }

        ~GLStateArray()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(), this);
        }
    }
}
