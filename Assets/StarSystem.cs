using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StarSystem
{

    // Settings
    int id;
    [SerializeField]
    string name;
    [SerializeField]
    Vector2 position;
    Vector2 min_pos; // if position is randomized
    Vector2 max_pos; 
    [SerializeField]
    string initializer;

    // Do spawn weights later
    
    public StarSystem(Vector2 syspos) {
        position = syspos;
    }

    public void SetName(string nname) {
        string cleaned_name = "";
        for (int i = 0; i < nname.Length; i++) {
            switch(nname[i]) {
                case ' ':
                    break;
                case '\n':
                    break;
                case '\r':
                    break;
                default:
                    cleaned_name += nname[i];
                    break;
            }
        }
        if (cleaned_name.Length > 0) {
            name = cleaned_name;
        }
    }

    public void SetInitializer(string init) {
        string cleaned_init = "";
        for (int i = 0; i < init.Length; i++) {
            switch(init[i]) {
                case ' ':
                    break;
                case '\n':
                    break;
                case '\r':
                    break;
                default:
                    cleaned_init += init[i];
                    break;
            }
        }
        if (cleaned_init.Length > 0) {
            initializer = cleaned_init;
        }
    }

    public void SetID(int nid) {
        id = nid;
    }

    public Vector2 GetPos() {
        return position;
    }

    public string GetName() {
        return name;
    }

    public string GetInitializer() {
        return initializer;
    }

    public int GetID() {
        return id;
    }

}
