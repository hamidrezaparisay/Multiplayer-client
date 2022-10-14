using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform follow;
    public Vector3 initalOffset;
    public float smoothness;
    public float rotSmotheness;

    private Vector3 cameraPosition;
    private Quaternion cameraRotation;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        cameraPosition = follow.position + initalOffset;
        transform.position = Vector3.Lerp(transform.position, cameraPosition, smoothness*Time.fixedDeltaTime);
        cameraRotation = Quaternion.LookRotation(follow.position-transform.position);
        transform.rotation=Quaternion.Lerp(transform.rotation,cameraRotation, rotSmotheness*Time.fixedDeltaTime);
    }
}
