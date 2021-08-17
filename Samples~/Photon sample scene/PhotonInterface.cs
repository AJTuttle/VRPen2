using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPen;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class PhotonInterface : NetworkInterface {
	

	protected override void sendPacketToAll(PacketCategory category, byte[] packet) {
		
		//offline
		if (VRPen.VectorDrawing.OfflineMode) return;
		
		//send to all
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions {
			Receivers = ReceiverGroup.All
		};

		SendOptions sendOptions = new SendOptions {
			Reliability = true,

		};
		PhotonNetwork.RaiseEvent((byte)category, packet, raiseEventOptions, sendOptions);

	}

	protected override void sendPacketToIndividual(PacketCategory category, byte[] packet, ulong receiverID) {
		//offline
		if (VRPen.VectorDrawing.OfflineMode) return;

		//pass
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions {
			TargetActors = new int[] { (int)receiverID }
		};

		SendOptions sendOptions = new SendOptions {
			Reliability = true,
		};
		PhotonNetwork.RaiseEvent((byte)category, packet, raiseEventOptions, sendOptions);

	}
}
