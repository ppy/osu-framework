// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// An interface which can be bound to in order to watch for (and react to) value changes.
    /// </summary>
    public interface IBindable : IParseable
    {
        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed.
        /// </summary>
        event Action<bool> DisabledChanged;

        /// <summary>
        /// Whether this bindable has been disabled.
        /// </summary>
        bool Disabled { get; }

        /// <summary>
        /// Check whether this bindable has its default value.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// Binds outselves to another bindable such that we receive any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable them);

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        IBindable GetBoundCopy();
    }

    public interface IBindable<T>
    {
        /// <summary>
        /// An event which is raised when <see cref="Value"/> has changed.
        /// </summary>
        event Action<T> ValueChanged;

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IBindable.IsDefault"/>.
        /// </summary>
        T Default { get; }

        string Description { get; }

        /// <summary>
        /// Binds outselves to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable<T> them);

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        IBindable<T> GetBoundCopy();
    }
}
