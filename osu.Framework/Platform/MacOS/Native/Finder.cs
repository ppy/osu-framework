// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    public class Finder
    {
        internal static bool OpenFolderAndSelectItem(string filename)
        {
            IntPtr nsWorkspace = Class.Get("NSWorkspace");
            IntPtr sharedWorkspaceSelector = Selector.Get("sharedWorkspace");
            IntPtr sharedWorkspace = Cocoa.SendIntPtr(nsWorkspace, sharedWorkspaceSelector);

            IntPtr filePathNSString = Cocoa.ToNSString(filename);
            IntPtr selector = Selector.Get("selectFile:inFileViewerRootedAtPath:");

            return Cocoa.SendBool(sharedWorkspace, selector, filePathNSString);
        }
    }
}
