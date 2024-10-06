using System;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    public interface IMeshVertex : IVertex
    {
    }
    public interface IMeshVertex<T> : IEquatable<T>, IMeshVertex where T : unmanaged, IEquatable<T>, IVertex
    {
        public abstract static T FromMesh(Mesh mesh, int index);
    }


}
