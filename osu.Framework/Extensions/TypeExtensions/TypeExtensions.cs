// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Extensions.TypeExtensions
{
    public static class TypeExtensions
    {
        private static string readableName(Type t, HashSet<Type> usedTypes)
        {
            usedTypes.Add(t);

            string result = t.Name;

            // Trim away amount of type arguments
            int amountTypeArgumentsPos = result.IndexOf("`", StringComparison.Ordinal);
            if (amountTypeArgumentsPos >= 0)
                result = result.Substring(0, amountTypeArgumentsPos);

            // We were declared inside another class. Preprend the name of that class.
            if (t.DeclaringType != null && !usedTypes.Contains(t.DeclaringType))
                result = readableName(t.DeclaringType, usedTypes) + "+" + result;

            if (t.IsGenericType)
            {
                var typeArgs = t.GetGenericArguments().Except(usedTypes);
                if (typeArgs.Any())
                {
                    result += "<";
                    bool first = true;
                    foreach (var genType in typeArgs)
                    {
                        if (!first)
                            result += ", ";
                        first = false;
                        result += readableName(genType, usedTypes);
                    }
                    result += ">";
                }
            }

            return result;
        }

        public static string ReadableName(this Type t) => readableName(t, new HashSet<Type>());
    }
}
