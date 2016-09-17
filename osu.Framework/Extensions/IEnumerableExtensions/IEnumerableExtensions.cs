using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Extensions.IEnumerableExtensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Performs an action on all the items in an IEnumearble collection.
        /// </summary>
        /// <typeparam name="T">The type of the items stored in the collection.</typeparam>
        /// <param name="collection">The collection to iterate on.</param>
        /// <param name="action">The action to be performed.</param>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }
    }
}
