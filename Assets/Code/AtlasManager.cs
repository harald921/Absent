using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtlasManager : MonoBehaviour
{
    [SerializeField] Texture2D _atlas;
    public Texture2D atlas
    {
        get
        {
            return _atlas;
        }
    }
}
