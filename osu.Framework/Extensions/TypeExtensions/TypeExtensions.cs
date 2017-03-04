// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Extensions.TypeExtensions
{
    public static class TypeExtensions
    {
        public static string ReadableName(this Type t)
        {
            string result = t.Name;

            // Trim away amount of type arguments
            int amountTypeArgumentsPos = result.IndexOf("`");
            if (amountTypeArgumentsPos >= 0)
                result = result.Substring(0, amountTypeArgumentsPos);

            // We were declared inside another class. Preprend the name of that class.
            if (t.DeclaringType != null)
                result = t.DeclaringType.ReadableName() + "+" + result;

            if (t.IsGenericType)
            {
                result += "<";
                var typeArgs = t.GetGenericArguments();
                for (int i = 0; i < typeArgs.Length; ++i)
                {
                    if (i > 0)
                        result += ", ";
                    result += typeArgs[i].ReadableName();
                }
                result += ">";
            }

            return result;
        }
    }
}
