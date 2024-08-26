// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;

namespace osu.Framework.Platform.MacOS.Native
{
    internal static class Finder
    {
        private static readonly NSWorkspace shared_workspace = NSWorkspace.SharedWorkspace();

        internal static void OpenFolderAndSelectItem(string filename)
        {
            Task.Run(() =>
            {
                shared_workspace.SelectFile(filename);
            });
        }
    }
}
