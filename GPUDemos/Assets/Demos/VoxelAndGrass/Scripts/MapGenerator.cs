using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

public class MapGenerator : MonoBehaviour
{
    #region Public Variables

    [Header("Compute Shaders")] 
    public ComputeShader GenerateMapCS;

    #endregion

    #region Map Generation

    public int[] GenerateMap(MapData mapData)
    {
        //Get Kernel
        int kernelID = GenerateMapCS.FindKernel("CSMain");

        //Calculate Size
        int3 mapSize = new int3(mapData.MapSize.x * WorldInfo.ChunkSize.x, WorldInfo.ChunkSize.y,
            mapData.MapSize.y * WorldInfo.ChunkSize.z);
        int totalMapSize = WorldInfo.ChunkSizeTotal * mapData.MapSize.x * mapData.MapSize.y;
        
        //DEBUG
        Debug.Log($"Generating {totalMapSize} Voxel map");
        
        //Create Compute Buffer
        ComputeBuffer voxelMap = new ComputeBuffer(totalMapSize, sizeof(int));
        
        //Create Noise Offset Based On Seed
        Random randomGen = new Random(mapData.Seed);
        int xOffset = randomGen.Next(1000000);
        int yOffset = randomGen.Next(1000000);
        
        //Set Values
        //Details
        GenerateMapCS.SetBuffer(kernelID, "voxelMap", voxelMap);
        GenerateMapCS.SetInts("mapSize", mapSize.x, mapSize.y, mapSize.z);
        GenerateMapCS.SetFloats("mapOffset", xOffset, yOffset);
        //Ground
        GenerateMapCS.SetInt("groundBlock", mapData.Biome.GroundBlock);
        GenerateMapCS.SetInt("secondaryGroundBlock", mapData.Biome.SecondaryGroundBlock);
        GenerateMapCS.SetFloat("secondaryGroundThreshold", mapData.Biome.SecondaryGroundThreshold);
        GenerateMapCS.SetFloat("secondaryGroundScale", mapData.Biome.SecondaryGroundScale);
        GenerateMapCS.SetInts("secondaryGroundOffset", mapData.Biome.SecondaryGroundOffset.x, mapData.Biome.SecondaryGroundOffset.y);
        GenerateMapCS.SetInt("undergroundBlock", mapData.Biome.UndergroundBlock);
        //Rocks
        GenerateMapCS.SetInts("rockTypes", mapData.Biome.RockTypes.x, mapData.Biome.RockTypes.y, mapData.Biome.RockTypes.z);
        GenerateMapCS.SetFloat("rockThreshold", mapData.Biome.RockThreshold);
        GenerateMapCS.SetFloat("rockScale", mapData.Biome.RockScale);
        GenerateMapCS.SetInts("rockOffset", mapData.Biome.RockOffset.x, mapData.Biome.RockOffset.y);

        //Dispatch Shader
        GenerateMapCS.Dispatch(kernelID, Mathf.CeilToInt(mapSize.x / 8f), Mathf.CeilToInt(mapSize.y / 4f), Mathf.CeilToInt(mapSize.z / 8f));
        
        //Get Data
        int[] extractedMap = new int[totalMapSize];
        voxelMap.GetData(extractedMap);
        
        //Dispose Of Buffers
        voxelMap.Dispose();

        return extractedMap;
    } 

    #endregion
}

public struct MapData
{
    //Details
    public int Seed;
    public int2 MapSize;
    //Biome
    public BiomeAttributes Biome;
}
