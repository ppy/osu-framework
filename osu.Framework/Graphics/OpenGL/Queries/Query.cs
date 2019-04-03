// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Queries
{
    internal class Query : IDisposable
    {
        public int Result => result;
        private int result;

        private readonly QueryTarget target;

        private int queryObject = -1;

        public Query(QueryTarget target)
        {
            this.target = target;
        }

        public InvokeOnDisposal Begin()
        {
            bool queryActive = false;

            if (queryObject == -1)
                queryObject = GL.GenQuery();
            else
            {
                GL.GetQueryObject(queryObject, GetQueryObjectParam.QueryResultAvailable, out int resultAvailable);
                queryActive = resultAvailable == 0;
            }

            bool queryStarted = false;

            if (!queryActive)
            {
                GL.GetQueryObject(queryObject, GetQueryObjectParam.QueryResult, out result);
                GL.BeginQuery(target, queryObject);

                queryStarted = true;
            }

            return queryStarted ? new InvokeOnDisposal(() => GL.EndQuery(target)) : null;
        }

        ~Query()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing) => GLWrapper.ScheduleDisposal(() =>
        {
            if (queryObject != -1)
                GL.DeleteQuery(queryObject);
        });
    }
}
