// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using osu.Framework.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace osu.Framework.Testing
{
    public class DynamicClassCompiler<T> : IDisposable
        where T : IDynamicallyCompile
    {
        public event Action CompilationStarted;

        public event Action<Type> CompilationFinished;

        public event Action<Exception> CompilationFailed;

        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private readonly HashSet<string> requiredFiles = new HashSet<string>();

        private T target;

        public void SetRecompilationTarget(T target)
        {
            if (this.target?.GetType().Name != target?.GetType().Name)
            {
                requiredFiles.Clear();
                referenceBuilder.Reset();
            }

            this.target = target;
        }

        private ITypeReferenceBuilder referenceBuilder;

        public void Start()
        {
            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            if (Debugger.IsAttached)
            {
                referenceBuilder = new EmptyTypeReferenceBuilder();

                Logger.Log("Dynamic compilation disabled (debugger attached).");
                return;
            }

#if NETCOREAPP
            referenceBuilder = new RoslynTypeReferenceBuilder();
#else
            referenceBuilder = new EmptyTypeReferenceBuilder();
#endif

            Task.Run(async () =>
            {
                Logger.Log("Initialising dynamic compilation...");

                var basePath = getSolutionPath(di);

                if (!Directory.Exists(basePath))
                    return;

                await referenceBuilder.Initialise(Directory.GetFiles(getSolutionPath(di), "*.sln").First());

                foreach (var dir in Directory.GetDirectories(basePath))
                {
                    // only watch directories which house a csproj. this avoids submodules and directories like .git which can contain many files.
                    if (!Directory.GetFiles(dir, "*.csproj").Any())
                        continue;

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

                Logger.Log("Dynamic compilation is now available.");
            });
        }

        private static string getSolutionPath(DirectoryInfo d)
        {
            if (d == null)
                return null;

            return d.GetFiles().Any(f => f.Extension == ".sln") ? d.FullName : getSolutionPath(d.Parent);
        }

        private void onChange(object sender, FileSystemEventArgs args) => Task.Run(async () => await recompileAsync(target?.GetType(), args.FullPath));

        private int currentVersion;
        private bool isCompiling;

        private async Task recompileAsync(Type targetType, string changedFile)
        {
            if (targetType == null || isCompiling || referenceBuilder is EmptyTypeReferenceBuilder)
                return;

            isCompiling = true;

            try
            {
                while (!checkFileReady(changedFile))
                    Thread.Sleep(10);

                Logger.Log($@"Recompiling {Path.GetFileName(targetType.Name)}...", LoggingTarget.Runtime, LogLevel.Important);

                CompilationStarted?.Invoke();

                // Compilation procedure:
                // 1. Find the files that need to be recompiled (get referenced files).
                // 2. Find the assemblies that need to be recompiled (get referenced assemblies).
                // 3. Create the compilation options.
                //    3.1. Import all metadata references.
                //    3.2. Set a (internal) property that allows the compiler to ignore accessibility, to support internals.
                // 4. Create the assembly namespace {currAssembly}.Dynamic.{version}, where {version} is incremented for each DCC. This allows the assembly to "replace" an existing one.
                // 5. Add a custom compiler attribute to ignore access checks (required to make use of 3.2).
                //    5.1. Ignore access checks to all required assemblies from 2.
                // 6. Compile and return the emitted assembly.

                // 1: Get all referenced files.
                var newRequiredFiles = await referenceBuilder.GetReferencedFiles(targetType, changedFile);
                foreach (var f in newRequiredFiles)
                    requiredFiles.Add(f);

                // 2: Get all referenced assemblies.
                var requiredAssemblies = await referenceBuilder.GetReferencedAssemblies(targetType, changedFile);
                var assemblyMetadata = requiredAssemblies.Where(a => !string.IsNullOrEmpty(a))
                                                         .Select(a => MetadataReference.CreateFromFile(a));

                // 3. Create the compilation options.
                var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithMetadataImportOptions(MetadataImportOptions.All);

                // This is an internal property which allows the compiler to ignore accessibility.
                // https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/
                var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
                Debug.Assert(topLevelBinderFlagsProperty != null);
                topLevelBinderFlagsProperty.SetValue(options, (uint)1 << 22);

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

                // 4: Create the custom assembly namespace, making sure it's always different.
                // ensure we don't duplicate the dynamic suffix.
                string assemblyNamespace = targetType.Assembly.GetName().Name?.Replace(".Dynamic", "");
                string dynamicNamespace = $"{assemblyNamespace}.Dynamic.{++currentVersion}";

                // Create the syntax trees, starting with the required files.
                var requiredSyntaxTrees = new List<SyntaxTree>();
                requiredSyntaxTrees.AddRange(requiredFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file, Encoding.UTF8), parseOptions, file, encoding: Encoding.UTF8)));

                // 5: Add custom compiler attribute to ignore access checks
                requiredSyntaxTrees.Add(CSharpSyntaxTree.ParseText(ignores_access_checks_to_attribute_string, parseOptions));

                var ignoreAccessChecksText = new StringBuilder();
                ignoreAccessChecksText.AppendLine("using System.Runtime.CompilerServices;");
                foreach (var asm in requiredAssemblies.Where(s => !string.IsNullOrEmpty(s)))
                    ignoreAccessChecksText.AppendLine($"[assembly: IgnoresAccessChecksTo(\"{Path.GetFileNameWithoutExtension(asm)}\")]");
                requiredSyntaxTrees.Add(CSharpSyntaxTree.ParseText(ignoreAccessChecksText.ToString(), parseOptions));

                // 6: Compile.
                var compilation = CSharpCompilation.Create(
                    dynamicNamespace,
                    requiredSyntaxTrees,
                    assemblyMetadata,
                    options
                );

                using (var pdbStream = new MemoryStream())
                using (var peStream = new MemoryStream())
                {
                    var compilationResult = compilation.Emit(peStream, pdbStream);

                    if (compilationResult.Success)
                    {
                        peStream.Seek(0, SeekOrigin.Begin);
                        pdbStream.Seek(0, SeekOrigin.Begin);

                        CompilationFinished?.Invoke(
                            Assembly.Load(peStream.ToArray(), pdbStream.ToArray()).GetModules()[0].GetTypes().LastOrDefault(t => t.FullName == targetType.FullName)
                        );
                    }
                    else
                    {
                        var exceptions = new List<Exception>();

                        foreach (var diagnostic in compilationResult.Diagnostics)
                        {
                            if (diagnostic.Severity < DiagnosticSeverity.Error)
                                continue;

                            exceptions.Add(new InvalidOperationException(diagnostic.ToString()));
                        }

                        throw new AggregateException(exceptions.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                CompilationFailed?.Invoke(ex);
            }
            finally
            {
                isCompiling = false;
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

        private const string ignores_access_checks_to_attribute_string = @"namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}";
    }
}
