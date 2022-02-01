using Mirror;
using UnityEngine;

// Custom NetworkManager that simply assigns the correct racket positions when
// spawning players. The built in RoundRobin spawn method wouldn't work after
// someone reconnects (both players would be on the same side).
[AddComponentMenu("")]
public class NetworkManagerRocket : NetworkManager
{
    GameObject RockField1;
    GameObject RockField2;

    private void OnLevelWasLoaded(int level)
    {
    }

    public override void OnStartHost()
    {
    }

    public override void OnStopHost()
    {
    }

    public static int level = 1;
    public int lastCount = 0;

    static bool preparing = false;

    void SwitchScenes()
    {
        RocketSphere[] players = FindObjectsOfType<RocketSphere>();

        for (int i = 0; i < players.Length; i++)
        {
            // deactivate all the ships on all clients
            players[i].RpcMySetActive(false, Quaternion.identity, true);

            // spawn ships, on all clients
            players[i].rpcSpawnShipDelay();
        }

        // switch to the next level
        ServerChangeScene("Game" + level);

        preparing = false;
    }

    void Update()
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

                preparing = true;

                // stop the music at the end of the level
                Camera.main.transform.gameObject.GetComponent<AudioSource>().Stop();

                // wait 10 seconds before switching to the next level
                Invoke("SwitchScenes", 10);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}