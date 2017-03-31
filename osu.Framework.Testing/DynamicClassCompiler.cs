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

namespace osu.Framework.Testing
{
    public class DynamicClassCompiler<T>
    {
        public Action CompilationStarted;

        public Action<T> CompilationFinished;

        private FileSystemWatcher fsw;

        public string WatchDirectory;

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

            var newPath = Path.Combine(findSolutionPath(), "packages", "Microsoft.Net.Compilers.2.0.1", "tools", "csc.exe");

            //Black magic to fix incorrect packaged path (http://stackoverflow.com/a/40311406)
            var settings = csc.GetType().GetField("_compilerSettings", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(csc);
            var path = settings?.GetType().GetField("_compilerFullPath", BindingFlags.Instance | BindingFlags.NonPublic);
            path?.SetValue(settings, newPath);

            return csc;
        }

        public void Start()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            var basePath = Path.Combine(di.Parent?.Parent?.FullName, WatchDirectory);

            if (!Directory.Exists(basePath))
                return;

            fsw = new FileSystemWatcher(basePath, @"*.cs")
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            };

            fsw.Changed += (sender, e) =>
            {
                CompilerParameters cp = new CompilerParameters
                {
                    GenerateInMemory = true,
                    TreatWarningsAsErrors = false,
                    GenerateExecutable = false,
                };

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location);
                cp.ReferencedAssemblies.AddRange(assemblies.ToArray());

                string source;
                while (true)
                {
                    try
                    {
                        source = File.ReadAllText(e.FullPath);
                        break;
                    }
                    catch
                    {
                        Thread.Sleep(100);
                    }
                }

                Logger.Log($@"Recompiling {e.Name}...", LoggingTarget.Runtime, LogLevel.Important);

                CompilationStarted?.Invoke();

                T newVersion = default(T);

                using (var provider = createCodeProvider())
                {
                    CompilerResults compile = provider.CompileAssemblyFromSource(cp, source);

                    if (compile.Errors.HasErrors)
                    {
                        string text = "Compile error: ";
                        foreach (CompilerError ce in compile.Errors)
                            text += "\r\n" + ce;

                        Logger.Log(text, LoggingTarget.Runtime, LogLevel.Error);
                    }
                    else
                    {
                        Module module = compile.CompiledAssembly.GetModules()[0];
                        if (module != null)
                            newVersion = (T)Activator.CreateInstance(module.GetTypes()[0]);
                    }
                }

                CompilationFinished?.Invoke(newVersion);

                if (newVersion != null)
                    Logger.Log(@"Complete!", LoggingTarget.Runtime, LogLevel.Important);

            };
        }
    }
}