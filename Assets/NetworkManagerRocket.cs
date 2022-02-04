using Mirror;
using UnityEngine;

[AddComponentMenu("")]
public class NetworkManagerRocket : NetworkManager
{
    private void OnLevelWasLoaded(int level)
    {
    }

    public override void OnStartHost()
    {
    }

    public override void OnStopHost()
    {
    }

    // need to persist this across instances.
    public static int level = 1;

    // used to avoid transitioning immediately before any rocks have been created in the level
    public int lastRockCount = 0;

    void SwitchScenes()
    {
        // find all the players
        RocketSphere[] players = FindObjectsOfType<RocketSphere>();

        // go through all the players
        for (int i = 0; i < players.Length; i++)
        {
            // deactivate all the ships on all clients
            players[i].RpcMySetActive(false, Quaternion.identity, true);

            // spawn ships, on all clients after a shot delay after which the scene will have been changed.
            players[i].rpcSpawnShipDelay();
        }

        // switch to the next level
        ServerChangeScene("Game" + level);

        // reset the static rock stats
        RockSphere.ResetRockStats();
    }

    void NextLevel()
    {
        // increment the level count
        level++;

        // we only have six levels
        if (level > 6)
        {
            level = 1;
        }

        // wait for outro music + last music loop + 5 seconds before switching to the next level
        Invoke("SwitchScenes", Camera.main.transform.gameObject.GetComponent<MusicHandler>().outroMusicClip.length + 
            Camera.main.transform.gameObject.GetComponent<MusicHandler>().musicClipLoops[Camera.main.transform.gameObject.GetComponent<MusicHandler>().musicClipLoops.Length - 1].length 
            + 5);
    }

    void Update()
    {
        // only check for zero rocks if the rock count changes
        if (RockSphere.currentRocks != lastRockCount)
        {
            lastRockCount = RockSphere.currentRocks;
            // are we done destorying rocks
            if (RockSphere.currentRocks == 0)
            {
                // go to next level
                NextLevel();
            }
        }

        // debug TODO: remove later
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextLevel();
        }

        // allow the desktop game to quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}