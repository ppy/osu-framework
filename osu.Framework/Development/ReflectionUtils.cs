// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using System.Reflection;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Development
{
    internal static class ReflectionUtils
    {
        // taken from https://github.com/nunit/nunit/blob/73dbcce0896a6897a2add4281cc48734eca546a2/src/NUnitFramework/framework/Internal/Reflect.cs
        // as it was removed/refactored in nunit 3.13.1

        // ***********************************************************************
        // Copyright (c) 2007-2018 Charlie Poole, Rob Prouse
        //
        // Permission is hereby granted, free of charge, to any person obtaining
        // a copy of this software and associated documentation files (the
        // "Software"), to deal in the Software without restriction, including
        // without limitation the rights to use, copy, modify, merge, publish,
        // distribute, sublicense, and/or sell copies of the Software, and to
        // permit persons to whom the Software is furnished to do so, subject to
        // the following conditions:
        //
        // The above copyright notice and this permission notice shall be
        // included in all copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
        // EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
        // MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
        // NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
        // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
        // OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
        // WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
        // ***********************************************************************

        private const BindingFlags all_members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>
        /// Returns all methods declared by the specified fixture type that have the specified attribute, optionally
        /// including base classes. Methods from a base class are always returned before methods from a class that
        /// inherits from it.
        /// </summary>
        /// <param name="fixtureType">The type to examine.</param>
        /// <param name="attributeType">Only methods to which this attribute is applied will be returned.</param>
        /// <param name="inherit">Specifies whether to search the fixture type inheritance chain.</param>
        internal static MethodInfo[] GetMethodsWithAttribute(Type fixtureType, Type attributeType, bool inherit)
        {
            if (!inherit)
            {
                return fixtureType
                       .GetMethods(all_members | BindingFlags.DeclaredOnly)
                       .Where(method => method.IsDefined(attributeType, inherit: false))
                       .ToArray();
            }

            var methodsByDeclaringType = fixtureType
                                         .GetMethods(all_members | BindingFlags.FlattenHierarchy) // FlattenHierarchy is complex to replicate by looping over base types with DeclaredOnly.
                                         .Where(method => method.IsDefined(attributeType, inherit: true))
                                         .ToLookup(method => method.DeclaringType);

            return fixtureType
                   .EnumerateBaseTypes()
                   .Reverse()
                   .SelectMany(declaringType => methodsByDeclaringType[declaringType])
                   .ToArray();
        }
    }
}
