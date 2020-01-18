using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//ideas
// fixed wormhole between two depth levels
// hyperspace between two depth levels
// multi-player
// shield, use physics to bounce off rocks and other ships
// UFO
// radar circle indicating rocks, UFOs, enemy rockets around
// black holes/gravity
// make front transparent when in background
// bright light shots with lens flare
// readjust center point if head moves too far away from center

//rockets, rocks and ray-guns
// stick man astronauts
// connected bar between ships co-operative multi-player
// multiple universes, travel between with wormholes to new play area, have level progression or adventure game

// laser instead of shots will cut rocks om half instead of break them up in two.
// need to scale speed (ship and shot and rocks) by radius otherwise the outer layers get really fast

public class RocketSphere : MonoBehaviour
{
    GameObject rocket;
    public float radius;
    public float rotationSpeed;
    public float forwardSpeed;
    public GameObject shotPrefab;
    public float shotSpeed;
    public float engineSize;
    ParticleSystem.EmissionModule emissionModule;
    ParticleSystem.MainModule mainModule;
    ParticleSystem engineParticleSystem;
    AudioSource hyperspaceSound;
    AudioSource engineSound;
    public GameObject explosionPrefab;
    public GameObject spawnPrefab;
    bool destroyed = false;
    bool engineOn = false;
    float engineStartLifetime = 0;
    Rigidbody rb;

    // Use this for initialization
    void Start()
    {
        // get the current radius from position
        //float radius = transform.position.magnitude;
        //Vector3 pos = transform.position;

        //reset the position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Vector3 pos = Vector3.forward * radius;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

        //rocket = Instantiate(rocketPrefab, pos, rot) as GameObject;
        //rocket.transform.SetParent(transform);
        rocket = transform.Find("Rocket").gameObject;
        rocket.transform.position = pos;
        rocket.transform.rotation = rot;

        GameObject engine = transform.Find("Rocket").Find("Engine").gameObject;
        engineParticleSystem = engine.GetComponent<ParticleSystem>();
        emissionModule = engineParticleSystem.emission;
        mainModule = engineParticleSystem.main;
        engineSound = transform.Find("Rocket").transform.GetComponent<AudioSource>();
        hyperspaceSound = transform.GetComponent<AudioSource>();
        rb = transform.GetComponent<Rigidbody>();
 
            PlayerTracker.SetTrackingObject(rocket.gameObject);
            Debug.Log("set");
    }
    
    void RpcMySetActive(bool active)
    {
        rocket.SetActive(active);
    }

    void SpawnShip()
    {
        rocket.transform.position = Vector3.zero;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        Vector3 pos = Vector3.forward * radius;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);
        rocket.transform.position = pos;
        rocket.transform.rotation = rot;

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        RpcMySetActive(true);
        
        rocket.SetActive(true);
        destroyed = false;
        hyperspaceSound.Play();
    }
    
    void OnTriggerEnter(Collider other)
    {
            if (destroyed == false)
            {
                destroyed = true;
                //Destroy(transform.gameObject);
                GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
                //GameObject spawn = Instantiate(spawnPrefab, rocket.transform.position, rocket.transform.rotation) as GameObject;
                rocket.SetActive(false);
                RpcMySetActive(false);
                rb.isKinematic = true;

                Invoke("SpawnShip", 5);
            }
    }

    void CmdFire()
    {
        // ??? need to move the shot spawn point to just in front of the rocket
        Vector3 pos = transform.rotation * Vector3.forward * radius + Vector3.Cross(transform.rotation * Vector3.down, transform.rotation * Vector3.forward);
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

        GameObject shot = Instantiate(shotPrefab, pos, rot) as GameObject;

        shot.transform.rotation = transform.rotation;

        Rigidbody rb = shot.GetComponent<Rigidbody>();
        Rigidbody rb1 = GetComponent<Rigidbody>();
        rb.angularVelocity = rb1.angularVelocity;

        Vector3 torque = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
        rb.AddTorque(torque.normalized * (shotSpeed / radius));
    }

    void FixedUpdate()
    {
        if (engineOn)
        {
            mainModule.startLifetime = engineStartLifetime;
            emissionModule.enabled = true;
        }
        else
        {
            emissionModule.enabled = false;
            mainModule.startLifetime = 0;
        }

        float rotation = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, 0, -rotation);

        Vector3 torque1 = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
        //Debug.DrawRay(transform.position, transform.rotation * Vector3.forward * radius);
        Debug.DrawRay(transform.rotation * Vector3.forward * radius, Vector3.Cross(transform.rotation * Vector3.down, transform.rotation * Vector3.forward) * 0.7f);
        //Debug.DrawRay(transform.rotation * Vector3.forward * radius, torque1);
        //Debug.DrawRay(Vector3.zero, torque1);

        float forward = Input.GetAxis("Vertical");
        if (forward > 0)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            Vector3 torque = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
            rb.AddTorque(torque.normalized * (forward * forwardSpeed * Time.deltaTime / radius));

            if (engineOn == false)
            {
                engineSound.Play();
                //emissionModule.enabled = true;
                engineOn = true;
            }

            if (forward > 0.1f)
            {
                engineSound.pitch = forward;
                engineSound.volume = forward;
                engineStartLifetime = forward * engineSize;
                mainModule.startLifetime = engineStartLifetime;
            }
            else
            {
                engineSound.pitch = 0.1f;
                engineSound.volume = 0.1f;
                engineStartLifetime = 0.1f * engineSize;
                mainModule.startLifetime = engineStartLifetime;
            }
        }
        else
        {
            if (engineOn == true)
            {
                engineSound.volume = 0.0f;
                engineSound.Stop();
                emissionModule.enabled = false;
                engineStartLifetime = 0;
                mainModule.startLifetime = engineStartLifetime;
                engineOn = false;
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            CmdFire();
        }

        //??? this should really animate between levels and make a hyperspace sound
        if (Input.GetButtonDown("Fire2"))
        {
            if (rocket.transform.position.magnitude < 15)
            {
                rocket.transform.position = transform.rotation * Vector3.forward * 30;
                radius = 30;
                hyperspaceSound.Play();
            }
            else
            {
                rocket.transform.position = transform.rotation * Vector3.forward * 10;
                radius = 10;
                hyperspaceSound.Play();
            }
        }
    }
}