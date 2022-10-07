// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Runtime.CompilerServices;
using ObjCRuntime;

// We publish our internal attributes to other sub-projects of the framework.
// Note, that we omit visual tests as they are meant to test the framework
// behavior "in the wild".

[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.Dynamic")]

[assembly: LinkWith(LinkerFlags = "-lstdc++ -lbz2")]
[assembly: LinkWith(Frameworks = "AudioToolbox AVFoundation CoreMedia VideoToolbox SystemConfiguration CFNetwork Accelerate")]
[assembly: LinkWith("libavcodec.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavdevice.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavfilter.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavformat.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavutil.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libbass.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libbass_fx.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libbassmix.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libswresample.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libswscale.a", SmartLink = false, ForceLoad = true)]
