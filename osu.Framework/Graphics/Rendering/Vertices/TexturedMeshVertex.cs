using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    public struct TexturedMeshVertex : IMeshVertex<TexturedMeshVertex>
    {
        [VertexMember(3, VertexAttribPointerType.Float)]
        public Vector3D Position;

        [VertexMember(3, VertexAttribPointerType.Float)]
        public Vector3D TexturePosition;


        public static TexturedMeshVertex FromMesh(Mesh mesh, int index)
        {
            if (mesh.TextureCoords[0].Count == 0)
            {
                return new TexturedMeshVertex
                {
                    Position = mesh.Vertices[index],
                    TexturePosition = new Vector3D(0)
                };
            }

            return new TexturedMeshVertex
            {
                Position = mesh.Vertices[index],
                TexturePosition = mesh.TextureCoords[0][index],
            };
        }

        public bool Equals(TexturedMeshVertex other)
        {
            return Position == other.Position && TexturePosition == other.TexturePosition;
        }


    }
}
