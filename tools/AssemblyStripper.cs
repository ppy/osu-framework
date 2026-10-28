// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// This app is used during iOS builds to strip methods and attributes from a subset of assemblies that prevent the .NET AOT compiler from working correctly.
//
// Usage: dotnet AssemblyStripper.cs -- <path-to-dll-1> [<path-to-dll-2>] ...

#:package Mono.Cecil@0.11.6

using Mono.Cecil;
using Mono.Cecil.Cil;

foreach (string path in args)
{
    Console.WriteLine($"Stripping assembly: {path}");

    using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadWrite = true });

    foreach (var module in assembly.Modules)
    {
        module.CustomAttributes.Clear();

        foreach (var type in module.Types)
            processType(type);
    }

    assembly.Write();
}

static void processType(TypeDefinition type)
{
    type.CustomAttributes.Clear();

    foreach (var nestedType in type.NestedTypes)
        processType(nestedType);

    foreach (var method in type.Methods)
    {
        method.CustomAttributes.Clear();

        if (!method.HasBody || method.IsAbstract || method.IsPInvokeImpl)
            continue;

        var il = method.Body.GetILProcessor();
        method.Body.Instructions.Clear();
        method.Body.Variables.Clear();
        method.Body.ExceptionHandlers.Clear();

        if (method.ReturnType.MetadataType == MetadataType.Void)
            il.Append(il.Create(OpCodes.Ret));
        else
        {
            il.Append(il.Create(OpCodes.Newobj, method.Module.ImportReference(typeof(PlatformNotSupportedException).GetConstructor(Type.EmptyTypes))));
            il.Append(il.Create(OpCodes.Throw));
        }
    }
}

// Appeases CodeFileSanity.
internal class AssemblyStripper;
