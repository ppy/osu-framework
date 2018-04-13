// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Instances of this class capture an action for later cleanup. When a method returns an instance of this class, the appropriate usage is:
    /// <code>using (SomeMethod())
    /// {
    ///     // ...
    /// }</code>
    /// The using block will automatically dispose the returned instance, doing the necessary cleanup work.
    /// </summary>
    public class InvokeOnDisposal : IDisposable
    {
        private readonly Action action;

        /// <summary>
        /// Constructs a new instance, capturing the given action to be run during disposal.
        /// </summary>
        /// <param name="action">The action to invoke during disposal.</param>
        public InvokeOnDisposal(Action action) => this.action = action ?? throw new ArgumentNullException(nameof(action));

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
