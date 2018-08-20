// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Extensions
{
    internal static class DrawableTestExtensions
    {
        public static IEnumerable<Drawable> GetDecendants(this Drawable root)
        {
            if (root is CompositeDrawable composite)
            {
                return composite.InternalChildren.SelectMany(child => child.GetDecendants()).Prepend(root);
            }
            else
            {
                return new[] { root };
            }
        }

        public static T Get<T>(this Drawable d, string fieldOrPropertyName, Type searchType = null)
        {
            Type type = searchType ?? d.GetType();

            while (type != typeof(object))
            {
                // ReSharper disable once PossibleNullReferenceException
                var fieldInfo = type.GetField(fieldOrPropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                    return (T)fieldInfo.GetValue(d);

                var propertyInfo = type.GetProperty(fieldOrPropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null)
                    return (T)propertyInfo.GetValue(d);

                type = type.BaseType;
            }

            throw new Exception($"The property or field {fieldOrPropertyName} could not be reflected.");
        }

        public static void Set<T>(this Drawable d, string fieldOrPropertyName, T value)
        {
            Type type = d.GetType();

            while (type != typeof(object))
            {
                // ReSharper disable once PossibleNullReferenceException
                var fieldInfo = type.GetField(fieldOrPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(d, value);
                    return;
                }

                var propertyInfo = type.GetProperty(fieldOrPropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(d, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new Exception($"The property or field {fieldOrPropertyName} could not be reflected.");
        }
    }
}
