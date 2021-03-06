﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CameraFollowRocket : MonoBehaviour
{

    public Transform player;
    public float rotSpeed;

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
