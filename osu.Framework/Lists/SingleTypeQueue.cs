// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;


namespace osu.Framework.Lists
{
    /// <summary>
    /// Represents a thread-safe queue with a type for each element.
    ///
    /// When adding an element with a specified type (not negative), all elements with this
    /// exact same type will be removed.
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

        /// When adding an element with a specified type (not negative), all elements with this

        /// <summary>
        /// Adds an element to the end of the queue with the specified type.
        /// </summary>
        /// <param name="elementType">The type of the new element. If there already
        /// is an element of this type, it will be removed in the process. A negative value
        /// represents an unlimited amount of elements of this type.</param>
        /// <param name="addNewElement">If true and there is a duplicate, the new element will be added
        /// to the list; if false, the old element is kept.</param>
        /// <param name="newElement">The element to add to the queue.</param>
        public void EnqueueType(int elementType, bool addNewElement, T newElement)
        {
            lock (listLock)
            {
                if(elementType >= 0) {

                    // Check if there already is an element of the specified type and remove it.
                    foreach (ListElement listElement in list)
                    {
                        if (listElement.ElementType == elementType)
                        {
                            if (!addNewElement)
                                return;

                            list.Remove(listElement);
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
            EnqueueType(-1, false, element);
        }

        /// <summary>
        /// Attempts to remove and return the element at the beginning of the queue.
        /// </summary>
        /// <param name="result">When this method returns, this contains the dequeued element. If there was no element queued, the value is the default value of <see cref="T"/>.</param>
        /// <returns>True if an element was removed and returned from the beginning of the queue.</returns>
        public bool TryDequeue(out T result)
        {
            lock (listLock)
            {
                result = default(T);

                if (list.Count == 0)
                    return false;

                result = list[0].Element;
                list.RemoveAt(0);

                return true;
            }
        }

        private class ListElement
        {
            public int ElementType;
            public T Element;

            public ListElement(int elementType, T element){
                ElementType = elementType;
                Element = element;
            }
        }
    }
}
