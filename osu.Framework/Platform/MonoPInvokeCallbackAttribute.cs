// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.CompilerServices;

namespace osu.Framework.Platform
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public Type DelegateType
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get;
            [MethodImpl(MethodImplOptions.NoInlining)]
            set;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public MonoPInvokeCallbackAttribute(Type t)
        {
            DelegateType = t;
        }
    }
}
