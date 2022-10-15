using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using UnityEngine;
using Unity.Mathematics;

namespace Net{

    [BurstCompile]
    struct ClientSendJob : IJob
    {
        public NativeArray<InputMessage> input;
        public NetworkDriver driver;
        public NativeArray<NetworkConnection> connection;
        public Header header;
        public void Execute()
        {
            DataStreamWriter writer;
            driver.BeginSend(connection[0],out writer);
            header.Serialize(ref writer);
            for(int i=0;i<input.Length;i++)
                input[i].Serialize(ref writer);
            driver.EndSend(writer);
        }
    }
    // [BurstCompile]
    struct ClientUpdateJob : IJob
    {
        public NetworkDriver driver;
        public NativeArray<NetworkConnection> connection;
        public NativeArray<Header> lastHeader;
        public NativeArray<SnapShot> lastSnapShot;

        public void Execute()
        {
            if (!connection[0].IsCreated)
            {
                return;
            }
            DataStreamReader stream;
            NetworkEvent.Type cmd;
            while ((cmd = connection[0].PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("We are now connected to the server");
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    Header temp=new Header(0);
                    temp.Deserialize(ref stream);
                    if(lastHeader[0].frame<temp.frame || lastHeader[0].frame-temp.frame>20)
                    {
                        lastHeader[0]=temp;
                        lastSnapShot[0].Deserialize(ref stream);
                    }
                    Debug.Log("Got a snapShot from the server");
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    connection[0] = default(NetworkConnection);
                }
            }
        }
    }
    public class ClientSocket : MonoBehaviour
    {
        static NetworkDriver m_Driver;
        static NativeArray<NetworkConnection> m_Connection;
        JobHandle ClientJobHandle;
        static JobHandle SendHandle;

        public static NativeArray<Header> lastHeader;
        public static NativeArray<SnapShot> lastSnapShot;


        public static void Send(NativeArray<InputMessage> inputs)
        {
            var job = new ClientSendJob{
                input=inputs,
                driver=m_Driver,
                connection=m_Connection,
                header=new Header(NetBufferClient.tick)
            };
            SendHandle = job.Schedule(SendHandle);
        }
        void Start ()
        {
            lastHeader=new NativeArray<Header>(1,Allocator.Persistent);
            Header temp;
            temp.frame=-1;
            lastHeader[0]=temp;
            lastSnapShot=new NativeArray<SnapShot>(1,Allocator.Persistent);

            NetBufferClient.start();
            // var settings = new NetworkSettings(); 
            // settings.WithNetworkConfigParameters();
            // settings.WithSecureClientParameters()
            m_Driver = NetworkDriver.Create();

            m_Connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);

            var endpoint = NetworkEndPoint.LoopbackIpv4;
            // NetworkEndPoint.TryParse("192.168.1.1",9000,out endpoint,NetworkFamily.Ipv4);
            endpoint.Port = 9000;

            m_Connection[0] = m_Driver.Connect(endpoint);

        }
        void OnDestroy()
        {
            NetBufferClient.exit();
            // Make sure we run our jobs to completion before exiting.
            ClientJobHandle.Complete();

            if(m_Driver.IsCreated)
            {
                m_Connection.Dispose();
                m_Driver.Dispose();
                lastHeader.Dispose();
                lastSnapShot.Dispose();
            }
        }

        // Update is called once per frame
        void Update()
        {
            ClientJobHandle.Complete();
            if(NetBufferClient.lastHeader.frame!=lastHeader[0].frame)
            {
                NetBufferClient.lastHeader=lastHeader[0];
                NetBufferClient.lastSnapShot=lastSnapShot[0];
                NetBufferClient.doneProcessingLast=false;
            }
            
            var job = new ClientUpdateJob
            {
                driver = m_Driver,
                connection = m_Connection,
                lastHeader=lastHeader,
                lastSnapShot=lastSnapShot,
            };
            //call send input also
            ClientJobHandle = m_Driver.ScheduleUpdate();
            ClientJobHandle = job.Schedule(ClientJobHandle);
            SendHandle.Complete();   
        }
        
    }
}
