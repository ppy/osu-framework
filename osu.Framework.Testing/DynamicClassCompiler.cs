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

namespace osu.Framework.Testing
{
    public class DynamicClassCompiler
    {
        public Action CompilationStarted;

        public Action<Type> CompilationFinished;

        private FileSystemWatcher fsw;

        private string lastTouchedFile;

        private Type checkpointedType;

        public void Checkpoint(Type type)
        {
            checkpointedType = type;
        }

        /// <summary>
        /// Find the base path of the closest solution folder
        /// </summary>
        private static string findSolutionPath()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (!Directory.GetFiles(di.FullName, "*.sln").Any() && di.Parent != null)
                di = di.Parent;

            return di.FullName;
        }

        private static CSharpCodeProvider createCodeProvider()
        {
            var csc = new CSharpCodeProvider();

            var newPath = Path.Combine(findSolutionPath(), "packages", "Microsoft.Net.Compilers.2.3.2", "tools", "csc.exe");

            //Black magic to fix incorrect packages path (http://stackoverflow.com/a/40311406)
            var settings = csc.GetType().GetField("_compilerSettings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(csc);
            var path = settings?.GetType().GetField("_compilerFullPath", BindingFlags.Instance | BindingFlags.NonPublic);
            path?.SetValue(settings, newPath);

            return csc;
        }

        private HashSet<string> overrideDlls = new HashSet<string>();

        private readonly HashSet<string> changedFiles = new HashSet<string>();

        //private List<string> ignoreTypes = new List<string>()
        //{
        //    "OsuGameBase",
        //    "APIAccess"
        //};

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
                lastTouchedFile = e.FullPath;

                var sln = findSolutionPath();

                var oneDeeper = new DirectoryInfo(Path.GetDirectoryName(e.FullPath));

                while (oneDeeper.Parent != null && oneDeeper.Parent.FullName != sln)
                    oneDeeper = oneDeeper.Parent;

                if (assemblies == null)
                {
                    assemblies = new HashSet<string>();

                    foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
                    {
                        foreach (var refAss in ass.GetReferencedAssemblies())
                            Assembly.Load(refAss);
                    }

                    foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
                        assemblies.Add(ass.Location);

                    foreach (var path in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"*.dll"))
                        assemblies.Add(path);
                }

                assemblies.Remove(oneDeeper.FullName.Split(Path.DirectorySeparatorChar).Last() + ".dll");

                changedFiles.Add(lastTouchedFile);

                /*

                foreach (var f in Directory.GetFiles(oneDeeper.FullName, "*.cs", SearchOption.AllDirectories))
                {
                    var parts = f.Split(Path.DirectorySeparatorChar);

                    // we don't want to include files that may be in the /obj/ directory.
                    if (parts.Any(p => p == "obj")) continue;

                    var filename = parts.Last();

                    if (ignoreTypes.Contains(filename.Replace(".cs", "")))
                        continue;

                    // we don't want to include extension methods. these get ambiguous.
                    if (filename.EndsWith("Extensions.cs"))
                        continue;

                    // we are an interface.
                    if (filename[0] == 'I' && char.IsUpper(filename[1]))
                        continue;

                    changedFiles.Add(f);
                }

                */

                recompile();
            };
        }

        private HashSet<string> assemblies;

        private void recompile()
        {
            CompilerParameters cp = new CompilerParameters
            {
                GenerateInMemory = true,
                TreatWarningsAsErrors = false,
                GenerateExecutable = false,

            };

            cp.ReferencedAssemblies.AddRange(assemblies.ToArray());

            while (true)
            {
                try
                {
                    File.ReadAllText(lastTouchedFile);
                    break;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }

            Logger.Log($@"Recompiling {Path.GetFileName(lastTouchedFile)}...", LoggingTarget.Runtime, LogLevel.Important);

            CompilationStarted?.Invoke();

            Type newType = null;

            using (var provider = createCodeProvider())
            {
                CompilerResults compile = provider.CompileAssemblyFromFile(cp, changedFiles.ToArray());

                if (compile.Errors.HasErrors)
                {
                    bool attemptRecompile = false;

                    foreach (CompilerError ce in compile.Errors)
                    {
                        if (ce.IsWarning) continue;

                        if (ce.ErrorNumber == "CS0121")
                        {
                            changedFiles.Remove(ce.FileName);
                            attemptRecompile = true;
                        }

                        if (ce.ErrorNumber == "CS0122")
                        {
                            string filename = ce.ErrorText.Split('\'')[1] + ".cs";
                            foreach (var f in Directory.GetFiles(findSolutionPath(), filename, SearchOption.AllDirectories))
                            {
                                changedFiles.Add(f);
                                attemptRecompile = true;
                            }
                        }

                        Logger.Log(ce.ToString(), LoggingTarget.Runtime, LogLevel.Error);
                    }

                    if (attemptRecompile)
                    {
                        recompile();
                        return;
                    }
                }
                else
                {
                    Module module = compile.CompiledAssembly.GetModules()[0];
                    if (module != null)
                        newType = module.GetTypes().LastOrDefault(t => t.Name == checkpointedType.Name);
                }
            }

            CompilationFinished?.Invoke(newType);

            if (newType != null)
                Logger.Log(@"Complete!", LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}