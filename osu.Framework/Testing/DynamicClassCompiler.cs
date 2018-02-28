// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using osu.Framework.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using osu.Framework.Development;

namespace osu.Framework.Testing
{
    public class DynamicClassCompiler
    {
        public const string DYNAMIC_ASSEMBLY_NAME = "osu.DynamicTestAssembly";
    }

    public class DynamicClassCompiler<T> : DynamicClassCompiler
        where T : IDynamicallyCompile
    {
        public Action CompilationStarted;

        public Action<Type> CompilationFinished;

        public Action<Exception> CompilationFailed;

        private FileSystemWatcher fsw;

        private string lastTouchedFile;

        private T checkpointObject;

        public void Checkpoint(T obj)
        {
            checkpointObject = obj;
        }

        private List<string> requiredFiles = new List<string>();
        private List<string> requiredTypeNames = new List<string>();

        private HashSet<string> assemblies;

        public void Start()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            var basePath = di.Parent?.Parent?.Parent?.FullName;

            if (!Directory.Exists(basePath))
                return;

            fsw = new FileSystemWatcher(basePath, @"*.cs")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            };

            fsw.Changed += (sender, e) =>
            {
                if (checkpointObject == null)
                    return;

                var checkpointName = checkpointObject.GetType().Name;

                var reqTypes = checkpointObject.RequiredTypes.Select(t => t.Name).ToList();

                // add ourselves as a required type.
                reqTypes.Add(checkpointName);
                // if we are a TestCase, add the class we are testing automatically.
                reqTypes.Add(checkpointName.Replace("TestCase", ""));

                if (!reqTypes.Contains(Path.GetFileNameWithoutExtension(e.Name)))
                    return;

                if (!reqTypes.SequenceEqual(requiredTypeNames))
                {
                    requiredTypeNames = reqTypes;
                    requiredFiles = Directory
                                    .EnumerateFiles(DebugUtils.GetSolutionPath(), "*.cs", SearchOption.AllDirectories)
                                    .Where(fw => requiredTypeNames.Contains(Path.GetFileNameWithoutExtension(fw)))
                                    .ToList();
                }

                lastTouchedFile = e.FullPath;

                Task.Run((Action)recompile);
            };
        }

        private int currentVersion;

        private bool isCompiling;

        private void recompile()
        {
            if (isCompiling)
                return;
            isCompiling = true;

            if (assemblies == null)
            {
                assemblies = new HashSet<string>();
                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
                    assemblies.Add(ass.Location);
            }

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var references = assemblies.Select(a => MetadataReference.CreateFromFile(a));

            while (!checkFileReady(lastTouchedFile))
                Thread.Sleep(10);

            Logger.Log($@"Recompiling {Path.GetFileName(checkpointObject.GetType().Name)}...", LoggingTarget.Runtime, LogLevel.Important);

            CompilationStarted?.Invoke();

            string assemblyVersion = $"{++currentVersion}.0.*";

            var compilation = CSharpCompilation.Create(
                DYNAMIC_ASSEMBLY_NAME,
                requiredFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), null, file))
                             // Compile the assembly with a new version so that it replaces the existing one
                             .Concat(new[] { CSharpSyntaxTree.ParseText($"using System.Reflection; [assembly: AssemblyVersion(\"{assemblyVersion}\")]") })
                ,
                references,
                options
            );

            using (var ms = new MemoryStream())
            {
                var compilationResult = compilation.Emit(ms);

                if (compilationResult.Success)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    CompilationFinished?.Invoke(
                        Assembly.Load(ms.ToArray()).GetModules()[0]?.GetTypes().LastOrDefault(t => t.FullName == checkpointObject.GetType().FullName)
                    );
                }
                else
                {
                    foreach (var diagnostic in compilationResult.Diagnostics)
                    {
                        if (diagnostic.Severity < DiagnosticSeverity.Error)
                            continue;

                        CompilationFailed?.Invoke(new Exception(diagnostic.ToString()));
                    }
                }
            }

            isCompiling = false;
        }

        /// <summary>
        /// Check whether a file has finished being written to.
        /// </summary>
        private static bool checkFileReady(string filename)
        {
            try
            {
                using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    return inputStream.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
