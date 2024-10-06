using System.Collections.Generic;
using Assimp;

namespace osu.Framework.Graphics.Rendering
{

    public class Model
    {
        public List<Assimp.Material> Materials;
        public List<Mesh> Meshes = [];
        public readonly string Filepath;
        public Model(IRenderer renderer, string filepath)
        {

            Filepath = filepath;
            AssimpContext importer = new AssimpContext();
            Scene sceneInfo = importer.ImportFile(filepath, PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs);
            Materials = sceneInfo.Materials;
            loadMeshes(sceneInfo.Meshes, renderer);
        }

        private void loadMeshes(List<Assimp.Mesh> assimpMeshes, IRenderer renderer)
        {

            foreach (Assimp.Mesh assimpMesh in assimpMeshes)
            {

                Mesh mesh = renderer.ImportMesh(assimpMesh);


                Meshes.Add(mesh);

            }
        }

        public static Model BOX_3D(IRenderer renderer)
        {
            return new Model(renderer, @"C:\Users\Tamaninja\source\repos\Tamaninja\supreme-broccoli\TestTest123.Resources\Models\Stone.fbx");
        }
        public static Model NOTE(IRenderer renderer)
        {
            return new Model(renderer, @"C:\Users\Tamaninja\source\repos\Tamaninja\supreme-broccoli\TestTest123.Resources\Models\Trashcan_Small1.fbx");
        }
    }
}
