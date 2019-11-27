// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ObjCRuntime;

[assembly: LinkWith("libavcodec.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavdevice.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavfilter.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavformat.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libavutil.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libbass.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libbass_fx.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libswresample.a", SmartLink = false, ForceLoad = true)]
[assembly: LinkWith("libswscale.a", SmartLink = false, ForceLoad = true)]
