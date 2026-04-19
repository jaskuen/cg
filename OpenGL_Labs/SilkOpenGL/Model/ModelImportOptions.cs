using Assimp;

namespace SilkOpenGL.Model;

public sealed class ModelImportOptions
{
    public bool Triangulate { get; init; } = true;
    public bool FlipUvCoordinates { get; init; } = true;
    public bool GenerateSmoothNormals { get; init; } = true;
    public bool CalculateTangentSpace { get; init; } = true;
    public bool JoinIdenticalVertices { get; init; }
    public bool ImproveCacheLocality { get; init; } = true;
    public bool FindInvalidData { get; init; } = true;
    public bool ValidateDataStructure { get; init; } = true;

    public bool IncludeTriangles { get; init; } = true;
    public bool IncludeLines { get; init; }
    public bool IncludePoints { get; init; }

    public IReadOnlyCollection<string> ExcludedMeshNameSubstrings { get; init; } = [];
    public float? MaxMeshExtent { get; init; }
    public int? MinimumPrimitiveCount { get; init; }

    internal PostProcessSteps ToPostProcessFlags()
    {
        PostProcessSteps flags = 0;

        if ( Triangulate )
        {
            flags |= PostProcessSteps.Triangulate;
        }

        if ( FlipUvCoordinates )
        {
            flags |= PostProcessSteps.FlipUVs;
        }

        if ( GenerateSmoothNormals )
        {
            flags |= PostProcessSteps.GenerateSmoothNormals;
        }

        if ( CalculateTangentSpace )
        {
            flags |= PostProcessSteps.CalculateTangentSpace;
        }

        // Joining is applied in ModelLoader after triangulation and attribute extraction.
        // Assimp's join step can corrupt some OBJ n-gons/quads before triangulation.

        if ( ImproveCacheLocality )
        {
            flags |= PostProcessSteps.ImproveCacheLocality;
        }

        if ( FindInvalidData )
        {
            flags |= PostProcessSteps.FindInvalidData;
        }

        if ( ValidateDataStructure )
        {
            flags |= PostProcessSteps.ValidateDataStructure;
        }

        return flags;
    }
}
