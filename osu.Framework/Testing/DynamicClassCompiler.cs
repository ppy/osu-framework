// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace osu.Framework.Testing
{
    public static class DynamicClassCompilerStatics
    {
        public static event Action<Type[]> CompilationFinished;

        public static void UpdateApplication([CanBeNull] Type[] updatedTypes) => CompilationFinished?.Invoke(updatedTypes);
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    internal class DynamicClassCompiler<T>
    {
        public event Action<Type> CompilationFinished;

        private T target;

        public void SetRecompilationTarget(T target) => this.target = target;

        public void Start()
        {
            DynamicClassCompilerStatics.CompilationFinished += types => CompilationFinished?.Invoke(target.GetType());
        }
    }
}
