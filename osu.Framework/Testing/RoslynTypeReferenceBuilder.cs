// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using osu.Framework.Logging;

namespace osu.Framework.Testing
{
    public class RoslynTypeReferenceBuilder : ITypeReferenceBuilder
    {
        private readonly Dictionary<Project, Compilation> compilationCache = new Dictionary<Project, Compilation>();
        private readonly Dictionary<SyntaxTree, SemanticModel> semanticModelCache = new Dictionary<SyntaxTree, SemanticModel>();

        private Solution solution;

        public async Task Initialise(string solutionFile)
        {
            MSBuildLocator.RegisterDefaults();
            solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionFile);
        }

        public async Task<IReadOnlyCollection<string>> GetReferencedFiles(Type testType, string changedFile)
        {
            clearCaches();
            updateFile(changedFile);

            var compiledTestProject = await compileProjectAsync(findTestProject());
            var compiledTestType = compiledTestProject.GetTypeByMetadataName(testType.FullName);

            Logger.Log("Finding all referenced types...");
            var disjointGraph = await getReferencedTypesRecursiveAsync(TypeReference.FromSymbol(compiledTestType));

            Logger.Log("Building type graph...");
            var directedGraph = getDirectedGraph(disjointGraph);

            Logger.Log("Retrieving required files...");
            var changedType = directedGraph
                              .Select(kvp => kvp.Key)
                              .FirstOrDefault(t => t.Symbol.Locations.Any(l => l.SourceTree?.FilePath == changedFile));

            if (changedType.Symbol == null)
                return Array.Empty<string>();

            return getRequiredFiles(changedType, directedGraph);
        }

        private void clearCaches()
        {
            compilationCache.Clear();
            semanticModelCache.Clear();
        }

        private void updateFile(string file)
        {
            Logger.Log($"Updating file {file} in solution...");

            var changedDoc = solution.GetDocumentIdsWithFilePath(file)[0];
            solution = solution.WithDocumentText(changedDoc, SourceText.From(File.ReadAllText(file)));
        }

        private async Task<Dictionary<TypeReference, IReadOnlyCollection<TypeReference>>> getReferencedTypesRecursiveAsync(TypeReference rootReference)
        {
            // There exists a graph of types from the root type symbol which we want to find.
            //
            //                            P
            //                          /  \
            //                         /    \
            //                        /      \
            //                      C1       C2 ---
            //                    /   \      |    /
            //                  C3    C4    C5   /
            //                          \  /    /
            //                           C6 ---
            //
            // We do this by constructing a disjoint graph between types and all their referenced types. This is done via a BFS, leading to the following:
            //
            // P -> { C1, C2 }
            // C1 -> { C3, C4 }
            // C2 -> { C5, C6 }
            // C3 -> { }
            // C4 -> { C6 }
            // C5 -> { C6 }
            // C6 -> { C2 }

            var result = new Dictionary<TypeReference, IReadOnlyCollection<TypeReference>>();

            var searchQueue = new Queue<TypeReference>();
            searchQueue.Enqueue(rootReference);

            while (searchQueue.Count > 0)
            {
                var toCheck = searchQueue.Dequeue();
                var referencedTypes = await getReferencedTypesAsync(toCheck, !toCheck.Equals(rootReference));

                result[toCheck] = referencedTypes;

                foreach (var referenced in referencedTypes)
                {
                    // We don't want to cycle over types that have already been explored.
                    if (!result.ContainsKey(referenced))
                    {
                        // Used for de-duping, so it must be added to the dictionary immediately.
                        result[referenced] = null;
                        searchQueue.Enqueue(referenced);
                    }
                }
            }

            return result;
        }

        private async Task<HashSet<TypeReference>> getReferencedTypesAsync(TypeReference typeReference, bool includeBaseType)
        {
            var result = new HashSet<TypeReference>();

            foreach (var reference in typeReference.Symbol.DeclaringSyntaxReferences)
            {
                var syntaxTree = reference.SyntaxTree;
                var semanticModel = await getSemanticModelAsync(reference.SyntaxTree);
                var root = await syntaxTree.GetRootAsync();

                var descendantNodes = root.DescendantNodes(n =>
                {
                    var kind = n.Kind();

                    return kind != SyntaxKind.UsingDirective
                           && kind != SyntaxKind.NamespaceKeyword
                           && (includeBaseType || kind != SyntaxKind.BaseList);
                });

                // Find all the named type symbols in the syntax tree, and mark + recursively iterate through them.
                foreach (var node in descendantNodes)
                {
                    switch (node.Kind())
                    {
                        case SyntaxKind.GenericName:
                        case SyntaxKind.IdentifierName:
                        {
                            if (semanticModel.GetSymbolInfo(node).Symbol is INamedTypeSymbol t)
                                result.Add(TypeReference.FromSymbol(t));

                            break;
                        }

                        case SyntaxKind.AsExpression:
                        case SyntaxKind.IsExpression:
                        case SyntaxKind.SizeOfExpression:
                        case SyntaxKind.TypeOfExpression:
                        case SyntaxKind.CastExpression:
                        case SyntaxKind.ObjectCreationExpression:
                        {
                            if (semanticModel.GetTypeInfo(node).Type is INamedTypeSymbol t)
                                result.Add(TypeReference.FromSymbol(t));

                            break;
                        }
                    }
                }
            }

            return result;
        }

        private Dictionary<TypeReference, TypeNode> getDirectedGraph(IReadOnlyDictionary<TypeReference, IReadOnlyCollection<TypeReference>> disjointGraph)
        {
            // Build an upwards directed graph by assigning parents to all the connections.
            //
            // foreach conn in dict
            //     foreach ref in connection
            //         node := get_existing_node_or_create_new(ref)
            //         node.parent := conn.node
            //

            var result = new Dictionary<TypeReference, TypeNode>();

            foreach (var kvp in disjointGraph)
            {
                var parentNode = getNode(kvp.Key);
                foreach (var typeRef in kvp.Value)
                    getNode(typeRef).Parents.Add(parentNode);
            }

            return result;

            TypeNode getNode(TypeReference typeSymbol)
            {
                if (!result.TryGetValue(typeSymbol, out var existing))
                    result[typeSymbol] = existing = new TypeNode(typeSymbol);
                return existing;
            }
        }

        private HashSet<string> getRequiredFiles(TypeReference start, IReadOnlyDictionary<TypeReference, TypeNode> directedGraph)
        {
            var result = new HashSet<string>();

            var seenTypes = new HashSet<TypeNode>();
            var searchQueue = new Queue<TypeNode>();
            searchQueue.Enqueue(directedGraph[start]);

            while (searchQueue.Count > 0)
            {
                var toCheck = searchQueue.Dequeue();

                seenTypes.Add(toCheck);

                foreach (var location in toCheck.Reference.Symbol.Locations)
                {
                    var syntaxTree = location.SourceTree;
                    if (syntaxTree != null)
                        result.Add(syntaxTree.FilePath);
                }

                foreach (var parent in toCheck.Parents)
                {
                    if (!seenTypes.Contains(parent))
                        searchQueue.Enqueue(parent);
                }
            }

            return result;
        }

        private async Task<Compilation> compileProjectAsync(Project project)
        {
            if (compilationCache.TryGetValue(project, out var existing))
                return existing;

            Logger.Log($"Compiling project {project.Name}...");
            return compilationCache[project] = await project.GetCompilationAsync();
        }

        private async Task<SemanticModel> getSemanticModelAsync(SyntaxTree syntaxTree)
        {
            if (semanticModelCache.TryGetValue(syntaxTree, out var existing))
                return existing;

            var project = solution.Projects.FirstOrDefault(p => p.Documents.Any(d => d.FilePath == syntaxTree.FilePath));
            return semanticModelCache[syntaxTree] = (await compileProjectAsync(project)).GetSemanticModel(syntaxTree, true);
        }

        private Project findTestProject()
        {
            var executingAssembly = Assembly.GetEntryAssembly()?.GetName().Name;
            return solution.Projects.FirstOrDefault(p => p.AssemblyName == executingAssembly);
        }

        private readonly struct TypeReference : IEquatable<TypeReference>
        {
            public readonly INamedTypeSymbol Symbol;

            public TypeReference(INamedTypeSymbol symbol)
            {
                Symbol = symbol;
            }

            public bool Equals(TypeReference other)
                => Symbol.ContainingNamespace.ToString() == other.Symbol.ContainingNamespace.ToString()
                   && Symbol.ToString() == other.Symbol.ToString();

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(Symbol.ToString(), StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            public static TypeReference FromSymbol(INamedTypeSymbol symbol) => new TypeReference(symbol);
        }

        private class TypeNode : IEquatable<TypeNode>
        {
            public readonly TypeReference Reference;
            public readonly List<TypeNode> Parents = new List<TypeNode>();

            public TypeNode(TypeReference reference)
            {
                Reference = reference;
            }

            public bool Equals(TypeNode other)
                => other != null
                   && Reference.Equals(other.Reference);

            public override int GetHashCode() => Reference.GetHashCode();
        }
    }
}

#endif
