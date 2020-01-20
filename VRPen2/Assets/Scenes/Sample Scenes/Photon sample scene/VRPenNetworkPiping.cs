using ExitGames.Client.Photon;
using Photon.Pun;
using Photon;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRPenNetworkPiping : MonoBehaviour {

	//scripts
	VRPen.NetworkManager pen;

	//timer
	const float PACKET_SEND_TIMER = 0.1f;
    

	void Start() {

		//get scripts
		pen = FindObjectOfType<VRPen.NetworkManager>();

		//listen for events
		pen.vrpenEvent += eventListener;

		//start send timer
		InvokeRepeating("sendPacket", 1.0f, PACKET_SEND_TIMER);

	}



	void sendPacket() {

		//get packet
		byte[] vrpenPacket = pen.packPenData();


        //check if there was data to send
        if (vrpenPacket == null) return;

        //make full packet with id (have to do this since photon global cache doesnt save the actornumbet)
        List<byte> packetList = new List<byte>();
        packetList.AddRange(BitConverter.GetBytes(PhotonNetwork.LocalPlayer.ActorNumber));
        packetList.AddRange(vrpenPacket);
        byte[] packet = packetList.ToArray();

        

		RaiseEventOptions raiseEventOptions = new RaiseEventOptions {
			CachingOption = EventCaching.AddToRoomCacheGlobal,
			Receivers = ReceiverGroup.All
        };

		SendOptions sendOptions = new SendOptions {
			Reliability = true,
		
		};
		PhotonNetwork.RaiseEvent(SamplePeer.drawPacket, packet, raiseEventOptions, sendOptions);
	}

    public void setLocalID(ulong id) {
        pen.setLocalId(id);
    }

	public void eventListener(byte[] vrpenPacket) {

        //make full packet with id (have to do this since photon global cache doesnt save the actornumbet)
        List<byte> packetList = new List<byte>();
        packetList.AddRange(BitConverter.GetBytes(PhotonNetwork.LocalPlayer.ActorNumber));
        packetList.AddRange(vrpenPacket);
        byte[] packet = packetList.ToArray();

        //pass
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions {
			CachingOption = EventCaching.AddToRoomCacheGlobal,
			Receivers = ReceiverGroup.All
		};

		SendOptions sendOptions = new SendOptions {
			Reliability = true,

		};
		PhotonNetwork.RaiseEvent(SamplePeer.drawEvent, packet, raiseEventOptions, sendOptions);
	}



	public void recievePacket(byte[] packet) {

        //get sender actornumber
        int index = 0;
        ulong actorNumber = (ulong)ReadInt(packet, ref index);

        //trimm packet
        byte[] trimmedPacket = new byte[packet.Length - 4];
        for (int x = 0; x < trimmedPacket.Length; x++) {
            trimmedPacket[x] = packet[x + 4];
        }

        Debug.Log("Packet recieved from AN = " + actorNumber);

        //move to unpacking
        pen.unpackPacket(actorNumber, trimmedPacket);

	}


    int ReadInt(byte[] buf, ref int offset) {
        int val = BitConverter.ToInt32(buf, offset);
        offset += sizeof(Int32);
        return val;
    }

}
