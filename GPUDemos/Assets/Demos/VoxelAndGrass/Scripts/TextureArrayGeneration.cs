using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class TextureArrayGeneration
{
    //This class will generate the required texture arrays for the game at startup
    //it does this based on the Block type list and their names, their texture folder must correspond to the block name
    //and the textures in that folder must be equal to their assigned textures in Blocks.cs

    //Path To Block Texture Folders
    private const string TextureRootPath = "Textures/Blocks";
    private const string OutputPath = "Assets/Resources/Textures/Generated Arrays";

    private Texture2DArray CreateTextureArray(List<Texture2D> textures, string outputName)
    {
        //Create texture array based on first texture
        Texture2D t = textures[0];
        Texture2DArray textureArray = new Texture2DArray
        (
            t.width, t.height, textures.Count, t.format, t.mipmapCount > 1
        )
        {
            anisoLevel = t.anisoLevel, filterMode = t.filterMode, wrapMode = t.wrapMode
        };

        //Copy each texture into the texture array
        for (int i = 0; i < textures.Count; i++)
        {
            for (int m = 0; m < t.mipmapCount; m++)
            {
                Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
            }
        }

#if UNITY_EDITOR
        AssetDatabase.CreateAsset(textureArray, OutputPath + $"/{outputName}.asset");
#endif

        return textureArray;
    }

    //This function creates the diffuse texture array, used if you only use the simple base colour shader
    public void CreateDiffuseArray()
    {
        //Create texture lists
        List<Texture2D> diffuseTextures = new List<Texture2D>();

        //Foreach block type in defined blocks
        foreach (KeyValuePair<int, Blocks.Block> block in Blocks.BlockTypes)
        {
            //Foreach diffuse texture for the block
            for (int i = 0; i <= block.Value.FaceTextureIndex.Max(); i++)
            {
                //Generate Texture Path based on block name and texture number
                string texturePath = TextureRootPath + $"/{block.Value.Name}/{block.Value.Name}_{i}";
                //Load Texture
                Texture2D texture = Resources.Load<Texture2D>(texturePath);

                //Warn if the texture doesn't exist, this will cause the texture array to be invalid
                if (texture == null)
                {
                    Debug.LogError(
                        $"Could not find texture '{block.Value.Name}/{block.Value.Name}_{i}' at '{texturePath}' - Texture Array Will Be Invalid");
                    break;
                }

                diffuseTextures.Add(texture);
            }
        }

        //Create Arrays
        CreateTextureArray(diffuseTextures, "VoxelDiffuse");
    }

    //This function creates the combined texture array, used for the more complex shader with normals and AO and metallic maps
    public Texture2DArray[] CreateCombinedArrays()
    {
        //Create texture lists
        List<Texture2D> diffuseTextures = new List<Texture2D>();
        List<Texture2D> normalTextures = new List<Texture2D>();
        List<Texture2D> metallicTextures = new List<Texture2D>();
        List<Texture2D> aoTextures = new List<Texture2D>();
        List<Texture2D> opacityTextures = new List<Texture2D>();

        //Foreach block type in defined blocks
        foreach (KeyValuePair<int, Blocks.Block> block in Blocks.BlockTypes)
        {
            //Foreach diffuse texture for the block
            for (int i = 0; i <= block.Value.FaceTextureIndex.Max(); i++)
            {
                string diffusePath;
                string normalPath;
                string metallicPath;
                string aoPath;
                string opacityPath;

                if (block.Value.CustomMeshID == 0)
                {
                    //Generate Texture Paths based on block name and texture number
                    diffusePath = TextureRootPath + $"/{block.Value.Name}/{block.Value.Name}_{i}";
                    normalPath = TextureRootPath + $"/{block.Value.Name}/{block.Value.Name}_{i}_Normal";
                    metallicPath = TextureRootPath + $"/{block.Value.Name}/{block.Value.Name}_{i}_Metallic";
                    aoPath = TextureRootPath + $"/{block.Value.Name}/{block.Value.Name}_{i}_AO";
                    opacityPath = TextureRootPath + $"/{block.Value.Name}/{block.Value.Name}_{i}_Opacity";
                }
                else
                {
                    //Generate Texture Paths based on block name and texture number
                    diffusePath = TextureRootPath + "/CustomMesh/CustomMesh";
                    normalPath = TextureRootPath + "/CustomMesh/CustomMesh";
                    metallicPath = TextureRootPath + "/CustomMesh/CustomMesh";
                    aoPath = TextureRootPath + "/CustomMesh/CustomMesh";
                    opacityPath = TextureRootPath + "/CustomMesh/CustomMesh";
                }

                //Load Textures
                Texture2D diffuseTexture = Resources.Load<Texture2D>(diffusePath);
                Texture2D normalTexture = Resources.Load<Texture2D>(normalPath);
                Texture2D metallicTexture = Resources.Load<Texture2D>(metallicPath);
                Texture2D aoTexture = Resources.Load<Texture2D>(aoPath);
                Texture2D opacityTexture = Resources.Load<Texture2D>(opacityPath);

                //Warn if the textures don't exist, this will cause the texture array to be invalid
                if (diffuseTexture == null)
                {
                    Debug.LogError(
                        $"Could not find texture '{block.Value.Name}_{i}' at '{diffusePath}' - Texture Array Will Be Invalid");
                    break;
                }

                if (normalTexture == null)
                {
                    Debug.LogError(
                        $"Could not find texture '{block.Value.Name}_{i}_Normal' at '{normalPath}' - Texture Array Will Be Invalid");
                    break;
                }

                if (metallicTexture == null)
                {
                    Debug.LogError(
                        $"Could not find texture '{block.Value.Name}_{i}_Metallic' at '{metallicPath}' - Texture Array Will Be Invalid");
                    break;
                }

                if (aoTexture == null)
                {
                    Debug.LogError(
                        $"Could not find texture '{block.Value.Name}_{i}_AO' at '{aoPath}' - Texture Array Will Be Invalid");
                    break;
                }

                if (opacityTexture == null)
                {
                    Debug.LogError(
                        $"Could not find texture '{block.Value.Name}_{i}_Opacity' at '{opacityPath}' - Texture Array Will Be Invalid");
                    break;
                }
                
                //Convert Textures To RGBA32
                Texture2D convertedDiffuse = new Texture2D(diffuseTexture.width, diffuseTexture.height, TextureFormat.RGBA32, true);
                convertedDiffuse.SetPixels(diffuseTexture.GetPixels());
                convertedDiffuse.Apply();
                Texture2D convertedNormal = new Texture2D(normalTexture.width, normalTexture.height, TextureFormat.RGBA32, true, true);
                convertedNormal.SetPixels(normalTexture.GetPixels());
                convertedNormal.Apply();
                Texture2D convertedMetallic = new Texture2D(metallicTexture.width, metallicTexture.height, TextureFormat.RGBA32, true);
                convertedMetallic.SetPixels(metallicTexture.GetPixels());
                convertedMetallic.Apply();
                Texture2D convertedAO = new Texture2D(aoTexture.width, aoTexture.height, TextureFormat.RGBA32, true);
                convertedAO.SetPixels(aoTexture.GetPixels());
                convertedAO.Apply();
                Texture2D convertedOpacity = new Texture2D(opacityTexture.width, opacityTexture.height, TextureFormat.RGBA32, true);
                convertedOpacity.SetPixels(opacityTexture.GetPixels());
                convertedOpacity.Apply();

                //Add Textures to lists
                diffuseTextures.Add(convertedDiffuse);
                normalTextures.Add(convertedNormal);
                metallicTextures.Add(convertedMetallic);
                aoTextures.Add(convertedAO);
                opacityTextures.Add(convertedOpacity);
            }
        }

        //Create Arrays
        Texture2DArray diffuseArray = CreateTextureArray(diffuseTextures, "VoxelDiffuse");
        Texture2DArray normalArray = CreateTextureArray(normalTextures, "VoxelNormal");
        Texture2DArray metallicArray = CreateTextureArray(metallicTextures, "VoxelMetallic");
        Texture2DArray aoArray = CreateTextureArray(aoTextures, "VoxelAO");
        Texture2DArray opacityArray = CreateTextureArray(opacityTextures, "VoxelOpacity");

        return new[] {diffuseArray, normalArray, metallicArray, aoArray, opacityArray};
    }
}