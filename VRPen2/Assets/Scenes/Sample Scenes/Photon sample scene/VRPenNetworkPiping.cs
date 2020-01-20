using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
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
		byte[] packet = pen.packPenData();

		//check if there was data to send
		if (packet == null) return;

		//pass through 
		//PhotonMan.instance?.photonView.RPC("SendArbitraryPacket", RpcTarget.Others, PhotonNetwork.LocalPlayer.ActorNumber, PhotonMan.MessageType.DrawingUpdate, packet);

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

	public void eventListener(byte[] packet) {
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



	public void recievePacket(ulong connectionId, byte[] packet) {

		//move to unpacking
		pen.unpackPacket(connectionId, packet);

	}

}
