namespace SilkOpenGL.Model;

public sealed class ModelData
{
    public ModelData(string sourcePath, IReadOnlyList<ModelMeshData> meshes)
    {
        SourcePath = sourcePath;
        Meshes = meshes;
    }

    public string SourcePath { get; }

    public IReadOnlyList<ModelMeshData> Meshes { get; }

    public int MeshCount => Meshes.Count;
}
