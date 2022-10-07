// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Runtime.CompilerServices;
#if NET6_0_OR_GREATER
using System.Reflection.Metadata;
using osu.Framework.Testing;

#endif

// We publish our internal attributes to other sub-projects of the framework.
// Note, that we omit visual tests as they are meant to test the framework
// behavior "in the wild".

[assembly: InternalsVisibleTo("osu.Framework.Android")]
[assembly: InternalsVisibleTo("osu.Framework.Benchmarks")]
[assembly: InternalsVisibleTo("osu.Framework.iOS")]
[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.Dynamic")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.iOS")]
[assembly: InternalsVisibleTo("osu.Framework.Tests.Android")]

#if NET6_0_OR_GREATER
[assembly: MetadataUpdateHandler(typeof(HotReloadCallbackReceiver))]
#endif
