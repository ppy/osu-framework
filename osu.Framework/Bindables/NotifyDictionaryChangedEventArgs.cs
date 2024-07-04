// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Arguments for a dictionary's CollectionChanged event.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public class NotifyDictionaryChangedEventArgs<TKey, TValue> : EventArgs
        where TKey : notnull
    {
        /// <summary>
        /// The action that caused the event.
        /// </summary>
        public readonly NotifyDictionaryChangedAction Action;

        /// <summary>
        /// All newly-added items.
        /// </summary>
        public readonly ICollection<KeyValuePair<TKey, TValue>>? NewItems;

        /// <summary>
        /// All removed items.
        /// </summary>
        public readonly ICollection<KeyValuePair<TKey, TValue>>? OldItems;

        /// <summary>
        /// Creates a new <see cref="NotifyDictionaryChangedEventArgs{TKey,TValue}"/> that describes an add or remove event.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="item">The item affected.</param>
        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, KeyValuePair<TKey, TValue> item)
            : this(action, new[] { item })
        {
        }

        /// <summary>
        /// Creates a new <see cref="NotifyDictionaryChangedEventArgs{TKey,TValue}"/> that describes an add or remove event.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="items">The items affected.</param>
        public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, ICollection<KeyValuePair<TKey, TValue>> items)
        {
            Action = action;

            switch (action)
            {
                case NotifyDictionaryChangedAction.Add:
                    NewItems = items;
                    break;

                case NotifyDictionaryChangedAction.Remove:
                    OldItems = items;
                    break;

                default:
                    throw new ArgumentException($"Action must be {nameof(NotifyDictionaryChangedAction.Add)} or {nameof(NotifyDictionaryChangedAction.Remove)}.", nameof(action));
            }
        }

        /// <summary>
        /// Creates a new <see cref="NotifyDictionaryChangedEventArgs{TKey,TValue}"/> that describes a replacement event.
        /// </summary>
        /// <param name="newItem">The new (added) item.</param>
        /// <param name="oldItem">The old (removed) item.</param>
        public NotifyDictionaryChangedEventArgs(KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem)
        {
            Action = NotifyDictionaryChangedAction.Replace;
            NewItems = new[] { newItem };
            OldItems = new[] { oldItem };
        }
    }

    /// <summary>
    /// The delegate to use for handlers that receive the CollectionChanged event.
    /// </summary>
    public delegate void NotifyDictionaryChangedEventHandler<TKey, TValue>(object? sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
        where TKey : notnull;

    public enum NotifyDictionaryChangedAction
    {
        /// <summary>
        /// One or more items were added to the dictionary.
        /// </summary>
        Add,

        /// <summary>
        /// One or more items were removed from the dictionary.
        /// </summary>
        Remove,

        /// <summary>
        /// One or more items were replaced in the dictionary.
        /// </summary>
        Replace,
    }
}
