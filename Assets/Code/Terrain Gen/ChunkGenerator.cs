using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class ChunkGenerator
{
    public Noise.Parameters[] _parameters { private get; set; }

    NoiseGenerator   _noiseGenerator;
    MeshGenerator    _meshGenerator;
    TextureGenerator _textureGenerator;

    public Queue<Action> _meshThreadInfoQueue    = new Queue<Action>();
    public Queue<Action> _textureThreadInfoQueue = new Queue<Action>();

    Transform _worldTransform;

    AtlasManager _atlasManager;

    public ChunkGenerator(Noise.Parameters[] inParemeters)
    {
        _atlasManager   = GameObject.FindObjectOfType<AtlasManager>();
        _worldTransform = GameObject.Find("World").transform;

        _parameters = inParemeters;

        _noiseGenerator   = new NoiseGenerator(this);
        _meshGenerator    = new MeshGenerator(this, inParemeters[0].resolution);
        _textureGenerator = new TextureGenerator(this, inParemeters[0].resolution);
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
        newGO.transform.position = new Vector3(inOffset.x * _parameters[0].resolution, 0, -inOffset.y * _parameters[0].resolution);

        newGO.AddComponent<MeshFilter>();
        newGO.AddComponent<MeshRenderer>();

        Chunk newChunk = new Chunk(newGO);

        new Thread(() => GenerateChunkData(inOffset, newChunk)).Start();

        newGO.GetComponent<MeshRenderer>().material.mainTexture = GameObject.FindObjectOfType<AtlasManager>().atlas;
        newGO.GetComponent<MeshRenderer>().material.SetFloat("_Glossiness", 0.0f);
        newGO.GetComponent<MeshRenderer>().material.SetFloat("_Metallic", 0.0f);

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

        new Thread(() => _meshGenerator.Generate(noiseResult, _atlasManager, inChunk)).Start();

        //new Thread(() => _textureGenerator.Generate(noiseResult, inChunk)).Start();
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

        //Texture2D newTexture = new Texture2D(_parameters.size, _parameters.size);
        //newTexture.filterMode = FilterMode.Point;
        //newTexture.SetPixels(inResult.pixels);
        //newTexture.Apply();
    }
}




public class NoiseGenerator
{
    readonly ChunkGenerator _chunkGenerator;

    public class Result
    {
        public float[,] heightMap;
        public float[,] mountainMap;
    }


    public NoiseGenerator(ChunkGenerator inChunkGenerator)
    {
        _chunkGenerator = inChunkGenerator;
    }


    public void Generate(Result inResult, Noise.Parameters[] inParameters, Vector2 inOffset, Chunk inChunk)
    {
        inResult.heightMap = Noise.Generate(inParameters[0], inOffset);
        inResult.mountainMap = Noise.Generate(inParameters[1], inOffset);
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
    readonly MeshData _meshData;

    public MeshGenerator(ChunkGenerator inChunkGenerator, int inSize)
    {
        _chunkGenerator = inChunkGenerator;

        _size          = inSize;
        _vertexSize    = inSize * 2;
        _vertexCount   = inSize * inSize * 4;

        _meshData = new MeshData()
        {
            triangles = new int[inSize * inSize * 6]
        };

        // Generate the triVertID's
        int currentQuad = 0;
        for (int y = 0; y < _vertexSize; y += 2)
            for (int x = 0; x < _vertexSize; x += 2)
            {
                int triangleOffset = currentQuad * 6;
                int currentVertex = y * _vertexSize + x;

                _meshData.triangles[triangleOffset + 0] = currentVertex + 0;
                _meshData.triangles[triangleOffset + 1] = currentVertex + _vertexSize + 1;
                _meshData.triangles[triangleOffset + 2] = currentVertex + 1;

                _meshData.triangles[triangleOffset + 3] = currentVertex + 0;
                _meshData.triangles[triangleOffset + 4] = currentVertex + _vertexSize + 0;
                _meshData.triangles[triangleOffset + 5] = currentVertex + _vertexSize + 1;

                currentQuad++;
            }
    }


    public void Generate(NoiseGenerator.Result inNoiseResult, AtlasManager inAtlasManager, Chunk inChunk)
    {
        // Generate the vertices of the mesh
        Vector3[] newVertices = new Vector3[_vertexCount];
        int vertexID = 0;
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                newVertices[vertexID].x = x;
                newVertices[vertexID].y = inNoiseResult.heightMap[x,y] * 100 + inNoiseResult.mountainMap[x,y] * 200;
                newVertices[vertexID].z = y;

                newVertices[vertexID + 1].x = x + 1;
                newVertices[vertexID + 1].y = inNoiseResult.heightMap[x + 1, y] * 100 + inNoiseResult.mountainMap[x + 1, y] * 200;
                newVertices[vertexID + 1].z = y;

                newVertices[vertexID + _vertexSize].x = x;
                newVertices[vertexID + _vertexSize].y = inNoiseResult.heightMap[x, y + 1] * 100 + inNoiseResult.mountainMap[x, y +1 ] * 200;
                newVertices[vertexID + _vertexSize].z = y + 1;

                newVertices[vertexID + _vertexSize + 1].x = x + 1;
                newVertices[vertexID + _vertexSize + 1].y = inNoiseResult.heightMap[x + 1, y + 1] * 100 + inNoiseResult.mountainMap[x + 1, y + 1] * 200;
                newVertices[vertexID + _vertexSize + 1].z = y + 1;

                vertexID += 2;
            }
            vertexID += _vertexSize;
        }


        vertexID = 0;
        Vector2[] newUV = new Vector2[_vertexCount];
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                if (inNoiseResult.heightMap[x,y] < 1.0f)
                {
                    Vector2[] UVs = inAtlasManager.GetSpriteUVs(0, 0);

                    newUV[vertexID].x = UVs[0].x;
                    newUV[vertexID].y = UVs[0].y;

                    newUV[vertexID + 1].x = UVs[1].x;
                    newUV[vertexID + 1].y = UVs[1].y;

                    newUV[vertexID + _vertexSize].x = UVs[2].x;
                    newUV[vertexID + _vertexSize].y = UVs[2].y;

                    newUV[vertexID + _vertexSize + 1].x = UVs[3].x;
                    newUV[vertexID + _vertexSize + 1].y = UVs[3].y;
                }

                else
                {
                    Vector2[] UVs = inAtlasManager.GetSpriteUVs(1, 0);

                    newUV[vertexID].x = UVs[0].x;
                    newUV[vertexID].y = UVs[0].y;

                    newUV[vertexID + 1].x = UVs[1].x;
                    newUV[vertexID + 1].y = UVs[1].y;

                    newUV[vertexID + _vertexSize].x = UVs[2].x;
                    newUV[vertexID + _vertexSize].y = UVs[2].y;

                    newUV[vertexID + _vertexSize + 1].x = UVs[3].x;
                    newUV[vertexID + _vertexSize + 1].y = UVs[3].y;
                }

                vertexID += 2;
            }
            vertexID += _vertexSize;
        }

        MeshData newMeshData = new MeshData()
        {
            vertices  = newVertices,
            uv        = newUV,
            triangles = _meshData.triangles
        };

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

    readonly int _size;

    public TextureGenerator(ChunkGenerator inChunkGenerator, int inSize)
    {
        _chunkGenerator = inChunkGenerator;

        _size = inSize;
    }


    public void Generate(NoiseGenerator.Result inNoiseResult, Chunk inChunk)
    {
        Color[] pixels = new Color[_size * _size];

        for (int y = 0; y < _size; y++)
            for (int x = 0; x < _size; x++)
                pixels[y * _size + x] = Color.Lerp(Color.black, Color.white, 0.5f);

        Result result = new Result();
        result.pixels = pixels;

        lock (_chunkGenerator._textureThreadInfoQueue)
            _chunkGenerator._textureThreadInfoQueue.Enqueue(() => _chunkGenerator.OnTextureDataRecieved(result, inChunk));
    }
}