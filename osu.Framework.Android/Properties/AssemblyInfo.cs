// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using Android;
using Android.App;

// We publish our internal attributes to other sub-projects of the framework.
// Note, that we omit visual tests as they are meant to test the framework
// behavior "in the wild".

[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.Dynamic")]

[assembly: Application(
    HardwareAccelerated = false,
    ResizeableActivity = true,
    Theme = "@android:style/Theme.Black.NoTitleBar"
)]
[assembly: UsesPermission(Manifest.Permission.ReadExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.WriteExternalStorage)]
[assembly: UsesPermission(Manifest.Permission.WakeLock)]
[assembly: UsesPermission(Manifest.Permission.ReadFrameBuffer)]
[assembly: UsesPermission(Manifest.Permission.Internet)]
