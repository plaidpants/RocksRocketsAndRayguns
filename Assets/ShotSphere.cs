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
    public int playerShooterColorIndex = -10; // used to keep track of the player who shot this shot
    bool destroyed = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
 
        // this object is short lived
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

        shot.transform.position = transform.rotation * Vector3.forward * radius;
        shot.transform.rotation = transform.rotation;
    }

    // ServerCallback because we don't want a warning
    // if OnTriggerEnter is called on the client
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // get the collider rocket if it exists
        RocketSphere rocket = other.attachedRigidbody.GetComponent<RocketSphere>();

        // have we collided with a rocket
        if (rocket)
        {
            // check if we are running into our own shots
            if (rocket.rocketColorIndex == playerShooterColorIndex)
            {
                // ignore our own shots
                return;
            }
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
