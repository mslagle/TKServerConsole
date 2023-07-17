using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace TKServerConsole
{
    public class TKPlayer
    {
        public string name;
        public int hat;
        public int color;
        public int soapbox;
        public NetConnection connection;
    }

    public static class TKPlayerManager
    {
        public static Dictionary<NetConnection, TKPlayer> players = new Dictionary<NetConnection, TKPlayer>();

        public static void PlayerLogIn(TKPlayer player)
        {
            if(players.ContainsKey(player.connection))
            {
                Program.Log("Player trying to log in is already logged in! Returning.");
                return;
            }

            players.Add(player.connection, player);

            Program.Log($"{player.name} joined the game!");
        }      
        
        public static void PlayerLogOut(NetConnection connection)
        {
            if (!players.ContainsKey(connection))
            {
                Program.Log("Player trying to log out is not registered! Returning.");
                return;
            }

            string playerName = players[connection].name;
            players.Remove(connection);

            Program.Log($"{playerName} left the game!");
        }

        public static void SendMessageToSinglePlayer(NetOutgoingMessage outgoingMessage, NetConnection connection)
        {
            TKServer.server.SendMessage(outgoingMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        public static void SendMessageToAllPlayers(NetOutgoingMessage outgoingMessage)
        {
            List<NetConnection> connections = players.Keys.ToList();

            if (connections.Count <= 0)
            {
                return;
            }

            TKServer.server.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, 0);
        }    

        public static void SendMessageToAllPlayersExceptProvided(NetOutgoingMessage outgoingMessage, NetConnection excludedConnection)
        {
            List<NetConnection> connections = players.Keys.Where(connection => connection != excludedConnection).ToList();

            if(connections.Count <= 0)
            {
                return;
            }

            TKServer.server.SendMessage(outgoingMessage, connections, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
