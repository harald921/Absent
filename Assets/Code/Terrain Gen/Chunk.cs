using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Chunk
{
    GameObject _gameObject;
    public GameObject gameObject
    {
        get { return _gameObject; }
    }

    public Chunk(GameObject inChunkGO)
    {
        _gameObject = inChunkGO;
    }
}