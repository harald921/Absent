using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Noise : MonoBehaviour
{
    [System.Serializable]
    public struct Parameters
    {
        public int resolution;

        [Space(10)]

        public int scale;
        public int octaves;
        public float persistance;
        public float lacunarity;

        public float rangeMultiplier;

        [Space(10)]

        public int seed;
    }
    [SerializeField] Parameters[] _parameters;
    public Parameters[] parameters
    {
        get { return _parameters; }
    }

    /* External Methods */
    public static float[,] Generate(Parameters inParameters, Vector2 inOffset)
    {
        // Cache all the parameters
        int   resolution      = inParameters.resolution;
        int   scale           = inParameters.scale;
        int   octaves         = inParameters.octaves;
        float persistance     = inParameters.persistance;
        float lacunarity      = inParameters.lacunarity;
        int   seed            = inParameters.seed;

        float amplitude = 1;

        System.Random rng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float octaveOffsetX = rng.Next(-100000, 100000) + (inOffset.x * resolution);
            float octaveOffsetY = rng.Next(-100000, 100000) - (inOffset.y * resolution);
            octaveOffsets[i] = new Vector2(octaveOffsetX, octaveOffsetY);

            amplitude *= persistance;
        }

        resolution += 1;

        float[,] noiseMap = new float[resolution, resolution];

        float halfSize = resolution / 2f;

        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfSize + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfSize + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseMap[x, y] = noiseHeight * inParameters.rangeMultiplier;
            }

        // Normalize noise map to a positive spectrum
        return noiseMap;
    }
}
 