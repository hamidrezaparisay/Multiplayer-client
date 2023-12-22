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
    [System.NonSerialized]public AnimationCurve powerCurve;
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
        timer=0;
        client_pos_error=float3.zero;
        client_rot_error=quaternion.identity;
        carRB=GetComponent<Rigidbody>();
        tires=new Transform[4];
        tires[0]=transform.GetChild(0);
        tires[1]=transform.GetChild(1);
        tires[2]=transform.GetChild(2);
        tires[3]=transform.GetChild(3);

        Keyframe[] keyframes=new Keyframe[2];
        keyframes[0]=new Keyframe(0,1,0,0,0,0.3333333f);
        keyframes[1]=new Keyframe(1,1,0,0,0.3333333f,0);
        powerCurve=new AnimationCurve(keyframes);

        timer=0;
    }

    void Update()
    {
        if(ClientSocket.cond!=ClientCondition.GameLoop)
            return;
        timer += Time.deltaTime;
        while (timer >= Time.fixedDeltaTime)
        {
            timer -= Time.fixedDeltaTime;
            inputData.frame=NetBufferClient.tick;
            
            int buffer_slot=NetBufferClient.tick%NetBufferClient.bufferSize;
            NetBufferClient.saveInputSend(inputData);

            ClientSocket.SendInput();

            this.AddForces(inputData);
            Physics.Simulate(Time.fixedDeltaTime);
            NetBufferClient.saveInputAndState(inputData,new State(carRB.position,carRB.rotation),buffer_slot);

            NetBufferClient.tick++;
            if(NetBufferClient.tick==1024)
                NetBufferClient.tick=0;
        }
        if(!NetBufferClient.doneProcessingLast[0])
            this.correct();
        this.smooth();
            
        carRB.transform.position=(float3)carRB.position+client_pos_error;
        carRB.transform.rotation=math.normalize(math.mul(carRB.rotation,client_rot_error));
    }
    void correct()
    {
        SnapShot s=NetBufferClient.lastSnapShot[0];
        Header h=NetBufferClient.lastHeader[0];
        State state=NetBufferClient.snapShotData[NetBufferClient.selfIndex];
        int buffer_slot= h.frame % NetBufferClient.bufferSize;
        State buffer=NetBufferClient.stateBuffer[buffer_slot];

        float3 position_error=state.position-buffer.position;
        float rotation_error=1.0f-math.dot(state.rotation,buffer.rotation);
        if(position_error.x > 0.01f || position_error.y>0.1 || position_error.z>0.01f || rotation_error > 0.0001f )//
        {
            // Debug.Log("Correcting at tick "+h.frame+"(rewinding "+(NetBufferClient.tick-h.frame)+"ticks)");
            string a="";
            if(position_error.x > 0.0001f)
                a+="x";
            if(position_error.y > 0.1f)
                a+="y";
            if(position_error.z > 0.0001f)
                a+="z";
            Debug.Log("correcting bcz of "+a);

            float3 prev_pos=(float3)carRB.position + client_pos_error;
            quaternion prev_rot=math.mul(carRB.rotation,client_rot_error);
            carRB.position=state.position;
            carRB.rotation=math.normalize(state.rotation);
            carRB.velocity=s.rbState.vol;
            carRB.angularVelocity=s.rbState.angularVol;

            int rewind_tick_number=h.frame;
            while(rewind_tick_number<NetBufferClient.tick)
            {
                buffer_slot= rewind_tick_number % NetBufferClient.bufferSize;
                NetBufferClient.saveState(new State(carRB.position,carRB.rotation),rewind_tick_number);
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
        NetBufferClient.doneProcessingLast[0]=true;
    }
    void smooth()
    {
        // client_pos_error*=0.9f;
        // client_rot_error=math.slerp(client_rot_error,quaternion.identity,0.1f);
        
        client_pos_error=float3.zero;
        client_rot_error=quaternion.identity;

        
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
