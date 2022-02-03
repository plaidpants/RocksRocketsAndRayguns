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

	// Use this for initialization
    public override void OnStartServer()
    {
        base.OnStartServer();

        for (int i = 0; i < numberOfRocks; i++)
        {
            //calculate random position and rotate so it faces the center
            Vector3 pos = Random.onUnitSphere * radius;
            Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

            GameObject rock = Instantiate(RockSpherePrefab, pos, rot) as GameObject;

            NetworkServer.Spawn(rock);
            //rock.transform.SetParent(transform);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

        // Update is called once per frame
        void Update ()
    {
	}
}
