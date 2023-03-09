using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Nebula
{
    [SerializeField]
    Vector2 pos;
    float size;

    public Nebula(Vector2 npos, float nsize) {
        pos = npos;
        size = nsize;
    }

    public Vector2 GetPos() {
        return pos;
    }

    public int GetRadius() {
        return (int)(size * 10);
    }

}
