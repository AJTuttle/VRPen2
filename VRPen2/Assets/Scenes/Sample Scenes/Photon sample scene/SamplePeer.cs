using System;
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class SamplePeer : MonoBehaviourPunCallbacks, IOnEventCallback {

    VRPenNetworkPiping pipe;

    bool connectedToRoom = false;

    public const byte drawPacket = 0;
    public const byte drawEvent = 1;

    private void Start() {

        pipe = GetComponent<VRPenNetworkPiping>();

        bool connected = PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Connected to photon: " + connected.ToString());

    }

    public override void OnConnectedToMaster() {

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = false;
        roomOptions.MaxPlayers = 4;
        connectedToRoom = PhotonNetwork.JoinOrCreateRoom("room", roomOptions, TypedLobby.Default);


    }

    public override void OnJoinedRoom() {

        Debug.Log("Joined Room: " + connectedToRoom.ToString());
        pipe.setLocalID((ulong)PhotonNetwork.LocalPlayer.ActorNumber);
        FindObjectOfType<VRPen.NetworkManager>().sendConnect();
    }

    public void OnEvent(EventData photonEvent) {

        byte eventCode = photonEvent.Code;
        ulong id = (ulong)photonEvent.Sender;

        if (eventCode == drawPacket || eventCode == drawEvent) {
            Debug.Log("event by " + id + "  - (if it is 0 then this is a catchup packet)");
            pipe.recievePacket((byte[])photonEvent.CustomData);
        }
    }


}
