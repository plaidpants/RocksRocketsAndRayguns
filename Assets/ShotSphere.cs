using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotSphere : MonoBehaviour {

    public GameObject shotPrefab;
    GameObject shot;
    public GameObject explosionPrefab;
    bool destroyed = false;

    // Use this for initialization
    void Start()
    {
        // get the current radius from position
        float radius = transform.position.magnitude;

        //reset the position back to the center
        transform.position = Vector3.zero;

        shot = transform.FindChild("shot").gameObject;

        //shot = Instantiate(shotPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        //shot.transform.SetParent(transform);
        shot.transform.position = transform.rotation * Vector3.forward * radius;
        shot.transform.rotation = transform.rotation;
        
        Destroy(transform.gameObject, 1.5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (destroyed == false)
        {
            destroyed = true;
            Destroy(transform.gameObject);
            //GameObject explosion = Instantiate(explosionPrefab, shot.transform.position, Quaternion.identity) as GameObject;
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}
