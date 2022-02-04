using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RockSphere : NetworkBehaviour
{
    GameObject rock;
    public float minSpeed;
    public float maxSpeed;
    public GameObject explosionPrefab;
    public GameObject rockSpherePrefab;
    public int pieces;
    //public float rotationSpeed;
    [SyncVar] public bool destroyed = false;
    [SyncVar] public float radius = 0;
    [SyncVar] public Vector3 pos = Vector3.zero;
    [SyncVar] public Quaternion rot = Quaternion.identity;
    static public int currentRocks = 0; // number of rocks currently active
    static public int totalRocks = 0; // number of rocks currently active plus subsequent rocks
    static public int destroyedRocks = 0; // number of rocks destoryed

    [ClientRpc]
    void rpcSetRockStats(int current, int total, int destroyed)
    {
        currentRocks = current;
        totalRocks = total;
        destroyedRocks = destroyed;

        Debug.Log("client RPC Rocks " + current + " total " + total + " destoyed " + destroyed);
    }

    [Server]
    public static void ResetRockStats()
    {
        currentRocks = 0; // number of rocks currently active
        totalRocks = 0; // number of rocks currently active plus subsequent rocks
        destroyedRocks = 0; // number of rocks destoryed
    }

    // Use this for initialization
    public override void OnStartServer()
    {
        // call the base function, probably is empty
        base.OnStartServer();

        // only do the final rock positioning for the rock on the server use syncvars to sync with the clients
 
        // get the current radius and position from parent gameobject
        radius = transform.position.magnitude;
        pos = transform.position;

        // get the current rotation from the parent position
        rot = Quaternion.FromToRotation(Vector3.forward, pos);

        // reset the parent gameobject position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // move the child rock to original location and rotation
        rock = transform.Find("Rock.old").gameObject;
        rock.transform.localPosition = pos;
        rock.transform.localRotation = rot;

        // apply some rotational torque to the parent gameobject object with the rock attached as a child
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 torque = Random.onUnitSphere * (Random.Range(minSpeed, maxSpeed) / radius);
        // ???? note this could be pointing right at us or away so no torque would be added need to catch this and get a new torque
        rb.AddTorque(Vector3.Cross(torque, pos.normalized));

        totalRocks++;
        currentRocks++;

        //Debug.Log("current rocks " + currentRocks + " Rocks " + currentRocks + " total " + totalRocks + " destoyed " + destroyedRocks);

        rpcSetRockStats(currentRocks, totalRocks, destroyedRocks);
    }

    void Start()
    {
    }

    public override void OnStartClient()
    {
        // move the child rock to original location and rotation from the syncvars since mirror will not do this for us
        // the rock has moved since creation for clients connecting mid-game, offset is in local coords
        rock = transform.Find("Rock.old").gameObject;
        rock.transform.localPosition = pos;
        rock.transform.localRotation = rot;
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (destroyed == false)
        {
            GameObject explosion = Instantiate(explosionPrefab, rock.transform.position, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(explosion);

            destroyed = true;
            NetworkServer.Destroy(transform.gameObject);

            destroyedRocks++;
            currentRocks--;

            if (rockSpherePrefab)
            {
                for (int i = 0; i < pieces; i++)
                {
                    GameObject newRock = Instantiate(rockSpherePrefab, rock.transform.position, rock.transform.rotation) as GameObject;
                    NetworkServer.Spawn(newRock);
                }
            }

            //Debug.Log("Rocks " + currentRocks + " total " + totalRocks + " destoyed " + destroyedRocks);

            // let all the clients know the current rock counts for music
            rpcSetRockStats(currentRocks, totalRocks, destroyedRocks);
        }
    }

    // Update is called once per frame
    void Update ()
    {
    }
}
