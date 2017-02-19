using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Levels : MonoBehaviour {

    public int level;

    // Use this for initialization
    void Start () {
        level = 0;
    }
	
	// Update is called once per frame
	void Update () {
        return;

        if (RockSphere.count == 0)
        {
            level++;
            if (level > 3)
            {
                level = 0;
            }
            Application.LoadLevel(level);
        }
    }
}
