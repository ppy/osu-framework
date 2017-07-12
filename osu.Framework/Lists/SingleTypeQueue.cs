// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Lists
{
    /// <summary>
    /// Represents a thread-safe queue with a type for each element.
    ///
    /// When adding an element with a specified type (not negative), it is checked whether
    /// there is already an element of this type.
    /// If there is a duplicate element, it either is removed and the new element is added,
    /// it is replaced with the new element or the new element is not added to the queue.
    /// </summary>
    public class SingleTypeQueue<T>
    {

        /// <summary>
        /// This list contains all queued elements in their appropriate order.
        /// </summary>
        private readonly List<ListElement> list = new List<ListElement>();

        /// <summary>
        /// The amount of queued elements.
        /// </summary>
        public int Count
        {
            get
            {
                lock (listLock)
                    return list.Count;
            }
        }

        private readonly object listLock = new object();

        private int typeAmount;
        /// <summary>
        /// Conveniently generates a type constant.
        /// </summary>
        /// <returns>A constant for a new type.</returns>
        public int AddType()
        {
            return typeAmount++;
        }

        /// <summary>
        /// Adds an element to the end of the queue with the specified type.
        /// </summary>
        /// <param name="elementType">The type of the new element. If there already is an
        /// element of this type, the appropriate action will be performed. A negative value
        /// represents an unlimited amount of elements of this type.</param>
        /// <param name="duplicateAction">The action to perform when finding a duplicate.</param>
        /// <param name="newElement">The element to add to the queue.</param>
        public void EnqueueType(int elementType, DuplicateAction duplicateAction, T newElement)
        {
            lock (listLock)
            {
                if (elementType >= 0)
                {

                    // Check if there already is an element of the specified type and perform the duplicate action if necessary.
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].ElementType == elementType)
                        {
                            if (duplicateAction == DuplicateAction.KeepDuplicate)
                                return;

                            list.RemoveAt(i);

                            if (duplicateAction == DuplicateAction.ReplaceDuplicate)
                            {
                                list.Insert(i, new ListElement(elementType, newElement));
                                return;
                            }

                            break;
                        }
                    }
                }

                list.Add(new ListElement(elementType, newElement));
            }
        }

        /// <summary>
        /// Adds an element to the end of the queue.
        /// </summary>
        /// <param name="element">The element to add to the queue.</param>
        public void Enqueue(T element)
        {
            EnqueueType(-1, DuplicateAction.RemoveDuplicate, element);
        }

        /// <summary>
        /// Attempts to remove and return the element at the beginning of the queue.
        /// </summary>
        /// <param name="result">When this method returns, this contains the dequeued element.
        /// If there was no element queued, the value is the default value of <see cref="T"/>.</param>
        /// <returns>True if an element was removed and returned from the beginning of the queue.</returns>
        public bool TryDequeue(out T result)
        {
            result = default(T);

            lock (listLock)
            {
                if (list.Count == 0)
                    return false;

                result = list[0].Element;
                list.RemoveAt(0);
            }

            return true;
        }

        private struct ListElement
        {
            public readonly int ElementType;
            public readonly T Element;

            public ListElement(int elementType, T element)
            {
                ElementType = elementType;
                Element = element;
            }
        }
    }

    public enum DuplicateAction
    {
        /// <summary>
        /// Remove the duplicate from the list and add the new element to the end of the list.
        /// </summary>
        RemoveDuplicate,
        /// <summary>
        /// Remove the duplicate from the list and add the new element to the duplicate's old position.
        /// </summary>
        ReplaceDuplicate,
        /// <summary>
        /// Keep the duplicate in the list.
        /// </summary>
        KeepDuplicate
    }
}
