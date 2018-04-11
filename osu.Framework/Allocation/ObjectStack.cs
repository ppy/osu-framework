// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Allocation
{
    public class ObjectStack<T> where T : new()
    {
        private readonly int maxAmountObjects;
        private readonly Stack<T> freeObjects = new Stack<T>();
        private int usedObjects;

        public ObjectStack(int maxAmountObjects = -1)
        {
            this.maxAmountObjects = maxAmountObjects;
        }

        private T findFreeObject()
        {
            T o = freeObjects.Count > 0 ? freeObjects.Pop() : new T();

            if (maxAmountObjects == -1 || usedObjects < maxAmountObjects)
                usedObjects++;

            return o;
        }

        private void returnFreeObject(T o)
        {
            if (usedObjects-- > 0)
                // We are here if the element was successfully found and removed
                freeObjects.Push(o);
        }

        /// <summary>
        /// Reserve an object from the pool. This is used to avoid excessive amounts of heap allocations.
        /// </summary>
        /// <returns>The reserved object.</returns>
        public T ReserveObject()
        {
            T o;
            lock (freeObjects)
                o = findFreeObject();

            return o;
        }

        /// <summary>
        /// Frees a previously reserved object for future reservations.
        /// </summary>
        /// <param name="o">The object to be freed. If the object has not previously been reserved then this method does nothing.</param>
        public void FreeObject(T o)
        {
            lock (freeObjects)
                returnFreeObject(o);
        }
    }
}
