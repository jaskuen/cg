namespace SilkOpenGL.Model;

public sealed class ModelData
{
    public ModelData(string sourcePath, IReadOnlyList<ModelMeshData> meshes, ModelDiagnostics? diagnostics = null)
    {
        SourcePath = sourcePath;
        Meshes = meshes;
        Diagnostics = diagnostics;
    }

    public string SourcePath { get; }

    public IReadOnlyList<ModelMeshData> Meshes { get; }

    public ModelDiagnostics? Diagnostics { get; }

    public int MeshCount => Meshes.Count;
}
