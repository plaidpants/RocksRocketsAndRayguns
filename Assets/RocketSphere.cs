﻿using Mirror;
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

// When disconnecting from the server before leaving all clients generate a hyperspace sound to indicate you are leaving

public class RocketSphere : NetworkBehaviour
{
    GameObject rocket;
    [SyncVar] float radius = 10;
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
    Rigidbody rb;
    [SyncVar] Color RocketColor = Color.white;
    [SyncVar] float hue = 0.0f;
    [SyncVar] bool visible = false;
    [SyncVar] Quaternion rot2Save = Quaternion.identity;

    // Unity makes a clone of the Material every time GetComponent<Renderer>().material is used.
    // Cache it here and Destroy it in OnDestroy to prevent a memory leak.
    Material cachedMaterial;

    void OnDestroy()
    {
        Destroy(cachedMaterial);
    }

    public override void OnStartServer()
    {
        // call the base function, important, odd behavior when connecting when not on same start scene
        base.OnStartServer();

        // create a random color for each player as they are created on the server
        //RocketColor = new Color(Random.Range(0.3f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(0.3f, 1.0f));
        
        float hue = Random.Range(0.0f, 1.0f);

        RocketSphere[] players = FindObjectsOfType<RocketSphere>();

        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            count++;
            if (count > 30)
            {
                Debug.Log("Give up finding a better color");
                break; // give up
            }

            // check for difference between player and chosen hue,
            // difference check must be small enough that we have enough colors for all players.
            // if max player count is increased from 4, this value should be reassessed
            // this could be an infinte check otherwise, might want a failsafe escape
            if ((Mathf.Abs(hue - players[i].hue) < 0.2f) && (players[i] != this))
            {
                // generate a new hue if it is too close to this other players color
                hue = Random.Range(0.0f, 1.0f);

                // restart check with new hue until we find a hue that is different enough from all the other players.
                i = 0;
            }
        }

        // rocket color is now set for this player until it is destroyed or disconnects
        RocketColor = Color.HSVToRGB(hue, 1.0f, 1.0f);

        // Find the rocket child object
        rocket = transform.Find("Rocket").gameObject;

        //reset the position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Move rocket child gameobject out to radius
        rocket.transform.position = Vector3.forward * radius;
        rocket.transform.rotation = Quaternion.FromToRotation(Vector3.forward, rocket.transform.position);
    }

    // Use this for initialization
    void Start()
    {
 
    }

    public override void OnStartClient()
    {
        // call the base function, important, odd behavior when connecting when not on same start scene
        base.OnStartClient();

        // Find the rocket child object
        rocket = transform.Find("Rocket").gameObject;

        // find the engine child object and attach the particle generator
        GameObject engine = transform.Find("Rocket").Find("Engine").gameObject;
        engineParticleSystem = engine.GetComponent<ParticleSystem>();
        emissionModule = engineParticleSystem.emission;
        mainModule = engineParticleSystem.main;
        engineSound = transform.Find("Rocket").transform.GetComponent<AudioSource>();
        hyperspaceSound = transform.GetComponent<AudioSource>();

        // find the rb so we can apply torque during the Update()
        rb = transform.GetComponent<Rigidbody>();

        //reset the position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Move rocket child gameobject out to radius
        rocket.transform.position = Vector3.forward * radius;
        rocket.transform.rotation = Quaternion.FromToRotation(Vector3.forward, rocket.transform.position);

        // Keep the player objects through level changes
        DontDestroyOnLoad(this);

        // attach the PC camera follow script to the local player so PC players can play with the VR players
        if (isLocalPlayer)
            Camera.main.GetComponent<CameraFollowRocket>().player = transform.gameObject.transform.Find("Rocket").gameObject.transform;

        // update the color to match the color on the server
        if (cachedMaterial == null)
            cachedMaterial = rocket.GetComponent<Renderer>().material;
        cachedMaterial.color = RocketColor;

        if (isLocalPlayer)
        {
            // spawn the ship on the client in the direction the player is currently looking after 5 seconds
            // which will enable the ship and then enable it on the other player clients through the server
            Invoke("SpawnShip", 5);
        }
        else
        {
            // not our rocket so we need to update the state based on the server sync var state
            rocket.SetActive(visible);
            transform.rotation = rot2Save;
        }
    }

    private void OnLevelWasLoaded(int level)
    {
        // attach the PC camera follow script to the local player
        if (isLocalPlayer)
            Camera.main.GetComponent<CameraFollowRocket>().player = transform.gameObject.transform.Find("Rocket").gameObject.transform;
    }

    void MySetActive(bool active, Quaternion rotation, bool playhyperspace)
    {
        if (active == false)
        {
            // stop the ship from moving due to rb momentum
            rb.isKinematic = true;

            // set player ship to be stopped
            rb.velocity = Vector3.zero;

            // make the rocket invisible
            rocket.SetActive(false);
            visible = false;
        }
        else
        {
            // put initial rotation of new spacecraft at current camera rotation so player can orient spawn position to a safe spot,
            // save it so new players know where to create the ship, if the player doesn't move
            transform.rotation = rotation;
            rot2Save = rotation;

            // restart rb movement
            rb.isKinematic = false;

            // make the rocket visible again
            rocket.SetActive(true);
            visible = true;

        }

        if (playhyperspace == true)
        {
            // play the hyperspace sound
            hyperspaceSound.Play();
        }
    }

    [ClientRpc]
    public void RpcMySetActive(bool active, Quaternion rotation, bool playhyperspace)
    {
        MySetActive(active, rotation, playhyperspace);
    }

    [Command]
    void CmdMySetActive(bool active, Quaternion rotation, bool playhyperspace)
    {
        RpcMySetActive(active, rotation, playhyperspace);
    }

    [Client]
    void SpawnShip()
    {
        if (!isLocalPlayer) return;

        // re-enable this rocket on all clients through server at the direction the user is looking at the moment
        CmdMySetActive(true, Camera.main.transform.rotation, true);
        isSpawning = false;
    }

    bool isSpawning;

    [ClientRpc]
    public void rpcSpawnShipDelay()
    {
        if (!isLocalPlayer) return;

        isSpawning = true;
        Invoke("SpawnShip", 5);
    }

    // we only process collisions on the server
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        // Rocket is already destroyed, ignore
        if (!rocket.activeSelf) return;

        // Spawn an explosion at player position on all clients
        GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(explosion);

        // deactivate the ship on all clients
        RpcMySetActive(false, Quaternion.identity, false);

        // spawn a new ship, do this on the local player client and it will re-enable on all clients
        rpcSpawnShipDelay();
    }

    // we only shoot on the server
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

        // need to rotate the shot just in front of the rocket so we don't run into our own shot
        Quaternion turn = Quaternion.Euler(0f, -50f/radius, 0f);
        rbshot.MoveRotation(rbshot.rotation * turn);

        // Fire the shot on the server by spawning it
        NetworkServer.Spawn(shot);
    }

    [Client]
    void EngineOn()
    {
        engineSound.pitch = 0.1f;
        engineSound.volume = 0.1f;
        mainModule.startLifetime = 0.1f * engineSize;
        emissionModule.enabled = true;
        engineSound.Play();
    }

    [ClientRpc]
    void RpcEngineOn()
    {
        EngineOn();
    }

    [Command]
    void CmdEngineOn()
    {
        RpcEngineOn();
    }

    [Client]
    void EngineForward(float forward)
    {
        if (forward > 0.1f)
        {
            engineSound.pitch = forward;
            engineSound.volume = forward;
            mainModule.startLifetime = forward * engineSize;
        }
        else
        {
            engineSound.pitch = 0.1f;
            engineSound.volume = 0.1f;
            mainModule.startLifetime = 0.1f * engineSize;
        }
    }

    [ClientRpc]
    void RpcEngineForward(float forward)
    {
        EngineForward(forward);
    }

    [Command]
    void CmdEngineForward(float forward)
    {
        RpcEngineForward(forward);
    }

    [Client]
    void EngineOff()
    {
        emissionModule.enabled = false;
        engineSound.Stop();
        engineSound.volume = 0.0f;
        engineSound.pitch = 0.0f;
        mainModule.startLifetime = 0;
    }

    [ClientRpc]
    void RpcEngineOff()
    {
        EngineOff();
    }

    // this is called on the server
    [Command]
    void CmdEngineOff()
    {
        RpcEngineOff();
    }

    //??? this would be nice to animate between radius levels
    [Client]
    void Hyperspace()
    {
        if (rocket.transform.position.magnitude < 15)
        {
            radius = 30;
        }
        else
        {
            radius = 10;
        }

        rocket.transform.position = transform.rotation * Vector3.forward * radius;
        hyperspaceSound.Play();
    }

    // this is called on the server
    [Command]
    void CmdFire()
    {
        Fire();
    }

    // this is called on the rocket that hyperspaced for all observers
    [ClientRpc]
    void RpcHyperspace()
    {
        Hyperspace();
    }

    // this is called on the server
    [Command]
    void CmdHyperspace()
    {
        RpcHyperspace();
    }

    void Update()
    {
        // make sure the state matches the server state
        if (visible != rocket.activeSelf)
        {
            rocket.SetActive(visible);
        }

        // only allow input for local player
        if (!isLocalPlayer) return;

        // ignore input while destroyed
        if (!rocket.activeSelf) return;

        // left and right rotation is controlled by the horizontal joystick axis
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;

        Quaternion turn = Quaternion.Euler(0f, 0f, -rotation);
        rb.MoveRotation(rb.rotation * turn);

        // forward momentum is controlled by the verticle joystick axis
        float forward = Input.GetAxis("Vertical");
        if (forward > 0)
        {
            Vector3 torque = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
            rb.AddTorque(torque.normalized * (forward * forwardSpeed * Time.deltaTime / radius));

            if (emissionModule.enabled == false)
            {
                CmdEngineOn();
            }

            CmdEngineForward(forward);
        }
        else
        {
            if (emissionModule.enabled == true)
            {
                CmdEngineOff();
            }
        }

        if (Input.GetButtonDown("Fire1"))
        {
            CmdFire();
        }

        if (Input.GetButtonDown("Fire2"))
        {
            CmdHyperspace();
        }

        if (Input.GetButtonDown("Submit"))
        {

        }

    }
}