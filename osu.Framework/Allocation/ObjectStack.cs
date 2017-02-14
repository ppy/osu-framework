// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Allocation
{
    public class ObjectStack<T> where T : new()
    {
        private int maxAmountObjects;
        private Stack<T> freeObjects = new Stack<T>();
        private HashSet<T> usedObjects = new HashSet<T>();

        public ObjectStack(int maxAmountObjects)
        {
            this.maxAmountObjects = maxAmountObjects;
        }

        private T findFreeObject()
        {
            T o = freeObjects.Count > 0 ? freeObjects.Pop() : new T();

            if (usedObjects.Count < maxAmountObjects)
                usedObjects.Add(o);

            return o;
        }

        private void returnFreeObject(T o)
        {
            if (usedObjects.Remove(o))
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
