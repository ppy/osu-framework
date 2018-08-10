// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Reflection;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Layout
{
    internal static class ValidationTestExtensions
    {
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
