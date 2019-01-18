// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
