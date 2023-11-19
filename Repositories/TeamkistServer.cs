using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.Extensions.Logging;
using TKServerConsole.Configuration;
using TKServerConsole.Managers;
using TKServerConsole.Models;
using TKServerConsole.Utils;

namespace TKServerConsole.Repositories
{
    public class TeamkistServer
    {
        public NetPeerConfiguration config { get; set; }
        public NetServer server { get; set; }

        public ILogger<TeamkistManager> logger { get; set; }
        public TeamkistConfiguration configuration { get; set; }

        // Eventing
        public event EventHandler<PlayerLogInArgs> PlayerLogIn;
        public event EventHandler<PlayerLogOutArgs> PlayerLogOut;

        public event EventHandler<PlayerTransformArgs> PlayerTransform;
        public event EventHandler<PlayerStateArgs> PlayerState;
        public event EventHandler<PlayerLevelChangedArgs> PlayerLevelChanged;

        public event EventHandler<EditorLevelChanged_BlockCreatedArgs> EditorLevelChanged_BlockCreated;
        public event EventHandler<EditorLevelChanged_BlockDestroyedArgs> EditorLevelChanged_BlockDestroyed;
        public event EventHandler<EditorLevelChanged_BlockChangedArgs> EditorLevelChanged_BlockChanged;

        public event EventHandler<EditorLevelChanged_FloorChangedArgs> EditorLevelChanged_FloorChanged;
        public event EventHandler<EditorLevelChanged_SkyboxChangedArgs> EditorLevelChanged_SkyboxChanged;

        public TeamkistServer(ILogger<TeamkistManager> logger, TeamkistConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;


            config = new NetPeerConfiguration("Teamkist");
            config.Port = configuration.Options.SERVER_PORT;
            config.LocalAddress = IPAddress.Parse(configuration.Options.SERVER_IP);

            try
            {
                server = new NetServer(config);
                server.Start();
            }
            catch (Exception e)
            {
                logger.LogError(e, "A problem occured when setting up the server. Please check configuration.");
                throw;
            }

            logger.LogInformation($"Started server on {configuration.Options.SERVER_IP}:{configuration.Options.SERVER_PORT}");
        }

        public void Shutdown()
        {
            server.Shutdown(NetReason.Empty);
        }

        public NetOutgoingMessage CreateMessage()
        {
            return server.CreateMessage();
        }

        public void Run()
        {
            NetIncomingMessage incomingMessage;
            while ((incomingMessage = server.ReadMessage()) != null)
            {
                //Get the connection of the player who send the message.
                NetConnection senderConnection = incomingMessage.SenderConnection;

                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        switch (senderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                //OnPlayerConnected(senderConnection);
                                break;
                            case NetConnectionStatus.Disconnected:
                                PlayerLogOut?.Invoke(this, new PlayerLogOutArgs(senderConnection));
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        TKMessageType messageType = (TKMessageType)incomingMessage.ReadByte();

                        switch (messageType)
                        {
                            case TKMessageType.LogIn:

                                TKPlayer player = new TKPlayer()
                                {
                                    name = incomingMessage.ReadString(),
                                    hat = incomingMessage.ReadInt32(),
                                    color = incomingMessage.ReadInt32(),
                                    soapbox = incomingMessage.ReadInt32(),
                                    connection = senderConnection,
                                    state = 0
                                };
                                bool requestServerData = incomingMessage.ReadBoolean();

                                PlayerLogIn?.Invoke(this, new PlayerLogInArgs(player, requestServerData));
                                break;
                            case TKMessageType.ServerData:
                                break;
                            case TKMessageType.PlayerTransformData:
                                Vector3 position = new Vector3();
                                position.x = incomingMessage.ReadFloat();
                                position.y = incomingMessage.ReadFloat();
                                position.z = incomingMessage.ReadFloat();
                                Vector3 euler = new Vector3();
                                euler.x = incomingMessage.ReadFloat();
                                euler.y = incomingMessage.ReadFloat();
                                euler.z = incomingMessage.ReadFloat();
                                byte pstate = incomingMessage.ReadByte();

                                PlayerTransform?.Invoke(this, new PlayerTransformArgs(senderConnection, position, euler, pstate));
                                break;
                            case TKMessageType.PlayerStateData:
                                byte state = incomingMessage.ReadByte();

                                PlayerState?.Invoke(this, new PlayerStateArgs(senderConnection, state));
                                break;
                            case TKMessageType.LevelEditorChangeEvents:

                                //Create a new message to send to other players so they receive the updates as well.
                                NetOutgoingMessage outgoingMessage = server.CreateMessage();
                                outgoingMessage.Write((byte)TKMessageType.LevelEditorChangeEvents);

                                int changeCount = incomingMessage.ReadInt32();
                                outgoingMessage.Write(changeCount);

                                string blockJSON;
                                string UID;
                                string properties;
                                int floor;
                                int skybox;

                                for (int i = 0; i < changeCount; i++)
                                {
                                    TKMessageType changeEventType = (TKMessageType)incomingMessage.ReadByte();
                                    outgoingMessage.Write((byte)changeEventType);

                                    switch (changeEventType)
                                    {
                                        case TKMessageType.BlockCreateEvent:
                                            blockJSON = incomingMessage.ReadString();
                                            EditorLevelChanged_BlockCreated?.Invoke(this, new EditorLevelChanged_BlockCreatedArgs(blockJSON));

                                            outgoingMessage.Write(blockJSON);
                                            break;
                                        case TKMessageType.BlockDestroyEvent:
                                            UID = incomingMessage.ReadString();
                                            EditorLevelChanged_BlockDestroyed?.Invoke(this, new EditorLevelChanged_BlockDestroyedArgs(UID));

                                            outgoingMessage.Write(UID);
                                            break;
                                        case TKMessageType.BlockChangeEvent:
                                            UID = incomingMessage.ReadString();
                                            properties = incomingMessage.ReadString();
                                            EditorLevelChanged_BlockChanged?.Invoke(this, new EditorLevelChanged_BlockChangedArgs(UID, properties));

                                            outgoingMessage.Write(UID);
                                            outgoingMessage.Write(properties);
                                            break;
                                        case TKMessageType.EditorFloorEvent:
                                            floor = incomingMessage.ReadInt32();
                                            EditorLevelChanged_FloorChanged?.Invoke(this, new EditorLevelChanged_FloorChangedArgs(floor));

                                            outgoingMessage.Write(floor);
                                            break;
                                        case TKMessageType.EditorSkyboxEvent:
                                            skybox = incomingMessage.ReadInt32();
                                            EditorLevelChanged_SkyboxChanged?.Invoke(this, new EditorLevelChanged_SkyboxChangedArgs(skybox));

                                            outgoingMessage.Write(skybox);
                                            break;
                                    }
                                }

                                PlayerLevelChanged?.Invoke(this, new PlayerLevelChangedArgs(outgoingMessage, senderConnection));
                                break;
                        }
                        break;
                }
            }
        }

        public void SendMessage(NetOutgoingMessage outgoingMessage, NetConnection connection)
        {
            server.SendMessage(outgoingMessage, connection, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendMessage(NetOutgoingMessage outgoingMessage, List<NetConnection> connections)
        {
            server.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }

    public class PlayerLogInArgs : EventArgs
    {
        public TKPlayer player { get; private set; }
        public bool requestingServerData { get; private set; }

        public PlayerLogInArgs(TKPlayer player, bool requestingServerData)
        {
            this.player = player;
            this.requestingServerData = requestingServerData;
        }
    }

    public class PlayerLogOutArgs : EventArgs
    {
        public NetConnection playerConnection { get; private set; }

        public PlayerLogOutArgs(NetConnection playerConnection)
        {
            this.playerConnection = playerConnection;
        }
    }

    public class PlayerTransformArgs : EventArgs
    {
        public NetConnection playerConnection { get; private set; }
        public Vector3 position { get; private set; }
        public Vector3 euler { get; private set; }
        public byte state { get; private set; }

        public PlayerTransformArgs(NetConnection playerConnection, Vector3 position, Vector3 euler, byte state)
        {
            this.playerConnection = playerConnection;
            this.position = position;
            this.euler = euler;
            this.state = state;
        }
    }

    public class PlayerStateArgs : EventArgs
    {
        public NetConnection playerConnection { get; private set; }
        public byte state { get; private set; }

        public PlayerStateArgs(NetConnection playerConnection, byte state)
        {
            this.playerConnection = playerConnection;
            this.state = state;
        }
    }

    public class PlayerLevelChangedArgs : EventArgs
    {
        public NetOutgoingMessage message { get; private set; }
        public NetConnection playerConnection { get; private set; }

        public PlayerLevelChangedArgs(NetOutgoingMessage message, NetConnection playerConnection)
        {
            this.message = message;
            this.playerConnection = playerConnection;
        }
    }

    public class EditorLevelChanged_BlockCreatedArgs : EventArgs
    {
        public string blockJson { get; set; }

        public EditorLevelChanged_BlockCreatedArgs(string blockJson)
        {
            this.blockJson = blockJson;
        }
    }

    public class EditorLevelChanged_BlockDestroyedArgs : EventArgs
    {
        public string uid { get; set; }

        public EditorLevelChanged_BlockDestroyedArgs(string uid)
        {
            this.uid = uid;
        }
    }

    public class EditorLevelChanged_BlockChangedArgs : EventArgs
    {
        public string uid { get; set; }
        public string properties { get; set; }

        public EditorLevelChanged_BlockChangedArgs(string uid, string properties)
        {
            this.uid = uid;
            this.properties = properties;
        }
    }

    public class EditorLevelChanged_FloorChangedArgs : EventArgs
    {
        public int floor { get; set; }

        public EditorLevelChanged_FloorChangedArgs(int floor)
        {
            this.floor = floor;
        }
    }

    public class EditorLevelChanged_SkyboxChangedArgs : EventArgs
    {
        public int skybox { get; set; }

        public EditorLevelChanged_SkyboxChangedArgs(int skybox)
        {
            this.skybox = skybox;
        }
    }

    public enum TKMessageType
    {
        LogIn = 10,
        JoinedPlayerData = 11,
        ServerPlayerData = 12,
        PlayerTransformData = 13,
        PlayerStateData = 14,
        PlayerLeft = 15,
        ServerData = 20,
        LevelEditorChangeEvents = 100,
        BlockCreateEvent = 101,
        BlockDestroyEvent = 102,
        BlockChangeEvent = 103,
        EditorFloorEvent = 104,
        EditorSkyboxEvent = 105
    }
}
