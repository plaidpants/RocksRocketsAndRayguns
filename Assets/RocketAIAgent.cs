
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

    public override void Initialize()
    {
        base.Initialize();

        lastPoints = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);

        RocketSphereAI rocket = transform.gameObject.GetComponent<RocketSphereAI>();
        if (rocket)
        {
            sensor.AddObservation(rocket.gameObject.transform.rotation);
            sensor.AddObservation(rocket.gameObject.GetComponent<Rigidbody>().angularVelocity);
        }
    }


    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);
        
        RocketSphereAI rocket = transform.gameObject.GetComponent<RocketSphereAI>();

        if (rocket)
        {
            rocket.horizontalInput = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
            rocket.verticalInput = 2f * Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
            rocket.fireInput = actionBuffers.ContinuousActions[2] > 0.0f;

            if (rocket.points > lastPoints)
            {
                // reward if we hit something
                SetReward(0.1f);
                lastPoints = rocket.points;
            }

            if (rocket.fireInput)
            {
                // don't reward excesive shooting
                SetReward(-0.01f);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        RocketSphereAI rocket = transform.gameObject.GetComponent<RocketSphereAI>();
        if (rocket)
        {
            lastPoints = rocket.points;
        }
        else
        {
            lastPoints = 0;
        }
    }

    public void EpisodeEnd()
    {
        SetReward(-1f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        base.Heuristic(actionsOut);

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        if (Input.GetButton("Fire"))
        {
            continuousActionsOut[2] = 1.0f;
        }
        else
        {
            continuousActionsOut[2] = 0.0f;
        }
    }
}
