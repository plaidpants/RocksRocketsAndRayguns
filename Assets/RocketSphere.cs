using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//ideas
// fixed wormhole between two depth levels
// hyperspace between two depth levels
// shield, use physics to bounce off rocks and other ships
// UFO
// radar circle indicating rocks, UFOs, enemy rockets around
// black holes/gravity
// make front transparent when in background
// bright light shots with lens flare
// readjust center point if head moves too far away from center
// add fade in and out of levels
// shots same color as ships
// animate hyperspace between radius levels

//rockets, rocks and ray-guns
// stick man astronauts
// connected bar between ships co-operative multi-player
// multiple universes, travel between with wormholes to new play area, have level progression or adventure game

// laser instead of shots will cut rocks om half instead of break them up in two.
// need to scale speed (ship and shot and rocks) by radius otherwise the outer layers get really fast

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
    [SyncVar] public int rocketColorIndex = -1;

    public GameObject explosionPrefab;
    Rigidbody rb;
    [SyncVar] Color RocketColor = Color.white;
    [SyncVar] bool visible = false;
    [SyncVar] Quaternion rot2Save = Quaternion.identity;

    // Unity makes a clone of the Material every time GetComponent<Renderer>().material is used.
    // Cache it here and Destroy it in OnDestroy to prevent a memory leak.
    Material cachedMaterial;

    void OnDestroy()
    {
        // avoid memory leak
        Destroy(cachedMaterial);

        // return the color so others can use it
        ColorManager[] cm = FindObjectsOfType<ColorManager>();
        if (cm.Length > 0)
        {
            if (cm[0])
            {
                if (rocketColorIndex >= 0)
                {
                    cm[0].ReleaseColorIndex(rocketColorIndex);
                }
            }
        }
    }

    public override void OnStartServer()
    {
        // call the base function, important, odd behavior when connecting when not on same start scene
        base.OnStartServer();

        // only use a colored ship if we are not the host so we can see who the host is and avoid shutdown of the host
        if (!isLocalPlayer)
        {
            ColorManager cm = FindObjectsOfType<ColorManager>()[0];
            rocketColorIndex = cm.ReserveColorIndex();
            RocketColor = cm.GetColor(rocketColorIndex);
            Debug.Log("player assigned color " + rocketColorIndex);
        } 
        else // server player
        {
            // server color is always white
            RocketColor = Color.white;

            // need to set the color index so we don't overlap any of the color positive or zero indexes
            rocketColorIndex = -1;
        }

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
        // mirror does not garantee that start functions will be called in the same order on the server and client and when a client connects
        // if (isServer) and if (isClient) are not sufficient controls
        // move all start functions to OnStartClient() or OnStartServer(),
        // be sure to call the base function or you will not get proper function when switching scenes and connecting
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

        ShotSphere shot = other.attachedRigidbody.GetComponent<ShotSphere>();

        if (shot)
        {
            if (shot.shooterColorIndex == rocketColorIndex)
            {
                // ignore our own shots
                return;
            }
        }

        // Spawn an explosion at player position on all clients
        GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(explosion);

        // deactivate the ship on all clients
        RpcMySetActive(false, Quaternion.identity, false);

        // spawn a new ship, do this on the local player client and it will re-enable on all clients
        rpcSpawnShipDelay();
    }

    // we only shoot on the server
    // clients control rockets but server controls shots,
    // there is a bit of a delay difference and can result in client player rockets running into their own shots at high speed
    // need to find a solution to this issue, moving the shot out further is not pretty
    [Server]
    void Fire()
    {
        Vector3 pos = transform.rotation * Vector3.forward * radius + Vector3.Cross(transform.rotation * Vector3.down, transform.rotation * Vector3.forward);
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

        GameObject shot = Instantiate(shotPrefab, pos, rot) as GameObject;

        // save the hue of the shooter in the shot so we won't collide with it later
        shot.GetComponent<ShotSphere>().shooterColorIndex = rocketColorIndex;

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
        // check if client start has been called yet
        if (!rocket)
            return;

        // make sure the state matches the server state,
        // don't do this on the server as you may enable the ship
        // while it is spawning in, this is only for clients
        // to catch up the other rocket enable states in the game
        if ((visible != rocket.activeSelf) && isClient)
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
    }
}