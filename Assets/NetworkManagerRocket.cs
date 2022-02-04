using Mirror;
using UnityEngine;

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

        RockSphere.ResetRockStats();

        preparing = false;
    }

    void StopMusic()
    {
        RocketSphere[] players = FindObjectsOfType<RocketSphere>();

        for (int i = 0; i < players.Length; i++)
        {
            // deactivate music all the ships on all clients
            players[i].RpcStopMusic();
        }
    }

    void NextLevel()
    {
        level++;
        if (level > 6)
        {
            level = 1;
        }

        preparing = true;

        StopMusic();

        // wait for outro music + last music loop + 5 seconds before switching to the next level
        Invoke("SwitchScenes", Camera.main.transform.gameObject.GetComponent<MusicHandler>().outroMusicClip.length + 
            Camera.main.transform.gameObject.GetComponent<MusicHandler>().musicClipLoops[Camera.main.transform.gameObject.GetComponent<MusicHandler>().musicClipLoops.Length - 1].length 
            + 5);
    }

    void Update()
    {
        if (RockSphere.currentRocks != lastCount)
        {
            lastCount = RockSphere.currentRocks;
            if (RockSphere.currentRocks == 0)
            {
                NextLevel();
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextLevel();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}