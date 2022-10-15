using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;

namespace Net{
    // [BurstCompile]
    struct ServerUpdateJob : IJobParallelForDefer
    {
        public NetworkDriver.Concurrent driver;
        public NativeArray<NetworkConnection> connections;


        public void Execute(int index)
        {
            DataStreamReader stream;
            Assert.IsTrue(connections[index].IsCreated);//what?
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[index], out stream)) !=
            NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    Debug.Log("Client sended data");
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[index] = default(NetworkConnection);
                }
            }
        }
    }
    public class Server : MonoBehaviour
    {
        public int clientCount=1;
        public NetworkDriver m_Driver;
        private NativeList<NetworkConnection> m_Connections;
        private JobHandle ServerJobHandle;
        // Start is called before the first frame update
        void Start ()
        {
            m_Connections = new NativeList<NetworkConnection>(clientCount, Allocator.Persistent);
            // var settings = new NetworkSettings(); 
            // settings.WithNetworkConfigParameters();
            // settings.WithSecureServerParameters()

            // m_Driver = NetworkDriver.Create(new SimulatorUtility.Parameters {MaxPacketSize = NetworkParameterConstants.MTU, MaxPacketCount = 30, PacketDelayMs = 25, PacketDropPercentage = 10});
            // m_Pipeline = m_Driver.CreatePipeline(typeof(SimulatorPipelineStage));

            m_Driver = NetworkDriver.Create();

            var endpoint = NetworkEndPoint.AnyIpv4;
            endpoint.Port = 9000;
            if (m_Driver.Bind(endpoint) != 0)
                Debug.Log("Failed to bind to port 9000");
            else
                m_Driver.Listen();

        }
        public void OnDestroy()
        {
            // Make sure we run our jobs to completion before exiting.
            if (m_Driver.IsCreated)
            {
                ServerJobHandle.Complete();
                m_Connections.Dispose();
                m_Driver.Dispose();
            }
        }

        // Update is called once per frame
        void Update()
        {
            ServerJobHandle.Complete();
            ServerUpdateConnectionsJob connectionJob = new ServerUpdateConnectionsJob
            {
                driver = m_Driver,
                connections = m_Connections
            };

            ServerUpdateJob serverUpdateJob = new ServerUpdateJob
            {
                driver = m_Driver.ToConcurrent(),
                connections = m_Connections.AsDeferredJobArray(),
            };

            ServerJobHandle = m_Driver.ScheduleUpdate();
            ServerJobHandle = connectionJob.Schedule(ServerJobHandle);
            ServerJobHandle = serverUpdateJob.Schedule(m_Connections, 1, ServerJobHandle);
        }

        // [BurstCompile]
        struct ServerUpdateConnectionsJob : IJob
        {
            public NetworkDriver driver;
            public NativeList<NetworkConnection> connections;
            public void Execute()
            {
                // Clean up connections
                for (int i = 0; i < connections.Length; i++)
                {
                    if (!connections[i].IsCreated)
                    {
                        connections.RemoveAtSwapBack(i);
                        --i;
                    }
                }
                // Accept new connections
                NetworkConnection c;
                while ((c = driver.Accept()) != default(NetworkConnection))
                {
                    connections.Add(c);
                    Debug.Log("internal id="+c.InternalId);
                    Debug.Log("Accepted a connection");
                }
            }
        }       
    }
}
