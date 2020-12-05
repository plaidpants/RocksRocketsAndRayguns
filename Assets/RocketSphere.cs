using Mirror;
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

public class RocketSphere : NetworkBehaviour
{
    GameObject rocket;
    [SyncVar] public float radius;
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
    [SyncVar] bool destroyed = false;
    [SyncVar] bool engineOn = false;
    [SyncVar] float engineStartLifetime = 0;
    Rigidbody rb;
    bool lastEngineOn;
    float lastRadius;
    Color RocketColor;

    // Use this for initialization
    void Start()
    {
        // get the current radius from position
        //float radius = transform.position.magnitude;
        //Vector3 pos = transform.position;

        //reset the position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Move rocket out to radius
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
        //RocketColor = new Color(Random.Range(64, 255), Random.Range(64, 255), Random.Range(64, 255));
        //GetComponentInChildren<Renderer>().material.SetColor("_Color", Color.red);

        //        PlayerTracker.SetTrackingObject(rocket.gameObject);
        //        Debug.Log("set");

        DontDestroyOnLoad(this);

        if (isLocalPlayer)
            Camera.main.GetComponent<CameraFollow360>().player = transform.gameObject.transform.Find("Rocket").gameObject.transform;
    }

    [ClientRpc]
    void RpcMySetActive(bool active)
    {
        rocket.SetActive(active);
    }

    [Command]
    void CmdMySetActive(bool active)
    {
        RpcMySetActive(active);
    }

    [Command]
    void CmdSpawnShip()
    {
        SpawnShip();
    }

    void SpawnShip()
    {
        // initial rotation of new spacecraft at current camera rotation so player can orient spawn position to a safe spot
        transform.rotation = Camera.main.transform.rotation;

        // set player ship to be stopped
        rb.velocity = Vector3.zero;
        // restart rb movement
        rb.isKinematic = false; 

        // re-enable player rocket
        rocket.SetActive(true);
        // re-enable this rocket on all clients through server
        CmdMySetActive(true);

        // update syncVar indication player rocket state
        destroyed = false;
    }


    [Server]
    void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return;

        if (destroyed == false)
        {
            destroyed = true;
            GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(explosion);
            rocket.SetActive(false);
            CmdMySetActive(false);
            rb.isKinematic = true; // stop the ship from moving due to rb momentum

            Invoke("SpawnShip", 5);
        }
    }

    [Server]
    void Fire()
    {
        Vector3 pos = transform.rotation * Vector3.forward * radius + Vector3.Cross(transform.rotation * Vector3.down, transform.rotation * Vector3.forward);
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

        GameObject shot = Instantiate(shotPrefab, pos, rot) as GameObject;

        shot.transform.rotation = transform.rotation;

        Rigidbody rbshot = shot.GetComponent<Rigidbody>();
        rbshot.angularVelocity = rb.angularVelocity;

        Vector3 torque = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
        rbshot.AddTorque(torque.normalized * (shotSpeed / radius));

        // need to rotate the shot just in front of the rocket
        Quaternion turn = Quaternion.Euler(0f, -40f/radius, 0f);
        rbshot.MoveRotation(rbshot.rotation * turn);

        NetworkServer.Spawn(shot);
    }

    void EngineOn()
    {
        //emissionModule.enabled = true;
        engineOn = true;
    }
    
    void EngineForward(float forward)
    {
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
    
    void EngineOff()
    {
        engineSound.volume = 0.0f;
        emissionModule.enabled = false;
        engineStartLifetime = 0;
        mainModule.startLifetime = engineStartLifetime;
        engineOn = false;
    }

    //??? this would be nice to animate between radius levels
    void Hyperspace()
    {
        if (rocket.transform.position.magnitude < 15)
        {
            rocket.transform.position = transform.rotation * Vector3.forward * 30;
            radius = 30;
        }
        else
        {
            rocket.transform.position = transform.rotation * Vector3.forward * 10;
            radius = 10;
        }
    }

    void FixedUpdate()
    {
        if (engineOn)
        {
            mainModule.startLifetime = engineStartLifetime;
            emissionModule.enabled = true;
            if (lastEngineOn != engineOn)
            {
                // start playing the engine sound if state has changed
                engineSound.Play();
                lastEngineOn = engineOn;
            }
        }
        else
        {
            emissionModule.enabled = false;
            mainModule.startLifetime = 0;
            if (lastEngineOn != engineOn)
            {
                // stop playing the engine sound if state has changed
                engineSound.Stop();
                lastEngineOn = engineOn;
            }
        }

        if (lastRadius != radius)
        {
            // play the hyperspace sound if the radius has changed
            hyperspaceSound.Play();
            lastRadius = radius;
        }

        if (!isLocalPlayer) return;

        float rotation = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;

        Quaternion turn = Quaternion.Euler(0f, 0f, -rotation);
        rb.MoveRotation(rb.rotation * turn);

        float forward = Input.GetAxis("Vertical");
        if (forward > 0)
        {
            Vector3 torque = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
            rb.AddTorque(torque.normalized * (forward * forwardSpeed * Time.deltaTime / radius));

            if (engineOn == false)
            {
                EngineOn();
            }

            EngineForward(forward);
        }
        else
        {
            if (engineOn == true)
            {
                EngineOff();
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            Fire();
        }

        if (Input.GetButtonDown("Fire2"))
        {
            Hyperspace();
        }
    }
}