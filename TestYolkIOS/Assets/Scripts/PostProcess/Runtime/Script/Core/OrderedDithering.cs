using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public static class OrderedDithering
    {
        public static Texture2D GetDitherMap(int size)
        {
            Texture2D map = new Texture2D(size, size, TextureFormat.Alpha8, false, true);
            map.filterMode = FilterMode.Point;

            float t = size * size;
            var matrix = GetDitherMatrix(size);
            Color32[] colors = new Color32[size * size];

            for (int j = 0; j < size; ++j)
            {
                for (int i = 0; i < size; ++i)
                {
                    byte byteValue = (byte)(matrix[i, j] / t * 255);
                    colors[j * size + i] = new Color32(byteValue, byteValue, byteValue, byteValue);
                }
            }

            map.SetPixels32(colors);
            map.Apply();

            return map;
        }

        public static int[,] GetDitherMatrix(int size)
        {
            if (size <= 1 || !Mathf.IsPowerOfTwo(size))
            {
                return null;
            }
            int inputSize = 1;
            int outputSize;
            int[,] input = new int[,] { { 0 } };
            int[,] output;
            while (true)
            {
                outputSize = inputSize * 2;
                output = new int[outputSize, outputSize];
                for (int i = 0; i < inputSize; i++)
                {
                    for (int j = 0; j < inputSize; j++)
                    {
                        int value = input[i, j] * 4;
                        output[i, j] = value;
                        output[i + inputSize, j] = value + 2;
                        output[i, j + inputSize] = value + 3;
                        output[i + inputSize, j + inputSize] = value + 1;
                    }
                }
                if (outputSize >= size)
                    break;
                else
                {
                    inputSize = outputSize;
                    input = output;
                }
            }
            return output;
        }

        //public static int[,] GetDitherMatrix(uint size)
        //{
        //    if (size <= 1 || !Mathf.IsPowerOfTwo((int)size))
        //        return null;

        //    int[,] result = new int[size, size];

        //    for (uint j = 0; j < size; ++j)
        //    {
        //        for (uint i = 0; i < size; ++i)
        //        {
        //            result[i, j] = ReverseByte(interleave_uint32_with_zeros(i ^ j, i)) / (long)Mathf.Pow(size, 2);
        //        }
        //    }

        //    return result;
        //}


        static long ReverseByte(long value)
        {
            long ret = value;
            ret = ret >> 16 | ret << 16;
            ret = (ret & 0xff00ff00) >> 8 | (ret & 0x00ff00ff) << 8;
            ret = (ret & 0xf0f0f0f0) >> 4 | (ret & 0x0f0f0f0f) << 4;
            ret = (ret & 0xcccccccc) >> 2 | (ret & 0x33333333) << 2;
            ret = (ret & 0xaaaaaaaa) >> 1 | (ret & 0x55555555) << 1;
            return ret;
        }

        static long interleave_uint32_with_zeros(long input, long b)
        {
            long word = input;
            word = (word ^ (word << 16)) & 0x0000ffff0000ffff;
            word = (word ^ (word << 8)) & 0x00ff00ff00ff00ff;
            word = (word ^ (word << 4)) & 0x0f0f0f0f0f0f0f0f;
            word = (word ^ (word << 2)) & 0x3333333333333333;
            word = (word ^ (word << 1)) & 0x5555555555555555;
            return word;
        }

    }
}