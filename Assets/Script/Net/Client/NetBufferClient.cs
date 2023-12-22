using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Net;
using Init;

namespace Net{
public static class NetBufferClient
{
    public static int selfIndex=0;//must be set in startup of the scene
    static public int playerCount=1;
    public static int tick=0;

    public static int bufferSize=1024;

    public static int sendSize=5;

    public static NativeArray<Header> lastHeader;
    public static NativeArray<SnapShot> lastSnapShot;
    public static NativeArray<bool> doneProcessingLast;

    public static NativeArray<InputMessage> inputBuffer;
    public static NativeArray<State> stateBuffer;
    public static NativeArray<InputMessage> inputSend;
    public static NativeArray<State> snapShotData;
    
    public static void start()
    {
        inputBuffer=new NativeArray<InputMessage>((int)bufferSize,Allocator.Persistent);
        stateBuffer=new NativeArray<State>((int)bufferSize,Allocator.Persistent);

        lastHeader=new NativeArray<Header>(1,Allocator.Persistent);
        lastSnapShot=new NativeArray<SnapShot>(1,Allocator.Persistent);
        doneProcessingLast=new NativeArray<bool>(1,Allocator.Persistent);
        doneProcessingLast[0]=true;
        
        inputSend=new NativeArray<InputMessage>(sendSize,Allocator.Persistent);
        InputMessage temp=new InputMessage();
        temp.frame=-1;
        for(int i=0;i<inputSend.Length;i++)
            inputSend[i]=temp;

        snapShotData=new NativeArray<State>(playerCount,Allocator.Persistent);
    }
    public static void exit()
    {
        if(inputBuffer.IsCreated)inputBuffer.Dispose();
        if(stateBuffer.IsCreated)stateBuffer.Dispose();
        if(inputSend.IsCreated)inputSend.Dispose();
        if(snapShotData.IsCreated)snapShotData.Dispose();
        if(lastSnapShot.IsCreated)lastSnapShot.Dispose();
        if(lastHeader.IsCreated)lastHeader.Dispose();
        if(doneProcessingLast.IsCreated)doneProcessingLast.Dispose();
    }
    public static void initClientScene()
    {
        InitData.setup();
        GameObject controllerGo=new GameObject("Controller");

        EnvLoader env=controllerGo.AddComponent<EnvLoader>();
        env.init();env.loadEnv();

        //add stuff

        ClientSocket.AddThisComponent(controllerGo);
    }
    public static void saveInputAndState(InputMessage input,State state,int slot)
    {
        inputBuffer[slot]=input;
        stateBuffer[slot]=state;
    }
    public static void saveState(State state,int slot)
    {
        stateBuffer[slot]=state;
    }
    public static void saveInputSend(InputMessage input)
    {
        ClientSocket.ClientJobHandle.Complete();
        for(int i=sendSize-1;i>0;i--)
        {
            inputSend[i]=inputSend[i-1];
        }
        inputSend[0]=input;
    }
}
public enum ClientCondition{
    Connecting=0,
    Auth=1,
    Connected=2,
    GameLoop=3,
    Wait=4,
}
}
