using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChunkGenerator
{
    readonly int size;
    public Noise.Parameters _parameters { private get; set; }

    NoiseGenerator   _noiseGenerator;
    MeshGenerator    _meshGenerator;
    TextureGenerator _textureGenerator;

    public ChunkGenerator(int inSize, Noise.Parameters inParemeters)
    {
        size = inSize;
        _parameters = inParemeters;

        _noiseGenerator   = new NoiseGenerator();
        _meshGenerator    = new MeshGenerator(inSize);
        _textureGenerator = new TextureGenerator(inSize);
    }

    public Chunk GenerateChunk(Vector2 inOffset)
    {
        Mesh newMesh = GenerateMesh();

        GameObject newGO = new GameObject();
        MeshFilter meshFilter = newGO.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = newGO.AddComponent<MeshRenderer>();

        float[,] noiseMap = GenerateNoise(_parameters, inOffset);

        meshFilter.mesh = newMesh;
        meshRenderer.material.mainTexture = GenerateTexture(noiseMap);

        newGO.transform.position = new Vector3(inOffset.x * size, 0, inOffset.y * size);

        Chunk newChunk = new Chunk(newGO);
        return newChunk;

    }

    float[,] GenerateNoise(Noise.Parameters inParameters, Vector2 inOffset)
    {
        return _noiseGenerator.Generate(inParameters, inOffset);
    }

    Mesh GenerateMesh()
    {
        return _meshGenerator.Generate();
    }

    Texture2D GenerateTexture(float[,] inNoiseMap)
    {
        return _textureGenerator.Generate(inNoiseMap);
    }
}


public class NoiseGenerator
{
    public float[,] Generate(Noise.Parameters inParameters, Vector2 inOffset)
    {
        return Noise.Generate(inParameters, inOffset);
    }
}

public class MeshGenerator
{
    readonly int size;
    readonly int tileCount;
    readonly int triangleCount;
    readonly int vertexSize;
    readonly int vertexCount;

    Vector3[] vertices;
    readonly Vector3[] normals;
    readonly Vector2[] uv;

    readonly int[] triVertIDs;


    public MeshGenerator(int inSize)
    {
        size          = inSize;
        tileCount     = size * size;
        triangleCount = tileCount * 2;
        vertexSize    = size + 1;
        vertexCount   = vertexSize * vertexSize;

        vertices = new Vector3[vertexCount];
        normals  = new Vector3[vertexCount];
        uv       = new Vector2[vertexCount];

        triVertIDs = new int[triangleCount * 3];

        // Generate the normals and UVs
        for (int y = 0; y < vertexSize; y++)
            for (int x = 0; x < vertexSize; x++)
            {
                int currentIndex = y * vertexSize + x;

                normals[currentIndex] = Vector2.up;
                uv[currentIndex] = new Vector2((float)x / size, (float)y / size);
            }

        // Generate the triVertID's
        bool diagonal = false;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int currentTileID = y * size + x;
                int triVertOffset = y * vertexSize + x;
                int triangleOffset = currentTileID * 6;

                if (diagonal)
                {
                    triVertIDs[triangleOffset + 0] = triVertOffset + 0;
                    triVertIDs[triangleOffset + 1] = triVertOffset + vertexSize + 0;
                    triVertIDs[triangleOffset + 2] = triVertOffset + vertexSize + 1;

                    triVertIDs[triangleOffset + 3] = triVertOffset + 0;
                    triVertIDs[triangleOffset + 4] = triVertOffset + vertexSize + 1;
                    triVertIDs[triangleOffset + 5] = triVertOffset + 1;
                }

                else
                {
                    triVertIDs[triangleOffset + 0] = triVertOffset + 0;
                    triVertIDs[triangleOffset + 1] = triVertOffset + vertexSize + 0;
                    triVertIDs[triangleOffset + 2] = triVertOffset + 1;

                    triVertIDs[triangleOffset + 3] = triVertOffset + 1;
                    triVertIDs[triangleOffset + 4] = triVertOffset + vertexSize + 0;
                    triVertIDs[triangleOffset + 5] = triVertOffset + vertexSize + 1;
                }

                diagonal = !diagonal;
            }

            diagonal = !diagonal;
        }
    }


    public Mesh Generate()
    {
        // Generate the vertices of the mesh
        for (int y = 0; y < vertexSize; y++)
            for (int x = 0; x < vertexSize; x++)
                vertices[y * vertexSize + x] = new Vector3(x, 0, y);

        Mesh newMesh = new Mesh
        {
            name = "Terrain Mesh",
            vertices = vertices,
            normals = normals,
            uv = uv,
            triangles = triVertIDs
        };

        newMesh.RecalculateNormals(); // http://schemingdeveloper.com/2014/10/17/better-method-recalculate-normals-unity/

        return newMesh;
    }
}

public class TextureGenerator
{
    readonly int size;
    readonly Color[] pixels;


    public TextureGenerator(int inSize)
    {
        size = inSize;
        pixels = new Color[size * size];
    }


    public Texture2D Generate(float[,] inNoiseMap)
    {
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Color.Lerp(Color.black, Color.white, inNoiseMap[x,y]);

        Texture2D newTexture = new Texture2D(size, size);
        newTexture.SetPixels(pixels);
        newTexture.Apply();

        return newTexture;
    }
}