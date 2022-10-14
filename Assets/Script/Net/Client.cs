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
        public InputMessage input;
        public NetworkDriver driver;
        public NativeArray<NetworkConnection> connection;
        public ClientHeader header;
        public void Execute()
        {
            DataStreamWriter writer;
            driver.BeginSend(connection[0],out writer);
            input.Serialize(ref writer,header);
            driver.EndSend(writer);
        }
    }
    struct ClientUpdateJob : IJob
    {
        public NetworkDriver driver;
        public NativeArray<NetworkConnection> connection;

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
                    Debug.Log("Got the data back from the server");
                    
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    connection[0] = default(NetworkConnection);
                }
            }
        }
    }
    public class Client : MonoBehaviour
    {
        public NetworkDriver m_Driver;
        public NativeArray<NetworkConnection> m_Connection;
        public JobHandle ClientJobHandle;
        ClientHeader clientHeader;
        InputMessage inMessage;
        // Start is called before the first frame update
        void Start ()
        {
            // var settings = new NetworkSettings(); 
            // settings.WithNetworkConfigParameters();
            // settings.WithSecureClientParameters()
            m_Driver = NetworkDriver.Create();

            m_Connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);

            var endpoint = NetworkEndPoint.LoopbackIpv4;
            // NetworkEndPoint.TryParse("192.168.1.1",9000,out endpoint,NetworkFamily.Ipv4);
            endpoint.Port = 9000;

            m_Connection[0] = m_Driver.Connect(endpoint);

            clientHeader=new ClientHeader(0,0,0);
            inMessage=new InputMessage(new float2());
        }
        public void OnDestroy()
        {
            // Make sure we run our jobs to completion before exiting.
            ClientJobHandle.Complete();

            if(m_Driver.IsCreated)
            {
                m_Connection.Dispose();
                m_Driver.Dispose();
            }
        }

        // Update is called once per frame
        void Update()
        {
            ClientJobHandle.Complete();
            var job = new ClientUpdateJob
            {
                driver = m_Driver,
                connection = m_Connection,
            };
            //call send input also
            ClientJobHandle = m_Driver.ScheduleUpdate();
            ClientJobHandle = job.Schedule(ClientJobHandle);
        }
        
    }
}
