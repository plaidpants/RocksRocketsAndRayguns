using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Levels : NetworkBehaviour
{
    public static int level = 0;
    public int lastCount = 0;

    // Use this for initialization
    void Start ()
    {
        level = 0;
    }

    // Update is called once per frame
    void Update ()
    {
        if (RockSphere.count != lastCount)
        {
            lastCount = RockSphere.count;
            if (RockSphere.count == 0)
            {
                level++;
                if (level > 3)
                {
                    level = 1;
                }

                SceneManager.LoadScene(level);
            }
        }
    }
}
