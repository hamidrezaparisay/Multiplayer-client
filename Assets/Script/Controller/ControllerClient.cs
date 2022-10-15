using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Net;

public class ControllerClient : MonoBehaviour
{
    public static float timer;

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
    [System.NonSerialized]public InputMessage inputData;

    Rigidbody carRB;
    RaycastHit hitData;
    Transform[] tires;

    float3 client_pos_error;
    quaternion client_rot_error;

    NativeArray<InputMessage> inputBuffer;
    void Start()
    {
        client_pos_error=float3.zero;
        carRB=GetComponent<Rigidbody>();
        tires=new Transform[4];
        tires[0]=transform.GetChild(0);
        tires[1]=transform.GetChild(1);
        tires[2]=transform.GetChild(2);
        tires[3]=transform.GetChild(3);

        timer=0;
    }

    void Update()
    {
        timer += Time.deltaTime;
        while (timer >= Time.fixedDeltaTime)
        {
            timer -= Time.fixedDeltaTime;

            int buffer_slot=NetBufferClient.tick%NetBufferClient.buuferSize;
            NetBufferClient.saveInputAndState(inputData,new State(carRB.position,carRB.rotation),buffer_slot);
            NetBufferClient.saveInputSend(inputData);

            ClientSocket.Send(NetBufferClient.inputSend);          

            this.AddForces(inputData);
            Physics.Simulate(Time.fixedDeltaTime);

            this.correct();
            this.smooth();
            
            NetBufferClient.tick++;
        }
    }
    void correct()
    {
        if(NetBufferClient.doneProcessingLast)
            return;
        SnapShot s=NetBufferClient.lastSnapShot;
        Header h=NetBufferClient.lastHeader;
        int buffer_slot= h.frame % NetBufferClient.buuferSize;
        State buffer=NetBufferClient.stateBuffer[buffer_slot];

        float3 position_error=s.state.position-buffer.position;
        float rotation_error=1.0f-math.dot(s.state.rotation,buffer.rotation);

        if(math.lengthsq(position_error) > 0.0000001f || rotation_error > 0.00001f)
        {
            Debug.Log("Correcting at tick "+h.frame+"(rewinding "+(NetBufferClient.tick-h.frame)+"ticks)");
            float3 prev_pos=(float3)carRB.position - client_pos_error;
            quaternion prev_rot=math.mul(carRB.rotation,client_rot_error);

            carRB.position=s.state.position;
            carRB.rotation=s.state.rotation;
            carRB.velocity=s.rbState.vol;
            carRB.angularVelocity=s.rbState.angularVol;

            int rewind_tick_number=h.frame;
            while(rewind_tick_number<NetBufferClient.tick)
            {
                buffer_slot= rewind_tick_number % NetBufferClient.buuferSize;
                NetBufferClient.saveState(s.state,rewind_tick_number);
                this.AddForces(NetBufferClient.inputBuffer[buffer_slot]);
                Physics.Simulate(Time.fixedDeltaTime);
                rewind_tick_number++;
            }
            if(math.distancesq(prev_pos,carRB.position)>=4.0f)
            {
                client_pos_error=float3.zero;
                client_rot_error=quaternion.identity;
            }
            else
            {
                client_pos_error=prev_pos-(float3)carRB.position;
                client_rot_error=math.mul(math.inverse(carRB.rotation),prev_rot);
            }
        }
    }
    void smooth()
    {
        client_pos_error*=0.9f;
        client_rot_error=math.slerp(client_rot_error,quaternion.identity,0.1f);

        carRB.position=(float3)carRB.position+client_pos_error;
        carRB.rotation=math.mul(carRB.rotation,client_rot_error);
    }
    void AddForces(InputMessage mInput)
    {
        float2 input=mInput.input;
        accelInput=input.y * maxSpeed ;
        rotate=input.x * maxAngle;
        tires[0].localRotation=Quaternion.Euler(0,rotate,0);
        tires[1].localRotation=Quaternion.Euler(0,rotate,0);

        for(int i=0;i<4;i++)
        {
            if(Physics.Raycast(tires[i].position,-tires[i].up,out hitData, 1.5f,groundMask))
            {
                float3 springDir=tires[i].up;
                float3 tireWorldVel=carRB.GetPointVelocity(tires[i].position);
                float offset=susRest-hitData.distance;
                float vel=math.dot(springDir,tireWorldVel);
                float force=(offset*springStr) - (vel * springDamp);
                carRB.AddForceAtPosition(springDir*force,tires[i].position);
                
                float3 accelDir=tires[i].forward;
                if(Mathf.Abs(accelInput)>0.0f)
                {
                    float carSpeed=math.dot(transform.forward,carRB.velocity);
                    float torque=Mathf.Clamp01(carSpeed/maxSpeed);
                    torque=powerCurve.Evaluate(torque)*accelInput;
                    carRB.AddForceAtPosition(accelDir*torque,tires[i].position);
                }
                else{
                    carRB.AddForceAtPosition(-carRB.velocity/10,tires[i].position);
                }

                float3 steerDir=tires[i].right;
                tireWorldVel=carRB.GetPointVelocity(tires[i].position);
                float steerVel=math.dot(steerDir,tireWorldVel);
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
            carRB.AddForceAtPosition(transform.right*10-transform.forward*1,contact.point,ForceMode.VelocityChange);
        }
    }
    void OnCollisionExit(Collision collision)
    {
        carRB.velocity*=0.4f;
    }
}
