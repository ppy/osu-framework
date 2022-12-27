// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLRawVertexArray : IRawVertexArray
    {
        // these 4 get set when un/binding, not for dynamic use
        public static int ImplicitAmountEnabledAttributes;
        public static int ImplicitBoundElementArray;

        protected int BoundElementArray { get; private set; }
        public int AmountEnabledAttributes;

        public static GLRawVertexArray? BoundArray;

        protected int Handle { get; private set; }
        protected readonly GLRenderer Renderer;

        public GLRawVertexArray(GLRenderer renderer)
        {
            Handle = GL.GenVertexArray();
            Renderer = renderer;
        }

        public bool Bind()
        {
            if (BoundArray == this)
                return false;

            FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
            GL.BindVertexArray(Handle);

            if (BoundArray != null)
            {
                BoundArray.AmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                BoundArray.BoundElementArray = Renderer.GetBoundBuffer(BufferTarget.ElementArrayBuffer);
            }
            else
            {
                ImplicitAmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                ImplicitBoundElementArray = Renderer.GetBoundBuffer(BufferTarget.ElementArrayBuffer);
            }

            GLVertexUtils.AmountEnabledAttributes = AmountEnabledAttributes;
            Renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, BoundElementArray);
            BoundArray = this;
            return true;
        }

        public void Unbind()
        {
            if (BoundArray == null)
                return;

            GL.BindVertexArray(0);
            BoundArray.AmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
            BoundArray.BoundElementArray = Renderer.GetBoundBuffer(BufferTarget.ElementArrayBuffer);

            GLVertexUtils.AmountEnabledAttributes = ImplicitAmountEnabledAttributes;
            Renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, ImplicitBoundElementArray);

            BoundArray = null;
        }

        protected bool IsDisposed => Handle == -1;
        public void Dispose()
        {
            if (IsDisposed)
                return;

            GL.DeleteVertexArray(Handle);
            Handle = -1;
            GC.SuppressFinalize(this);
        }

        ~GLRawVertexArray()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(), this);
        }
    }
}
