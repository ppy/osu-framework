// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK.Input;

namespace osu.Framework.Platform.Windows
{
    internal class WindowsGameWindow : DesktopGameWindow
    {
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                // This code is kind of... bad. But it saves us a lot of trouble in the end.
                // Basically, what this does is, it grabs various System.Windows.Forms internals
                // by using reflection and orchestrates them so that we can register this window
                // for drag&drop with windows without needing a Form object.
                // We cannot retrieve a Form object using the typical Control.FromHandle method because
                // the window we are operating on has not been originally created by WinForms and thus
                // this will only give us null back.
                // Utilizing the .NET Framework internals directly instead of using the Win32 API saves
                // us the trouble of creating and using our own custom-built COM-Wrapper for drag&drop
                // and generally makes our lives much easier (trust me, you don't want to implement drag&drop with win32).

                var windowsFormsTypes = typeof(Form).Assembly.GetTypes();
                // Internal DropTarget. This is a COM-object that we can use for RegisterDragDrop which does the bulk of win32 API interaction for us.
                var dropTarget = Activator.CreateInstance(windowsFormsTypes.Single(x => x.Name == "DropTarget"), this);
                var unsafeNativeMethods = windowsFormsTypes.Single(x => x.Name == "UnsafeNativeMethods");
                var oleInitializeMethod = unsafeNativeMethods.GetMethod("OleInitialize", BindingFlags.Static | BindingFlags.Public);
                var registerDragDropMethod = unsafeNativeMethods.GetMethod("RegisterDragDrop", BindingFlags.Static | BindingFlags.Public);

                // We need to call the OleInitialize()-Method because otherwise we will get an E_OUTOFMEMORY-Error from the RegisterDragDrop-Method we are about to call.
                oleInitializeMethod.Invoke(null, null);
                //note that this returns an error code we may want to handle in the future.
                registerDragDropMethod.Invoke(null, new[] { new HandleRef(this, WindowInfo.Handle), dropTarget });
            }
            catch
            {
                // Ignore whatever exceptions may occur in the above code.
                // If something goes wrong, we just won't have Drag&Drop functionality.
            }
            base.OnLoad(e);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F4 && e.Alt)
            {
                Exit();
                return;
            }

            base.OnKeyDown(e);
        }
    }
}
