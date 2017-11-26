using UnityEngine;

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class ChunkGenerator
{
    // Fields
    readonly Noise.Parameters[] _parameters;

    readonly NoiseGenerator _noiseGenerator;
    readonly MeshGenerator  _meshGenerator;

    readonly Transform _worldTransform;

    // Constructor
    public ChunkGenerator(Noise.Parameters[] inParemeters)
    {
        _worldTransform = GameObject.Find("World").transform;

        _parameters = inParemeters;

        _noiseGenerator   = new NoiseGenerator();
        _meshGenerator    = new MeshGenerator(inParemeters[0].resolution);
    }

    public Chunk GenerateChunk(Vector2 inOffset, Material veryTemp)
    {
        GameObject newGO = new GameObject();
        newGO.transform.SetParent(_worldTransform);
        newGO.transform.position = new Vector3(inOffset.x * _parameters[0].resolution, 0, -inOffset.y * _parameters[0].resolution);

        newGO.AddComponent<MeshFilter>();
        newGO.AddComponent<MeshRenderer>();

        Chunk newChunk = new Chunk(newGO);

        new Thread(() => GenerateChunkData(inOffset, newChunk)).Start();

        newGO.GetComponent<MeshRenderer>().material = veryTemp;
        newGO.GetComponent<MeshRenderer>().material.SetFloat("_Glossiness", 0.0f);
        newGO.GetComponent<MeshRenderer>().material.SetFloat("_Metallic", 0.0f);

        return newChunk;
    }


    // Internal
    void GenerateChunkData(Vector2 inOffset, Chunk inChunk)
    {
        NoiseGenerator.Result noiseResult = _noiseGenerator.Generate(_parameters, inOffset);

        JobSystem.instance.DoThreaded(() => _meshGenerator.Generate(noiseResult), (o) => OnMeshDataRecieved(o, inChunk));
    }

    void OnMeshDataRecieved(object inResult, Chunk inChunk)
    {
        if (!inChunk.gameObject)
            return;

        MeshGenerator.Result result = (MeshGenerator.Result)inResult;

        Mesh chunkMesh = inChunk.gameObject.GetComponent<MeshFilter>().mesh;

        chunkMesh.vertices  = result.vertices;
        chunkMesh.uv        = result.uv;
        chunkMesh.uv2       = result.uv2;
        chunkMesh.triangles = result.triangles;

        chunkMesh.RecalculateNormals(); // http://schemingdeveloper.com/2014/10/17/better-method-recalculate-normals-unity/

        inChunk.gameObject.GetComponent<MeshFilter>().mesh = chunkMesh;
    }
}




public class NoiseGenerator
{
    public class Result
    {
        public float[,] heightMap;
    }

    public Result Generate(Noise.Parameters[] inParameters, Vector2 inOffset)
    {
        Result result = new Result();
        result.heightMap = Noise.Generate(inParameters[0], inOffset);
        return result;
    }
}




public class MeshGenerator
{
    // Cached data
    readonly int   _size;
    readonly int   _vertexSize;
    readonly int   _vertexCount;
    readonly int[] _triangles;


    // What the generate method will return
    public class Result 
    {
        public Vector3[] vertices;
        public Vector2[] uv;
        public Vector2[] uv2;
        public int[]     triangles;
    }


    // Constructor
    public MeshGenerator(int inSize)
    {
        _size        = inSize;
        _vertexSize  = inSize * 2;
        _vertexCount = inSize * inSize * 4;

        // Generate triangle IDs
        _triangles = new int[inSize * inSize * 6];
        int currentQuad = 0;
        for (int y = 0; y < _vertexSize; y += 2)
            for (int x = 0; x < _vertexSize; x += 2)
            {
                int triangleOffset = currentQuad * 6;
                int currentVertex  = y * _vertexSize + x;

                _triangles[triangleOffset + 0] = currentVertex + 0;
                _triangles[triangleOffset + 1] = currentVertex + _vertexSize + 1;
                _triangles[triangleOffset + 2] = currentVertex + 1;

                _triangles[triangleOffset + 3] = currentVertex + 0;
                _triangles[triangleOffset + 4] = currentVertex + _vertexSize + 0;
                _triangles[triangleOffset + 5] = currentVertex + _vertexSize + 1;

                currentQuad++;
            }
    }


    public Result Generate(NoiseGenerator.Result inHeightData)
    {
        Result result = new Result();
        result.triangles = _triangles;

        // Generate vertices
        result.vertices = new Vector3[_vertexCount];
        int vertexID = 0;
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                result.vertices[vertexID].x = x;
                result.vertices[vertexID].y = inHeightData.heightMap[x,y] * 100;
                result.vertices[vertexID].z = y;

                result.vertices[vertexID + 1].x = x + 1;
                result.vertices[vertexID + 1].y = inHeightData.heightMap[x + 1, y] * 100;
                result.vertices[vertexID + 1].z = y;

                result.vertices[vertexID + _vertexSize].x = x;
                result.vertices[vertexID + _vertexSize].y = inHeightData.heightMap[x, y + 1] * 100;
                result.vertices[vertexID + _vertexSize].z = y + 1;

                result.vertices[vertexID + _vertexSize + 1].x = x + 1;
                result.vertices[vertexID + _vertexSize + 1].y = inHeightData.heightMap[x + 1, y + 1] * 100;
                result.vertices[vertexID + _vertexSize + 1].z = y + 1;

                vertexID += 2;
            }
            vertexID += _vertexSize;
        }


        // Generate UV2 (This is temporary)
        result.uv  = new Vector2[_vertexCount];
        result.uv2 = new Vector2[_vertexCount];
        vertexID = 0;
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                if (inHeightData.heightMap[x, y] < 0.8f)
                {
                    int textureID = 2;

                    result.uv2[vertexID]                   = new Vector2(textureID, textureID);
                    result.uv2[vertexID + 1]               = new Vector2(textureID, textureID);
                    result.uv2[vertexID + _vertexSize]     = new Vector2(textureID, textureID);
                    result.uv2[vertexID + _vertexSize + 1] = new Vector2(textureID, textureID);
                }

                else
                {
                    int textureID = 1;

                    result.uv2[vertexID]                   = new Vector2(textureID, textureID + 1);
                    result.uv2[vertexID + 1]               = new Vector2(textureID, textureID + 1);
                    result.uv2[vertexID + _vertexSize]     = new Vector2(textureID, textureID + 1);
                    result.uv2[vertexID + _vertexSize + 1] = new Vector2(textureID, textureID + 1);
                }

                vertexID += 2;
            }
            vertexID += _vertexSize;
        }


        return result;
    }
}