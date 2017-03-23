// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Allocation
{
    public class InvokeOnDisposal : IDisposable
    {
        private readonly Action action;

        public InvokeOnDisposal(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            this.action = action;
        }

        #region IDisposable Support

        public void Dispose()
        {
            //no isDisposed check here so we can reuse these instances multiple times to save on allocations.
            action();
        }

        #endregion
    }
}
