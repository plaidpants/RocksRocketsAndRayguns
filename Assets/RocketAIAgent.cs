
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class RocketAIAgent : Agent
{
    RocketSphereAI rocket;
    int lastPoints = 0;
    RayPerceptionSensorComponent3D sensor;
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
            sensor.AddObservation(transform.rotation.eulerAngles.x); // ship on the sphere
            sensor.AddObservation(transform.rotation.eulerAngles.y); // ship on the sphere
            sensor.AddObservation(transform.rotation.eulerAngles.z); // ship rotation

            //Debug.Log("transform x " + transform.rotation.eulerAngles.x + " y " + transform.rotation.eulerAngles.y + " z " + transform.rotation.eulerAngles.z);

            sensor.AddObservation(rb.angularVelocity.x);
            sensor.AddObservation(rb.angularVelocity.y);
            sensor.AddObservation(rb.angularVelocity.z);
            //Debug.Log("angularVelocity x " + rb.angularVelocity.x + " y " + rb.angularVelocity.y + " z " + rb.angularVelocity.z);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);
        
        RocketSphereAI rocket = transform.gameObject.GetComponent<RocketSphereAI>();

        if (rocket)
        {
            rocket.horizontalInput = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
            rocket.verticalInput = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
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
                // reward if we hit something
                SetReward(1.0f * (rocket.points - lastPoints));
                Debug.Log("Reward for points " + rocket.points + " last points " + lastPoints + " reward " + 0.5f * (rocket.points - lastPoints));
                lastPoints = rocket.points;
            }

            if (rocket.fireInput)
            {
                SetReward(0.01f);
                Debug.Log("Reward shooting " + 0.001f);
            }
        }

        if (sensor)
        {
            if (sensor.DetectableTags.Count > 0)
            {
                // reward being near something
                SetReward(0.001f);
                Debug.Log("Reward for being near something " + sensor.DetectableTags.Count + " reward "+ 0.001f);
            }
        }

        if (rb)
        {
            if (rb.angularVelocity.magnitude > 0.0f)
            {
                // reward for moving
                SetReward(0.01f);
                Debug.Log("Reward for moving " + 0.001f);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();

        sensor = transform.gameObject.GetComponent<RayPerceptionSensorComponent3D>();
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
        Debug.Log("survived for lifetime " + 0.2f);
        SetReward(0.2f);
        EndEpisode();
    }

    public void EpisodeEndBad()
    {
        Debug.Log("Reward for dying " + -1.0f);
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
