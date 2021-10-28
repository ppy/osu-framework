// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using Android.App;

// We publish our internal attributes to other sub-projects of the framework.
// Note, that we omit visual tests as they are meant to test the framework
// behavior "in the wild".

[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.Dynamic")]

// https://docs.microsoft.com/en-us/answers/questions/182601/does-xamarinandroid-suppor-manifest-merging.html
[assembly: UsesPermission(Android.Manifest.Permission.ReadExternalStorage)]
