using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Noise : MonoBehaviour
{
    [System.Serializable]
    public struct Parameters
    {
        public int size;

        [Space(10)]

        public int scale;
        public int octaves;
        public float persistance;
        public float lacunarity;
        public float redistribution;

        [Space(10)]

        public int seed;
    }
    [SerializeField] Parameters _parameters;
    public Parameters parameters
    {
        get { return _parameters; }

        private set { _parameters = value; }
    }

    /* External Methods */
    public static float[,] Generate(Parameters inParameters, Vector2 inOffset)
    {
        // Cache all the parameters
        int   size            = inParameters.size;
        int   scale           = inParameters.scale;
        int   octaves         = inParameters.octaves;
        float persistance     = inParameters.persistance;
        float lacunarity      = inParameters.lacunarity;
        float redistribution  = inParameters.redistribution;
        int   seed            = inParameters.seed;

        float maxPossibleHeight = 0;
        float amplitude = 1;

        System.Random rng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float octaveOffsetX = rng.Next(-100000, 100000) + (inOffset.x * size);
            float octaveOffsetY = rng.Next(-100000, 100000) - (inOffset.y * size);
            octaveOffsets[i] = new Vector2(octaveOffsetX, octaveOffsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        size += 1;

        float[,] noiseMap = new float[size, size];

        float halfSize = size / 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfSize + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfSize + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight;
            }

        // Normalize noise map to a positive spectrum
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                noiseMap[x, y] += redistribution;

                float normalizedHeight = (noiseMap[x, y] + 1) / maxPossibleHeight;
                noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
            }

        return noiseMap;
    }
}
 