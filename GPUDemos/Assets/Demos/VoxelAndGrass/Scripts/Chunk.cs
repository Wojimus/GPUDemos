using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static ComputeStructs;

public class Chunk
{
    #region Private Variables
    
    //World Reference
    private World _world;

    //Voxel Map
    private NativeArray<int> _voxelMap;
    private int[] _voxelCounts;
    
    //Grass
    private List<int3> _grassMap = new List<int3>();
    private ComputeBuffer _grassTriangles;
    private ComputeBuffer _grassArgs;
    private int _grassLODMultiplier;
    
    //Crops
    private Dictionary<int3, int> _cropMap = new Dictionary<int3, int>();

    //Position
    private int2 _chunkCoord;
    private int3 _chunkPos;
    private Bounds _bounds;
    
    //Object And Components
    private GameObject _chunkObject;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    //DEBUG
    public int GrassTriangleCount = 0;
    public int VoxelTriangleCount = 0;

    #endregion

    #region Constructor

    public Chunk(World world, int2 chunkCoord)
    {
        //Set Properties
        _world = world;
        _chunkCoord = chunkCoord;
        _chunkPos = new int3(chunkCoord.x * WorldInfo.ChunkSize.x, 0, chunkCoord.y * WorldInfo.ChunkSize.z);
        
        //Bounds
        Vector3 boundSize = new Vector3(WorldInfo.ChunkSize.x / 2f, WorldInfo.ChunkSize.y / 2f,
            WorldInfo.ChunkSize.z / 2f);
        Vector3 boundCenter =
            new Vector3(_chunkPos.x + boundSize.x, _chunkPos.y + boundSize.y, _chunkPos.z + boundSize.z);
        _bounds = new Bounds(boundCenter, boundSize * 2f);
        
        //Create VoxelMap
        _voxelMap = new NativeArray<int>(WorldInfo.ChunkSizeTotal, Allocator.Persistent);
        _voxelCounts = new int[2];
        
        //Create Object And Components
        _chunkObject = new GameObject();
        _chunkObject.transform.parent = _world.transform;
        _chunkObject.name = $"Chunk: ({_chunkCoord.x}, {_chunkCoord.y})";
        _meshFilter = _chunkObject.AddComponent<MeshFilter>();
        _meshRenderer = _chunkObject.AddComponent<MeshRenderer>();
        
        //Grass
        _grassArgs = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
    }

    #endregion

    #region Get/Set

    public NativeArray<int> VoxelMap => _voxelMap;

    public int[] VoxelCounts => _voxelCounts;

    public void SetVoxelCounts(int opaqueCount, int transparentCount)
    {
        _voxelCounts[0] = opaqueCount;
        _voxelCounts[1] = transparentCount;
    }

    public int3 ChunkPos => _chunkPos;

    public Bounds Bounds => _bounds;

    #endregion

    #region Update Loop

    public void Update()
    {
        CalculateGrassLOD();
        DisplayGrass();
    }

    #endregion

    #region Meshing

    public void SetMesh(Mesh mesh)
    {
        _meshFilter.mesh = mesh;
        _meshRenderer.materials = new[] {_world.VoxelMaterial, _world.VoxelMaterialTransparent};
    }

    public Mesh CreateMesh(ChunkTriangle[] opaqueTrianglesRaw, ChunkTriangle[] transparentTrianglesRaw)
    {
        //Create Lists
        List<Vector3> vertices = new List<Vector3>();
        List<int> opaqueTriangles = new List<int>();
        List<int> transparentTriangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector2> uv4 = new List<Vector2>();
        
        //Opaque Triangles
        foreach (ChunkTriangle shaderTriangle in opaqueTrianglesRaw)
        {
            //Get Vertex Count
            int vertexCount = vertices.Count;
            
            //Add Vertices
            vertices.Add(shaderTriangle.vert1.position);
            vertices.Add(shaderTriangle.vert2.position);
            vertices.Add(shaderTriangle.vert3.position);
            
            //Add Triangles
            opaqueTriangles.AddRange(new []{vertexCount, vertexCount + 1, vertexCount + 2});
            
            //Add UV
            uv.Add(shaderTriangle.vert1.uv);
            uv.Add(shaderTriangle.vert2.uv);
            uv.Add(shaderTriangle.vert3.uv);
            
            //Add UV4
            uv4.Add(shaderTriangle.uv4);
            uv4.Add(shaderTriangle.uv4);
            uv4.Add(shaderTriangle.uv4);
        }
        
        //Transparent Triangles
        foreach (ChunkTriangle shaderTriangle in transparentTrianglesRaw)
        {
            //Get Vertex Count
            int vertexCount = vertices.Count;
            
            //Add Vertices
            vertices.Add(shaderTriangle.vert1.position);
            vertices.Add(shaderTriangle.vert2.position);
            vertices.Add(shaderTriangle.vert3.position);
            
            //Add Triangles
            transparentTriangles.AddRange(new []{vertexCount, vertexCount + 1, vertexCount + 2});
            
            //Add UV
            uv.Add(shaderTriangle.vert1.uv);
            uv.Add(shaderTriangle.vert2.uv);
            uv.Add(shaderTriangle.vert3.uv);
            
            //Add UV4
            uv4.Add(shaderTriangle.uv4);
            uv4.Add(shaderTriangle.uv4);
            uv4.Add(shaderTriangle.uv4);
        }
        
        //Create Mesh
        Mesh mesh = new Mesh();
        mesh.subMeshCount = 2;
        mesh.SetVertices(vertices.ToArray());
        mesh.SetTriangles(opaqueTriangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.SetUVs(0, uv.ToArray());
        mesh.SetUVs(3, uv4.ToArray());

        VoxelTriangleCount = opaqueTriangles.Count;// + transparentTriangles.Count;

        return mesh;
    }

    #endregion

    #region Grass

    private void DisplayGrass()
    {
        if (_grassTriangles != null)
        {
            //Get Material
            Material grassMat = _world.GrassMaterial;
            
            //Create Property Block
            MaterialPropertyBlock prop = new MaterialPropertyBlock();
            prop.SetBuffer("triangles", _grassTriangles);

            //Set LOD
            _grassArgs.SetData(new []{3, _grassMap.Count * WorldInfo.GrassPerTile / _grassLODMultiplier, 0, 0});
            prop.SetInt("LODMultiplier", _grassLODMultiplier);

            //Draw
            Graphics.DrawProceduralIndirect(grassMat, _bounds, MeshTopology.Triangles, _grassArgs, properties: prop);
        }
    }

    private void CalculateGrassLOD()
    {
        //Get Distance From Player Camera
        //float playerDistance = Vector3.Distance(new Vector3(_chunkPos.x, _chunkPos.y, _chunkPos.z), _world.PlayerCamera.position);
        float playerDistance = _world.PlayerCamera.transform.position.y;
        
        //Max LOD
        if (playerDistance <= WorldInfo.GrassLODMaxMin.x) _grassLODMultiplier = 1;
        //Min LOD
        else if (playerDistance > WorldInfo.GrassLODMaxMin.y) _grassLODMultiplier = WorldInfo.GrassLODMaxMultiplier;
        //Calculate
        else
        {
            //Zero Distance
            float zeroDistance = playerDistance - WorldInfo.GrassLODMaxMin.x;

            //Calculate LOD
            float lodStep = (WorldInfo.GrassLODMaxMin.y - WorldInfo.GrassLODMaxMin.x) / WorldInfo.GrassLODMaxMultiplier;
            float lod = zeroDistance / lodStep;

            //Bias
            if (lod <= WorldInfo.GrassLODMaxMultiplier * WorldInfo.GrassLODMaxBias.x)
            {
                //lod *= WorldInfo.GrassLODMaxBias.y;
                lod *= Mathf.SmoothStep(WorldInfo.GrassLODMaxBias.y, 1, zeroDistance / WorldInfo.GrassLODMaxMin.y);
            }

            //Set LOD
            _grassLODMultiplier = Mathf.CeilToInt(lod);
        }
    }

    public void FillGrassMap()
    {
        for (int i = 0; i < _voxelMap.Length; i++)
        {
            if (_voxelMap[i] == Blocks.BlockTypesKeyLookup["Grass Foliage"])
            {
                //Get Position
                int3 pos = JobUtilities.ConvertTo3DIndex(i, WorldInfo.ChunkSize.x, WorldInfo.ChunkSize.y);
                
                if (!_grassMap.Contains(pos)) _grassMap.Add(pos);
            }
        }
    }

    public void UpdateGrassBuffer()
    {
        //0 Check
        if (_grassMap.Count == 0)
        {
            _grassTriangles?.Release();
            return;
        }

        //Get Compute Shader
        ComputeShader cs = _world.CreateGrassTrianglesCS;
        
        //Get Kernel Index
        int kernelIndex = cs.FindKernel("CSMain");
        
        //Create Grass Positions
        ComputeBuffer grassPositions = new ComputeBuffer(_grassMap.Count, sizeof(int) * 3);
        grassPositions.SetData(_grassMap.ToArray());
        
        //Create Grass Triangles Buffer
        int triangleCount = _grassMap.Count * WorldInfo.GrassPerTile;
        
        //DEBUG
        GrassTriangleCount = triangleCount;
        
        //Debug.Log($"{triangleCount} - {_grassMap.Count}");
        _grassTriangles?.Release();
        _grassTriangles = new ComputeBuffer(triangleCount, 60, ComputeBufferType.Append);
        _grassTriangles.SetCounterValue(0);
        
        //Set Data
        cs.SetInt("triangleCount", triangleCount);
        cs.SetInt("grassPerTile", WorldInfo.GrassPerTile);
        cs.SetFloat("grassWidth", WorldInfo.GrassWidth);
        cs.SetFloat("grassHeight", WorldInfo.GrassHeight);
        cs.SetFloat("grassCurveMultiplier", WorldInfo.GrassCurveMultiplier);
        cs.SetFloats("chunkPos", _chunkPos.x, _chunkPos.y, _chunkPos.z);
        cs.SetBuffer(kernelIndex, "grassPositions", grassPositions);
        cs.SetBuffer(kernelIndex, "triangles", _grassTriangles);
        
        //Dispatch Shader
        cs.Dispatch(kernelIndex, Mathf.CeilToInt(triangleCount / 128f), 1, 1);
        
        //Dispose Of Buffers
        grassPositions.Release();
        
        //Update Args
        _grassArgs.SetData(new []{3, triangleCount, 0, 0});
    }

    #endregion

    #region Utility Functions

    public void CleanUp()
    {
        _voxelMap.Dispose();
        _grassTriangles?.Release();
        _grassArgs?.Release();
    }

    public GameObject GetGameObject()
    {
        return _chunkObject;
    }

    #endregion
}
