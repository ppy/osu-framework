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

        private FileSystemWatcher fsw_cs;
        private FileSystemWatcher fsw_dll;

        private string CurrentCodeFilePath;

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

        private Dictionary<string, string> overrideDlls = new Dictionary<string, string>();

        private HashSet<string> changedFiles = new HashSet<string>();

        public void Start()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            var basePath = di.Parent?.Parent?.Parent?.FullName;

            if (!Directory.Exists(basePath))
                return;

            fsw_dll = new FileSystemWatcher(basePath, @"*.dll")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            };

            fsw_dll.Changed += (sender, e) =>
            {
                Logger.Log($"{e.ChangeType} on {e.FullPath}");
                var tempName = Path.GetTempFileName() + ".dll";

                while (true)
                {
                    try
                    {
                        File.Copy(e.FullPath, tempName);
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(50);
                    }
                }

                overrideDlls[Path.GetFileName(e.Name)] = tempName;

                recompile();
            };

            fsw_cs = new FileSystemWatcher(basePath, @"*.cs")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            };

            fsw_cs.Changed += (sender, e) =>
            {

                changedFiles.Add(e.FullPath);

                //if (!Path.GetFileName(e.Name).StartsWith("TestCase"))
                //    return;

                CurrentCodeFilePath = e.FullPath;

                recompile();
            };
        }

        private Lazy<List<string>> initialAssemblies = new Lazy<List<string>>(() => AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location).ToList());

        private void recompile()
        {
            if (string.IsNullOrEmpty(CurrentCodeFilePath)) return;

            CompilerParameters cp = new CompilerParameters
            {
                GenerateInMemory = true,
                TreatWarningsAsErrors = false,
                GenerateExecutable = false,
            };

            var assemblies = initialAssemblies.Value.ToList();

            foreach (var a in assemblies.ToList())
            {
                string overridePath;
                if (overrideDlls.TryGetValue(Path.GetFileName(a), out overridePath))
                {
                    assemblies.Remove(a);
                    assemblies.Add(overridePath);
                }
            }

            cp.ReferencedAssemblies.AddRange(assemblies.ToArray());

            string source;
            while (true)
            {
                try
                {
                    source = File.ReadAllText(CurrentCodeFilePath);
                    break;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }

            Logger.Log($@"Recompiling {Path.GetFileName(CurrentCodeFilePath)}...", LoggingTarget.Runtime, LogLevel.Important);

            CompilationStarted?.Invoke();

            Type newType = null;

            using (var provider = createCodeProvider())
            {
                CompilerResults compile = provider.CompileAssemblyFromFile(cp, changedFiles.ToArray());

                if (compile.Errors.HasErrors)
                {
                    string text = "Compile error: ";
                    bool attemptRecompile = false;
                    foreach (CompilerError ce in compile.Errors)
                    {
                        if (ce.ErrorNumber == "CS0122")
                        {
                            string filename = ce.ErrorText.Split('\'')[1] + ".cs";
                            foreach (var f in Directory.GetFiles(findSolutionPath(), filename, SearchOption.AllDirectories))
                            {
                                changedFiles.Add(f);
                                attemptRecompile = true;
                            }
                        }

                        text += "\r\n" + ce;
                    }

                    if (attemptRecompile)
                    {
                        recompile();
                        return;
                    }

                    Logger.Log(text, LoggingTarget.Runtime, LogLevel.Error);
                }
                else
                {
                    Module module = compile.CompiledAssembly.GetModules()[0];
                    if (module != null)
                        newType = module.GetTypes().Last(t => t.Name.StartsWith("TestCase"));
                }
            }

            CompilationFinished?.Invoke(newType);

            if (newType != null)
                Logger.Log(@"Complete!", LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}