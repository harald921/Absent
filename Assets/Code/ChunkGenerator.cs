using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class ChunkGenerator
{
    public Noise.Parameters _parameters { private get; set; }

    NoiseGenerator   _noiseGenerator;
    MeshGenerator    _meshGenerator;
    TextureGenerator _textureGenerator;

    public Queue<Action> _meshThreadInfoQueue    = new Queue<Action>();
    public Queue<Action> _textureThreadInfoQueue = new Queue<Action>();

    public ChunkGenerator(Noise.Parameters inParemeters)
    {
        _parameters = inParemeters;

        _noiseGenerator   = new NoiseGenerator(this);
        _meshGenerator    = new MeshGenerator(this, inParemeters.size);
        _textureGenerator = new TextureGenerator(this, inParemeters.size);
    }


    // External
    public void Update()
    {
        ProcessQueues();
    }

    public Chunk GenerateChunk(Vector2 inOffset)
    {
        GameObject newGO = new GameObject();
        newGO.AddComponent<MeshFilter>();
        newGO.AddComponent<MeshRenderer>();
        newGO.transform.position = new Vector3(inOffset.x * _parameters.size, 0, -inOffset.y * _parameters.size);

        Chunk newChunk = new Chunk(newGO);

        new Thread(() => GenerateChunkData(inOffset, newChunk)).Start();

        return newChunk;
    }


    // Internal
    void ProcessQueues()
    {
        while (_meshThreadInfoQueue.Count > 0)
            _meshThreadInfoQueue.Dequeue()();

        while (_textureThreadInfoQueue.Count > 0)
            _textureThreadInfoQueue.Dequeue()();
    }

    void GenerateChunkData(Vector2 inOffset, Chunk inChunk)
    {
        NoiseGenerator.Result noiseResult = new NoiseGenerator.Result();
        Thread noiseThread = new Thread(() => _noiseGenerator.Generate(noiseResult, _parameters, inOffset, inChunk));

        noiseThread.Start();
        noiseThread.Join();

        new Thread(() => _meshGenerator.Generate(noiseResult, inChunk)).Start();
        new Thread(() => _textureGenerator.Generate(noiseResult, inChunk)).Start();
    }

    public void OnMeshDataRecieved(MeshGenerator.Result inResult, Chunk inChunk)
    {
        if (!inChunk.gameObject)
            return;

        Mesh generatedMesh      = inChunk.gameObject.GetComponent<MeshFilter>().mesh;
        generatedMesh.vertices  = inResult.meshData.vertices;
        generatedMesh.uv        = inResult.meshData.uv;
        generatedMesh.triangles = inResult.meshData.triangles;
        
        generatedMesh.RecalculateNormals(); // http://schemingdeveloper.com/2014/10/17/better-method-recalculate-normals-unity/

        inChunk.gameObject.GetComponent<MeshFilter>().mesh = generatedMesh;
    }

    public void OnTextureDataRecieved(TextureGenerator.Result inResult, Chunk inChunk)
    {
        if (!inChunk.gameObject)
            return;

        Texture2D newTexture = new Texture2D(_parameters.size, _parameters.size);
        newTexture.filterMode = FilterMode.Point;
        newTexture.SetPixels(inResult.pixels);
        newTexture.Apply();

        inChunk.gameObject.GetComponent<MeshRenderer>().material.mainTexture = newTexture;
    }
}




public class NoiseGenerator
{
    readonly ChunkGenerator _chunkGenerator;

    public class Result
    {
        public float[,] heightMap;
    }


    public NoiseGenerator(ChunkGenerator inChunkGenerator)
    {
        _chunkGenerator = inChunkGenerator;
    }


    public void Generate(Result inResult, Noise.Parameters inParameters, Vector2 inOffset, Chunk inChunk)
    {
        inResult.heightMap = Noise.Generate(inParameters, inOffset);
    }
}




public class MeshGenerator
{
    readonly ChunkGenerator _chunkGenerator;

    public class Result
    {
        public MeshData meshData;
    }

    readonly int vertexSize;
    readonly int vertexCount;

    public struct MeshData
    {
        public Vector3[] vertices;
        public Vector2[] uv;
        public int[]     triangles;
    }
    MeshData meshData;

    public MeshGenerator(ChunkGenerator inChunkGenerator, int inSize)
    {
        _chunkGenerator = inChunkGenerator;

        vertexSize    = inSize + 1;
        vertexCount   = vertexSize * vertexSize;

        meshData = new MeshData()
        {
            uv         = new Vector2[vertexCount],
            triangles = new int[inSize * inSize * 6]
        };

        // Generate the normals and UVs
        for (int y = 0; y < vertexSize; y++)
            for (int x = 0; x < vertexSize; x++)
                meshData.uv[y * vertexSize + x] = new Vector2((float)x / inSize, (float)y / inSize);

        // Generate the triVertID's
        bool diagonal = false;
        for (int y = 0; y < inSize; y++)
        {
            for (int x = 0; x < inSize; x++)
            {
                int currentTileID = y * inSize + x;
                int triVertOffset = y * vertexSize + x;
                int triangleOffset = currentTileID * 6;

                if (diagonal)
                {
                    meshData.triangles[triangleOffset + 0] = triVertOffset + 0;
                    meshData.triangles[triangleOffset + 1] = triVertOffset + vertexSize + 0;
                    meshData.triangles[triangleOffset + 2] = triVertOffset + vertexSize + 1;
                    meshData.triangles[triangleOffset + 3] = triVertOffset + 0;
                    meshData.triangles[triangleOffset + 4] = triVertOffset + vertexSize + 1;
                    meshData.triangles[triangleOffset + 5] = triVertOffset + 1;
                }

                else
                {
                    meshData.triangles[triangleOffset + 0] = triVertOffset + 0;
                    meshData.triangles[triangleOffset + 1] = triVertOffset + vertexSize + 0;
                    meshData.triangles[triangleOffset + 2] = triVertOffset + 1;
                    meshData.triangles[triangleOffset + 3] = triVertOffset + 1;
                    meshData.triangles[triangleOffset + 4] = triVertOffset + vertexSize + 0;
                    meshData.triangles[triangleOffset + 5] = triVertOffset + vertexSize + 1;
                }

                diagonal = !diagonal;
            }
        }
    }


    public void Generate(NoiseGenerator.Result inNoiseResult, Chunk inChunk)
    {
        MeshData newMeshData = new MeshData()
        {
            vertices = new Vector3[vertexCount],
            uv = meshData.uv,
            triangles = meshData.triangles
        };

        // Generate the vertices of the mesh
        for (int y = 0; y < vertexSize; y++)
            for (int x = 0; x < vertexSize; x++)
            {
                int iteration = y * vertexSize + x;

                newMeshData.vertices[iteration].x = x;
                newMeshData.vertices[iteration].y = inNoiseResult.heightMap[x,y] * 100;
                newMeshData.vertices[iteration].z = y;
            }

        Result result = new Result();
        result.meshData = newMeshData;

        lock (_chunkGenerator._meshThreadInfoQueue)
            _chunkGenerator._meshThreadInfoQueue.Enqueue(() => _chunkGenerator.OnMeshDataRecieved(result, inChunk));
    }
}




public class TextureGenerator
{
    readonly ChunkGenerator _chunkGenerator;

    public class Result
    {
        public Color[] pixels;
    }

    readonly int size;

    public TextureGenerator(ChunkGenerator inChunkGenerator, int inSize)
    {
        _chunkGenerator = inChunkGenerator;

        size = inSize;
    }


    public void Generate(NoiseGenerator.Result inNoiseResult, Chunk inChunk)
    {
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Color.Lerp(Color.black, Color.white, inNoiseResult.heightMap[x,y]);

        Result result = new Result();
        result.pixels = pixels;

        lock (_chunkGenerator._textureThreadInfoQueue)
            _chunkGenerator._textureThreadInfoQueue.Enqueue(() => _chunkGenerator.OnTextureDataRecieved(result, inChunk));
    }
}