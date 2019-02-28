// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Contains data that is common between all <see cref="DrawNode"/>s of the same <see cref="Drawable"/> object.
    /// </summary>
    public class DrawNodeSharedData : IDisposable
    {
        ~DrawNodeSharedData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }
    }
}
