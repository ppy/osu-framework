// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace osu.Framework.Testing
{
    public interface ITypeReferenceBuilder
    {
        /// <summary>
        /// Initialises this <see cref="ITypeReferenceBuilder"/> with a given solution file.
        /// </summary>
        /// <param name="solutionFile">The solution file.</param>
        Task Initialise(string solutionFile);

        /// <summary>
        /// Retrieves all files referenced by the type hierarchy joining a given <see cref="Type"/> to a given file.
        /// </summary>
        /// <param name="testType">The <see cref="Type"/>.</param>
        /// <param name="changedFile">The file.</param>
        /// <returns>The file names containing all types referenced between <paramref name="testType"/> and <paramref name="changedFile"/>.</returns>
        Task<IReadOnlyCollection<string>> GetReferencedFiles(Type testType, string changedFile);

        /// <summary>
        /// Retrieves all assemblies referenced by the type hierarchy joining a given <see cref="Type"/> to a given file.
        /// </summary>
        /// <param name="testType">The <see cref="Type"/>.</param>
        /// <param name="changedFile">The file.</param>
        /// <returns>The file names containing all assemblies referenced between <paramref name="testType"/> and <paramref name="changedFile"/>.</returns>
        Task<IReadOnlyCollection<string>> GetReferencedAssemblies(Type testType, string changedFile);

        /// <summary>
        /// Resets this <see cref="ITypeReferenceBuilder"/>.
        /// </summary>
        void Reset();
    }
}
