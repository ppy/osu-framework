// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using osu.Framework.Logging;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Explorer
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);

        internal static void OpenFolderAndSelectItem(string filename)
        {
            Task.Run(() =>
            {
                IntPtr nativeFile = IntPtr.Zero;
                IntPtr nativeFolder = IntPtr.Zero;

                try
                {
                    filename = filename.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    string folderPath = Path.GetDirectoryName(filename);

                    SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out _);

                    if (nativeFolder == IntPtr.Zero)
                    {
                        Logger.Log($"Cannot find native folder for '{folderPath}'", level: LogLevel.Error);
                        return;
                    }

                    SHParseDisplayName(filename, IntPtr.Zero, out nativeFile, 0, out _);

                    IntPtr[] fileArray;

                    if (nativeFile != IntPtr.Zero)
                    {
                        fileArray = new[] { nativeFile };
                    }
                    else
                    {
                        Logger.Log($"Cannot find native file for '{filename}'", level: LogLevel.Debug);

                        // Open the folder without the file selected if we can't find the file
                        fileArray = new[] { nativeFolder };
                    }

                    SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);
                }
                finally
                {
                    if (nativeFolder != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(nativeFolder);

                    if (nativeFile != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(nativeFile);
                }
            });
        }
    }
}
