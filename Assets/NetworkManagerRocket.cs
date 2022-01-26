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
        RockField1 = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "RockField1"));
        RockField2 = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "RockField2"));
        NetworkServer.Spawn(RockField1);
        NetworkServer.Spawn(RockField2);
    }

    public override void OnStartHost()
    {
        RockField1 = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "RockField1"));
        RockField2 = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "RockField2"));
        NetworkServer.Spawn(RockField1);
        NetworkServer.Spawn(RockField2);
    }

    public override void OnStopHost()
    {
        // destroy Rockfield
        if (RockField1 != null)
            NetworkServer.Destroy(RockField1);
        if (RockField2 != null)
            NetworkServer.Destroy(RockField2);
    }
}