// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using osu.Framework.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Development;

namespace osu.Framework.Testing
{
    public class DynamicClassCompiler<T>
        where T : IDynamicallyCompile
    {
        public Action CompilationStarted;

        public Action<Type> CompilationFinished;

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
        private readonly CSharpCodeProvider codeProvider;

        public DynamicClassCompiler()
        {
            codeProvider = new CSharpCodeProvider();

            var newPath = Path.Combine(DebugUtils.GetSolutionPath(), "packages", "Microsoft.Net.Compilers.2.3.2", "tools", "csc.exe");

            //Black magic to fix incorrect packages path (http://stackoverflow.com/a/40311406)
            var settings = codeProvider.GetType().GetField("_compilerSettings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(codeProvider);
            var path = settings?.GetType().GetField("_compilerFullPath", BindingFlags.Instance | BindingFlags.NonPublic);
            path?.SetValue(settings, newPath);
        }

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

        private bool isCompiling;

        private void recompile()
        {
            if (isCompiling)
                return;
            isCompiling = true;

            CompilerParameters cp = new CompilerParameters
            {
                GenerateInMemory = true,
                TreatWarningsAsErrors = false,
                GenerateExecutable = false,
            };

            if (assemblies == null)
            {
                assemblies = new HashSet<string>();
                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
                    assemblies.Add(ass.Location);
            }

            cp.ReferencedAssemblies.AddRange(assemblies.ToArray());

            while (!checkFileReady(lastTouchedFile))
                Thread.Sleep(10);

            Logger.Log($@"Recompiling {Path.GetFileName(checkpointObject.GetType().Name)}...", LoggingTarget.Runtime, LogLevel.Important);

            CompilationStarted?.Invoke();

            CompilerResults compile = codeProvider.CompileAssemblyFromFile(cp, requiredFiles.ToArray());

            Type compiledType = null;

            if (compile.Errors.HasErrors)
            {
                foreach (CompilerError ce in compile.Errors)
                {
                    if (ce.IsWarning) continue;
                    Logger.Log(ce.ToString(), LoggingTarget.Runtime, LogLevel.Error);
                }
            }
            else
            {
                compiledType = compile.CompiledAssembly.GetModules()[0]?.GetTypes().LastOrDefault(t => t.Name == checkpointObject.GetType().Name);
            }

            CompilationFinished?.Invoke(compiledType);

            if (compiledType != null)
                Logger.Log(@"Complete!", LoggingTarget.Runtime, LogLevel.Important);

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
