using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.Extensions.Logging;
using TKServerConsole.Models;
using TKServerConsole.Repositories;
using TKServerConsole.Utils;

namespace TKServerConsole.Managers
{
    public class TKPlayerManager
    {
        public ILogger logger { get; private set; }
        public TeamkistServer server { get; private set; }
        public TKEditorState state { get; private set; }

        public Dictionary<NetConnection, TKPlayer> Players { get; set; }
        public int ConnectionIDCounter { get; set; }

        public TKPlayerManager(ILogger<TKPlayerManager> logger, TeamkistServer server, TKEditorState state)
        {
            this.logger = logger;
            this.server = server;
            this.state = state;

            this.Players = new Dictionary<NetConnection, TKPlayer>();
            this.ConnectionIDCounter = 100;

            this.server.PlayerLevelChanged += Server_PlayerLevelChanged;
            this.server.PlayerLogIn += Server_PlayerLogIn;
            this.server.PlayerLogOut += Server_PlayerLogOut;
            this.server.PlayerTransform += Server_PlayerTransform;
            this.server.PlayerState += Server_PlayerState;
        }

        private void Server_PlayerState(object? sender, PlayerStateArgs e)
        {
            if (!Players.ContainsKey(e.playerConnection))
            {
                logger.LogWarning("Can't process transform data as player connection is not known!");
                return;
            }

            int playerID = Players[e.playerConnection].ID;

            if (Players.Count <= 1)
            {
                //Theres only 1 player, no need to send it.
                return;
            }

            NetOutgoingMessage outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerStateData);
            outgoingMessage.Write(playerID);
            outgoingMessage.Write(e.state);
            SendMessageToAllPlayersExceptProvided(outgoingMessage, e.playerConnection);
        }

        private void Server_PlayerTransform(object? sender, PlayerTransformArgs e)
        {
            if (!Players.ContainsKey(e.playerConnection))
            {
                logger.LogWarning("Can't process transform data as player connection is not known!");
                return;
            }

            int playerID = Players[e.playerConnection].ID;

            if (Players.Count <= 1)
            {
                //Theres only 1 player, no need to send it.
                return;
            }

            NetOutgoingMessage outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerTransformData);
            outgoingMessage.Write(playerID);
            outgoingMessage.Write(e.position.x);
            outgoingMessage.Write(e.position.y);
            outgoingMessage.Write(e.position.z);
            outgoingMessage.Write(e.euler.x);
            outgoingMessage.Write(e.euler.y);
            outgoingMessage.Write(e.euler.z);
            outgoingMessage.Write(e.state);
            SendMessageToAllPlayersExceptProvided(outgoingMessage, e.playerConnection);
        }

        private void Server_PlayerLogOut(object? sender, PlayerLogOutArgs e)
        {
            if (!Players.ContainsKey(e.playerConnection))
            {
                logger.LogWarning("Player trying to log out is not registered! Returning.");
                return;
            }

            TKPlayer p = Players[e.playerConnection];
            Players.Remove(e.playerConnection);

            logger.LogInformation($"{p.name} left the game!");

            //To all the other Players, send a message with the ID of the player that left.
            NetOutgoingMessage playerLeftMessage = CreatePlayerLeftMessage(p.ID);
            SendMessageToAllPlayers(playerLeftMessage);
        }

        private void Server_PlayerLogIn(object? sender, PlayerLogInArgs e)
        {
            var player = e.player;

            if (Players.ContainsKey(player.connection))
            {
                logger.LogWarning($"Player {player.name} is already logged in, ignoring duplicate login event!");
                return;
            }

            //Create a new ID for the player and add it to the dictionary.
            ConnectionIDCounter++;
            player.ID = ConnectionIDCounter;
            Players.Add(player.connection, player);

            logger.LogInformation($"{player.name} joined the game!");

            // If requesting current state, send it now!
            if (e.requestingServerData)
            {
                logger.LogDebug("New connected player is requesting current state, sending now...");
                var serverDataMessage = state.GenerateServerDataMessage();
                SendMessageToSinglePlayer(serverDataMessage, player.connection);
            }

            //If there is only 1 player right now, we don't need to send any messages.
            if (Players.Count == 1)
            {
                return;
            }

            //To the connecting player, send a message with all the information about the other Players that are already online.
            NetOutgoingMessage serverPlayerDataMessage = CreateServerPlayerDataMessage(player.connection);

            if (serverPlayerDataMessage != null)
            {
                //Send the message.
                SendMessageToSinglePlayer(serverPlayerDataMessage, player.connection);
            }

            //To all the other Players, send a message with the information about the current connecting player.
            NetOutgoingMessage joinedPlayerDataMessage = CreateJoinedPlayerDataMessage(player.connection);
            SendMessageToAllPlayersExceptProvided(joinedPlayerDataMessage, player.connection);
        }

        private void Server_PlayerLevelChanged(object? sender, PlayerLevelChangedArgs e)
        {
            SendMessageToAllPlayersExceptProvided(e.message, e.playerConnection);
        }


        public NetOutgoingMessage CreateServerPlayerDataMessage(NetConnection exclude)
        {
            List<NetConnection> connections = Players.Keys.Where(connection => connection != exclude).ToList();

            if (connections.Count == 0)
            {
                //Shouldn't happen but just in case.
                return null;
            }

            NetOutgoingMessage outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.ServerPlayerData);
            outgoingMessage.Write(connections.Count);

            //Foreach connections write the data in the message.
            foreach (NetConnection connection in connections)
            {
                TKPlayer p = Players[connection];
                outgoingMessage.Write(p.ID);
                outgoingMessage.Write(p.state);
                outgoingMessage.Write(p.name);
                outgoingMessage.Write(p.hat);
                outgoingMessage.Write(p.color);
                outgoingMessage.Write(p.soapbox);
            }

            return outgoingMessage;
        }

        public NetOutgoingMessage CreateJoinedPlayerDataMessage(NetConnection joinedConnection)
        {
            TKPlayer p = Players[joinedConnection];
            NetOutgoingMessage outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.JoinedPlayerData);
            outgoingMessage.Write(p.ID);
            outgoingMessage.Write(p.state);
            outgoingMessage.Write(p.name);
            outgoingMessage.Write(p.hat);
            outgoingMessage.Write(p.color);
            outgoingMessage.Write(p.soapbox);

            return outgoingMessage;
        }

        public NetOutgoingMessage CreatePlayerLeftMessage(int leavingID)
        {
            NetOutgoingMessage outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerLeft);
            outgoingMessage.Write(leavingID);
            return outgoingMessage;
        }

        public void SendMessageToSinglePlayer(NetOutgoingMessage outgoingMessage, NetConnection connection)
        {
            server.SendMessage(outgoingMessage, connection);
        }

        public void SendMessageToAllPlayers(NetOutgoingMessage outgoingMessage)
        {
            List<NetConnection> connections = Players.Keys.ToList();

            if (connections.Count <= 0)
            {
                return;
            }

            server.SendMessage(outgoingMessage, connections);
        }

        public void SendMessageToAllPlayersExceptProvided(NetOutgoingMessage outgoingMessage, NetConnection excludedConnection)
        {
            List<NetConnection> connections = Players.Keys.Where(connection => connection != excludedConnection).ToList();

            if (connections.Count <= 0)
            {
                return;
            }

            server.SendMessage(outgoingMessage, connections);
        }
    }
}
