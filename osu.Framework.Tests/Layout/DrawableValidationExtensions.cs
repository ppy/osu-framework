// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Reflection;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Layout
{
    public static class DrawableValidationExtensions
    {
        public static void Validate(this Drawable d, string cachedMemberName)
        {
            var obj = Get<object>(d, cachedMemberName);

            // ReSharper disable once PossibleNullReferenceException
            obj.GetType().GetField("isValid", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, true);

            Set(d, cachedMemberName, obj);
        }

        public static void Validate<T>(this Drawable d, string cachedMemberName, T value)
        {
            var obj = Get<object>(d, cachedMemberName);

            // ReSharper disable once PossibleNullReferenceException
            obj.GetType().GetField("isValid", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, true);

            // ReSharper disable once PossibleNullReferenceException
            obj.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);

            Set(d, cachedMemberName, obj);
        }

        public static void Invalidate(this Drawable d, string cachedMemberName)
        {
            var obj = Get<object>(d, cachedMemberName);

            // ReSharper disable once PossibleNullReferenceException
            obj.GetType().GetField("isValid", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, false);

            Set(d, cachedMemberName, obj);
        }

        public static T Get<T>(this Drawable d, string fieldOrPropertyName)
        {
            Type type = d.GetType();

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

        public static void Invoke(this Drawable d, string methodName, params object[] args)
        {
            Type type = d.GetType();

            while (type != typeof(object))
            {
                // ReSharper disable once PossibleNullReferenceException
                var methodInfo = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(d, args);
                    return;
                }

                type = type.BaseType;
            }

            throw new Exception($"The method {methodName} could not be reflected.");
        }
    }
}
