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

    Transform _worldTransform;

    public ChunkGenerator(Noise.Parameters inParemeters)
    {
        _worldTransform = GameObject.Find("World").transform;

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
        GameObject newGO = new GameObject(inOffset.ToString());
        newGO.transform.SetParent(_worldTransform);
        newGO.transform.position = new Vector3(inOffset.x * _parameters.size, 0, -inOffset.y * _parameters.size);

        newGO.AddComponent<MeshFilter>();
        newGO.AddComponent<MeshRenderer>();

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

    readonly int _size;
    readonly int _vertexSize;
    readonly int _vertexCount;

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

        _size          = inSize;
        _vertexSize    = inSize * 2;
        _vertexCount   = inSize * inSize * 4;

        meshData = new MeshData()
        {
            uv         = new Vector2[_vertexCount],
            triangles = new int[inSize * inSize * 6]
        };

        // Generate the normals and UVs
        for (int y = 0; y < _vertexSize; y++)
            for (int x = 0; x < _vertexSize; x++)
                meshData.uv[y * _vertexSize + x] = new Vector2((float)x / inSize, (float)y / inSize);

        // Generate the triVertID's
        int currentQuad = 0;
        for (int y = 0; y < _vertexSize; y += 2)
            for (int x = 0; x < _vertexSize; x += 2)
            {
                int triangleOffset = currentQuad * 6;
                int currentVertex = y * _vertexSize + x;

                meshData.triangles[triangleOffset + 0] = currentVertex + 0;
                meshData.triangles[triangleOffset + 1] = currentVertex + _vertexSize + 1;
                meshData.triangles[triangleOffset + 2] = currentVertex + 1;

                meshData.triangles[triangleOffset + 3] = currentVertex + 0;
                meshData.triangles[triangleOffset + 4] = currentVertex + _vertexSize + 0;
                meshData.triangles[triangleOffset + 5] = currentVertex + _vertexSize + 1;

                currentQuad++;
            }
    }


    public void Generate(NoiseGenerator.Result inNoiseResult, Chunk inChunk)
    {
        MeshData newMeshData = new MeshData()
        {
            vertices = new Vector3[_vertexCount],
            uv = meshData.uv,
            triangles = meshData.triangles
        };

        // Generate the vertices of the mesh
        int vertexID = 0;
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                newMeshData.vertices[vertexID].x = x;
                newMeshData.vertices[vertexID].y = inNoiseResult.heightMap[x,y] * 100;
                newMeshData.vertices[vertexID].z = y;

                newMeshData.vertices[vertexID + 1].x = x + 1;
                newMeshData.vertices[vertexID + 1].y = inNoiseResult.heightMap[x + 1, y] * 100;
                newMeshData.vertices[vertexID + 1].z = y;

                newMeshData.vertices[vertexID + _vertexSize].x = x;
                newMeshData.vertices[vertexID + _vertexSize].y = inNoiseResult.heightMap[x, y + 1] * 100;
                newMeshData.vertices[vertexID + _vertexSize].z = y + 1;

                newMeshData.vertices[vertexID + _vertexSize + 1].x = x + 1;
                newMeshData.vertices[vertexID + _vertexSize + 1].y = inNoiseResult.heightMap[x + 1, y + 1] * 100;
                newMeshData.vertices[vertexID + _vertexSize + 1].z = y + 1;

                vertexID += 2;
            }
            vertexID += _vertexSize;
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
                pixels[y * size + x] = Color.Lerp(Color.black, Color.white, 0.5f);

        Result result = new Result();
        result.pixels = pixels;

        lock (_chunkGenerator._textureThreadInfoQueue)
            _chunkGenerator._textureThreadInfoQueue.Enqueue(() => _chunkGenerator.OnTextureDataRecieved(result, inChunk));
    }
}