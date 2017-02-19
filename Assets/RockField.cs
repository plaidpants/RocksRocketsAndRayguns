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
	void Start ()
    {
        if (!isServer)
        {
            return;
        }

        for (int i = 0; i< numberOfRocks;i++)
        {
            //calculate random position and rotate so it faces the center
            Vector3 pos = Random.onUnitSphere * radius;

            GameObject rock = Instantiate(RockSpherePrefab, pos, Quaternion.identity) as GameObject;
            //rock.transform.SetParent(transform);

            // Spawn the rock on the Clients
            NetworkServer.Spawn(rock);
        }
    }
    

// Update is called once per frame
void Update () {
		
	}
}
