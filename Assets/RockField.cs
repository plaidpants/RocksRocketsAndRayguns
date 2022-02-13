using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RockField : NetworkBehaviour
{
    public int numberOfRocks;
    public float radius;
    public GameObject RockSpherePrefab;
    public GameObject Alien;
    public int NumberOfRockLeftBeforeSpawning = 5;
    public float minSpawnTimer = 10.0f;
    public float maxSpawnTimer = 30.0f;
    public bool contineSpawningRocks = false;

    float timer = 0.0f;
    float timerExpired = 1.0f;

    // Use this for initialization
    public override void OnStartServer()
    {
        base.OnStartServer();

        // create a bunch of rocks
        for (int i = 0; i < numberOfRocks; i++)
        {
            //calculate random position and rotate so it faces the center
            Vector3 pos = Random.onUnitSphere * radius;
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

            GameObject rock = Instantiate(RockSpherePrefab, pos, rot) as GameObject;

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
        // update the timer
        timer += Time.deltaTime;

        // Check if we have expired the timer
        if (timer > timerExpired)
        {
            // generate a new AI rocket
            RocketSphereAI[] objects = FindObjectsOfType<RocketSphereAI>();

            // only spawn an AI rocket if there isn't one already spawned
            if (objects.Length < 10 && Alien)
            {
                Vector3 pos = Random.onUnitSphere * radius;
                Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);
                GameObject alien = Instantiate(Alien, pos, rot) as GameObject;
                NetworkServer.Spawn(alien);
            }

            // 
            RockSphere[] Rocks = FindObjectsOfType<RockSphere>();

            // only spawn the ai rocket at the end of a level
            if (Rocks.Length < 10 && RockSpherePrefab)
            {
                if (contineSpawningRocks && isServer)
                {
                    //calculate random position and rotate so it faces the center
                    Vector3 pos = Random.onUnitSphere * radius;
                    Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);
                    GameObject rock = Instantiate(RockSpherePrefab, pos, rot) as GameObject;
                    NetworkServer.Spawn(rock);
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
