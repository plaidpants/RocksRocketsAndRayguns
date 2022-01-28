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

// When disconnecting from the server before leaving all clients generate a hyperspace sound to indicate you are leaving

public class RocketSphere : NetworkBehaviour
{
    GameObject rocket;
    [SyncVar] float radius = 10;
    [SyncVar] bool visible = false;
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
    Rigidbody rb;
    [SyncVar(hook = "OnChangeColour")]
    Color RocketColor = Color.white;

    // Unity makes a clone of the Material every time GetComponent<Renderer>().material is used.
    // Cache it here and Destroy it in OnDestroy to prevent a memory leak.
    Material cachedMaterial;

    // create a random color for each player as they are created on the server
    public override void OnStartServer()
    {
        base.OnStartServer();
        RocketColor = new Color(Random.Range(0.3f, 1.0f), Random.Range(0.3f, 1.0f), Random.Range(0.3f, 1.0f));
    }

    void OnDestroy()
    {
        Destroy(cachedMaterial);
    }

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

        // Find the rocket child object
        rocket = transform.Find("Rocket").gameObject;
        rocket.transform.position = pos;
        rocket.transform.rotation = rot;

        // find the engine child object
        GameObject engine = transform.Find("Rocket").Find("Engine").gameObject;
        engineParticleSystem = engine.GetComponent<ParticleSystem>();
        emissionModule = engineParticleSystem.emission;
        mainModule = engineParticleSystem.main;
        engineSound = transform.Find("Rocket").transform.GetComponent<AudioSource>();
        hyperspaceSound = transform.GetComponent<AudioSource>();

        // find the rb
        rb = transform.GetComponent<Rigidbody>();

        //OnChangeColour(RocketColor, RocketColor);

        //        PlayerTracker.SetTrackingObject(rocket.gameObject);
        //        Debug.Log("set");

        // Keep the player objects through level changes
        DontDestroyOnLoad(this);

           // need to maintain this separately so we can sync state on client connect with existing objects
            visible = false;
 
        // attach the PC camera follow script to the local player
            if (isLocalPlayer)
                Camera.main.GetComponent<CameraFollowRocket>().player = transform.gameObject.transform.Find("Rocket").gameObject.transform;

            // spawn the ship on the server
            CmdSpawnShip();

    }

    void ChangeColour()
    {
        if (cachedMaterial == null)
            cachedMaterial = rocket.GetComponent<Renderer>().material;

        cachedMaterial.color = RocketColor;
    }

    void OnChangeColour(Color oldColor, Color newColor)
    {
        CmdChangeColour();
    }
    
    [ClientRpc]
    void RpcChangeColour()
    {
        ChangeColour();
    }

    [Command]
    void CmdChangeColour()
    {
        RpcChangeColour();
    }

    private void OnLevelWasLoaded(int level)
    {
        // attach the PC camera follow script to the local player
        if (isLocalPlayer)
            Camera.main.GetComponent<CameraFollowRocket>().player = transform.gameObject.transform.Find("Rocket").gameObject.transform;
    }

  
    void MySetActive(bool active, Quaternion rotation)
    {
        if (active == false)
        {
            // stop the ship from moving due to rb momentum
            rb.isKinematic = true;
            // set player ship to be stopped
            rb.velocity = Vector3.zero;
            rocket.SetActive(false);
            visible = false;
        }
        else
        {
            // put initial rotation of new spacecraft at current camera rotation so player can orient spawn position to a safe spot
            transform.rotation = rotation;

            // restart rb movement
            rb.isKinematic = false;

            rocket.SetActive(true);
            visible = true;
            // play the hyperspace sound on enable
            hyperspaceSound.Play();
        }
    }

    [ClientRpc]
    void RpcMySetActive(bool active, Quaternion rotation)
    {
        MySetActive(active, rotation);
    }

    [Command]
    void CmdMySetActive(bool active, Quaternion rotation)
    {
        RpcMySetActive(active, rotation);
    }

    [Command]
    void CmdSpawnShip()
    {
	    rpcSpawnShipDelay();

        //RpcSpawnShip();
    }

    [ClientRpc]
    void RpcSpawnShip()
    {
        SpawnShip();
    }

    void SpawnShip()
    {
        if (!isLocalPlayer) return;

        CmdChangeColour();

        // re-enable this rocket on all clients through server
        CmdMySetActive(true, Camera.main.transform.rotation);
    }

    [ClientRpc]
    void rpcSpawnShipDelay()
    {
        if (!isLocalPlayer) return;

        Invoke("SpawnShip", 5);
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        //if (!isLocalPlayer) return;

        // Rocket is already destroyed, ignore
        if (!rocket.activeSelf) return;

        // Spawn an explosion at player position on all clients
        GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(explosion);

        // stop the ship from moving due to rb momentum
        //rb.isKinematic = true;
        // set player ship to be stopped
        //rb.velocity = Vector3.zero;

        // deactivate the ship on all clients
        RpcMySetActive(false, Quaternion.identity);

        // spawn a new ship, do this on the local player client and it will re-enable on all clients
        rpcSpawnShipDelay();

        //Invoke("SpawnShip", 5);
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
        //rbshot.velocity = torque.normalized * (shotSpeed / radius);

        // need to rotate the shot just in front of the rocket
        Quaternion turn = Quaternion.Euler(0f, -50f/radius, 0f);
        rbshot.MoveRotation(rbshot.rotation * turn);

        // Fire the shot locally do not spawn on server
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

    [Command]
    void CmdUpdateState()
    {
        
    }

    [Client]
    public override void OnStartClient()
    {
        if(!isLocalPlayer)
            CmdUpdateState();
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

    // this is called on the rocket that fired for all observers
    [ClientRpc]
    void RpcOnFire()
    {
        // create the shot locally on each client and run them independently
        //Fire();
    }

    // this is called on the server
    [Command]
    void CmdFire()
    {
        Fire();
        RpcOnFire();
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
        if (rocket.activeSelf != visible)
        {
            // we are out of sync with the server
            rocket.SetActive(visible);
        }

        // only allow input for local player
        if (!isLocalPlayer) return;

        // ignore input while destroyed
        if (!rocket.activeSelf) return;

        float rotation = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;

        Quaternion turn = Quaternion.Euler(0f, 0f, -rotation);
        rb.MoveRotation(rb.rotation * turn);

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