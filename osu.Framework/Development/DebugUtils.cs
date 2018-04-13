// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Linq;

namespace osu.Framework.Development
{
    public static class DebugUtils
    {
        public static bool IsDebug
        {
            get
            {
                // ReSharper disable once RedundantAssignment
                bool isDebug = false;
                // Debug.Assert conditions are only evaluated in debug mode
                System.Diagnostics.Debug.Assert(isDebug = true);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return isDebug;
            }
        }

        /// <summary>
        /// Find the containing solution path.
        /// </summary>
        /// <returns>An absolute path containing the first parent .sln file. Null if no such file exists in any parent.</returns>
        public static string GetSolutionPath()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (!Directory.GetFiles(di.FullName, "*.sln").Any() && di.Parent != null)
                di = di.Parent;

            return di?.FullName;
        }
    }
}
