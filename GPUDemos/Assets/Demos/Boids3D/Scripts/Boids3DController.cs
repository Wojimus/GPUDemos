using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class Boids3DController : MonoBehaviour
{
    //CONSTANTS
    private const int BoidStructSize = 36;
    private const int BoidVertexSize = 24;
    
    //Public References
    [Header("Compute Shaders")]
    public ComputeShader BoidSpawnerCS;
    public ComputeShader BoidBehaviourCS;
    public ComputeShader TransferBuffersCS;
    [Header("Field Parameters")] 
    public float FieldSize;
    public GameObject FieldObject;
    [Header("Boid Parameters")] 
    public Material BoidMaterial;
    public Mesh BoidMesh;
    public int BoidAmount;
    [Range(0.1f, 3)]
    public float BoidSize = 1;
    [Range(1, 10)]
    public int BoidViewRadius = 1;
    [Range(1, 10)]
    public int BoidAvoidRadius = 1;
    [Range(0f, 1f)]
    public float Alignment = 0.5f;
    [Range(0f, 1f)]
    public float Cohesion = 0.5f;
    [Range(0f, 1f)]
    public float Separation = 0.5f;
    [Range(0f, 0.1f)]
    public float CentrePull = 0.05f;
    [Range(2f, 50f)]
    public float Speed = 5f;

    //Private References
    private Bounds _bounds;
    private MaterialPropertyBlock _prop;
    private ComputeBuffer _boidsBuffer;
    private ComputeBuffer _boidVertices;
    private ComputeBuffer _boidArgs;
    private RenderTexture _velocityLookup;
    private RenderTexture _velocityLookupBuffer;

    #region Unity Functions

    private void Start()
    {
        //Turn Off Vsync
        QualitySettings.vSyncCount = 0;
        
        //Boid Mesh
        LoadBoidMesh();
        
        //Field
        ResizeField();

        //Spawn Boids
        BoidAmount = 1000;
        SpawnBoids();
    }

    private void Update()
    {
        BoidBehaviour();
        TransferBuffers();
        DrawBoids();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    #endregion
    
    #region Field

    private void ResizeField()
    {
        //Scale
        FieldObject.transform.localScale = new Vector3(FieldSize, FieldSize, FieldSize);
        
        //Position
        Vector3 fieldPos = new Vector3
        {
            x = FieldSize / 2, 
            y = FieldSize / 2, 
            z = FieldSize / 2
        };
        FieldObject.transform.localPosition = fieldPos;
        
        //Set Bounds
        _bounds = new Bounds
        {
            center = fieldPos,
            size = new Vector3(FieldSize, FieldSize, FieldSize)
        };
    }

    #endregion

    #region Spawn Boids

    private int[] GenerateSeeds()
    {
        //Create Seeds For Boid Spawning
        int[] seeds = new int[BoidAmount];
        for (int i = 0; i < BoidAmount; i++)
        {
            seeds[i] = (int) (Random.value * int.MaxValue);
        }
        
        //Return
        return seeds;
    }

    private void SpawnBoids()
    {
        //Create Textures Set To 3D And Enable Random Write
        _velocityLookup = new RenderTexture((int)FieldSize, (int)FieldSize, 0);
        _velocityLookupBuffer = new RenderTexture((int)FieldSize, (int)FieldSize, 0);
        _velocityLookup.dimension = TextureDimension.Tex3D;
        _velocityLookupBuffer.dimension = TextureDimension.Tex3D;
        _velocityLookup.volumeDepth = (int) FieldSize;
        _velocityLookupBuffer.volumeDepth = (int) FieldSize;
        _velocityLookup.enableRandomWrite = true;
        _velocityLookupBuffer.enableRandomWrite = true;

        //Generate Seeds
        int[] seeds = GenerateSeeds();
        ComputeBuffer seedsBuffer = new ComputeBuffer(seeds.Length, sizeof(int));
        seedsBuffer.SetData(seeds);
        
        //Create Boids Buffer
        _boidsBuffer?.Release();
        _boidsBuffer = new ComputeBuffer(BoidAmount, BoidStructSize);
        
        //Empty Buffer (So We Don't Get Random Values Which Can Sometimes Happen)
        Boid3D[] boids = new Boid3D[BoidAmount];
        _boidsBuffer.SetData(boids);

        //Get Kernel Index
        int kernelIndex = BoidSpawnerCS.FindKernel("CSMain");
        
        //Set Boid Spawner Parameters
        BoidSpawnerCS.SetBuffer(kernelIndex, "Boids", _boidsBuffer);
        BoidSpawnerCS.SetBuffer(kernelIndex, "Seeds", seedsBuffer);
        BoidSpawnerCS.SetTexture(kernelIndex, "VelocityTexture", _velocityLookup);
        BoidSpawnerCS.SetFloat("FieldSize", FieldSize);
        
        //Calculate Thread Groups
        int threadGroups = Mathf.CeilToInt((float)BoidAmount / 1024);
        
        //Dispatch Shader
        BoidSpawnerCS.Dispatch(kernelIndex, threadGroups, 1, 1);
        
        //Release Seeds
        seedsBuffer.Release();

        //Boid Shader Values
        _prop = new MaterialPropertyBlock();
        _prop.SetBuffer("boidVertices", _boidVertices);
        _prop.SetBuffer("boids", _boidsBuffer);
    }

    public void RespawnBoids(int boidAmount)
    {
        //Update Boid Amount
        BoidAmount = boidAmount;

        //Release Textures
        _velocityLookup.Release();
        _velocityLookupBuffer.Release();
        
        //Release Buffers
        _boidsBuffer.Release();
        
        //Reload Boid Mesh
        LoadBoidMesh();

        SpawnBoids();
    }

    #endregion

    #region Boid Behaviour

    private void BoidBehaviour()
    {
        //Get Kernel Index
        int kernelIndex = BoidBehaviourCS.FindKernel("CSMain");
        
        //Set Parameters
        BoidBehaviourCS.SetBuffer(kernelIndex, "Boids", _boidsBuffer);
        BoidBehaviourCS.SetTexture(kernelIndex, "VelocityTexture", _velocityLookup);
        BoidBehaviourCS.SetTexture(kernelIndex, "VelocityTextureBuffer", _velocityLookupBuffer);
        BoidBehaviourCS.SetFloat("FieldSize", FieldSize);
        BoidBehaviourCS.SetInt("ViewRadius", BoidViewRadius);
        BoidBehaviourCS.SetInt("AvoidRadius", BoidAvoidRadius);
        BoidBehaviourCS.SetFloat("Alignment", Alignment);
        BoidBehaviourCS.SetFloat("Cohesion", Cohesion);
        BoidBehaviourCS.SetFloat("Seperation", Separation);
        BoidBehaviourCS.SetFloat("CentrePull", CentrePull);
        BoidBehaviourCS.SetFloat("TimeDelta", Time.deltaTime);
        BoidBehaviourCS.SetFloat("Speed", Speed);

        //Calculate Thread Groups
        int threadGroups = Mathf.CeilToInt((float)BoidAmount / 1024);
        
        //Dispatch Shader
        BoidBehaviourCS.Dispatch(kernelIndex, threadGroups, 1, 1);
    }

    private void TransferBuffers()
    {
        //Get Kernel Index
        int kernelIndex = TransferBuffersCS.FindKernel("CSMain");
        
        //Set Parameters
        TransferBuffersCS.SetTexture(kernelIndex, "VelocityTexture", _velocityLookup);
        TransferBuffersCS.SetTexture(kernelIndex, "VelocityTextureBuffer", _velocityLookupBuffer);
        
        //Calculate Thread Groups
        int3 threadGroups = new int3(
            Mathf.CeilToInt(_velocityLookup.width / 10f),
            Mathf.CeilToInt(_velocityLookup.height / 10f),
            Mathf.CeilToInt(_velocityLookup.volumeDepth / 10f)
        );

        //Dispatch Shader
        TransferBuffersCS.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, threadGroups.z);
    }

    #endregion

    #region DrawBoids
    
    private void LoadBoidMesh()
    {
        //Create Array - For each triangle add 3 vertices
        ShaderVertex[] boidVertices = new ShaderVertex[BoidMesh.triangles.Length * 3];

        //Fill Array
        for (int t = 0; t < Mathf.Floor(BoidMesh.triangles.Length / 3f); t++)
        {
            for (int v = 0; v < 3; v++)
            {
                //Get Vertex Index
                int vertexIndex = BoidMesh.triangles[t * 3 + v];

                //Get Data
                ShaderVertex shaderVertex = new ShaderVertex(BoidMesh.vertices[vertexIndex], BoidMesh.normals[vertexIndex]);

                //Set Data
                boidVertices[t * 3 + v] = shaderVertex;
            }
        }
        
        //Create Compute Buffer And Set Data
        _boidVertices?.Release();
        _boidVertices = new ComputeBuffer(boidVertices.Length, BoidVertexSize);
        _boidVertices.SetData(boidVertices);
        
        //Boid Args
        _boidArgs?.Release();
        _boidArgs = new ComputeBuffer(4, sizeof(int), ComputeBufferType.IndirectArguments);
        _boidArgs.SetData(new []
        {
            _boidVertices.count,
            BoidAmount,
            0,
            0
        });
    }

    private void DrawBoids()
    {
        if (_boidsBuffer != null && _boidVertices != null)
        {
            //Get Material
            Material boidMat = BoidMaterial;
            
            //Boid Size
            _prop.SetFloat("boidSize", BoidSize);

            //Draw
            Graphics.DrawProceduralIndirect(boidMat, _bounds, MeshTopology.Triangles, _boidArgs, properties: _prop);
        }
    }

    #endregion

    #region Utility

    private void CleanUp()
    {
        _boidsBuffer?.Release();
        _boidVertices?.Release();
        _boidArgs?.Release();
        _velocityLookup.Release();
        _velocityLookupBuffer.Release();
    }

    public void SetValues(SimulationValues values)
    {
        //Extract And Set Values
        BoidSize = values.BoidSize;
        BoidViewRadius = values.ViewRadius;
        BoidAvoidRadius = values.AvoidRadius;
        Alignment = values.Alignment;
        Cohesion = values.Cohesion;
        Separation = values.Separation;
        CentrePull = values.CentrePull;
        Speed = values.Speed;
    }

    #endregion

    #region Structs

    private struct Boid3D
    {
        public float3 Position;
        public float3 Direction;
        public float3 Velocity;

        public Boid3D(float3 position, float3 direction, float3 velocity)
        {
            Position = position;
            Direction = direction;
            Velocity = velocity;
        }
    }

    private struct ShaderVertex
    {
        public float3 Position;
        public float3 Normal;

        public ShaderVertex(float3 position, float3 normal)
        {
            Position = position;
            Normal = normal;
        }
    }

    public struct SimulationValues
    {
        public float BoidSize;
        public int ViewRadius;
        public int AvoidRadius;
        public float Alignment;
        public float Cohesion;
        public float Separation;
        public float CentrePull;
        public float Speed;
    }
    
    #endregion
    
}