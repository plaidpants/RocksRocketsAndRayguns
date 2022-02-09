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
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    // Update is called once per frame
    void Update ()
    {
        // update the timer
        timer += Time.deltaTime;

        // Check if we have expired the timer
        if (timer > timerExpired)
        {
            // generate a new AI rocket
            RocketSphereAI objects = FindObjectOfType<RocketSphereAI>();

            if (!objects && Alien)
            {
                Vector3 pos = Random.onUnitSphere * radius;
                Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);
                GameObject alien = Instantiate(Alien, pos, rot) as GameObject;
                NetworkServer.Spawn(alien);
            }

            // update the time, important to subtract the last timer value instead of reseting to zero
            // as we make not have expired the time at precisely the timer period
            timer = timer - timerExpired;

            // next timer check is random also
            timerExpired = Random.Range(1.0f, 5.0f);
        }
    }
}
