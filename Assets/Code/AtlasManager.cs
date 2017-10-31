using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtlasManager : MonoBehaviour
{
    [SerializeField] int _spriteResolution = 512;
    [SerializeField] int _paddingThickness = 10; // Implement

    [SerializeField] Texture2D _atlas;
    public Texture2D atlas
    {
        get
        {
            return _atlas;
        }
    }

    float _spriteUVSize;
    float _borderUVSize;

    private void Start()
    {
        _spriteUVSize = 1.0f / (_atlas.width / _spriteResolution);
        _borderUVSize = 1.0f / (_atlas.width / _paddingThickness);
    }
    
    public Vector2[] GetSpriteUVs(int inX, int inY)
    {
        Vector2 botLeft  = new Vector2(inX * _spriteUVSize,                 inY * _spriteUVSize);
        Vector2 botRight = new Vector2(inX * _spriteUVSize + _spriteUVSize, inY * _spriteUVSize);
        Vector2 topLeft  = new Vector2(inX * _spriteUVSize,                 inY * _spriteUVSize + _spriteUVSize);
        Vector2 topRight = new Vector2(inX * _spriteUVSize + _spriteUVSize, inY * _spriteUVSize + _spriteUVSize);

        botLeft.x += _borderUVSize;
        botLeft.y += _borderUVSize;

        botRight.x += _borderUVSize;
        botRight.y += _borderUVSize;

        topLeft.x += _borderUVSize;
        topLeft.y += _borderUVSize;

        topRight.x += _borderUVSize;
        topRight.y += _borderUVSize;

        Vector2[] UVs =
        {
            botLeft,
            botRight,
            topLeft,
            topRight
        };

        return UVs;
    }
}
