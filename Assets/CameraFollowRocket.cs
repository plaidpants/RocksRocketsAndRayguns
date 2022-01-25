using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CameraFollowRocket : MonoBehaviour
{

    public Transform player;
    public float rotSpeed;

    private void Start()
    {
        // start the camera in a random direction
        Quaternion rot = Quaternion.LookRotation(Random.onUnitSphere);
        transform.rotation = rot;
    }

    // this camera repositioning will get overridden with any VR camera update if it is active and tracking
    void LateUpdate()
    {
        if (player.transform)
        {
            Quaternion rot = Quaternion.LookRotation(player.position);

            transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, Time.deltaTime * rotSpeed * 0.1f);
        }
    }
}
