using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPen;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class PhotonImplementation : MonoBehaviourPunCallbacks, IOnEventCallback {

	//network interface for vrpen
	public PhotonInterface networkInterface;

	//photon overrides
	[System.NonSerialized]
	static public string photonServerOverride = null;
	[System.NonSerialized]
	static public string photonRoomOverride = null;
	
	

	private void Start() {
		
		//dont connect if offline mode
		if (VRPen.VectorDrawing.OfflineMode) return;

		//connect
		string server = (photonServerOverride != null) ? photonServerOverride : PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
		PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = server;
		UnityEngine.Debug.Log("Attempting to connect to appid: " + server);
		bool connected = PhotonNetwork.ConnectUsingSettings();

	}

	public override void OnConnectedToMaster() {

		//connected to master server
		UnityEngine.Debug.Log("Connected to Master");

		//join room
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.IsVisible = false;
		roomOptions.MaxPlayers = 10;
		string room = (photonRoomOverride != null) ? photonRoomOverride : "default";
		UnityEngine.Debug.Log("Attempting to connect to room: " + room);
		bool connectedToRoom = PhotonNetwork.JoinOrCreateRoom(room, roomOptions, TypedLobby.Default);


	}

	public override void OnJoinedRoom() {

		//connected to room
		UnityEngine.Debug.Log("Joined Room");

		//first in room?
		bool isAloneInRoom = PhotonNetwork.CurrentRoom.Players.Count == 1;

		//Connect to room, request cache if not first person in room
		if (isAloneInRoom) {
			networkInterface.connectedToServer((ulong)PhotonNetwork.LocalPlayer.ActorNumber, false);
		}
		else {
			Player randomOtherPlayerInRoom = null;
			foreach (KeyValuePair<int, Player> player in PhotonNetwork.CurrentRoom.Players) {
				if (player.Value.ActorNumber != PhotonNetwork.LocalPlayer.ActorNumber) {
					randomOtherPlayerInRoom = player.Value;
					break;
				}
			}
			networkInterface.connectedToServer((ulong)PhotonNetwork.LocalPlayer.ActorNumber, true, (ulong)randomOtherPlayerInRoom.ActorNumber);
		}
	}

	public void OnEvent(EventData photonEvent) {

		//return if in offline mode
		if (VRPen.VectorDrawing.OfflineMode) return;

		//packet data
		NetworkInterface.PacketCategory packetCategory = (NetworkInterface.PacketCategory)photonEvent.Code;
		ulong id = (ulong)photonEvent.Sender;

		//pipe the data
		networkInterface.receivePacket(packetCategory, (byte[])photonEvent.CustomData, id);

	}
}
