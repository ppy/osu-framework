// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Emitters
{
    public interface IStatementEmitter
    {
        IEnumerable<StatementSyntax> Emit();
    }
}
