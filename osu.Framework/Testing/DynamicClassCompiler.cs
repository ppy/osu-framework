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
    internal class DynamicClassCompiler<T> : IDisposable
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
            if (Debugger.IsAttached)
            {
                referenceBuilder = new EmptyTypeReferenceBuilder();

                Logger.Log("Dynamic compilation disabled (debugger attached).");
                return;
            }

            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            string basePath = getSolutionPath(di);

            if (!Directory.Exists(basePath))
            {
                referenceBuilder = new EmptyTypeReferenceBuilder();

                Logger.Log("Dynamic compilation disabled (no solution file found).");
                return;
            }

#if NET5_0
            referenceBuilder = new RoslynTypeReferenceBuilder();
#else
            referenceBuilder = new EmptyTypeReferenceBuilder();
#endif

            Task.Run(async () =>
            {
                Logger.Log("Initialising dynamic compilation...");

                await referenceBuilder.Initialise(Directory.GetFiles(basePath, "*.sln").First()).ConfigureAwait(false);

                foreach (string dir in Directory.GetDirectories(basePath))
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

        private void onChange(object sender, FileSystemEventArgs args) => Task.Run(async () => await recompileAsync(target?.GetType(), args.FullPath).ConfigureAwait(false));

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

                foreach (string f in await referenceBuilder.GetReferencedFiles(targetType, changedFile).ConfigureAwait(false))
                    requiredFiles.Add(f);

                var assemblies = await referenceBuilder.GetReferencedAssemblies(targetType, changedFile).ConfigureAwait(false);

                using (var pdbStream = new MemoryStream())
                using (var peStream = new MemoryStream())
                {
                    var compilationResult = createCompilation(targetType, requiredFiles, assemblies).Emit(peStream, pdbStream);

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

        private CSharpCompilationOptions createCompilationOptions()
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithMetadataImportOptions(MetadataImportOptions.Internal);

            // This is an internal property which allows the compiler to ignore accessibility checks.
            // https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/
            var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(topLevelBinderFlagsProperty != null);
            topLevelBinderFlagsProperty.SetValue(options, (uint)1 << 22);

            return options;
        }

        private CSharpCompilation createCompilation(Type targetType, IEnumerable<string> files, IEnumerable<AssemblyReference> assemblies)
        {
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

            // Add the syntax trees for all referenced files.
            var syntaxTrees = new List<SyntaxTree>();
            foreach (string f in files)
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(f, Encoding.UTF8), parseOptions, f, encoding: Encoding.UTF8));

            // Add the new assembly version, such that it replaces any existing dynamic assembly.
            string assemblyVersion = $"{++currentVersion}.0.*";
            syntaxTrees.Add(CSharpSyntaxTree.ParseText($"using System.Reflection; [assembly: AssemblyVersion(\"{assemblyVersion}\")]", parseOptions));

            // Add a custom compiler attribute to allow ignoring access checks.
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(ignores_access_checks_to_attribute_syntax, parseOptions));

            // Ignore access checks for assemblies that have had their internal types referenced.
            var ignoreAccessChecksText = new StringBuilder();
            ignoreAccessChecksText.AppendLine("using System.Runtime.CompilerServices;");
            foreach (var asm in assemblies.Where(asm => asm.IgnoreAccessChecks))
                ignoreAccessChecksText.AppendLine($"[assembly: IgnoresAccessChecksTo(\"{asm.Assembly.GetName().Name}\")]");
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(ignoreAccessChecksText.ToString(), parseOptions));

            // Determine the new assembly name, ensuring that the dynamic suffix is not duplicated.
            string assemblyNamespace = targetType.Assembly.GetName().Name?.Replace(".Dynamic", "");
            string dynamicNamespace = $"{assemblyNamespace}.Dynamic";

            return CSharpCompilation.Create(
                dynamicNamespace,
                syntaxTrees,
                assemblies.Select(asm => asm.GetReference()),
                createCompilationOptions()
            );
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private const string ignores_access_checks_to_attribute_syntax =
            @"namespace System.Runtime.CompilerServices
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
