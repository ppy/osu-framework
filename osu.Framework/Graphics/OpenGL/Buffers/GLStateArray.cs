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
        // these 3 get set when un/binding, not for dynamic use
        public int ElementBufferOverride; // state arrays which cache the index but not use a vao can mess up the bound index
        public int BoundElementBuffer;
        public int AmountEnabledAttributes;

        public static GLStateArray VAOBoundArray = null!;
        public static GLStateArray BoundArray = null!;

        public int VAOHandle { get; private set; }
        public bool UsesVAO => CachesVertexLayout;
        protected readonly GLRenderer Renderer;

        public StateArrayFlags CachedState { get; private set; }
        public bool CachesVertexLayout { get; private set; }
        public bool CachesIndexBuffer { get; private set; }

        public GLStateArray(GLRenderer renderer, StateArrayFlags flags = StateArrayFlags.VertexArray)
        {
            CachesVertexLayout = flags.HasFlagFast(StateArrayFlags.VertexLayout);
            CachesIndexBuffer = flags.HasFlagFast(StateArrayFlags.IndexBuffer);

            if (UsesVAO)
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

            var currentElementBuffer = Renderer.GetBoundBuffer(BufferTarget.ElementArrayBuffer);

            if (BoundArray.UsesVAO)
            {
                BoundArray.AmountEnabledAttributes = GLVertexUtils.AmountEnabledAttributes;
                BoundArray.ElementBufferOverride = currentElementBuffer;
                if (BoundArray.CachesIndexBuffer)
                    BoundArray.BoundElementBuffer = currentElementBuffer;
            }
            else if (BoundArray.CachesIndexBuffer)
            {
                VAOBoundArray.ElementBufferOverride = BoundArray.BoundElementBuffer = currentElementBuffer;
            }

            if (UsesVAO)
                bindVAO(Renderer, this, CachesIndexBuffer ? BoundElementBuffer : currentElementBuffer);
            else
                bindVAO(Renderer, Renderer.ImplicitStateArray, CachesIndexBuffer ? BoundElementBuffer : Renderer.ImplicitStateArray.BoundElementBuffer);

            BoundArray = this;
            return true;
        }

        private static void bindVAO(GLRenderer renderer, GLStateArray array, int ebo)
        {
            if (VAOBoundArray != array)
            {
                FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
                GL.BindVertexArray(array.VAOHandle);
                VAOBoundArray = array;

                GLVertexUtils.AmountEnabledAttributes = array.AmountEnabledAttributes;
                renderer.ResetBoundBuffer(BufferTarget.ElementArrayBuffer, array.ElementBufferOverride);
            }

            renderer.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        }

        public void Unbind()
        {
            if (BoundArray != this)
                return;

            Renderer.ImplicitStateArray.Bind();
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
