
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Mathematics;

public class RocketAIAgent : Agent
{
    RocketSphereAI rocket;
    int lastPoints = 0;
    int lastCountRotations = 0;
    RayPerceptionSensorComponent3D raySensor;
    Rigidbody rb;

    public override void Initialize()
    {
        base.Initialize();
        
        lastPoints = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);

        if (rocket)
        {
            //sensor.AddObservation(transform.rotation.eulerAngles); // ship on the sphere

            /*
            sensor.AddObservation(transform.rotation.eulerAngles.x); // ship on the sphere
            sensor.AddObservation(transform.rotation.eulerAngles.y); // ship on the sphere
            sensor.AddObservation(transform.rotation.eulerAngles.z); // ship rotation
            */

            //sensor.AddObservation(transform.rotation); // ship rotation

            /*
             sensor.AddObservation(transform.rotation.w); // ship rotation
             sensor.AddObservation(transform.rotation.x); // ship rotation
             sensor.AddObservation(transform.rotation.y); // ship rotation
             sensor.AddObservation(transform.rotation.z); // ship rotation
             */

            //sensor.AddObservation(transform.position); // ship coords
            /*
            sensor.AddObservation(transform.position.x); // ship coords
            sensor.AddObservation(transform.position.y); // ship coords
            sensor.AddObservation(transform.position.z); // ship coords
            */
            //Debug.Log("transform x " + transform.rotation.eulerAngles.x + " y " + transform.rotation.eulerAngles.y + " z " + transform.rotation.eulerAngles.z);

            sensor.AddObservation(rb.angularVelocity);

            //sensor.AddObservation(raySensor.RaySensor.);
            /*
            sensor.AddObservation(rb.angularVelocity.x);
            sensor.AddObservation(rb.angularVelocity.y);
            sensor.AddObservation(rb.angularVelocity.z);
            */
            //sensor.AddObservation(rb.angularVelocity.magnitude);

            //sensor.AddObservation(rocket.countRotations);

            //Debug.Log("angularVelocity x " + rb.angularVelocity.x + " y " + rb.angularVelocity.y + " z " + rb.angularVelocity.z);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float reward = 0.0f;

        base.OnActionReceived(actionBuffers);

        RocketSphereAI rocket = transform.gameObject.GetComponent<RocketSphereAI>();

        if (rocket)
        {
            rocket.horizontalInput = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
            // little reward turning
            //SetReward(0.001f * Mathf.Abs(rocket.horizontalInput));

            rocket.verticalInput = Mathf.Clamp(actionBuffers.ContinuousActions[1], 0f, 1f);
            // little reward thrusting
            //SetReward(0.001f * rocket.verticalInput);

            if (actionBuffers.ContinuousActions[2] > 0.0f)
            {
                rocket.fireInput = true;
            }
            else
            {
                rocket.fireInput = false;
            }

            if (rocket.points > lastPoints)
            {
                // big reward if we hit something
                reward = 3.0f * (rocket.points - lastPoints);
                SetReward(reward);
                Debug.Log("Reward for points " + rocket.points + " last points " + lastPoints + " reward " + reward);
                lastPoints = rocket.points;
            }

            /*
            if (rocket.fireInput)
            {
                // little reward for shooting
                reward = 0.01f;
                SetReward(reward);
                //Debug.Log("Reward for shooting " + reward);
            }
            else
            {
                // little negative reward for not shooting
                reward = -0.001f;
                SetReward(reward);
                //Debug.Log("little negative reward for not shooting " + reward);
            }
            */
        }

 
        if (rocket.countRotations != lastCountRotations)
        {
            // negative reward for excessive rotations
            reward = -0.01f * math.abs(rocket.countRotations);
            SetReward(reward);
            if (reward < -0.1f)
            {
                //Debug.Log("Negative reward for excessive rotations " + rocket.countRotations + " reward " + reward);
            }

            lastCountRotations = rocket.countRotations;
        }

        if (rb)
        {
            if (rb.angularVelocity.magnitude < 0.02f)
            {
                // negative reward for not moving
                reward = -0.01f * (0.02f - rb.angularVelocity.magnitude);
                SetReward(reward);
                //Debug.Log("Negative Reward for not moving " + rb.angularVelocity.magnitude + " reward " + reward);
            }

            if ((rb.angularVelocity.magnitude >= 0.02f) && (rb.angularVelocity.magnitude <= 0.5f))
            {
                // reward for moving
                reward = 0.01f * (rb.angularVelocity.magnitude - 0.02f);
                SetReward(reward);
                //Debug.Log("Reward for moving " + rb.angularVelocity.magnitude + " reward " + reward);
            }

            if (rb.angularVelocity.magnitude > 0.5f)
            {
                // negative reward for moving too fast
                reward = 0.01f * 2.0f * (2.0f * 0.5f - rb.angularVelocity.magnitude);
                if (reward > 0.0f)
                {
                    //Debug.Log("Decreasing reward for moving " + rb.angularVelocity.magnitude + " reward " + reward);
                }
                else
                {
                    //Debug.Log("Increasing negative Reward for moving too fast " + rb.angularVelocity.magnitude + " reward " + reward);
                }
                SetReward(reward);
            }
        }

        if (raySensor)
        {
            /*
            if (raySensor.DetectableTags.Count > 0)
            {
                // reward being near something
                reward = 0.001f * raySensor.DetectableTags.Count;
                SetReward(reward);
                //Debug.Log("Reward for being near something " + raySensor.DetectableTags.Count + " reward " + reward);
            }
            */

            if (raySensor.RaySensor.RayPerceptionOutput.RayOutputs[8].HasHit)
            {
                if (rocket.fireInput)
                {
                    // reward pointing the ship right at something and shooting
                    reward = 0.2f;
                    SetReward(reward);
                    //Debug.Log("reward pointing the ship right at something and shooting " + reward);
                }
                else
                {
                    // negative reward for pointing the ship right at something and not shooting
                    reward = -0.05f;
                    SetReward(reward);
                    //Debug.Log("negative reward for pointing the ship right at something and not shooting " + reward);
                }
            }
            else if (raySensor.RaySensor.RayPerceptionOutput.RayOutputs[6].HasHit || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[10].HasHit)
            {
                if (rocket.fireInput)
                {
                    // reward pointing the ship at something and shooting
                    reward = 0.1f;
                    SetReward(reward);
                    //Debug.Log("reward pointing the ship at something and shooting " + reward);
                }
                else
                {
                    // negative reward for pointing the ship at something and not shooting
                    reward = -0.025f;
                    SetReward(reward);
                    //Debug.Log("negative reward for pointing the ship at something and not shooting " + reward);
                }
            }

            if (raySensor.RaySensor.RayPerceptionOutput.RayOutputs[7].HasHit 
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[9].HasHit 
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[11].HasHit)
            {
                if (rocket.verticalInput > 0.2f)
                {
                    // reward for thrusting away from something
                    reward = 0.01f;
                    SetReward(reward);
                    //Debug.Log("reward thrusting away from something " + reward);
                }
                else
                {
                    // negative reward for not thrusting away from something
                    reward = -0.01f;
                    SetReward(reward);
                    //Debug.Log("reward thrusting away from something " + reward);
                }
            }
            
            /*
            if (raySensor.RaySensor.RayPerceptionOutput.RayOutputs[0].HasHit 
                ||  raySensor.RaySensor.RayPerceptionOutput.RayOutputs[1].HasHit
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[2].HasHit
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[3].HasHit
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[13].HasHit
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[14].HasHit
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[15].HasHit
                || raySensor.RaySensor.RayPerceptionOutput.RayOutputs[16].HasHit)
            {
                if (rocket.horizontalInput > 0.2f)
                {
                    // reward turning either away from or towards something on the side
                    reward = 0.01f;
                    SetReward(reward);
                    //Debug.Log("reward turning either away from or towards something " + reward);
                }
                else
                {
                    // negative reward for not turning when something is on the side
                    reward = -0.01f;
                    SetReward(reward);
                    //Debug.Log("negative reward for not turning when something is on the side " + reward);
                }
            }
            */
        }
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        raySensor = transform.gameObject.GetComponentInChildren<RayPerceptionSensorComponent3D>();
        rocket = transform.gameObject.GetComponent<RocketSphereAI>();
        rb = transform.gameObject.GetComponent<Rigidbody>();

        if (rocket)
        {
            lastPoints = rocket.points;
        }
        else
        {
            lastPoints = 0;
        }
    }

    public void EpisodeEndGood()
    {
        Debug.Log("reward survived for lifetime " + 0.2f);
        SetReward(0.2f);
        EndEpisode();
    }

    public void EpisodeEndBad()
    {
        Debug.Log("Negative reward for dying " + -1.0f);
        SetReward(-1.0f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(actionsOut);

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        if (Input.GetButton("Fire1"))
        {
            continuousActionsOut[2] = 1.0f;
        }
        else
        {
            continuousActionsOut[2] = 0.0f;
        }
    }
}
