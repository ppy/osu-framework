// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Tests.Verifiers
{
    public partial class CSharpMultiPhaseSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        private static readonly Regex multi_phase = new(@"^(?<filename>.*)\.(?<num>\d+)\.cs$", RegexOptions.Compiled);

        public static void Verify(
            (string filename, string content)[] commonSources,
            (string filename, string content)[] sources,
            (string filename, string content)[] commonGenerated,
            (string filename, string content)[] generated)
        {
            List<List<(string filename, string content)>>
                multiPhaseSources = new(),
                multiPhaseGenerated = new();

            extractPhases(sources, multiPhaseSources);
            extractPhases(generated, multiPhaseGenerated, isGenerated: true);

            verifySamePhaseCount(multiPhaseSources, multiPhaseGenerated);

            new Test(commonSources, commonGenerated, multiPhaseSources, multiPhaseGenerated).Verify();

            // The filenames for the sources and generated files are expected to be in the format of:
            //     <filename>.<phase>.cs
            // where <phase> is a zero-based counter and represents a time frame of code states.
            //
            // If one of the filenames does not match above format, it is assumed that all the files
            // are in the same phase, 0.
            static void extractPhases(
                (string filename, string content)[] sources,
                List<List<(string filename, string content)>> multiPhaseList,
                bool isGenerated = false)
            {
                foreach (var s in sources)
                {
                    var match = multi_phase.Match(s.filename);
                    // Is multi-phase
                    if (match.Success)
                    {
                        int phase = int.Parse(match.Groups["num"].Value);

                        while (multiPhaseList.Count <= phase)
                            multiPhaseList.Add(new());

                        string filename = match.Groups["filename"].Value + ".cs";
                        multiPhaseList[phase].Add((filename, s.content));
                    }
                    // Not multi-phase, at this point all the files should be single-phase
                    else
                    {
                        if (multiPhaseList.Count == 0)
                            multiPhaseList.Add(new());
                        if (multiPhaseList.Count > 1)
                            throw new InvalidOperationException($"Found {(isGenerated ? "generated" : "source")} file {s.filename} outside of a multi-phase directory.");

                        multiPhaseList[0].Add(s);
                    }
                }
            }

            static void verifySamePhaseCount(List<List<(string filename, string content)>> sources, List<List<(string filename, string content)>> generated)
            {
                // support cases where no generated files are expected.
                if (generated.Count == 0)
                {
                    while (generated.Count < sources.Count)
                        generated.Add(new());
                }

                if (sources.Count != generated.Count)
                    throw new InvalidOperationException($"The number of phases for source and generated do not match. Sources: {sources.Count}, Generated: {generated.Count}");
            }
        }
    }
}
