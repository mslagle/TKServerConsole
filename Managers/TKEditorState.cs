using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.Extensions.Logging;
using TKServerConsole.Configuration;
using TKServerConsole.Models;
using TKServerConsole.Repositories;
using TKServerConsole.Utils;

namespace TKServerConsole.Managers
{
    public class TKEditorState
    {
        public int FloorID { get; set; }
        public int SkyboxID { get; set; }
        public Dictionary<string, TKBlock> Blocks { get; set; }

        private ILogger<TKEditorState> logger { get; set; }
        private TeamkistServer server { get; set; }

        public TKEditorState(ILogger<TKEditorState> logger, TeamkistServer server)
        {
            this.logger = logger;
            this.server = server;

            FloorID = 90;
            SkyboxID = 0;
            Blocks = new Dictionary<string, TKBlock>();

            server.EditorLevelChanged_BlockCreated += BlockCreated;
            server.EditorLevelChanged_BlockChanged += BlockChanged;
            server.EditorLevelChanged_BlockDestroyed += BlockDestroyed;
            server.EditorLevelChanged_FloorChanged += FloorUpdated;
            server.EditorLevelChanged_SkyboxChanged += SkyboxUpdated;
        }

        public NetOutgoingMessage GenerateServerDataMessage()
        {
            NetOutgoingMessage outgoingMessage = server.server.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.ServerData);
            outgoingMessage.Write(FloorID);
            outgoingMessage.Write(SkyboxID);
            outgoingMessage.Write(Blocks.Count);
            foreach (KeyValuePair<string, TKBlock> tkblock in Blocks)
            {
                outgoingMessage.Write(TKUtilities.GetJSONString(tkblock.Value));
            }
            return outgoingMessage;
        }

        public void BlockCreated(object? sender, EditorLevelChanged_BlockCreatedArgs e)
        {
            TKBlock tkBlock = TKUtilities.JSONToTKBlock(e.blockJson);
            if (!Blocks.ContainsKey(tkBlock.UID))
            {
                Blocks.Add(tkBlock.UID, tkBlock);
                logger.LogDebug($"Added block {tkBlock.blockID} with UID {tkBlock.UID} to level state");
            }
            else
            {
                logger.LogWarning($"Can't add block because UID already exists. UID: {tkBlock.UID}");
            }
        }

        public void BlockDestroyed(object? sender, EditorLevelChanged_BlockDestroyedArgs e)
        {
            if (Blocks.ContainsKey(e.uid))
            {
                Blocks.Remove(e.uid);
                logger.LogDebug($"Removed block with UID {e.uid} from level state");
            }
            else
            {
                logger.LogWarning($"Can't remove block because UID does not exist. UID: {e.uid}");
            }
        }

        public void BlockChanged(object? sender, EditorLevelChanged_BlockChangedArgs e)
        {
            if (Blocks.ContainsKey(e.uid))
            {
                Blocks[e.uid].AssignProperties(e.properties);
                logger.LogDebug($"Updated block with UID {e.uid} with properties {e.properties}");
            }
            else
            {
                logger.LogWarning($"Can't update block because UID does not exist. UID: {e.uid}");
            }
        }

        public void FloorUpdated(object? sender, EditorLevelChanged_FloorChangedArgs e)
        {
            FloorID = e.floor;
        }

        public void SkyboxUpdated(object? sender, EditorLevelChanged_SkyboxChangedArgs e)
        {
            SkyboxID = e.skybox;
        }
    }
}
