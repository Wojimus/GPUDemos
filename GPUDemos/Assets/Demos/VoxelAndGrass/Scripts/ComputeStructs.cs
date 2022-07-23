using System;
using Unity.Mathematics;
using UnityEngine;

public static class ComputeStructs
{
    public unsafe struct VoxelMapGenBiome
    {
        public int GroundBlock;
        public int BelowGroundBlock;
        public int GroundHeight;
        public int TerrainHeight;
        public float TerrainScale;

        fixed int Trees[8];
        fixed int Lodes[24];
        fixed int Foliages[24];

        public VoxelMapGenBiome(int groundBlock, int belowGroundBlock, int groundHeight, int terrainHeight,
            float terrainScale, int[] trees, int[] lodes, int[] foliages)
        {
            GroundBlock = groundBlock;
            BelowGroundBlock = belowGroundBlock;
            GroundHeight = groundHeight;
            TerrainHeight = terrainHeight;
            TerrainScale = terrainScale;

            //Hardcoded values to keep stride in compute shader consistent

            //Fill Trees, Lodes etc
            for (int i = 0; i < trees.Length; i++)
            {
                if (i < trees.Length)
                {
                    Trees[i] = trees[i];
                }
            }

            for (int i = 0; i < lodes.Length; i++)
            {
                if (i < lodes.Length)
                {
                    Lodes[i] = lodes[i];
                }
            }

            for (int i = 0; i < foliages.Length; i++)
            {
                if (i < foliages.Length)
                {
                    Foliages[i] = foliages[i];
                }
            }
        }
    }

    public struct VoxelMapGenTree
    {
        public int MinHeight;
        public int MaxHeight;
        public int TrunkBlockID;
        public int LeafBlockID;
        public float NoiseScale;
        public float NoiseThreshold;
        public float PlacementScale;
        public float PlacementThreshold;

        public VoxelMapGenTree(int minHeight, int maxHeight, int trunkBlockID, int leafBlockID, float noiseScale,
            float noiseThreshold, float placementScale, float placementThreshold)
        {
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            TrunkBlockID = trunkBlockID;
            LeafBlockID = leafBlockID;
            NoiseScale = noiseScale;
            NoiseThreshold = noiseThreshold;
            PlacementScale = placementScale;
            PlacementThreshold = placementThreshold;
        }
    }

    public struct VoxelMapGenLode
    {
        public int BlockID;
        public int MinHeight;
        public int MaxHeight;
        public float Scale;
        public float Threshold;
        public float NoiseOffset;

        public VoxelMapGenLode(int blockID, int minHeight, int maxHeight, float scale, float threshold,
            float noiseOffset)
        {
            BlockID = blockID;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
            Scale = scale;
            Threshold = threshold;
            NoiseOffset = noiseOffset;
        }
    }

    public struct VoxelMapGenFoliage
    {
        public int BlockID;
        public float NoiseScale;
        public float NoiseThreshold;

        public VoxelMapGenFoliage(int blockID, float noiseScale, float noiseThreshold)
        {
            BlockID = blockID;
            NoiseScale = noiseScale;
            NoiseThreshold = noiseThreshold;
        }
    }

    public struct BlittableBool : IEquatable<BlittableBool>
    {
        private byte _value;

        public BlittableBool(bool value)
        {
            this._value = Convert.ToByte(value);
        }

        public static implicit operator bool(BlittableBool blittableBool)
        {
            return blittableBool._value != 0;
        }

        public static implicit operator BlittableBool(bool value)
        {
            return new BlittableBool(value);
        }

        public bool Equals(BlittableBool other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is BlittableBool && Equals((BlittableBool) obj);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static bool operator ==(BlittableBool left, BlittableBool right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlittableBool left, BlittableBool right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ((bool) this).ToString();
        }
    }

    //A Vertex And Its Data
    //Stride = 12 + 8 = 20
    public struct ShaderVertex
    {
        public float3 position;
        public float2 uv;
    };
    
    //Triangle To Render
    //Stride = 20 * 3 + 12 + 8 = 80
    public struct ChunkTriangle
    {
        public ShaderVertex vert1;
        public ShaderVertex vert2;
        public ShaderVertex vert3;
        public float3 normal;
        public float2 uv4;
    };
    
    //Triangle To Render
    //Stride = 20 * 3 = 60
    public struct ShaderTriangle
    {
        public ShaderVertex vert1;
        public ShaderVertex vert2;
        public ShaderVertex vert3;
    };

    //Face
    //Stride = 20
    public struct ShaderFace
    {
        public int4 facePos; //Contains xyz voxel position and w as the face number 0-5 (front-right)
        public uint textureIndex; //Texture Index Of Face
    };

    public struct ShaderVoxel
    {
        public float4 VoxelData;
        public uint FrontFaceTextureIndex;
        public uint BackFaceTextureIndex;
        public uint TopFaceTextureIndex;
        public uint BottomFaceTextureIndex;
        public uint LeftFaceTextureIndex;
        public uint RightFaceTextureIndex;
        public BlittableBool FrontFace;
        public BlittableBool BackFace;
        public BlittableBool TopFace;
        public BlittableBool BottomFace;
        public BlittableBool LeftFace;
        public BlittableBool RightFace;
    };

    public struct UShort4
    {
        public ushort x;
        public ushort y;
        public ushort z;
        public ushort w;

        public UShort4(ushort x, ushort y, ushort z, ushort w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public struct UShort3
    {
        public ushort x;
        public ushort y;
        public ushort z;

        public UShort3(ushort x, ushort y, ushort z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}