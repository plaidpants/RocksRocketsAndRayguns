using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketSphereAI : NetworkBehaviour
{
    GameObject rocket;
    float radius = 10;
    public float rotationSpeed;
    public float forwardSpeed;
    public GameObject shotPrefab;
    public float shotSpeed;
    public float engineSize;
    ParticleSystem.EmissionModule emissionModule;
    ParticleSystem.MainModule mainModule;
    ParticleSystem engineParticleSystem;
    AudioSource engineSound;
    float horizontalInput = 0.0f;
    float verticalInput = 0.0f;
    bool fireInput = false;
    float timer = 0.0f;
    float timerExpired = 1.0f;
    [SyncVar] public int rocketAIColorIndex = -1;
    float copyPlayerInverted = 1.0f;
    bool copyPlayer = false;
    float shotTimer = 0.0f;
    float shotTimerExpired = 1.0f;

    public GameObject explosionPrefab;
    Rigidbody rb;
    [SyncVar] Color RocketColor = Color.white;

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
                cm[0].ReleaseColorIndex(rocketAIColorIndex);
            }
        }
    }

    public override void OnStartServer()
    {
        // call the base function, important, odd behavior when connecting when not on same start scene
        base.OnStartServer();

        // get a color from the color manager
        // return the color so others can use it
        ColorManager[] cm = FindObjectsOfType<ColorManager>();
        if (cm.Length > 0)
        {
            if (cm[0])
            {
                rocketAIColorIndex = cm[0].ReserveColorIndex();
                RocketColor = cm[0].GetColor(rocketAIColorIndex);
            }
        }

        // Find the rocket child object
        rocket = transform.Find("Rocket").gameObject;

        //radius = transform.position.magnitude;

        //reset the position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Move rocket child gameobject out to radius
        rocket.transform.position = Vector3.forward * radius;
        rocket.transform.rotation = Quaternion.FromToRotation(Vector3.forward, rocket.transform.position);

        rocket.SetActive(true);
    }

    public override void OnStartClient()
    {
        // call the base function, important, odd behavior when connecting when not on same start scene
        base.OnStartClient();

        // Find the rocket child object
        rocket = transform.Find("Rocket").gameObject;

        //radius = transform.position.magnitude;

        // find the engine child object and attach the particle generator
        GameObject engine = transform.Find("Rocket").Find("Engine").gameObject;
        engineParticleSystem = engine.GetComponent<ParticleSystem>();
        emissionModule = engineParticleSystem.emission;
        mainModule = engineParticleSystem.main;
        engineSound = transform.Find("Rocket").transform.GetComponent<AudioSource>();

        // find the rb so we can apply torque during the Update()
        rb = transform.GetComponent<Rigidbody>();

        //reset the position back to the center
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Move rocket child gameobject out to radius
        rocket.transform.position = Vector3.forward * radius;
        rocket.transform.rotation = Quaternion.FromToRotation(Vector3.forward, rocket.transform.position);

        // update the color to match the color on the server
        if (cachedMaterial == null)
            cachedMaterial = rocket.GetComponent<Renderer>().material;
        cachedMaterial.color = RocketColor;
    }

    // we only process collisions on the server
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        ShotSphere shot = other.attachedRigidbody.GetComponent<ShotSphere>();

        if (shot)
        {
            if (shot.shooterColorIndex == rocketAIColorIndex)
            {
                // ignore our own shots
                return;
            }
        }

        // Spawn an explosion at rocket position on all clients
        GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(explosion);

        // destory the ship on all clients
        NetworkServer.Destroy(transform.gameObject);
    }

    // we only shoot on the server
    // clients control rockets but server controls shots,
    // there is a bit of a delay difference and can result in client rockets running into their own shots at high speed
    // need to find a solution to this issue, moving the shot out further is not pretty
    [Server]
    void Fire()
    {
        Vector3 pos = transform.rotation * Vector3.forward * radius + Vector3.Cross(transform.rotation * Vector3.down, transform.rotation * Vector3.forward);
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, pos);

        GameObject shot = Instantiate(shotPrefab, pos, rot) as GameObject;

        // save the hue of the shooter in the shot so we won't collide with it later
        shot.GetComponent<ShotSphere>().shooterColorIndex = rocketAIColorIndex;

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

    [Server]
    void Update()
    {
        float rotation = 0.0f;
        float forward = 0.0f;

        // check if client start has been called yet
        if (!rocket) return;

        // this is a server controlled ship
        if (!isServer) return;

        // update the timer
        timer += Time.deltaTime;

        // Check if we have expired the timer
        if (timer > timerExpired)
        {
            // should we copy the player 
            if (Random.Range(0.0f, 1.0f) < 0.2f)
            {
                copyPlayer = true;

                // should we copy the player input inverted
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    copyPlayerInverted = 1.0f;
                }
                else
                {
                    copyPlayerInverted = -1.0f;
                }
            }
            else
            {
                copyPlayer = false;
            }

            // should we turn
            if (Random.Range(0.0f, 1.0f) < 0.3f) 
            {
                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    horizontalInput = Random.Range(0.25f, 1.0f);
                }
                else
                {
                    horizontalInput = Random.Range(-1.0f, -0.25f);
                }
            }
 //           else
            {
                // either turn or go forward not both at the same time
 //               horizontalInput = 0.0f;

                // should we go forward
                if (Random.Range(0.0f, 1.0f) < 0.2f)
                {
                    verticalInput = Random.Range(0.25f, 1.0f);
                }
                else
                {
                    verticalInput = 0.0f;
                }
            }

            // should we shoot
            if (Random.Range(0.0f, 1.0f) < 0.2f) 
            {
                fireInput = true;
                timer = 0.0f;
                shotTimerExpired = Random.Range(0.15f, 0.25f);
            }
            else
            {
                fireInput = false;
            }

            // update the time, important to subtract the last timer value instead of reseting to zero
            // as we make not have expired the time at precisely the timer period
            timer = timer - timerExpired;

            // next timer check is random also
            timerExpired = Random.Range(0.25f, 1.0f);
        }

        if (copyPlayer)
        {
            // left and right rotation is controlled by the horizontal joystick axis
            horizontalInput = copyPlayerInverted * Input.GetAxis("Horizontal");

            // forward momentum is controlled by the verticle joystick axis
            verticalInput = copyPlayerInverted * Input.GetAxis("Vertical");
        }

        // should
        // left and right rotation is controlled by the horizontal joystick axis
        rotation = horizontalInput * rotationSpeed * Time.deltaTime;

        Quaternion turn = Quaternion.Euler(0f, 0f, -rotation);
        rb.MoveRotation(rb.rotation * turn);

        // forward momentum is controlled by the verticle joystick axis
        forward = verticalInput;
        if (forward > 0)
        {
            Vector3 torque = Vector3.Cross(transform.rotation * Vector3.right, transform.rotation * Vector3.forward);
            rb.AddTorque(torque.normalized * (forward * forwardSpeed * Time.deltaTime / radius));

            if (emissionModule.enabled == false)
            {
                RpcEngineOn();
            }

            RpcEngineForward(forward);
        }
        else
        {
            if (emissionModule.enabled == true)
            {
                RpcEngineOff();
            }
        }

        if (fireInput == true)
        {
            // update the timer
            shotTimer += Time.deltaTime;
            
            if (shotTimer > shotTimerExpired)
            {
                //fireInput = false; // only fire once
                Fire();

                // update the time, important to subtract the last timer value instead of reseting to zero
                // as we make not have expired the time at precisely the timer period
                shotTimer = shotTimer - shotTimerExpired;
            }
        }
    }
}