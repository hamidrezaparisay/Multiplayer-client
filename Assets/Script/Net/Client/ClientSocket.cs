using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Networking.Transport.TLS;
using UnityEngine;
using Unity.Mathematics;

namespace Net{

    // [BurstCompile]
    struct ClientSendInputJob : IJob
    {
        [ReadOnly]public NativeArray<InputMessage> input;
        public NetworkDriver driver;
        public NativeArray<NetworkConnection> connection;
        public Header header;
        public void Execute()
        {
            driver.BeginSend(connection[0],out DataStreamWriter writer);
            header.Serialize(ref writer);
            for(int i=0;i<input.Length;i++)
            {
                input[i].Serialize(ref writer);
            }
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
        public NativeArray<State> lastSnapShotData;
        public NativeArray<bool> doneProcessing;
        public NativeArray<bool> connected;

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
                    connected[0]=true;
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    Header temp=new Header();
                    temp.Deserialize(ref stream);
                    #region Get SnapShot
                    if(temp.OpCode==2)
                    {
                        if(lastHeader[0].frame<temp.frame || lastHeader[0].frame-temp.frame>100)
                        {
                            lastHeader[0]=temp;
                            lastSnapShot[0].Deserialize(ref stream,lastSnapShotData);
                            doneProcessing[0]=false;
                        }
                    }
                    #endregion Get SnapShot
                    
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    connected[0]=false;
                    connection[0] = default(NetworkConnection);
                }
            }
        }
    }
    public class ClientSocket : MonoBehaviour
    {
        static NetworkDriver m_Driver;
        static NativeArray<NetworkConnection> m_Connection;
        public static JobHandle ClientJobHandle;
        public static NetworkEndPoint endpoint;

        NativeArray<bool> connected;

        public static NativeArray<Header> lastHeader;
        public static NativeArray<SnapShot> lastSnapShot;

        public static ClientCondition cond;
        public static void SendInput()
        {
            var job = new ClientSendInputJob{
                input=NetBufferClient.inputSend,
                driver=m_Driver,
                connection=m_Connection,
                header=new Header(NetBufferClient.tick,2),
            };
            ClientJobHandle = job.Schedule(ClientJobHandle);
        }
        public static ClientSocket AddThisComponent(GameObject myObject)
        {
            ClientSocket result=myObject.AddComponent<ClientSocket>();
            result.Start();
            return result;
        }
        void Start ()
        {
            cond=ClientCondition.Connecting;
            lastHeader=new NativeArray<Header>(1,Allocator.Persistent);
            lastSnapShot=new NativeArray<SnapShot>(1,Allocator.Persistent);

            connected=new NativeArray<bool>(1,Allocator.Persistent);
            connected[0]=false;

            NetBufferClient.start();
            // var settings = new NetworkSettings(); 
            // settings.WithNetworkConfigParameters();
            // settings.WithSecureClientParameters()
            m_Driver = NetworkDriver.Create();

            m_Connection = new NativeArray<NetworkConnection>(1, Allocator.Persistent);

            endpoint = NetworkEndPoint.LoopbackIpv4;//matchmaker should set this

            // NetworkEndPoint.TryParse("192.168.1.1",9000,out endpoint,NetworkFamily.Ipv4);
            endpoint.Port = 9000;


            // m_Connection[0] = m_Driver.Connect(endpoint);

        }
        void OnDestroy()
        {
            ClientJobHandle.Complete();
            NetBufferClient.exit();
            // Make sure we run our jobs to completion before exiting.
            if(m_Driver.IsCreated)
            {
                m_Connection.Dispose();
                m_Driver.Dispose();
                lastHeader.Dispose();
                lastSnapShot.Dispose();
                connected.Dispose();
            }
        }

        void setupForGameLoop()
        {
            NetBufferClient.tick=0;
        }
        // Update is called once per frame
        void Update()
        {
            var job = new ClientUpdateJob
            {
                driver = m_Driver,
                connection = m_Connection,
                lastHeader=NetBufferClient.lastHeader,
                lastSnapShot=NetBufferClient.lastSnapShot,
                lastSnapShotData=NetBufferClient.snapShotData,
                doneProcessing=NetBufferClient.doneProcessingLast,
                connected=connected,
            };
                //call send input also
            ClientJobHandle = m_Driver.ScheduleUpdate(ClientJobHandle);
            ClientJobHandle = job.Schedule(ClientJobHandle);
            ClientJobHandle.Complete();
            if(cond==ClientCondition.Connecting)
            {
                if(!m_Connection[0].IsCreated)
                    m_Connection[0]=m_Driver.Connect(endpoint);
                if(connected[0])
                {
                    setupForGameLoop();
                    cond=ClientCondition.GameLoop;//we should go to Auth and Connected but for now we stright go to the gameloop
                    Debug.Log("going to gameloop");
                }
            }
            else if(cond==ClientCondition.GameLoop)
            {
                
            }
            else if(cond==ClientCondition.Wait)
            {

            }
            
        }
        
    }
}
