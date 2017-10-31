using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

[CustomEditor(typeof(Test))]
public class TestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Test script = (Test)target;

        if (GUILayout.Button("Generate"))
            if (Application.isPlaying)
                script.Generate();
    }
}

public class Test : MonoBehaviour 
{
    public int noiseMapToCheck = 0;

    Material material;
    Noise noiseClass;

    private void Awake()
    {
        noiseClass = FindObjectOfType<Noise>();
        material = GetComponent<MeshRenderer>().material;
        material.mainTexture = new Texture2D(noiseClass.parameters[noiseMapToCheck].resolution, noiseClass.parameters[noiseMapToCheck].resolution);
        ((Texture2D)material.mainTexture).filterMode = FilterMode.Point;

        Generate();
    }



    public void Generate()
    {
        float[,] noiseMap = Noise.Generate(noiseClass.parameters[noiseMapToCheck], Vector2.zero);

        int resolution = noiseClass.parameters[noiseMapToCheck].resolution;

        Color[] colors = new Color[resolution * resolution];
        for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                colors[y * resolution + x] = Color.Lerp(Color.black, Color.white, noiseMap[x, y]);

        Texture2D texture = material.mainTexture as Texture2D;

        texture.SetPixels(colors);
        texture.Apply();
    }
}