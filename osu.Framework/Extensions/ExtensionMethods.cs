﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// this is an abusive thing to do, but it increases the visibility of Extension Methods to virtually every file.

namespace osu.Framework.Extensions
{
    /// <summary>
    /// This class holds extension methods for various purposes and should not be used explicitly, ever.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="list">The list to take values</param>
        /// <param name="match">The predicate that needs to be matched.</param>
        /// <param name="startIndex">The index to start conditional search.</param>
        /// <returns>The matched item, or the default value for the type if no item was matched.</returns>
        public static T Find<T>(this List<T> list, Predicate<T> match, int startIndex)
        {
            if (!list.IsValidIndex(startIndex)) return default(T);

            int val = list.FindIndex(startIndex, list.Count - startIndex - 1, match);

            return list.ElementAtOrDefault(val);
        }

        /// <summary>
        /// Adds the given item to the list according to standard sorting rules. Do not use on unsorted lists.
        /// </summary>
        /// <param name="list">The list to take values</param>
        /// <param name="item">The item that should be added.</param>
        /// <returns>The index in the list where the item was inserted.</returns>
        public static int AddInPlace<T>(this List<T> list, T item)
        {
            int index = list.BinarySearch(item);
            if (index < 0) index = ~index; // BinarySearch hacks multiple return values with 2's complement.
            list.Insert(index, item);
            return index;
        }

        /// <summary>
        /// Adds the given item to the list according to the comparers sorting rules. Do not use on unsorted lists.
        /// </summary>
        /// <param name="list">The list to take values</param>
        /// <param name="item">The item that should be added.</param>
        /// <param name="comparer">The comparer that should be used for sorting.</param>
        /// <returns>The index in the list where the item was inserted.</returns>
        public static int AddInPlace<T>(this List<T> list, T item, IComparer<T> comparer)
        {
            int index = list.BinarySearch(item, comparer);
            if (index < 0) index = ~index; // BinarySearch hacks multiple return values with 2's complement.
            list.Insert(index, item);
            return index;
        }

        /// <summary>
        /// Try to get a value from the <paramref name="dictionary"/>. Returns a default(TValue) if the key does not exist.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="lookup">The lookup key.</param>
        /// <returns></returns>
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey lookup)
        {
            TValue outVal;
            return dictionary.TryGetValue(lookup, out outVal) ? outVal : default(TValue);
        }

        public static bool IsValidIndex<T>(this List<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }

        /// <summary>
        /// Compares every item in list to given list.
        /// </summary>
        public static bool CompareTo<T>(this List<T> list, List<T> list2)
        {
            if (list.Count != list2.Count) return false;

            return !list.Where((t, i) => !EqualityComparer<T>.Default.Equals(t, list2[i])).Any();
        }

        public static string ToResolutionString(this Size size)
        {
            return size.Width.ToString() + 'x' + size.Height;
        }

        public static void WriteLineExplicit(this Stream s, string str = @"")
        {
            byte[] data = Encoding.UTF8.GetBytes($"{str}\r\n");
            s.Write(data, 0, data.Length);
        }

        public static string UnsecureRepresentation(this SecureString s)
        {
            IntPtr bstr = Marshal.SecureStringToBSTR(s);

            try
            {
                return Marshal.PtrToStringBSTR(bstr);
            }
            finally
            {
                Marshal.FreeBSTR(bstr);
            }
        }

        public static long ToUnixTimestamp(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (date - epoch).Ticks / TimeSpan.TicksPerSecond;
        }

        public static long TotalMilliseconds(this DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (date - epoch).Ticks / TimeSpan.TicksPerMillisecond;
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static string GetDescription(this Enum value)
            => value.GetType().GetField(value.ToString())
                    .GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();

        public static void ThrowIfFaulted(this Task task)
        {
            if (task.IsFaulted)
            {
                Exception e = task.Exception;

                Debug.Assert(e != null);

                while (e.InnerException != null)
                    e = e.InnerException;

                ExceptionDispatchInfo.Capture(e).Throw();
            }
        }

        /// <summary>
        /// Gets a SHA-2 (256bit) hash for the given stream, seeking the stream before and after.
        /// </summary>
        /// <param name="stream">The stream to create a hash from.</param>
        /// <returns>A lower-case hex string representation of the has (64 characters).</returns>
        public static string ComputeSHA2Hash(this Stream stream)
        {
            string hash;

            stream.Seek(0, SeekOrigin.Begin);

            using (var alg = SHA256.Create())
            {
                alg.ComputeHash(stream);
                hash = BitConverter.ToString(alg.Hash).Replace("-", "").ToLowerInvariant();
            }

            stream.Seek(0, SeekOrigin.Begin);

            return hash;
        }

        public static string ComputeMD5Hash(this Stream stream)
        {
            string hash;

            stream.Seek(0, SeekOrigin.Begin);
            using (var md5 = MD5.Create())
                hash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            stream.Seek(0, SeekOrigin.Begin);

            return hash;
        }
    }
}
