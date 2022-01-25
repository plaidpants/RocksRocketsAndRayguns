using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Levels : NetworkBehaviour
{
    public static int level = 0;
    public int lastCount = -1;

    // Use this for initialization
    void Start ()
    {
        level = 0;
    }

    void NextLevel()
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

    [ClientRpc]
    void RpcNextLevel()
    {
        NextLevel();
    }

    [Command]
    void CmdNextLevel()
    {
        RpcNextLevel();
    }

    // Update is called once per frame
    void Update ()
    {
        if (RockSphere.count != lastCount)
        {
            if (RockSphere.count == 0)
            {
                RpcNextLevel();
            }
        }
    }
}
