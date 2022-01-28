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
    [SyncVar] bool destroyed = false;
    [SyncVar] float radius = 0;
    [SyncVar] Vector3 pos = Vector3.zero;
    [SyncVar] Quaternion rot = Quaternion.identity;
    public static int count = 0;

    // Use this for initialization
//    [Server]
    void Start()
    {
        // only do this on the server
        if (isServer)
        {
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
            rock.transform.position = pos;
            rock.transform.rotation = rot;

            Debug.Log(pos);

            // make the rock a child of the rock sphere so we can use the ridgid body attached
            //rock.transform.SetParent(transform);

            // apply some rotational torque to the parent gameobject object with the rock attached as a child
            Rigidbody rb = GetComponent<Rigidbody>();
            Vector3 torque = Random.onUnitSphere * (Random.Range(minSpeed, maxSpeed) / radius);
            // ???? note this could be pointing right at us or away so no torque would be added need to catch this and get a new torque
            rb.AddTorque(Vector3.Cross(torque, pos.normalized));
        }
        else
        {
            // move the child rock to original location and rotation
            rock = transform.Find("Rock.old").gameObject;
            rock.transform.position = pos;
            rock.transform.rotation = rot;
        }
        count++;
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
            count--;

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
        /*
        // parent object is in the center
        if (transform.position.magnitude == 0)
        {
            // get the child rock
            rock = transform.Find("Rock.old").gameObject;

            // is the child rock at the center also
            if (rock.transform.position.magnitude == 0)
            {
                // We need to move child out since mirror did not do this for us when the client connected to the server initially
                rock.transform.position = pos;
                rock.transform.rotation = rot;
                Debug.Log("Command1");
            }
        }
        else
        {
            // get the child rock
            rock = transform.Find("Rock.old").gameObject;

            // is the child rock at the same position as the parent gameobject
            if (rock.transform.position.magnitude == 0)
            {
                rock.transform.position = pos;
                rock.transform.rotation = rot;
            }
        }
        */

        //Rigidbody rb = GetComponent<Rigidbody>();
        //Quaternion turn = Quaternion.Euler(0f, 0f, -10);
        //rb.MoveRotation(rb.rotation * turn);

        //float rotation =  rotationSpeed * Time.deltaTime;
        //transform.Rotate(0, rotation, 0);
    }
}
