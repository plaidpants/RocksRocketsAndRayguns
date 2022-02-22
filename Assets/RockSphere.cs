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
    public int points = 1;
    //public float rotationSpeed;
    [SyncVar] public bool destroyed = false;
    [SyncVar] public float radius = 0;
    [SyncVar] public Vector3 pos = Vector3.zero;
    [SyncVar] public Quaternion rot = Quaternion.identity;
    static public int currentRocks = 0; // number of rocks currently active
    static public int totalRocks = 0; // number of rocks currently active plus subsequent rocks
    static public int destroyedRocks = 0; // number of rocks destoryed
    Rigidbody rb;
    public float rotationSpeed;
    float currentRotationSpeed;

    [ClientRpc]
    void rpcSetRockStats(int current, int total, int destroyed)
    {
        currentRocks = current;
        totalRocks = total;
        destroyedRocks = destroyed;

//        Debug.Log("client RPC Rocks " + current + " total " + total + " destoyed " + destroyed);
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

        // Find the rock child object
        rock = transform.Find("Rock.old").gameObject;

        // get the current radius and position from parent gameobject
        radius = transform.position.magnitude;

        // get the current rotation from the parent position
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);

        // reset the parent gameobject position back to the center
        transform.position = Vector3.zero;
        //transform.rotation = Quaternion.identity;

        // Move rock child gameobject out to radius in local coords
        pos = rock.transform.localPosition = Vector3.forward * radius;

        // rotations is handled by the parent
        rot = rock.transform.localRotation = Quaternion.identity;

        // Move rock child gameobject out to radius
        //rock.transform.position = Vector3.forward * radius;
        //rock.transform.rotation = Quaternion.FromToRotation(Vector3.forward, rocket.transform.position);

        // apply some rotational torque to the parent gameobject object with the rock attached as a child
        rb = GetComponent<Rigidbody>();

        Vector3 torque = Random.onUnitSphere * (Random.Range(minSpeed, maxSpeed) / radius);
        // ???? note this could be pointing right at us or away so no torque would be added need to catch this and get a new torque
        rb.AddTorque(Vector3.Cross(torque, rock.transform.position.normalized));

        // add some rotation to the rock
        //float rotation = Random.Range(minRotationSpeed, maxRotationSpeed);
        //rb.AddTorque(transform.forward * 100);

        totalRocks++;
        currentRocks++;

 //       Debug.Log("OnStartServer current rocks " + currentRocks + " Rocks " + currentRocks + " total " + totalRocks + " destoyed " + destroyedRocks);
        rpcSetRockStats(currentRocks, totalRocks, destroyedRocks);

        currentRotationSpeed = Random.Range(-rotationSpeed, rotationSpeed);
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
            ShotSphere shot = other.attachedRigidbody.GetComponent<ShotSphere>();

            // did the rock get hit with a shot
            if (shot)
            {
                // does the shot have the shooter set so we can reward them
                if (shot.shooter)
                {
                    // did a player shoot us, give them some points
                    RocketSphere rocketPlayer = shot.shooter.GetComponent<RocketSphere>();
                    if (rocketPlayer)
                    {
                        rocketPlayer.points += points;
                    }

                    // did an AI shoot us, give them some points
                    RocketSphereAI rocketAI = shot.shooter.GetComponent<RocketSphereAI>();
                    if (rocketAI)
                    {
                        rocketAI.points += points;
                    }
                }
            }

            GameObject explosion = Instantiate(explosionPrefab, rock.transform.position, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(explosion);

            destroyedRocks++;
            currentRocks--;

//            Debug.Log("OntriggerEnter Rocks " + currentRocks + " total " + totalRocks + " destoyed " + destroyedRocks);

            // let all the clients know the current rock counts for music
            // IMPORTANT!!!!, this must be called before the detroy or the rpc will never go out.
            rpcSetRockStats(currentRocks, totalRocks, destroyedRocks);

            destroyed = true;
            NetworkServer.Destroy(transform.gameObject);

            if (rockSpherePrefab)
            {
                for (int i = 0; i < pieces; i++)
                {
                    GameObject newRock = Instantiate(rockSpherePrefab, rock.transform.position, rock.transform.rotation) as GameObject;
                    NetworkServer.Spawn(newRock);
                }
            }
        }
    }

    // Update is called once per frame
    void Update ()
    {
        if (!isServer) return;

        if (!rb) return;

        Quaternion turn = Quaternion.Euler(0f, 0f, currentRotationSpeed * Time.deltaTime);
        rb.MoveRotation(rb.rotation * turn);
    }
}
