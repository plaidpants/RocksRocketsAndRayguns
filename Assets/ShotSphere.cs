using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShotSphere : NetworkBehaviour
{
    public GameObject shotPrefab;
    GameObject shot;
    public GameObject explosionPrefab;
    bool destroyed = false;

    public override void OnStartServer()
    {
        Invoke(nameof(DestroySelf), 1.5f);
    }

    // Use this for initialization
    void Start()
    {
        // get the current radius from position
        float radius = transform.position.magnitude;

        //reset the position back to the center
        transform.position = Vector3.zero;

        // we use the rotation that was set by the player object

        shot = transform.Find("shot").gameObject;

        //shot = Instantiate(shotPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        //shot.transform.SetParent(transform);
        shot.transform.position = transform.rotation * Vector3.forward * radius;
        shot.transform.rotation = transform.rotation;
        
        //Destroy(transform.gameObject, 1.5f);
    }

    // ServerCallback because we don't want a warning
    // if OnTriggerEnter is called on the client
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (destroyed == false)
        {
            destroyed = true;
            NetworkServer.Destroy(transform.gameObject);
        }
    }

    // destroy for everyone on the server
    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }

    // Update is called once per frame
    void Update ()
    {
		
	}
}
