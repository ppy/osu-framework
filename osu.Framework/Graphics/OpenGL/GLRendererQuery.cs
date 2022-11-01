// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL
{
    public class GLRendererQuery : IRendererQuery
    {
        public QueryType Type { get; }

        private readonly QueryTarget glType;

        private int queryId;
        private bool started;

        public GLRendererQuery(QueryType type)
        {
            Trace.Assert(ThreadSafety.IsDrawThread);

            Type = type;

            switch (Type)
            {
                case QueryType.TimeElapsed:
                    glType = QueryTarget.TimeElapsed;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public ValueInvokeOnDisposal<IRendererQuery> Begin()
        {
            if (started)
                return new ValueInvokeOnDisposal<IRendererQuery>(this, q => { });

            started = true;
            queryId = GL.GenQuery();
            GL.BeginQuery(glType, queryId);

            return new ValueInvokeOnDisposal<IRendererQuery>(this, q => GL.EndQuery(((GLRendererQuery)q).glType));
        }

        public void Reset() => started = false;

        public bool TryGetResult([NotNullWhen(true)] out int? result)
        {
            result = default;

            if (!started)
                return false;

            GL.GetQueryObject(queryId, GetQueryObjectParam.QueryResultAvailable, out int resultAvailable);

            if (resultAvailable == 0)
                return false;

            GL.GetQueryObject(queryId, GetQueryObjectParam.QueryResult, out int glResult);
            result = glResult;
            return true;
        }
    }
}
