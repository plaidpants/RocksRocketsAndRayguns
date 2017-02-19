using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour {

    public float delay;
    public GameObject rocketSpherePrefab;

    // Use this for initialization
    void Start () {
        Invoke("spawnShip", delay);
	}

    void spawnShip()
    {
        Destroy(transform.gameObject);
        //GameObject RocketSphere = Instantiate(rocketSpherePrefab, transform.position, transform.rotation) as GameObject;
        GameObject RocketSphere = Instantiate(rocketSpherePrefab, Vector3.zero, Quaternion.identity) as GameObject;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
