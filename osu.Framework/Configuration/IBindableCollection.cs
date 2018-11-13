// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections;
using System.Collections.Generic;

namespace osu.Framework.Configuration
{
    public interface IBindableCollection : ICollection, IParseable, ICanBeDisabled, IUnbindable, IHasDescription
    {
        /// <summary>
        /// Binds self to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindableCollection them);

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        IBindableCollection GetBoundCopy();
    }

    /// <summary>
    /// An interface which can be bound to other <see cref="IBindableCollection{T}"/>s in order to watch for (and react to) <see cref="IBindableCollection{T}.Disabled"/> and item changes.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IBindableCollection<T> : ICollection<T>, IBindableCollection, IReadonlyBindableCollection<T>
    {
        /// <summary>
        /// Adds the elements of the specified collection to this collection.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to this collection.</param>
        void AddRange(IEnumerable<T> collection);

        /// <summary>
        /// Binds self to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindableCollection<T> them);

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        new IBindableCollection<T> GetBoundCopy();

        //avoiding possible ambiguity
        new int Count { get; }
    }
}
