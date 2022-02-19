using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RockField : NetworkBehaviour
{
    public int initialNumberOfRocksToSpawn;
    public float radius;
    public GameObject rockSpherePrefab;
    public GameObject alienSpherePrefab;
    public int numberOfRocksLeftBeforeSpawningAlien = 5;
    public float minSpawnTimer = 10.0f;
    public float maxSpawnTimer = 30.0f;
    public bool contineSpawningRocks = false;
    public int maxAlienShips = 1;
    public int minRocks = 10;

    float timer = 0.0f;
    float timerExpired = 1.0f;

    // Use this for initialization
    public override void OnStartServer()
    {
        base.OnStartServer();

        // create a bunch of rocks
        for (int i = 0; i < initialNumberOfRocksToSpawn; i++)
        {
            //calculate random position and rotate so it faces the center
            Vector3 pos = Random.onUnitSphere * radius;
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

            GameObject rock = Instantiate(rockSpherePrefab, pos, rot) as GameObject;

            NetworkServer.Spawn(rock);
        }

        // initialize time for next ship
        timerExpired = Random.Range(minSpawnTimer, maxSpawnTimer);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    // Update is called once per frame
    void Update()
    {
        // extra spawning is only done on server
        if (!isServer)
            return;

        // update the timer
        timer += Time.deltaTime;

        // Check if we have expired the timer
        if (timer > timerExpired)
        {
            RockSphere[] Rocks = FindObjectsOfType<RockSphere>();

            // only spawn rocks when there are not enough and there is a prefab to spawn and we want to continue spawning rocks
            if (Rocks.Length < minRocks && rockSpherePrefab && contineSpawningRocks)
            {
                //calculate random position and rotate so it faces the center
                Vector3 pos = Random.onUnitSphere * radius;
                Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);
                GameObject rock = Instantiate(rockSpherePrefab, pos, rot) as GameObject;
                NetworkServer.Spawn(rock);
            }

            // only attempt to spawn aliens at the end of the level when there are less rocks but not if we finished the level
            if ((RockSphere.destroyedRocks >= numberOfRocksLeftBeforeSpawningAlien) && (Rocks.Length != 0))
            {
                RocketSphereAI[] objects = FindObjectsOfType<RocketSphereAI>();

                // only spawn an AI rocket if there isn't too many already spawned and we have a prefab to spawn
                if ((objects.Length < maxAlienShips) && alienSpherePrefab)
                {
                    Vector3 pos = Random.onUnitSphere * radius;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);
                    GameObject alien = Instantiate(alienSpherePrefab, pos, rot) as GameObject;
                    NetworkServer.Spawn(alien);
                }
            }

            // update the time, important to subtract the last timer value instead of reseting to zero
            // as we make not have expired the time at precisely the timer period
            timer = timer - timerExpired;

            // next timer check is random also
            timerExpired = Random.Range(minSpawnTimer, maxSpawnTimer);
        }
    }
}
