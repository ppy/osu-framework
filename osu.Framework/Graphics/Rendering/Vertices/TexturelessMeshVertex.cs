
using System;
using System.Runtime.InteropServices;
using Assimp;
using osu.Framework.Graphics.Rendering;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    public struct TexturelessMeshVertex : IMeshVertex<TexturelessMeshVertex>
    {
        [VertexMember(3, VertexAttribPointerType.Float)]
        public Vector3D Position;

        public static TexturelessMeshVertex FromMesh(Mesh mesh, int index)
        {
            return new TexturelessMeshVertex
            {
                Position = mesh.Vertices[index],
            };
        }

        public bool Equals(TexturelessMeshVertex other)
        {
            return Position == other.Position;
        }

    }
}
