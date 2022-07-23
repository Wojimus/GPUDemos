using Unity.Mathematics;
public static class JobUtilities
{
    //Converting 3D arrays to 1D arrays and vice versa (For jobs)
    public static int[] ConvertTo1D(int[,,] array)
    {
        int[] convertedArray = new int[array.GetLength(0) * array.GetLength(1) * array.GetLength(2)];
        int counter = 0;
        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                for (int z = 0; z < array.GetLength(2); z++)
                {
                    convertedArray[counter] = array[x, y, z];
                    counter++;
                }
            }
        }
     
        return convertedArray;
    }
    
    public static int ConvertTo1DIndex(int x, int y, int z,int width, int height)
    {
        return x + width * (y + height * z);
    }
     
    public static int[,,] ConvertTo3D(int[] array,int width, int height, int depth)
    {
        int[,,] convertedArray = new int[width, height, depth];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    convertedArray[x, y, z] = array[x + width * (y + height * z)];
                }
            }
        }
     
        return convertedArray;
    }

    public static int3 ConvertTo3DIndex(int index, int width, int height)
    {
        int z = index / (width * height);
        index -= (z * width * height);
        int y = index / width;
        int x = index % width;

        return new int3(x, y, z);
    }
}
