// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        public InvokeOnDisposal(Action action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        #region IDisposable Support

        /// <summary>
        /// Disposes this instance, calling the initially captured action.
        /// </summary>
        public virtual void Dispose()
        {
            //no isDisposed check here so we can reuse these instances multiple times to save on allocations.
            action();
        }

        #endregion
    }

    /// <summary>
    /// Instances of this class capture an action for later cleanup. When a method returns an instance of this class, the appropriate usage is:
    /// <code>using (SomeMethod())
    /// {
    ///     // ...
    /// }</code>
    /// The using block will automatically dispose the returned instance, doing the necessary cleanup work.
    /// </summary>
    public class InvokeOnDisposal<T> : IDisposable
    {
        private readonly T sender;
        private readonly Action<T> action;

        /// <summary>
        /// Constructs a new instance, capturing the given action to be run during disposal.
        /// </summary>
        /// <param name="sender">The sender which should appear in the <paramref name="action"/> callback.</param>
        /// <param name="action">The action to invoke during disposal.</param>
        public InvokeOnDisposal(T sender, Action<T> action)
        {
            this.sender = sender;
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        #region IDisposable Support

        /// <summary>
        /// Disposes this instance, calling the initially captured action.
        /// </summary>
        public virtual void Dispose()
        {
            //no isDisposed check here so we can reuse these instances multiple times to save on allocations.
            action(sender);
        }

        #endregion
    }
}
