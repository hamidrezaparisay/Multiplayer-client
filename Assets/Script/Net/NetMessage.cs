using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Mathematics;

namespace Net
{
    public struct Header
    {
        public int frame;//max=1024
        public Header(int frame)
        {
            this.frame=frame;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteRawBits((uint)frame,10);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            this.frame=(int)reader.ReadRawBits(10);
        }
    }
    
    //client sends&serever recives
    public struct InputMessage
    {
        public float2 input;
        public InputMessage(float2 input)
        {
            this.input=input;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteFloat(input.x);
            writer.WriteFloat(input.y);
        }
        public void Deserialize(ref DataStreamReader reader)//reader have been readed header
        {
            float2 input;
            input.x=reader.ReadFloat();
            input.y=reader.ReadFloat();
        }
    }

    //server sends&client receives
    public struct State
    {
        public float3 position;
        public quaternion rotation;
        public State(float3 position, quaternion rotation)
        {
            this.position=position;
            this.rotation=rotation;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteFloat(position.x);
            writer.WriteFloat(position.y);
            writer.WriteFloat(position.z);

            writer.WriteFloat(rotation.value.x);
            writer.WriteFloat(rotation.value.y);
            writer.WriteFloat(rotation.value.z);
            writer.WriteFloat(rotation.value.w);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            position.x=reader.ReadFloat();
            position.y=reader.ReadFloat();
            position.z=reader.ReadFloat();

            float4 q;
            q.x=reader.ReadFloat();
            q.y=reader.ReadFloat();
            q.z=reader.ReadFloat();
            q.w=reader.ReadFloat();
            rotation.value=q;
        }
    }
    public struct ServerState
    {
        public State state;
        public int entityId;//max=15
        public void Serialize(ref DataStreamWriter writer)
        {
            state.Serialize(ref writer);
            writer.WriteRawBits((uint)entityId,4);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            state.Deserialize(ref reader);
            entityId=(int)reader.ReadRawBits(4);
        }
    }
    public struct rigidBodyState
    {
        public float3 vol;
        public float3 angularVol;
        public rigidBodyState(float3 vol, float3 angularVol)
        {
            this.vol=vol;
            this.angularVol=angularVol;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteFloat(vol.x);
            writer.WriteFloat(vol.y);
            writer.WriteFloat(vol.z);

            writer.WriteFloat(angularVol.x);
            writer.WriteFloat(angularVol.y);
            writer.WriteFloat(angularVol.z);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            vol.x=reader.ReadFloat();
            vol.y=reader.ReadFloat();
            vol.z=reader.ReadFloat();

            angularVol.x=reader.ReadFloat();
            angularVol.y=reader.ReadFloat();
            angularVol.z=reader.ReadFloat();
        }
    }
    public struct SnapShot
    {
        public rigidBodyState rbState;
        public State state;
        public NativeArray<ServerState> entityStates;
        public SnapShot(rigidBodyState rbState,State state, NativeArray<ServerState> entityStates)
        {
            this.rbState=rbState;
            this.state=state;
            this.entityStates=entityStates;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            rbState.Serialize(ref writer);
            state.Serialize(ref writer);
            for(int i=0;i<entityStates.Length;i++)
                entityStates[i].Serialize(ref writer);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            rbState.Deserialize(ref reader);
            state.Deserialize(ref reader);
            for(int i=0;i<entityStates.Length;i++)
                entityStates[i].Deserialize(ref reader);
        }
    }
}

