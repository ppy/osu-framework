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
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

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

        public void SetRecompilationTarget(T target) => this.target = target;

        private readonly HashSet<string> requiredFiles = new HashSet<string>();

        private HashSet<string> assemblies;

        private readonly List<string> validDirectories = new List<string>();

        private Solution solution;

        public void Start()
        {
            MSBuildLocator.RegisterDefaults();

            var di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            solution = MSBuildWorkspace.Create().OpenSolutionAsync(Path.Combine(getSolutionPath(di), "osu-framework.sln")).Result;

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

                var testProj = findTestProject();

                var changedDoc = solution.GetDocumentIdsWithFilePath(args.FullPath)[0];
                var changedProj = findProjectFromDocumentId(changedDoc);

                Logger.Log("Updating solution...");

                solution = solution.WithDocumentText(changedDoc, SourceText.From(File.ReadAllText(args.FullPath)));

                Logger.Log("Compiling test project...");

                // Find the syntax tree for the target TestScene.
                var testProjCompilation = testProj.GetCompilationAsync().Result;
                var testProjTargetType = testProjCompilation.GetTypeByMetadataName(target.GetType().FullName);

                // The following goes through the following process. Given the test scene hierarchy (this graph is unknown to us by this point):
                //
                //                            P
                //                          /  \
                //                         /    \
                //                        /      \
                //                      C1       C2
                //                    /   \      |
                //                  C3    C4    C5
                //                          \  /
                //                           C6
                //
                // We generate a disjoint graph of connections between types and all their referenced types:
                //
                // P -> { C1, C2 }
                // C1 -> { C3, C4 }
                // C2 -> { C5 }
                // C3 -> { }
                // C4 -> { C6 }
                // C5 -> { C6 }
                // C6 -> { }
                //
                // Then we go through all the connections and assign parents, building a directed graph towards P.
                //
                // foreach conn in dict
                //     foreach ref in connection
                //         node := get_existing_node_or_create_new(ref)
                //         node.parent := conn.node
                //
                // Then we find the changed type (e.g. C6) traverse the directed graph, adding each corresponding file as a required file.
                //
                // fun add_required_files(node):
                //     add_file(node.file)
                //     foreach parent in node
                //         add_required_files(parent)
                //
                // We're then left with the set of required files that need to be re-compilated.

                // 1. Build the disjoint graph of connections.
                Logger.Log("Finding all referenced types...");

                var dict = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>();
                getReferencedTypes(testProjTargetType, dict, compilations: new Dictionary<string, Compilation> { { testProj.Name, testProjCompilation } });

                // 2. Merge disjoint graph by parents.
                Logger.Log("Building reverse lookup graph...");

                var revDict = new Dictionary<string, TypeNode>();

                foreach (var kvp in dict)
                {
                    var parentNode = getNode(kvp.Key);
                    foreach (var typeRef in kvp.Value)
                        getNode(typeRef).Parents.Add(parentNode);
                }

                TypeNode getNode(INamedTypeSymbol typeSymbol)
                {
                    var stringTypeSymbol = typeSymbol.ToString();
                    if (!revDict.TryGetValue(stringTypeSymbol, out var existing))
                        revDict[stringTypeSymbol] = existing = new TypeNode(typeSymbol);
                    return existing;
                }

                // 3. Traverse the directed graph and build the set of required files.
                Logger.Log("Building required files...");

                // Traverse the parent-hirerchy of the changed file
                var changedTypeNode = revDict.Where(kvp =>
                {
                    foreach (var loc in kvp.Value.TypeSymbol.Locations)
                    {
                        var tree = loc.SourceTree;
                        if (tree == null)
                            return false;

                        return tree.FilePath == args.FullPath;
                    }

                    return false;
                }).First(); // Todo: FirstOrDefault() with a null check.

                requiredFiles.Clear();

                var seenNodes = new HashSet<TypeNode>();
                addRequiredFiles(changedTypeNode.Value);

                void addRequiredFiles(TypeNode node)
                {
                    if (seenNodes.Contains(node))
                        return;

                    seenNodes.Add(node);

                    foreach (var loc in node.TypeSymbol.Locations)
                    {
                        if (loc.SourceTree != null)
                            requiredFiles.Add(loc.SourceTree.FilePath);
                    }

                    foreach (var p in node.Parents)
                        addRequiredFiles(p);
                }

                // Finally, recompile.
                lastTouchedFile = args.FullPath;

                Task.Run(recompile)
                    .ContinueWith(_ => isCompiling = false);
            }
        }

        private Project findTestProject()
        {
            var expectedAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            return solution.Projects.FirstOrDefault(p => p.AssemblyName == expectedAssemblyName);
        }

        private Project findProjectFromDocumentId(DocumentId document) => solution.Projects.SingleOrDefault(p => p.ContainsDocument(document));

        private void getReferencedTypes(INamedTypeSymbol typeSymbol, Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> result, HashSet<string> checkedTypes = null,
                                        Dictionary<string, Compilation> compilations = null, bool allowBaseTypes = false)
        {
            checkedTypes ??= new HashSet<string>();
            compilations ??= new Dictionary<string, Compilation>();

            var typeString = typeSymbol.ToString();

            // If the type has already been checked, it doesn't need to be iterated over again.
            if (checkedTypes.Contains(typeString))
                return;

            // Mark the current type as checked.
            checkedTypes.Add(typeString);

            if (typeSymbol.DeclaringSyntaxReferences.Length == 0)
                return;

            // Add storage for the type relationship.
            var resultList = new List<INamedTypeSymbol>();
            result[typeSymbol] = resultList;

            // A type may exist in multiple syntax trees via partial classes.
            foreach (var reference in typeSymbol.DeclaringSyntaxReferences)
            {
                var syntaxTree = reference.SyntaxTree;

                // Find the project which the syntax tree is contained in.
                var project = solution.Projects.FirstOrDefault(p => p.Documents.Any(d => d.FilePath == syntaxTree.FilePath));
                if (project == null)
                    continue;

                // Referenced types may exist in a separate project, however types referenced to the same project should come from a single compilation.
                if (!compilations.TryGetValue(project.Name, out var compilation))
                    compilations[project.Name] = compilation = project.GetCompilationAsync().Result;

                // We need to re-retrieve the syntax tree on the correct compilation.
                syntaxTree = compilation!.SyntaxTrees.Single(s => s.FilePath == syntaxTree.FilePath);

                // Get the syntax model corresponding to the tree.
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                var descendantNodes = syntaxTree.GetRoot().DescendantNodes(n =>
                {
                    var kind = n.Kind();

                    return kind != SyntaxKind.UsingDirective
                           && kind != SyntaxKind.NamespaceKeyword
                           && (allowBaseTypes || kind != SyntaxKind.BaseList);
                });

                // Find all the named type symbols in the syntax tree, and mark + recursively iterate through them.
                foreach (var n in descendantNodes)
                {
                    switch (n.Kind())
                    {
                        case SyntaxKind.GenericName:
                        case SyntaxKind.IdentifierName:
                        {
                            if (semanticModel.GetSymbolInfo(n).Symbol is INamedTypeSymbol t)
                            {
                                resultList.Add(t);
                                getReferencedTypes(t, result, checkedTypes, compilations, true);
                            }

                            break;
                        }

                        case SyntaxKind.AsExpression:
                        case SyntaxKind.IsExpression:
                        case SyntaxKind.SizeOfExpression:
                        case SyntaxKind.TypeOfExpression:
                        case SyntaxKind.CastExpression:
                        case SyntaxKind.ObjectCreationExpression:
                        {
                            if (semanticModel.GetTypeInfo(n).Type is INamedTypeSymbol t)
                            {
                                resultList.Add(t);
                                getReferencedTypes(t, result, checkedTypes, compilations, true);
                            }

                            break;
                        }
                    }
                }
            }
        }

        private class TypeNode : IEquatable<TypeNode>
        {
            public readonly INamedTypeSymbol TypeSymbol;
            public readonly HashSet<TypeNode> Parents = new HashSet<TypeNode>();

            public TypeNode(INamedTypeSymbol typeSymbol)
            {
                TypeSymbol = typeSymbol;
            }

            public override int GetHashCode() => TypeSymbol.GetHashCode();

            public bool Equals(TypeNode other) => TypeSymbol.Equals(other?.TypeSymbol);
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
