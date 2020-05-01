// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace osu.Framework.Testing
{
    public class EmptyTypeReferenceBuilder : ITypeReferenceBuilder
    {
        public Task Initialise(string solutionFile) => Task.CompletedTask;

        public async Task<IReadOnlyCollection<string>> GetReferencedFiles(Type testType, string changedFile)
            => await Task.FromResult(Array.Empty<string>());

        public async Task<IReadOnlyCollection<string>> GetReferencedAssemblies(Type testType, string changedFile)
            => await Task.FromResult(Array.Empty<string>());

        public void Reset()
        {
        }
    }
}
