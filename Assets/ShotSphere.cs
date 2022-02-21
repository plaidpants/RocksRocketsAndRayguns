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
    //public int shooterColorIndex = -10; // used to keep track of the player who shot this shot
    public float lifeTime = 1.5f;
    bool destroyed = false;
    public GameObject shooter; // who shot the shot

    public override void OnStartServer()
    {
        base.OnStartServer();
 
        // this object is short lived
        Invoke(nameof(DestroySelf), lifeTime);
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

        shot.transform.position = transform.rotation * Vector3.forward * radius;
        shot.transform.rotation = transform.rotation;
    }

    // ServerCallback because we don't want a warning
    // if OnTriggerEnter is called on the client
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // check if rockets are running into their own shots or if this is also a shot
        if ((other.transform.gameObject == transform.gameObject) || other.CompareTag("Shot"))
        {
            // ignore our own shots
            return;
        }

        // don't destroy ourself more than once
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
        // don't destroy ourself more than once
        if (destroyed == false)
        {
            destroyed = true;
            NetworkServer.Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update ()
    {
		
	}
}
