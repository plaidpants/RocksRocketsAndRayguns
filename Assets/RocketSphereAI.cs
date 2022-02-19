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
    public float horizontalInput = 0.0f;
    public float verticalInput = 0.0f;
    public bool fireInput = false;
    float timer = 0.0f;
    float timerExpired = 1.0f;
    [SyncVar] public int rocketAIColorIndex = -1;
    float copyPlayerInverted = 1.0f;
    bool copyPlayer = false;
    float shotTimer = 0.0f;
    float shotTimerExpired = 0.0f;
    public float shotDelay = 0.1f;
    bool fireInputReleased = true;
    [SyncVar] public int points = 0;
    public bool trackRocketAI = true;
    bool destroyed = false;
    public float lifeTime = 120.0f;
    float lastRotation = 0;
    public int countRotations = 0;

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


    [SyncVar] public Vector3 pos = Vector3.zero;
    [SyncVar] public Quaternion rot = Quaternion.identity;

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

        // get the distance from the center
        radius = transform.position.magnitude;

        // get the current rotation from the parent position
        transform.rotation = Quaternion.FromToRotation(Vector3.forward, transform.position);

        // reset the parent position back to the center
        transform.position = Vector3.zero;

        // Move rocket child gameobject out to radius in local coords
        rocket.transform.localPosition = Vector3.forward * radius;

        // rotations is handled by the parent
        rocket.transform.localRotation = Quaternion.identity;

        rocket.SetActive(true);

        // this object is short lived
        //Invoke(nameof(DestroySelf), lifeTime);
    }

    // destroy for everyone on the server
    [Server]
    void DestroySelf()
    {
        // don't destroy ourself more than once
        if (destroyed == false)
        {
            transform.gameObject.GetComponent<RocketAIAgent>().EpisodeEndGood();

            destroyed = true;
            NetworkServer.Destroy(gameObject);
        }
    }

    public override void OnStartClient()
    {
        // call the base function, important, odd behavior when connecting when not on same start scene
        base.OnStartClient();

        // Find the rocket child object
        rocket = transform.Find("Rocket").gameObject;

        // find the engine child object and attach the particle generator
        engineParticleSystem = rocket.transform.Find("Engine").GetComponent<ParticleSystem>();
        emissionModule = engineParticleSystem.emission;
        mainModule = engineParticleSystem.main;
        // get the engine audio source
        engineSound = rocket.transform.GetComponent<AudioSource>();

        // find the rb so we can apply torque during the Update()
        rb = transform.GetComponent<Rigidbody>();

        //if (trackRocketAI)
        {
            if ((Camera.main.GetComponent<CameraFollowRocket>().player == null) || (Camera.main.GetComponent<CameraFollowRocket>().player == Camera.main.transform))
            {
                Camera.main.GetComponent<CameraFollowRocket>().player = transform.gameObject.transform.Find("Rocket").gameObject.transform;
            }
        }

        // update the color to match the color on the server
        if (cachedMaterial == null)
        {
            cachedMaterial = rocket.GetComponent<Renderer>().material;
        }
        cachedMaterial.color = RocketColor;
    }

    // we only process collisions on the server
    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        ShotSphere shot = other.attachedRigidbody.GetComponent<ShotSphere>();

        // did a shot hit us
        if (shot)
        {
            // make sure the shooter still exists
            if (shot.shooter)
            {
                // ignore our own shots
                if (shot.shooter == transform.gameObject)
                {
                    return;
                }

                // did a player shoot us, give them some points
                RocketSphere playerShooter = shot.shooter.GetComponent<RocketSphere>();
                if (playerShooter)
                {
                    playerShooter.points += 2;
                }

                // did a AI shoot us, give them some points
                RocketSphereAI rocketShooterAI = shot.shooter.GetComponent<RocketSphereAI>();
                if (rocketShooterAI)
                {
                    rocketShooterAI.points += 2;
                }
            }
        }

        // Spawn an explosion at rocket position on all clients
        GameObject explosion = Instantiate(explosionPrefab, rocket.transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(explosion);

        // let the AI know we finished
        transform.gameObject.GetComponent<RocketAIAgent>().EpisodeEndBad();

        // destory the ship on all clients
        destroyed = true;
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

        // save the the shooter in the shot so we won't collide with it later
        shot.GetComponent<ShotSphere>().shooter = transform.gameObject;

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

        /*
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
 
            // should we go forward
            if (Random.Range(0.0f, 1.0f) < 0.2f)
            {
                verticalInput = Random.Range(0.25f, 1.0f);
            }
            else
            {
                verticalInput = 0.0f;
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

        */

        // should
        // left and right rotation is controlled by the horizontal joystick axis
        rotation = horizontalInput * rotationSpeed * Time.deltaTime;

        Quaternion turn = Quaternion.Euler(0f, 0f, -rotation);
        if (!rb) return;
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
 
        if (fireInput == false)
        {
            fireInputReleased = true;
        }

        shotTimer += Time.deltaTime;
        if ((fireInput == true) && (fireInputReleased == true) && (shotTimer >= shotTimerExpired))
        {
            fireInputReleased = false;
            shotTimerExpired = shotTimer + shotDelay; // set new shot timer expiration based on current shot timer + the shot delay
            Fire();
        }

        if (lastRotation - transform.rotation.eulerAngles.z > 250)
        {
            countRotations++;
        }

        if (transform.rotation.eulerAngles.z - lastRotation > 250)
        {
            countRotations--;
        }

        lastRotation = transform.rotation.eulerAngles.z;

        /*
        // too many rotations
        if (Mathf.Abs(countRotations) > 20)
        {
            // let the AI know we finished
            transform.gameObject.GetComponent<RocketAIAgent>().EpisodeEndBad();
            Debug.Log("Killed for spinning");

            // destory the ship on all clients
            destroyed = true;
            NetworkServer.Destroy(transform.gameObject);
        }

        // too fast
        if (rb.angularVelocity.magnitude > 1.0f)
        {
            // let the AI know we finished
            transform.gameObject.GetComponent<RocketAIAgent>().EpisodeEndBad();

            Debug.Log("Killed for going to fast");
            // destory the ship on all clients
            destroyed = true;
            NetworkServer.Destroy(transform.gameObject);
        }
        */
    }
}