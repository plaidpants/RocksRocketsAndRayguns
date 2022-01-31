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
        Debug.Log("Current level " + level);

        lastCount = RockSphere.count;
        if (RockSphere.count == 0)
        {
            level++;
            if (level > 3)
            {
                level = 1;
            }

            Debug.Log("Next level " + level);
            SceneManager.LoadScene(level);
        }
    }

    [ClientRpc]
    void RpcNextLevel()
    {
        Debug.Log("RpcNextLevel " + level);
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
            lastCount = RockSphere.count;
            Debug.Log("Rocks " + lastCount + " " + Random.Range(0.0f, 1.0f).ToString());
            if (RockSphere.count == 0)
            {
                Debug.Log("Rocks " + lastCount);
                RpcNextLevel();
            }
        }
    }
}
