// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Testing
{
    internal interface ITypeReferenceBuilder
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
        Task<IReadOnlyCollection<AssemblyReference>> GetReferencedAssemblies(Type testType, string changedFile);

        /// <summary>
        /// Resets this <see cref="ITypeReferenceBuilder"/>.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Indicates that there was no link between a given test type and the changed file.
    /// </summary>
    internal class NoLinkBetweenTypesException : Exception
    {
        public NoLinkBetweenTypesException(Type testType, string changedFile)
            : base($"The changed file \"{Path.GetFileName(changedFile)}\" is not used by the test \"{testType.ReadableName()}\".")
        {
        }
    }
}
