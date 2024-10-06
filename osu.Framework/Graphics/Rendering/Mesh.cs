using System.Collections.Generic;
using Assimp;

namespace osu.Framework.Graphics.Rendering
{
    public class Mesh
    {

        public int MaterialIndex;
        public uint[] Indices;
        public string Name;
        public Vector3D[] Vertices;
        public List<Vector3D>[] TextureCoords;

        public Mesh(Assimp.Mesh mesh)
        {
            Name = mesh.Name;
            MaterialIndex = mesh.MaterialIndex;
            Vertices = mesh.Vertices.ToArray();
            Indices = mesh.GetUnsignedIndices();
            TextureCoords = mesh.TextureCoordinateChannels;
        }

        public virtual void Draw()
        {

        }
        public virtual void Dispose()
        {

        }
    }
}
