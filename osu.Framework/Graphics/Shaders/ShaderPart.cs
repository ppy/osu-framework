// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    internal abstract class ShaderPart : IDisposable
    {
        internal const string BOUNDARY = @"----------------------{0}";

        internal StringBuilder Log = new StringBuilder();

        internal List<AttributeInfo> Attributes = new List<AttributeInfo>();

        internal string Name;
        internal bool HasCode;
        internal bool Compiled;

        internal ShaderType Type;

        internal abstract bool Compile();

        internal abstract void Init(string name, byte[] data, ShaderType type, ShaderManager manager);

        public abstract void Dispose();

        protected abstract int ToInt();

        public static implicit operator int(ShaderPart program)
        {
            return program.ToInt();
        }

    }
}
