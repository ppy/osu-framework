// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.SourceGeneration.Tests
{
    public class TestableDependencyInjectionSourceGenerator : DependencyInjectionSourceGenerator
    {
        protected override bool AddUniqueNameSuffix => false;
    }
}
