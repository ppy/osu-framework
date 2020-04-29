// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace osu.Framework.Testing
{
    public interface ITypeReferenceBuilder
    {
        Task Initialise(string solutionFile);

        Task<IReadOnlyCollection<string>> GetReferencedFiles(Type testType, string changedFile);

        void Reset();
    }
}
