using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class Controller : MonoBehaviour
{
    
    public LayerMask groundMask;
    public float rayOffset=1.5f;
    public float susRest=7.5f;
    public float springStr=500;
    public float springDamp=10;
    public float maxSpeed=100;
    public float maxAngle=20;
    public AnimationCurve powerCurve;
    public float tireGripFactor=0.3f;

    float accelInput;
    float rotate;
    [System.NonSerialized]public Vector2 inputData;

    Rigidbody carRB;
    RaycastHit hitData;
    Transform[] tires;
    void Start()
    {
        carRB=GetComponent<Rigidbody>();
        tires=new Transform[4];
        tires[0]=transform.GetChild(0);
        tires[1]=transform.GetChild(1);
        tires[2]=transform.GetChild(2);
        tires[3]=transform.GetChild(3);
        inputData=Vector2.zero;
    }

    void Update()
    {
        accelInput=inputData.y * maxSpeed ;
        rotate=inputData.x * maxAngle;
        tires[0].localRotation=Quaternion.Euler(0,rotate,0);
        tires[1].localRotation=Quaternion.Euler(0,rotate,0);
    }
    void FixedUpdate()
    {
        for(int i=0;i<4;i++)
        {
            if(Physics.Raycast(tires[i].position,-tires[i].up,out hitData, 1.5f,groundMask))
            {
                Vector3 springDir=tires[i].up;
                Vector3 tireWorldVel=carRB.GetPointVelocity(tires[i].position);
                float offset=susRest-hitData.distance;
                float vel=Vector3.Dot(springDir,tireWorldVel);
                float force=(offset*springStr) - (vel * springDamp);
                carRB.AddForceAtPosition(springDir*force,tires[i].position);
                
                Vector3 accelDir=tires[i].forward;
                if(Mathf.Abs(accelInput)>0.0f)
                {
                    float carSpeed=Vector3.Dot(transform.forward,carRB.velocity);
                    float torque=Mathf.Clamp01(carSpeed/maxSpeed);
                    torque=powerCurve.Evaluate(torque)*accelInput;
                    carRB.AddForceAtPosition(accelDir*torque,tires[i].position);
                }
                else{
                    carRB.AddForceAtPosition(-carRB.velocity/10,tires[i].position);
                }

                Vector3 steerDir=tires[i].right;
                tireWorldVel=carRB.GetPointVelocity(tires[i].position);
                float steerVel=Vector3.Dot(steerDir,tireWorldVel);
                float desiredVelChange=-steerVel * tireGripFactor;
                float desiredAccel=desiredVelChange/Time.fixedDeltaTime;
                carRB.AddForceAtPosition(steerDir*desiredAccel,tires[i].position);
            }
        }
        
    }
    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // carRB.isKinematic=true;
            // transform.Rotate(0, 100, 0);
            // carRB.isKinematic=false;

            // newForce-=carRB.velocity;
            carRB.AddForceAtPosition(transform.right*10-transform.forward*1,contact.point,ForceMode.VelocityChange);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        carRB.velocity*=0.4f;
        // Debug.Log("exit");
    }
}
