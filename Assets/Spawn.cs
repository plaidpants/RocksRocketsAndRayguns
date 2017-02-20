using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Spawn : NetworkBehaviour {

    public float delay;
    public GameObject rocketSpherePrefab;

    // Use this for initialization
    void Start () {
        Invoke("spawnShip", delay);
	}

    void spawnShip()
    {
        //if (isLocalPlayer)
        {
            Destroy(transform.gameObject);
            //GameObject RocketSphere = Instantiate(rocketSpherePrefab, transform.position, transform.rotation) as GameObject;
            GameObject RocketSphere = Instantiate(rocketSpherePrefab, Vector3.zero, Quaternion.identity) as GameObject;
            NetworkServer.SpawnWithClientAuthority(RocketSphere, RocketSphere);
            //ClientScene.AddPlayer(
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}
