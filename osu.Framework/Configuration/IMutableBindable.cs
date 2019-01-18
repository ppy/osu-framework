// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable{T}"/>s in order to watch for (and react to) <see cref="IBindable{T}.Disabled"/> and <see cref="IBindable{T}.Value"/> changes.
    /// Adds mutability to some properties, including <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IMutableBindable<T> : IBindable<T>, IMutableBindable
    {
        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        new T Value { get; set; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IBindable{T}.IsDefault"/>.
        /// </summary>
        new T Default { get; set; }
        
        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        new IMutableBindable<T> GetBoundCopy();

        WeakReference<Bindable<T>> WeakReference { get; }

        void AddWeakReference(WeakReference<Bindable<T>> weakReference);
    }

    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable"/>s in order to watch for (and react to) <see cref="IBindable.Disabled"/> changes.
    /// Adds mutability to some properties.
    /// </summary>
    public interface IMutableBindable : IBindable
    {
        /// <summary>
        /// Whether this object has been disabled.
        /// </summary>
        new bool Disabled { get; set; }
    }
}
