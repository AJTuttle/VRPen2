using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VelNet;
using VRPen;
using Debug = UnityEngine.Debug;

public class VelNetInterface : NetworkInterface
{
    
    protected override void sendPacketToAll(PacketCategory category, byte[] packet) {
        
        //offline
        if (VRPen.VectorDrawing.OfflineMode) return;

        //add cat
        byte[] packetWithCategory = new byte[packet.Length + 1];
        packetWithCategory[0] = (byte)category;
        for (int x = 0; x < packet.Length; x++) {
            packetWithCategory[x + 1] = packet[x];
        }
        
        //send
        VelNetManager.SendCustomMessage(packetWithCategory, true, true, false); 
    }

    protected override void sendPacketToIndividual(PacketCategory category, byte[] packet, ulong receiverID) {
        
        //offline
        if (VRPen.VectorDrawing.OfflineMode) return;

        //add cat
        byte[] packetWithCategory = new byte[packet.Length + 1];
        packetWithCategory[0] = (byte)category;
        for (int x = 0; x < packet.Length; x++) {
            packetWithCategory[x + 1] = packet[x];
        }
        
        //setup velnet group to send to if it doesnt already exist
        int receiverIDSingle = (int)receiverID;
        if (!VelNetManager.instance.groups.ContainsKey(receiverIDSingle.ToString())) {
            List<int> group = new List<int>{ receiverIDSingle }; 
            VelNetManager.SetupMessageGroup(receiverIDSingle.ToString(), group);
            Debug.Log("Setting up VelNet group with ID: " + receiverIDSingle);
        }
        
        //send
        VelNetManager.SendCustomMessageToGroup(receiverIDSingle.ToString(), packetWithCategory, true);
    }

}
