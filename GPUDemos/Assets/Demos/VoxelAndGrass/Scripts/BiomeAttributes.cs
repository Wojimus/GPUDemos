using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome Attributes", menuName = "World/Biome")]
public class BiomeAttributes : ScriptableObject
{
    //Details
    public string BiomeName;
    //Ground
    public int GroundBlock;
    public int SecondaryGroundBlock;
    [Range(0, 1)]
    public float SecondaryGroundThreshold;
    public float SecondaryGroundScale;
    public int2 SecondaryGroundOffset;
    public int UndergroundBlock;
    //Rocks
    public int3 RockTypes;
    [Range(0, 1)]
    public float RockThreshold;
    public float RockScale;
    public int2 RockOffset;
}