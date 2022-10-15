using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Net;

namespace Net{
public static class NetBufferClient
{
    static int playerCount=1;

    public static int buuferSize=1024;
    public static int sendSize=5;

    public static int tick=0;

    public static Header lastHeader;
    public static SnapShot lastSnapShot;
    public static bool doneProcessingLast=true;

    public static NativeArray<InputMessage> inputBuffer;
    public static NativeArray<State> stateBuffer;
    public static NativeArray<InputMessage> inputSend;
    
    public static void start()
    {
        inputBuffer=new NativeArray<InputMessage>((int)buuferSize,Allocator.Persistent);
        stateBuffer=new NativeArray<State>((int)buuferSize,Allocator.Persistent);
        
        inputSend=new NativeArray<InputMessage>(sendSize,Allocator.Persistent);
    }
    public static void exit()
    {
        if(inputBuffer.IsCreated)inputBuffer.Dispose();
        if(stateBuffer.IsCreated)stateBuffer.Dispose();
        if(inputSend.IsCreated)inputSend.Dispose();
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
        for(int i=sendSize-2;i>=0;i--)
        {
            inputSend[i]=inputSend[i+1];
        }
        inputSend[sendSize-1]=input;
    }
}
}
