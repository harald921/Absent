using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    Noise.Parameters _parameters;

    ChunkGenerator _chunkGenerator;

    int meshSize; // amount of vertices on one axis

    void Start()
    {
        _parameters = GetComponent<Noise>().parameters;

        meshSize = _parameters.size + 1;

        _chunkGenerator = new ChunkGenerator(meshSize, _parameters);

        TestGenerate();
    }

    void TestGenerate()
    {
        int worldSize = 4;

        for (int y = 0; y < worldSize; y++)
            for (int x = 0; x < worldSize; x++)
                _chunkGenerator.GenerateChunk(_parameters, new Vector2(x, y));
    }
}