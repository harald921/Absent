using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour
{
    Noise.Parameters _parameters;
    ChunkGenerator _chunkGenerator;

    [SerializeField] int _radius = 3;

    Dictionary<Vector2, Chunk> _worldChunks = new Dictionary<Vector2, Chunk>();
    List<Vector2> _chunksToGenerate = new List<Vector2>();
    List<Vector2> _chunksToDelete   = new List<Vector2>();

    Transform _camTransform;

    Vector2 _currentCameraChunkCoord = Vector2.one;
    public Vector2 currentCameraChunkCoord
    {
        get { return _currentCameraChunkCoord; }

        private set
        {
            if (_currentCameraChunkCoord != value)
                OnCameraChunkEnter(value);

            _currentCameraChunkCoord = value;
        }
    }

    bool _isGeneratingChunks = false;


    void Start()
    {
        _camTransform = Camera.main.transform;

        _parameters = GetComponent<Noise>().parameters;

        int meshSize = _parameters.size + 1;

        _chunkGenerator = new ChunkGenerator(meshSize, _parameters);
    }

    void Update()
    {
        currentCameraChunkCoord = GetChunkCoord(_camTransform.position);

        ProcessQueues();
    }

    void ProcessQueues()
    {
        if (!_isGeneratingChunks)
            StartCoroutine(GenerateChunks());

        if (_chunksToDelete.Count > 0)
            DeleteChunks();
            
    }

    void DeleteChunks()
    {
        while (_chunksToDelete.Count > 0)
        {
            Destroy(_worldChunks[_chunksToDelete[0]].gameObject.GetComponent<MeshFilter>().mesh);
            Destroy(_worldChunks[_chunksToDelete[0]].gameObject.GetComponent<MeshRenderer>().material);

            Destroy(_worldChunks[_chunksToDelete[0]].gameObject);
            _worldChunks.Remove(_chunksToDelete[0]);
            _chunksToDelete.RemoveAt(0);
        }
    }


    Vector2 GetChunkCoord(Vector3 inWorldPosition)
    {
        Vector2 chunkCoords = new Vector2(inWorldPosition.x / _parameters.size, -(inWorldPosition.z / _parameters.size));

        chunkCoords.x = Mathf.Floor(chunkCoords.x);
        chunkCoords.y = Mathf.Ceil(chunkCoords.y);

        return chunkCoords;
    }

    void OnCameraChunkEnter(Vector2 inChunkCoord)
    {
        int chunksToSide = (_radius - 1) / 2;

        List<Vector2> visibleChunkCoords = new List<Vector2>();

        // Calculate the coordinates of all the chunks that are within the radius

        for (int zCircle = -_radius; zCircle <= _radius; zCircle++)
            for (int xCircle = -_radius; xCircle <= _radius; xCircle++)
                if (xCircle * xCircle + zCircle * zCircle < _radius * _radius)
                    visibleChunkCoords.Add(new Vector2(xCircle - chunksToSide + inChunkCoord.x, 
                                                       zCircle - chunksToSide + inChunkCoord.y));
        
        // Add all the visible chunks to the delete queue
        _chunksToDelete.Clear();
        _chunksToDelete.AddRange(_worldChunks.Keys);

        // Queue up coords to generate chunks on
        for (int i = 0; i < visibleChunkCoords.Count; i++)
        {
            // Add visible non-generated chunks to the generation-queue
            if (!_worldChunks.ContainsKey(visibleChunkCoords[i]))
                _chunksToGenerate.Add(visibleChunkCoords[i]);

            // Remove visible and generated chunks from the delete queue
            else if (_chunksToDelete.Contains(visibleChunkCoords[i]))
                _chunksToDelete.Remove(visibleChunkCoords[i]);
        }
    }


    IEnumerator GenerateChunks()
    {
        _isGeneratingChunks = true;
        
        while (_chunksToGenerate.Count > 0)
        {
            Vector2 newChunkCoord = _chunksToGenerate[0];

            _worldChunks.Add(newChunkCoord, _chunkGenerator.GenerateChunk(newChunkCoord));

            _chunksToGenerate.RemoveAt(0);

           // yield return null; //I don't think this should be here?
        }


        _isGeneratingChunks = false;
        yield return null;
    }
}

// Fix chunks not seamlessly aligning