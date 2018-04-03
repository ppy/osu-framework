// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

extern alias IOS;

using System.Runtime.CompilerServices;
using IOS::Foundation;
using osu.Framework.Testing;

// We publish our internal attributes to other sub-projects of the framework.
// Note, that we omit visual tests as they are meant to test the framework
// behavior "in the wild".

[assembly: Preserve]
[assembly: InternalsVisibleTo("osu.Framework.Tests")]
[assembly: InternalsVisibleTo(DynamicClassCompiler.DYNAMIC_ASSEMBLY_NAME)]
