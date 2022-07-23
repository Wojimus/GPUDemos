using Unity.Mathematics;

public static class WorldInfo
{
    //Chunks
    /*
     * Chunks have 4 Y Levels
     * 0 - Under Ground
     * 1 - Ground
     * 2 - Above Ground
     * 3 - Sky
     */
    public static readonly int3 ChunkSize = new int3(16, 4, 16);
    public static readonly int ChunkSizeTotal = ChunkSize.x * ChunkSize.y * ChunkSize.z;
    
    //Grass
    public static int GrassPerTile = 1024;
    public static float GrassHeight = 0.2f;
    public static float GrassWidth = 0.025f;
    public static float GrassCurveMultiplier = 0.2f;
    public static float2 GrassLODMaxMin = new float2(30f, 200f);
    public static int GrassLODMaxMultiplier = 64;
    public static float2 GrassLODMaxBias = new float2(0.8f, 0.12f);
}
