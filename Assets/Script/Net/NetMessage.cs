using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Mathematics;

namespace Net
{
    public struct ClientHeader
    {
        public int seq_no;
        public int last_rcv;
        public float sendTime;
        public ClientHeader(byte seq_no, byte last_rcv,float sendTime)
        {
            this.seq_no=seq_no;
            this.last_rcv=last_rcv;
            this.sendTime=sendTime;
        }
        public void Serialize(ref DataStreamWriter writer)
        {
            writer.WriteRawBits((uint)this.seq_no,4);//0-31
            writer.WriteRawBits((uint)this.last_rcv,3);//0-15
            writer.WriteFloat(sendTime);
        }
        public void Deserialize(ref DataStreamReader reader)
        {
            this.seq_no=(byte)reader.ReadRawBits(4);//0-31
            this.last_rcv=(byte)reader.ReadRawBits(3);//0-31
            this.sendTime=reader.ReadFloat();
        }
    }
    public struct InputMessage
    {
        public float2 input;
        public InputMessage(float2 input)
        {
            this.input=input;
        }
        public void Serialize(ref DataStreamWriter writer, ClientHeader header)
        {
            header.Serialize(ref writer);
            writer.WriteFloat(input.x);
            writer.WriteFloat(input.y);
        }
        public float2 Deserialize(ref DataStreamReader reader)//reader have been readed header
        {
            float2 input;
            input.x=reader.ReadFloat();
            input.y=reader.ReadFloat();
            return input;
        }
    }
    public enum OpCode
    {
        IN_MSG=0,
    }
}

