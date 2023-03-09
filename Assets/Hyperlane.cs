using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Hyperlane
{
    [SerializeField]
    Vector2 start;

    [SerializeField]
    Vector2 end;

    public Hyperlane(StarSystem s, StarSystem t) {
        start = s.GetPos();
        end = t.GetPos();
    }

    public Vector2 GetStart() {
        return start;
    }

    public Vector2 GetEnd() {
        return end;
    }
}
