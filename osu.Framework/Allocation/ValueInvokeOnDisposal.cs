// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Instances of this struct capture an action for later cleanup. The appropriate usage is:
    /// <code>using (SomeMethod())
    /// {
    ///     // ...
    /// }</code>
    /// The using block will automatically dispose the returned instance, doing the necessary cleanup work.
    ///
    /// This is a struct version of <see cref="InvokeOnDisposal"/> to be used when allocations are to be minimised.
    /// </summary>
    public struct ValueInvokeOnDisposal : IDisposable
    {
        private readonly Action action;

        /// <summary>
        /// Constructs a new instance, capturing the given action to be run during disposal.
        /// </summary>
        /// <param name="action">The action to invoke during disposal.</param>
        public ValueInvokeOnDisposal(Action action) => this.action = action ?? throw new ArgumentNullException(nameof(action));

        #region IDisposable Support

        /// <summary>
        /// Disposes this instance, calling the initially captured action.
        /// </summary>
        public void Dispose()
        {
            //no isDisposed check here so we can reuse these instances multiple times to save on allocations.
            action();
        }

        #endregion
    }
}
