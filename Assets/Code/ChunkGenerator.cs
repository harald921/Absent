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

    public Queue<Action> _noiseThreadInfoQueue   = new Queue<Action>();
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

        RequestNoiseData(inOffset, newChunk);

        return newChunk;
    }


    // Internal
    void ProcessQueues()
    {
        while (_noiseThreadInfoQueue.Count > 0)
            _noiseThreadInfoQueue.Dequeue()();

        while (_meshThreadInfoQueue.Count > 0)
            _meshThreadInfoQueue.Dequeue()();

        while (_textureThreadInfoQueue.Count > 0)
            _textureThreadInfoQueue.Dequeue()();
    }



    // Request methods
    void RequestNoiseData(Vector2 inOffset, Chunk inChunk)
    {
        NoiseGenerator.Result result = new NoiseGenerator.Result();
        Action callbackMethod = new Action(() => OnNoiseDataRecieved(result, inChunk));

        ThreadStart threadStart = delegate { _noiseGenerator.Generate(callbackMethod, result, _parameters, inOffset); };

        new Thread(threadStart).Start();
    }


    void RequestMeshData(float[,] inNoiseMap, Chunk inChunk)
    {
        MeshGenerator.Result result = new MeshGenerator.Result();
        Action callbackMethod = new Action(() => OnMeshDataRecieved(result, inChunk));

        ThreadStart threadStart = delegate { _meshGenerator.Generate(callbackMethod, result, inNoiseMap); };

        new Thread(threadStart).Start();
    }

    void RequestTextureData(float[,] inNoiseMap, Chunk inChunk)
    {
        TextureGenerator.Result result = new TextureGenerator.Result();
        Action callbackMethod = new Action(() => OnTextureDataRecieved(result, inChunk));

        ThreadStart threadStart = delegate { _textureGenerator.Generate(callbackMethod, result, inNoiseMap); };

        new Thread(threadStart).Start();
    }



    // Recieved methods
    void OnNoiseDataRecieved(NoiseGenerator.Result inResult, Chunk inChunk)
    {
        RequestMeshData(inResult.heightMap, inChunk);
        RequestTextureData(inResult.heightMap, inChunk);
    }

    void OnMeshDataRecieved(MeshGenerator.Result inResult, Chunk inChunk)
    {
        if (!inChunk.gameObject)
            return;

        Mesh generatedMesh      = inChunk.gameObject.GetComponent<MeshFilter>().mesh;
        generatedMesh.vertices  = inResult.meshData.vertices;
        generatedMesh.normals   = inResult.meshData.normals;
        generatedMesh.uv        = inResult.meshData.uv;
        generatedMesh.triangles = inResult.meshData.triVertIDs;
        
        generatedMesh.RecalculateNormals(); // http://schemingdeveloper.com/2014/10/17/better-method-recalculate-normals-unity/

        inChunk.gameObject.GetComponent<MeshFilter>().mesh = generatedMesh;
    }

    void OnTextureDataRecieved(TextureGenerator.Result inResult, Chunk inChunk)
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


    public void Generate(Action inCallbackMethod, Result inResult, Noise.Parameters inParameters, Vector2 inOffset)
    {
        inResult.heightMap = Noise.Generate(inParameters, inOffset);

        lock (_chunkGenerator._noiseThreadInfoQueue)
            _chunkGenerator._noiseThreadInfoQueue.Enqueue(inCallbackMethod);
    }
}




public class MeshGenerator
{
    readonly ChunkGenerator _chunkGenerator;

    public class Result
    {
        public MeshData meshData;
    }

    readonly int size;
    readonly int tileCount;
    readonly int triangleCount;
    readonly int vertexSize;
    readonly int vertexCount;

    public readonly Vector3[] normals;
    public readonly Vector2[] uv;
    public readonly int[] triVertIDs;

    public struct MeshData
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv;
        public int[]     triVertIDs;
    }
    MeshData meshData;

    public MeshGenerator(ChunkGenerator inChunkGenerator, int inSize)
    {
        _chunkGenerator = inChunkGenerator;

        size          = inSize;
        tileCount     = size * size;
        triangleCount = tileCount * 2;
        vertexSize    = size + 1;
        vertexCount   = vertexSize * vertexSize;

        meshData = new MeshData()
        {
            normals    = new Vector3[vertexCount],
            uv         = new Vector2[vertexCount],
            triVertIDs = new int[triangleCount * 3]
        };

        // Generate the normals and UVs
        for (int y = 0; y < vertexSize; y++)
            for (int x = 0; x < vertexSize; x++)
            {
                int currentIndex = y * vertexSize + x;

                meshData.normals[currentIndex] = Vector2.up;
                meshData.uv[currentIndex] = new Vector2((float)x / size, (float)y / size);
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
                    meshData.triVertIDs[triangleOffset + 0] = triVertOffset + 0;
                    meshData.triVertIDs[triangleOffset + 1] = triVertOffset + vertexSize + 0;
                    meshData.triVertIDs[triangleOffset + 2] = triVertOffset + vertexSize + 1;
                    meshData.triVertIDs[triangleOffset + 3] = triVertOffset + 0;
                    meshData.triVertIDs[triangleOffset + 4] = triVertOffset + vertexSize + 1;
                    meshData.triVertIDs[triangleOffset + 5] = triVertOffset + 1;
                }

                else
                {
                    meshData.triVertIDs[triangleOffset + 0] = triVertOffset + 0;
                    meshData.triVertIDs[triangleOffset + 1] = triVertOffset + vertexSize + 0;
                    meshData.triVertIDs[triangleOffset + 2] = triVertOffset + 1;
                    meshData.triVertIDs[triangleOffset + 3] = triVertOffset + 1;
                    meshData.triVertIDs[triangleOffset + 4] = triVertOffset + vertexSize + 0;
                    meshData.triVertIDs[triangleOffset + 5] = triVertOffset + vertexSize + 1;
                }

                diagonal = !diagonal;
            }
        }
    }


    public void Generate(Action inCallbackMethod, Result inResult, float[,] inNoiseMap)
    {
        MeshData newMeshData = new MeshData()
        {
            vertices = new Vector3[vertexCount],
            normals = meshData.normals,
            uv = meshData.uv,
            triVertIDs = meshData.triVertIDs
        };

        // Generate the vertices of the mesh
        for (int y = 0; y < vertexSize; y++)
            for (int x = 0; x < vertexSize; x++)
            {
                int iteration = y * vertexSize + x;

                newMeshData.vertices[iteration].x = x;
                newMeshData.vertices[iteration].y = inNoiseMap[x,y] * 100; // Possible performance tweak
                newMeshData.vertices[iteration].z = y;
            }

        inResult.meshData = newMeshData;

        lock (_chunkGenerator._meshThreadInfoQueue)
            _chunkGenerator._meshThreadInfoQueue.Enqueue(inCallbackMethod);
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


    public void Generate(Action inCallbackMethod, Result inResult, float[,] inNoiseMap)
    {
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = Color.Lerp(Color.black, Color.white, inNoiseMap[x,y]);

        inResult.pixels = pixels;

        lock (_chunkGenerator._textureThreadInfoQueue)
            _chunkGenerator._textureThreadInfoQueue.Enqueue(inCallbackMethod);
    }
}