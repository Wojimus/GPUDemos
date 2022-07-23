using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static ComputeStructs;

public class World : MonoBehaviour
{
    #region Public Variables

    [Header("Map")]
    public MapGenerator MapGen;
    public int2 MapSize;
    public string Seed;
    public BiomeAttributes Biome;
    public Transform PlayerCamera;

    [Header("Materials")]
    public Material VoxelMaterial;
    public Material VoxelMaterialTransparent;
    public Material GrassMaterial;

    [Header("Compute Shaders")]
    public ComputeShader MeshChunkCS;
    public ComputeShader CreateGrassTrianglesCS;

    #endregion

    #region Private Variables

    //Chunks
    private Dictionary<int2, Chunk> _chunks = new Dictionary<int2, Chunk>();

    //Debugging Information
    private int _textureArraySize;

    #endregion

    #region Unity Functions

    private void Start()
    {
        //Material Generation
        GenerateVoxelMaterial();
        
        //Map Generation
        GenerateMap();
    }

    private void Update()
    {
        //Run Chunks Update Loop
        foreach (KeyValuePair<int2,Chunk> chunk in _chunks)
        {
            chunk.Value.Update();
        }
        
        //Get Chunks Tri Counters
        int grassAmount = 0;
        int voxelAmount = 0;
        foreach (KeyValuePair<int2,Chunk> chunk in _chunks)
        {
            grassAmount += chunk.Value.GrassTriangleCount;
            voxelAmount += chunk.Value.VoxelTriangleCount;
        }
    }

    private void OnDestroy()
    {
        //Clean Up Chunks
        foreach (KeyValuePair<int2, Chunk> chunk in _chunks)
        {
            chunk.Value.CleanUp();
        }
    }

    #endregion

    #region Map Generation

    private void GenerateMap()
    {
        //Create Map Data
        MapData mapData = new MapData
        {
            MapSize = new int2(MapSize.x, MapSize.y),
            Seed = Seed.GetHashCode(),
            Biome = Biome
        };
        
        //Generate Map
        int[] map = MapGen.GenerateMap(mapData);

        //Split Into Chunks
        GenerateChunks(map);
    }

    public void ForceGenerate()
    {
        //Clean Up Chunks
        foreach (KeyValuePair<int2,Chunk> chunk in _chunks)
        {
            chunk.Value.CleanUp();
            Destroy(chunk.Value.GetGameObject());
        }
        
        GenerateMap();
    }

    private void GenerateChunks(int[] map)
    {
        //Clear Chunks
        _chunks = new Dictionary<int2, Chunk>();
        
        //For Each Chunk
        List<int2> chunksToMesh = new List<int2>();
        for (int i = 0; i < MapSize.x; i++)
        {
            for (int j = 0; j < MapSize.y; j++)
            {
                //ChunkCoord
                int2 chunkCoord = new int2(i, j);
                
                //Create Chunk
                Chunk chunk = new Chunk(this, chunkCoord);
                
                //Add To Chunks
                _chunks.Add(chunkCoord, chunk);
                
                //Get Chunks Voxel Map
                NativeArray<int> voxelMap = chunk.VoxelMap;
                
                //Voxel Counts
                int[] voxelCounts = new int[2];
                
                //For Each Voxel
                for (int x = 0; x < WorldInfo.ChunkSize.x; x++)
                {
                    for (int y = 0; y < WorldInfo.ChunkSize.y; y++)
                    {
                        for (int z = 0; z < WorldInfo.ChunkSize.z; z++)
                        {
                            //Get Region Pos
                            int3 regionPos = new int3(x + WorldInfo.ChunkSize.x * i, y, z + WorldInfo.ChunkSize.z * j);
                            
                            //Get Region Index
                            int regionIndex = JobUtilities.ConvertTo1DIndex(regionPos.x, regionPos.y, regionPos.z,
                                MapSize.x * WorldInfo.ChunkSize.x, WorldInfo.ChunkSize.y);
                            
                            //Get Chunk Index
                            int chunkIndex = JobUtilities.ConvertTo1DIndex(x, y, z, WorldInfo.ChunkSize.x,
                                WorldInfo.ChunkSize.y);
                            
                            //Get Voxel ID
                            int voxelID = map[regionIndex];
                            
                            //Set Data
                            voxelMap[chunkIndex] = voxelID;
                            
                            //Increment Voxel Counts
                            if (voxelID != 0)
                            {
                                if (Blocks.TransparentIDs.Contains(voxelID)) voxelCounts[1]++;
                                else voxelCounts[0]++;
                            }
                        }
                    }
                }
                
                //Fill Grass Map
                chunk.FillGrassMap();
                
                //Update Grass Buffer
                chunk.UpdateGrassBuffer();

                //Set Voxel Counts
                chunk.SetVoxelCounts(voxelCounts[0], voxelCounts[1]);
                
                //Add To Mesh List
                chunksToMesh.Add(chunkCoord);
            }
        }

        foreach (int2 chunkCoord in chunksToMesh)
        {
            //Mesh Chunks
            MeshChunk(chunkCoord);
        }
    }

    #endregion

    #region Mesh Chunks
    private void MeshChunk(int2 chunkCoord)
    {
        //Timer
        //Stopwatch chunkTimer = Stopwatch.StartNew();
        
        //Get Chunk
        Chunk chunk = _chunks[chunkCoord];
        
        //Get Voxel Count
        int[] voxelCount = chunk.VoxelCounts;

        //Determine Mesh Validity
        bool opaqueMeshValid = true;
        bool transparentMeshValid = true;
        if (voxelCount[0] == 0) opaqueMeshValid = false;
        if (voxelCount[1] == 0) transparentMeshValid = false;
        if (!opaqueMeshValid && !transparentMeshValid) return;
        
        #region Mesh Triangles

        //Get VoxelMap
        NativeArray<int> voxelMap = chunk.VoxelMap;
        
        //Get ChunkPos
        int3 chunkPos = chunk.ChunkPos;

        #region Calculate Active Faces

        //Get Kernel Index
        int activeFaceKernelIndex = MeshChunkCS.FindKernel("CSMain");

        //Create Buffers
        ComputeBuffer transparentIDBuffer = new ComputeBuffer(Blocks.TransparentIDs.Count, sizeof(int));
        ComputeBuffer customMeshIDBuffer = new ComputeBuffer(Blocks.CustomMeshIDs.Count, sizeof(int));
        ComputeBuffer textureIDBuffer = new ComputeBuffer(Blocks.TextureIndexes.Count, sizeof(int));
        ComputeBuffer voxelMapBuffer = new ComputeBuffer(voxelMap.Length, sizeof(int));
        ComputeBuffer voxelMapFrontBuffer = new ComputeBuffer(voxelMap.Length, sizeof(int));
        ComputeBuffer voxelMapBackBuffer = new ComputeBuffer(voxelMap.Length, sizeof(int));
        ComputeBuffer voxelMapLeftBuffer = new ComputeBuffer(voxelMap.Length, sizeof(int));
        ComputeBuffer voxelMapRightBuffer = new ComputeBuffer(voxelMap.Length, sizeof(int));
        ComputeBuffer opaqueTriangleBuffer = voxelCount[0] != 0
            ? new ComputeBuffer(voxelCount[0] * 12, 80, ComputeBufferType.Append)
            : new ComputeBuffer(1, 80, ComputeBufferType.Append);
        ComputeBuffer transparentTriangleBuffer = voxelCount[1] != 0
            ? new ComputeBuffer(voxelCount[1] * 12, 80, ComputeBufferType.Append)
            : new ComputeBuffer(1, 80, ComputeBufferType.Append);

        //Reset Counters
        opaqueTriangleBuffer.SetCounterValue(0);
        transparentTriangleBuffer.SetCounterValue(0);

        //Fill Buffers
        transparentIDBuffer.SetData(Blocks.TransparentIDs);
        customMeshIDBuffer.SetData(Blocks.CustomMeshIDs);
        textureIDBuffer.SetData(Blocks.TextureIndexes);
        voxelMapBuffer.SetData(voxelMap);
        
        //Adjacent Voxel Maps
        Chunk frontChunk = GetChunkAtCoord(chunkCoord + new int2(0, -1));
        if (frontChunk != null) voxelMapFrontBuffer.SetData(frontChunk.VoxelMap);
        else voxelMapFrontBuffer.SetData(new int[voxelMap.Length]);
        
        Chunk backChunk = GetChunkAtCoord(chunkCoord + new int2(0, 1));
        if (backChunk != null) voxelMapBackBuffer.SetData(backChunk.VoxelMap);
        else voxelMapBackBuffer.SetData(new int[voxelMap.Length]);
        
        Chunk leftChunk = GetChunkAtCoord(chunkCoord + new int2(-1, 0));
        if (leftChunk != null) voxelMapLeftBuffer.SetData(leftChunk.VoxelMap);
        else voxelMapLeftBuffer.SetData(new int[voxelMap.Length]);
        
        Chunk rightChunk = GetChunkAtCoord(chunkCoord + new int2(1, 0));
        if (rightChunk != null) voxelMapRightBuffer.SetData(rightChunk.VoxelMap);
        else voxelMapRightBuffer.SetData(new int[voxelMap.Length]);
        

        //Set Values
        MeshChunkCS.SetInts("chunkPos", chunkPos.x, chunkPos.y, chunkPos.z);
        MeshChunkCS.SetInts("chunkSize", WorldInfo.ChunkSize.x, WorldInfo.ChunkSize.y, WorldInfo.ChunkSize.z);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "transparentIDs", transparentIDBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "customMeshIDs", customMeshIDBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "textureIndexes", textureIDBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "voxelMap", voxelMapBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "voxelMapFront", voxelMapFrontBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "voxelMapBack", voxelMapBackBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "voxelMapLeft", voxelMapLeftBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "voxelMapRight", voxelMapRightBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "opaqueTriangles", opaqueTriangleBuffer);
        MeshChunkCS.SetBuffer(activeFaceKernelIndex, "transparentTriangles", transparentTriangleBuffer);

        MeshChunkCS.Dispatch(activeFaceKernelIndex, Mathf.CeilToInt(WorldInfo.ChunkSize.x / 8f),
            Mathf.CeilToInt(WorldInfo.ChunkSize.y / 4f), Mathf.CeilToInt(WorldInfo.ChunkSize.z / 8f));

        //Copy Counts
        ComputeBuffer triangleCountsBuffer = new ComputeBuffer(2, sizeof(int), ComputeBufferType.IndirectArguments);
        ComputeBuffer.CopyCount(opaqueTriangleBuffer, triangleCountsBuffer, 0);
        ComputeBuffer.CopyCount(transparentTriangleBuffer, triangleCountsBuffer, sizeof(int));
        
        //Get Counts
        int[] triangleCounts = new int[2];
        triangleCountsBuffer.GetData(triangleCounts);

        //Get Triangles
        ChunkTriangle[] opaqueTriangles = new ChunkTriangle[triangleCounts[0]];
        opaqueTriangleBuffer.GetData(opaqueTriangles, 0, 0, triangleCounts[0]);
        ChunkTriangle[] transparentTriangles = new ChunkTriangle[triangleCounts[1]];
        transparentTriangleBuffer.GetData(transparentTriangles, 0, 0, triangleCounts[1]);

        //Create Mesh And Set Mesh
        Mesh mesh = chunk.CreateMesh(opaqueTriangles, transparentTriangles);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        chunk.SetMesh(mesh);

        #endregion

        //Release Buffers
        transparentIDBuffer.Release();
        customMeshIDBuffer.Release();
        textureIDBuffer.Release();
        voxelMapBuffer.Release();
        voxelMapFrontBuffer.Release();
        voxelMapBackBuffer.Release();
        voxelMapLeftBuffer.Release();
        voxelMapRightBuffer.Release();
        opaqueTriangleBuffer.Release();
        transparentTriangleBuffer.Release();
        triangleCountsBuffer.Release();
        
        //Timer
        //Logging.Print($"Chunk Meshing Took: {chunkTimer.ElapsedMilliseconds}ms");

        #endregion
    }

    #endregion
    
    #region Material Generation

    private void GenerateVoxelMaterial()
    {
        //Material Generation
        TextureArrayGeneration textureArrayGen = new TextureArrayGeneration();
        Texture2DArray[] textureArrays = textureArrayGen.CreateCombinedArrays();

        //Set Material Textures
        VoxelMaterial.SetTexture("_DiffuseArray", textureArrays[0]);
        VoxelMaterial.SetTexture("_NormalArray", textureArrays[1]);
        VoxelMaterial.SetTexture("_MetallicArray", textureArrays[2]);
        VoxelMaterial.SetTexture("_AOArray", textureArrays[3]);

        VoxelMaterialTransparent.SetTexture("_DiffuseArray", textureArrays[0]);
        VoxelMaterialTransparent.SetTexture("_NormalArray", textureArrays[1]);
        VoxelMaterialTransparent.SetTexture("_MetallicArray", textureArrays[2]);
        VoxelMaterialTransparent.SetTexture("_AOArray", textureArrays[3]);
        VoxelMaterialTransparent.SetTexture("_OpacityArray", textureArrays[4]);

        _textureArraySize = textureArrays[0].depth;
    }

    #endregion

    #region Utility Functions

    public GameObject InstantiateObject(GameObject gameObject)
    {
        return Instantiate(gameObject);
    }

    private Chunk GetChunkAtCoord(int2 chunkCoord)
    {
        if (!_chunks.ContainsKey(chunkCoord)) return null;

        return _chunks[chunkCoord];
    }

    public void UpdateGrassBuffers()
    {
        foreach (KeyValuePair<int2,Chunk> chunk in _chunks)
        {
            chunk.Value.UpdateGrassBuffer();
        }
    }

    #endregion
    
}
