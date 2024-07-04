// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    public interface IBindableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, ICanBeDisabled, IHasDefaultValue, IUnbindable, IHasDescription
        where TKey : notnull
    {
        /// <summary>
        /// An event which is raised when this <see cref="IBindableDictionary{TKey, TValue}"/> changes.
        /// </summary>
        event NotifyDictionaryChangedEventHandler<TKey, TValue>? CollectionChanged;

        /// <summary>
        /// Binds self to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindableDictionary<TKey, TValue> them);

        /// <summary>
        /// Bind an action to <see cref="CollectionChanged"/> with the option of running the bound action once immediately
        /// with an <see cref="NotifyDictionaryChangedAction.Add"/> event for the entire contents of this <see cref="BindableDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="onChange">The action to perform when this <see cref="BindableDictionary{TKey, TValue}"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        void BindCollectionChanged(NotifyDictionaryChangedEventHandler<TKey, TValue> onChange, bool runOnceImmediately = false);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        sealed IBindableDictionary<TKey, TValue> BindTarget
        {
            set => BindTo(value);
        }

        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindableDictionary<TKey, TValue> GetBoundCopy();
    }
}
