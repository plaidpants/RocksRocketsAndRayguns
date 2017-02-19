﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockSphere : MonoBehaviour {

    public GameObject rockPrefab;
    GameObject rock;
    public float minSpeed;
    public float maxSpeed;
    public GameObject explosionPrefab;
    public GameObject rockSpherePrefab;
    public int pieces;
    public float rotationSpeed;
    bool destroyed = false;
    public static int count = 0;

    // Use this for initialization
    void Start () {
        // get the current radius from position
        float radius = transform.position.magnitude;
        Vector3 pos = transform.position;

        //reset the position back to the center
        transform.position = Vector3.zero;

        //calculate random position and rotate so it faces the center
        //Vector3 pos = Random.onUnitSphere * radius;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

        // create the rock at that location
        //rock = Instantiate(rockPrefab, pos, rot) as GameObject;
        rock = transform.FindChild("Rock").gameObject;
        rock.transform.position = pos;
        rock.transform.rotation = rot;

        // make the rock a child of the rock sphere so we can use the ridgid body attached
        //rock.transform.SetParent(transform);

        // apply some rotational torque to the rock sphere object with the rock attached
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 torque = Random.onUnitSphere * (Random.Range(minSpeed, maxSpeed) / radius);
        // ???? note this could be pointing right at us or away so no torque would be added need to catch this and get a new torque
        rb.AddTorque(Vector3.Cross(torque, pos.normalized));

        count++;
     }

    void OnTriggerEnter(Collider other)
    {
        if (destroyed == false)
        {
            destroyed = true;
            Destroy(transform.gameObject);
            count--;

            GameObject explosion = Instantiate(explosionPrefab, rock.transform.position, Quaternion.identity) as GameObject;
            if (rockSpherePrefab)
            {
                for (int i = 0; i < pieces; i++)
                {
                    GameObject newRock = Instantiate(rockSpherePrefab, rock.transform.position, rock.transform.rotation) as GameObject;
                }
            }
        }
    }

    // Update is called once per frame
    void Update () {
        float rotation =  rotationSpeed * Time.deltaTime;
        transform.Rotate(0, 0, -rotation);
    }
}
