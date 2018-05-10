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

namespace osu.Framework.Testing
{
    public class DynamicClassCompiler<T> : IDisposable
        where T : IDynamicallyCompile
    {
        public Action CompilationStarted;

        public Action<Type> CompilationFinished;

        public Action<Exception> CompilationFailed;

        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        private string lastTouchedFile;

        private T checkpointObject;

        public void Checkpoint(T obj)
        {
            checkpointObject = obj;
        }

        private readonly List<string> requiredFiles = new List<string>();
        private List<string> requiredTypeNames = new List<string>();

        private HashSet<string> assemblies;

        private readonly List<string> validDirectories = new List<string>();

        public void Start()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            Task.Run(() =>
            {
                var basePath = di.Parent?.Parent?.Parent?.Parent?.FullName;

                if (!Directory.Exists(basePath))
                    return;

                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    // only watch directories which house a csproj. this avoids submodules and directories like .git which can contain many files.
                    if (!Directory.GetFiles(dir, "*.csproj").Any())
                        continue;

                    validDirectories.Add(dir);

                    var fsw = new FileSystemWatcher(dir, @"*.cs")
                    {
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    };

                    fsw.Changed += onChange;
                    fsw.Created += onChange;

                    watchers.Add(fsw);
                }
            });
        }

        private void onChange(object sender, FileSystemEventArgs e)
        {
            lock (compileLock)
            {
                if (checkpointObject == null || isCompiling)
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

                    requiredFiles.Clear();
                    foreach (var d in validDirectories)
                        requiredFiles.AddRange(Directory
                                               .EnumerateFiles(d, "*.cs", SearchOption.AllDirectories)
                                               .Where(fw => requiredTypeNames.Contains(Path.GetFileNameWithoutExtension(fw))));
                }

                lastTouchedFile = e.FullPath;

                isCompiling = true;
                Task.Run((Action)recompile)
                    .ContinueWith(_ => isCompiling = false);
            }
        }

        private int currentVersion;

        private bool isCompiling;
        private readonly object compileLock = new object();

        private void recompile()
        {
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

            // ensure we don't duplicate the dynamic suffix.
            string assemblyNamespace = checkpointObject.GetType().Assembly.GetName().Name.Replace(".Dynamic", "");

            string assemblyVersion = $"{++currentVersion}.0.*";
            string dynamicNamespace = $"{assemblyNamespace}.Dynamic";

            var compilation = CSharpCompilation.Create(
                dynamicNamespace,
                requiredFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), null, file))
                             // Compile the assembly with a new version so that it replaces the existing one
                             .Append(CSharpSyntaxTree.ParseText($"using System.Reflection; [assembly: AssemblyVersion(\"{assemblyVersion}\")]"))
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

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                watchers.ForEach(w => w.Dispose());
            }
        }

        ~DynamicClassCompiler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
