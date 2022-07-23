using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class FlowFieldController : MonoBehaviour
{
    //Constants
    private const int PerlinParticleStructSize = sizeof(float) * 6;
    
    //Public References
    [Header("Compute Shaders")] 
    public ComputeShader FlowFieldCS;
    public ComputeShader ParticleBehaviourCS;
    public ComputeShader ClearParticleTextureCS;
    public ComputeShader DrawParticleTextureCS;

    [Header("Field")]
    public int2 FieldSize;
    
    [Header("FlowField Parameters")] 
    public Material FlowFieldMaterial;
    [Range(0.1f, 100)]
    public float FlowFieldScale;
    public float FlowFieldSpeed;
    public float FlowFieldStrength;
    
    [Header("Particle Parameters")] 
    public int ParticleAmount;
    public int ParticleSize;
    public float ParticleSpeed;

    //Private References
    public RenderTexture _flowField;
    public RenderTexture _particleTexture;
    public ComputeBuffer _particleBuffer;
    private float _timeOffset;

    #region Unity Functions

    private void Start()
    {
        //Turn Off Vsync
        QualitySettings.vSyncCount = 0;
        
        CreateField();
        SpawnParticles();
        CalculateFlowField();
    }

    private void Update()
    {
        //Update Time Offset
        _timeOffset += FlowFieldSpeed * Time.deltaTime;
        
        CalculateFlowField();
        ClearParticleTexture();
        DrawParticleTexture();
        ParticleBehaviour();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    #endregion

    #region FlowField And Particles

    private void CreateField()
    {
        //Create Textures And Enable Random Write
        _flowField = new RenderTexture(FieldSize.x, FieldSize.y, 0);
        _particleTexture = new RenderTexture(FieldSize.x, FieldSize.y, 0);
        _flowField.enableRandomWrite = true;
        _particleTexture.enableRandomWrite = true;

        //Set Material
        FlowFieldMaterial.mainTexture = _particleTexture;
    }

    private void CalculateFlowField()
    {
        //Get Kernel Index
        int kernelIndex = FlowFieldCS.FindKernel("CSMain");
        
        //Set Parameters
        FlowFieldCS.SetTexture(kernelIndex, "FlowFieldTexture", _flowField);
        FlowFieldCS.SetFloats("FieldSize", FieldSize.x, FieldSize.y);
        FlowFieldCS.SetFloats("Offset", 0, 0);
        FlowFieldCS.SetFloat("Scale", FlowFieldScale);
        FlowFieldCS.SetFloats("Time", _timeOffset);
        FlowFieldCS.SetFloat("FieldStrength", FlowFieldStrength);

        //Calculate Thread Groups
        int2 threadGroups = new int2(Mathf.CeilToInt(FieldSize.x / 32f), Mathf.CeilToInt(FieldSize.y / 32f));
        
        //Dispatch Shader
        FlowFieldCS.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, 1);
    }

    private void SpawnParticles()
    {
        //Create Array And Fill Buffer
        Particle[] particles = new Particle[ParticleAmount];

        //Create Random Positions
        for (int i = 0; i < ParticleAmount; i++)
        {
            particles[i].Position = new float2(Random.value * FieldSize.x, Random.value * FieldSize.y);
        }
        
        //Set Buffer
        _particleBuffer?.Release();
        _particleBuffer = new ComputeBuffer(ParticleAmount, PerlinParticleStructSize);
        _particleBuffer.SetData(particles);
    }

    public void RespawnParticles(int particleAmount)
    {
        ParticleAmount = particleAmount;
        SpawnParticles();
    }

    private void ParticleBehaviour()
    {
        //Get Kernel Index
        int kernelIndex = ParticleBehaviourCS.FindKernel("CSMain");
        
        //Set Parameters
        ParticleBehaviourCS.SetBuffer(kernelIndex, "Particles", _particleBuffer);
        ParticleBehaviourCS.SetTexture(kernelIndex, "FlowFieldTexture", _flowField);
        ParticleBehaviourCS.SetFloats("FieldSize", FieldSize.x, FieldSize.y);
        ParticleBehaviourCS.SetFloat("ParticleSpeed", ParticleSpeed);
        ParticleBehaviourCS.SetFloat("TimeDelta", Time.deltaTime);
        
        //Calculate Thread Groups
        int threadGroups = Mathf.CeilToInt(ParticleAmount / 1024f);
        
        //Dispatch Shader
        ParticleBehaviourCS.Dispatch(kernelIndex, threadGroups, 1, 1);
    }

    private void ClearParticleTexture()
    {
        //Get Kernel Index
        int kernelIndex = ClearParticleTextureCS.FindKernel("CSMain");
        
        //Set Parameters
        ClearParticleTextureCS.SetTexture(kernelIndex, "ParticleTexture", _particleTexture);
        
        //Calculate Thread Groups
        int2 threadGroups = new int2(Mathf.CeilToInt(FieldSize.x / 32f), Mathf.CeilToInt(FieldSize.y / 32f));
        
        //Dispatch Shader
        ClearParticleTextureCS.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, 1);
    }

    private void DrawParticleTexture()
    {
        //Get Kernel Index
        int kernelIndex = DrawParticleTextureCS.FindKernel("CSMain");
        
        //Set Parameters
        DrawParticleTextureCS.SetTexture(kernelIndex, "ParticleTexture", _particleTexture);
        DrawParticleTextureCS.SetBuffer(kernelIndex, "Particles", _particleBuffer);
        DrawParticleTextureCS.SetInt("ParticleSize", ParticleSize);
        
        //Calculate Thread Groups
        int threadGroups = Mathf.CeilToInt(ParticleAmount / 1024f);
        
        //Dispatch Shader
        DrawParticleTextureCS.Dispatch(kernelIndex, threadGroups, 1, 1);
    }

    #endregion

    #region Utility

    private void CleanUp()
    {
        _flowField.Release();
        _particleTexture.Release();
        _particleBuffer.Release();
    }

    #endregion

    #region Structs

    private struct Particle
    {
        public float2 Position;
        public float2 Velocity;
        public float2 Acceleration;
    }

    #endregion
}
