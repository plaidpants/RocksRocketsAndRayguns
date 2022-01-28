using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CameraFollowRocket : MonoBehaviour
{

    public Transform player;
    public float rotSpeed;

#if !UNITY_ANDROID || UNITY_EDITOR
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;
    public float sensitivity = 0.1f;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    Quaternion originalRotation;

    float rotationX = 0;
    float rotationY = 0;
#endif

    private void Start()
    {
        // start the camera in a random direction
        Quaternion rot = Quaternion.LookRotation(Random.onUnitSphere);
        transform.rotation = rot;

#if !UNITY_ANDROID || UNITY_EDITOR
        // Make the rigid body not change rotation
        originalRotation = transform.localRotation;
#endif
    }

#if !UNITY_ANDROID || UNITY_EDITOR
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
#endif

    // this camera repositioning will get overridden with any VR camera update if it is active and tracking
    void LateUpdate()
    {

        if (player.transform && player.gameObject.activeSelf)
        {
            Quaternion rot = Quaternion.LookRotation(player.position);

            transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, Time.deltaTime * rotSpeed * 0.1f);
        }
        else
        {
#if !UNITY_ANDROID || UNITY_EDITOR
    // Read the mouse input axis
            rotationX = rotationX + Input.GetAxis("Mouse X") * sensitivityX;
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

            rotationX = ClampAngle(rotationX, minimumX, maximumX);
            rotationY = ClampAngle(rotationY, minimumY, maximumY);

            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

            transform.localRotation = originalRotation * xQuaternion * yQuaternion;
#endif
        }
    }
}
