using System.Collections.Generic;
using System.Linq;

public static class Blocks
{
    //This class contains the information on all block types and their base properties
    public static Dictionary<int, Block> BlockTypes = new Dictionary<int, Block>();
    public static Dictionary<string, int> BlockTypesKeyLookup = new Dictionary<string, int>();
    
    //These provide easy reference for creating native arrays to pass into jobs so they can access critical information
    //In an unmanaged way
    public static List<int> TextureIndexes = new List<int>();
    public static List<int> TransparentIDs = new List<int>();
    public static List<int> NonSolidIDs = new List<int>();
    public static List<int> CustomMeshIDs = new List<int>();

    //Counter Variable to keep track of the number of textures added
    private static int _textureCount;
    public class Block
    {
        public int BlockID;
        public int BlockType;
        public string Name;
        public string Description;
        public bool IsSolid;
        public bool IsTransparent;
        public int TextureOffset;
        public int[] FaceTextureIndex;
        public int CustomMeshID;

        public Block(string name, string description, int blockType, bool isSolid, bool isTransparent, int[] faceTextureIndex, int customMeshID)
        {
            BlockID = BlockTypes.Count + 1; //Offset ID by 1 as 0 is empty or air
            BlockType = blockType;
            Name = name;
            Description = description;
            IsSolid = isSolid;
            IsTransparent = isTransparent;
            TextureOffset = _textureCount;
            FaceTextureIndex = faceTextureIndex;
            CustomMeshID = customMeshID;

            //Add Block to blockTypes and key lookup table
            BlockTypes.Add(BlockID, this);
            BlockTypesKeyLookup.Add(Name, BlockID);
            
            //Add TextureIndexes
            TextureIndexes.Add(FaceTextureIndex[0] + _textureCount);
            TextureIndexes.Add(FaceTextureIndex[1] + _textureCount);
            TextureIndexes.Add(FaceTextureIndex[2] + _textureCount);
            TextureIndexes.Add(FaceTextureIndex[3] + _textureCount);
            TextureIndexes.Add(FaceTextureIndex[4] + _textureCount);
            TextureIndexes.Add(FaceTextureIndex[5] + _textureCount);
            
            //Transparent IDs
            if (IsTransparent)
                TransparentIDs.Add(BlockID);
            
            //Non Solid IDs
            if (!IsSolid)
                NonSolidIDs.Add(BlockID);
            
            //Custom Mesh IDS
            if (CustomMeshID != 0)
                CustomMeshIDs.Add(BlockID);
            
            //Increment texture count
            _textureCount += FaceTextureIndex.Max() + 1;
        }
    }

    //Blocks
    //Each block contains its name, whether it is solid and the texture index of each face
    //The texture face index corresponds to the order of faces in VoxelData - Front, Back, Top, Bottom, Left, Right
    public static Block Dirt = new Block("Dirt", 
        "It's mushy.... and brown", 
        0, true, false, new []{0, 0, 0, 0, 0, 0}, 0);
    public static Block Grass = new Block("Grass", 
        "Dirt with a healthy green coating",
        0, true, false, new []{0, 0, 1, 2, 0, 0}, 0);
    public static Block GrassFoliage = new Block("Grass Foliage", 
        "Grass that grows on top of grass",
        1, false, true, new[] {0, 0, 0, 0, 0, 0}, 1);
    public static Block Stone = new Block("Stone", 
        "A hard, mineral based material",
        0, true, false, new []{0, 0, 0, 0, 0, 0}, 0);
    public static Block Basalt = new Block("Basalt", 
        "A fine grain volcanic rock",
        0, true, false, new[] {0, 0, 0, 0, 0, 0}, 0);
    public static Block Marble = new Block("Marble", 
        "A crystalline form of limestone",
        0, true, false, new[] {0, 0, 0, 0, 0, 0}, 0);
    public static Block BlackMarble = new Block("Black Marble", 
        "A crystalline form of limestone",
        0, true, false, new[] {0, 0, 0, 0, 0, 0}, 0);
    public static Block Chalk = new Block("Chalk", 
        "A crumbly limestone composition",
        0, true, false, new[] {0, 0, 0, 0, 0, 0}, 0);
    public static Block Sand = new Block("Sand", 
        "Rock crushed by nature",
        0, true, false, new[] {0, 0, 0, 0, 0, 0}, 0);
}
