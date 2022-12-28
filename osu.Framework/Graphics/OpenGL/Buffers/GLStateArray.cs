// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLStateArray : IRenderStateArray
    {
        // these 6 get set when un/binding, not for dynamic use
        public static int ImplicitElementBufferOverride; // state arrays which cache the index but not use a vao can mess up the bound index
        public static int ImplicitAmountEnabledAttributes;
        public static int ImplicitBoundElementBuffer;

        public int ElementBufferOverride;
        public int BoundElementBuffer;
        public int AmountEnabledAttributes;

        public static int ImplicitArray;
        public static GLStateArray? BoundArray;

        public static bool IsImplicitArrayBound => BoundArray == null || !BoundArray.CachesVertexLayout;

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

            Bind(Renderer, VAOHandle, CachesVertexLayout, CachesIndexBuffer, ref AmountEnabledAttributes, ref BoundElementBuffer, ref ElementBufferOverride);
            BoundArray = this;
            return true;
        }

        public void Unbind()
        {
            if (BoundArray != this)
                return;

            Bind(Renderer, ImplicitArray, true, true, ref ImplicitAmountEnabledAttributes, ref ImplicitBoundElementBuffer, ref ImplicitElementBufferOverride);
            BoundArray = null;
        }

        static void Bind(GLRenderer renderer, int vao, bool cacheVertexLayout, bool cacheIndexBuffer,
            ref int amountEnabledAttributes, ref int boundElementBuffer, ref int elementBufferOverride) // these are by-ref because the might get modified inside this method
        {
            var currentElementBuffer = renderer.GetBoundBuffer(BufferTarget.ElementArrayBuffer);
            if (IsImplicitArrayBound)
            {
                ImplicitAmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                if (BoundArray?.CachesIndexBuffer == true)
                    ImplicitElementBufferOverride = currentElementBuffer;
                else
                    ImplicitElementBufferOverride = ImplicitBoundElementBuffer = currentElementBuffer;
            }

            if (BoundArray != null)
            {
                if (BoundArray.CachesVertexLayout)
                    BoundArray.AmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                if (BoundArray.CachesIndexBuffer)
                    BoundArray.ElementBufferOverride = BoundArray.BoundElementBuffer = currentElementBuffer;
                else if (BoundArray.CachesVertexLayout)
                    BoundArray.ElementBufferOverride = currentElementBuffer;
            }

            if (cacheVertexLayout)
            {
                FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
                GL.BindVertexArray(vao);

                GLVertexUtils.AmountEnabledAttributes = amountEnabledAttributes;
                renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, elementBufferOverride);

                if (cacheIndexBuffer)
                    renderer.BindBuffer(BufferTarget.ElementArrayBuffer, boundElementBuffer);
                else
                    renderer.BindBuffer(BufferTarget.ElementArrayBuffer, currentElementBuffer);
            }
            else
            {
                if (!IsImplicitArrayBound)
                {
                    FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
                    GL.BindVertexArray(ImplicitArray);

                    GLVertexUtils.AmountEnabledAttributes = ImplicitAmountEnabledAttributes;
                    renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, ImplicitElementBufferOverride);
                    renderer.BindBuffer(BufferTarget.ElementArrayBuffer, ImplicitBoundElementBuffer);
                }

                if (cacheIndexBuffer)
                {
                    renderer.BindBuffer(BufferTarget.ElementArrayBuffer, boundElementBuffer);
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
