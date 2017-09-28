using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    Noise.Parameters _parameters;

    ChunkGenerator _chunkGenerator;

    [SerializeField] int radius = 3;

    void Start()
    {
        _parameters = GetComponent<Noise>().parameters;

        int meshSize = _parameters.size + 1;

        _chunkGenerator = new ChunkGenerator(meshSize, _parameters);

        TestGenerate();
    }

    void TestGenerate()
    {
        for (int zCircle = -radius; zCircle <= radius; zCircle++)
            for (int xCircle = -radius; xCircle <= radius; xCircle++)
                if (xCircle * xCircle + zCircle * zCircle < radius * radius)
                    _chunkGenerator.GenerateChunk(new Vector2(xCircle, zCircle));
    }
}