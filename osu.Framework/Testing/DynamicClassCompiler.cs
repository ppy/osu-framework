// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        public event Action CompilationStarted;

        public event Action<Type> CompilationFinished;

        public event Action<Exception> CompilationFailed;

        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        private string lastTouchedFile;

        private T target;

        public void SetRecompilationTarget(T target)
        {
            if (this.target?.GetType().FullName != target?.GetType().FullName)
                referenceBuilder.Reset();

            this.target = target;
        }

        private HashSet<string> assemblies;

        private readonly List<string> validDirectories = new List<string>();

        private ITypeReferenceBuilder referenceBuilder;

        public void Start()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

#if NETCOREAPP
            referenceBuilder = new RoslynTypeReferenceBuilder();
#else
            referenceBuilder = new EmptyTypeReferenceBuilder();
#endif

            referenceBuilder.Initialise(Directory.GetFiles(getSolutionPath(di), "*.sln").First());

            Task.Run(() =>
            {
                var basePath = getSolutionPath(di);

                if (!Directory.Exists(basePath))
                    return;

                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    // only watch directories which house a csproj. this avoids submodules and directories like .git which can contain many files.
                    if (!Directory.GetFiles(dir, "*.csproj").Any())
                        continue;

                    lock (compileLock) // enumeration over this list occurs during compilation
                        validDirectories.Add(dir);

                    var fsw = new FileSystemWatcher(dir, @"*.cs")
                    {
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
                    };

                    fsw.Renamed += onChange;
                    fsw.Changed += onChange;
                    fsw.Created += onChange;

                    watchers.Add(fsw);
                }
            });
        }

        private static string getSolutionPath(DirectoryInfo d)
        {
            if (d == null)
                return null;

            return d.GetFiles().Any(f => f.Extension == ".sln") ? d.FullName : getSolutionPath(d.Parent);
        }

        private void onChange(object sender, FileSystemEventArgs args)
        {
            lock (compileLock)
            {
                if (target == null || isCompiling)
                    return;

                isCompiling = true;
                lastTouchedFile = args.FullPath;

                Task.Run(async () => recompile(await referenceBuilder.GetReferencedFiles(target.GetType(), args.FullPath)))
                    .ContinueWith(_ => isCompiling = false);
            }
        }

        private int currentVersion;

        private bool isCompiling;
        private readonly object compileLock = new object();

        private void recompile(IReadOnlyCollection<string> requiredFiles)
        {
            if (assemblies == null)
            {
                assemblies = new HashSet<string>();
                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic))
                    assemblies.Add(ass.Location);
            }

            assemblies.Add(typeof(JetBrains.Annotations.NotNullAttribute).Assembly.Location);

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            // ReSharper disable once RedundantExplicitArrayCreation this doesn't compile when the array is empty
            var parseOptions = new CSharpParseOptions(preprocessorSymbols: new string[]
            {
#if DEBUG
                "DEBUG",
#endif
#if TRACE
                "TRACE",
#endif
#if RELEASE
                "RELEASE",
#endif
            }, languageVersion: LanguageVersion.Latest);
            var references = assemblies.Select(a => MetadataReference.CreateFromFile(a));

            while (!checkFileReady(lastTouchedFile))
                Thread.Sleep(10);

            Logger.Log($@"Recompiling {Path.GetFileName(target.GetType().Name)}...", LoggingTarget.Runtime, LogLevel.Important);

            CompilationStarted?.Invoke();

            // ensure we don't duplicate the dynamic suffix.
            string assemblyNamespace = target.GetType().Assembly.GetName().Name.Replace(".Dynamic", "");

            string assemblyVersion = $"{++currentVersion}.0.*";
            string dynamicNamespace = $"{assemblyNamespace}.Dynamic";

            var compilation = CSharpCompilation.Create(
                dynamicNamespace,
                requiredFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), parseOptions, file))
                             // Compile the assembly with a new version so that it replaces the existing one
                             .Append(CSharpSyntaxTree.ParseText($"using System.Reflection; [assembly: AssemblyVersion(\"{assemblyVersion}\")]", parseOptions))
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
                        Assembly.Load(ms.ToArray()).GetModules()[0].GetTypes().LastOrDefault(t => t.FullName == target.GetType().FullName)
                    );
                }
                else
                {
                    foreach (var diagnostic in compilationResult.Diagnostics)
                    {
                        if (diagnostic.Severity < DiagnosticSeverity.Error)
                            continue;

                        CompilationFailed?.Invoke(new InvalidOperationException(diagnostic.ToString()));
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
