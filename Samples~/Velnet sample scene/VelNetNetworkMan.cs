using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VelNet;
using VRPen;
using Debug = UnityEngine.Debug;

public class VelNetNetworkMan : MonoBehaviour {
    
    public NetworkInterface networkInterface;
    public List<VRPenInput> localInputDevicesToSetLocalID;
    public string roomToJoin;
    
    void Start() {
        setupCallbacks();
    }

    void setupCallbacks() {
        
        VelNetManager.OnConnectedToServer += () => {
            Debug.Log("Connected to VelNet");
            login();
        };
        
        VelNetManager.OnLoggedIn += () => {
            Debug.Log("Logged into VelNet");
            joinRoom();
        };
        
        VelNetManager.OnPlayerJoined += player => {
            Debug.Log("Other VelNet player joined: " + player.userid);
        };
        
        VelNetManager.OnJoinedRoom += roomName => {
            Debug.Log("VelNet room joined: " + roomName);
            VelNetManager.GetRoomData(roomName);
        };
        
        VelNetManager.RoomDataReceived += roomData => {
            Debug.Log("VelNet room data received: " + roomData.members.Count + " users");
            joinVRPen(roomData);
        };
        
        VelNetManager.CustomMessageReceived += (senderId, dataWithCategory) => {
            customPacketReceived(senderId, dataWithCategory);
        };
        
    }

    void login() {
        VelNetManager.Login("user_name", "vrpen");
    }
    
    void joinRoom() {
        VelNetManager.Join(roomToJoin);
    }

    void joinVRPen(VelNetManager.RoomDataMessage roomData) {
        
        //first in room?
        bool isAloneInRoom = roomData.members.Count == 1;

        //Connect to room, request cache if not first person in room
        if (isAloneInRoom) {
            networkInterface.connectedToServer((ulong)VelNetManager.LocalPlayer.userid, false);
        }
        else {
            ulong randomOtherPlayerInRoom = 0;
            foreach (Tuple<int, string> player in roomData.members) {
                if (player.Item1 != VelNetManager.LocalPlayer.userid) {
                    randomOtherPlayerInRoom = (ulong)player.Item1;
                    break;
                }
            }
            Debug.Log("Requesting VRPen cache from user ID: " + randomOtherPlayerInRoom);
            networkInterface.connectedToServer((ulong)VelNetManager.LocalPlayer.userid, true, randomOtherPlayerInRoom);
        }
		
        //set id on chosen inputs
        foreach (var device in localInputDevicesToSetLocalID) {
            device.ownerID = (ulong)VelNetManager.LocalPlayer.userid;
        }
        
    }

    void customPacketReceived(int senderId, byte[] dataWithCategory) {
        
        //vrpen packet
        if (VRPen.VectorDrawing.OfflineMode) return;
        
        //get data
        NetworkInterface.PacketCategory cat = (NetworkInterface.PacketCategory)dataWithCategory[0];
        byte[] data = new byte[dataWithCategory.Length - 1];
        for (int x = 0; x < data.Length; x++) {
            data[x] = dataWithCategory[x + 1];
        }
        ulong id = (ulong)senderId;
            

        Debug.Log("message received - " + cat);

        //pipe the data
        networkInterface.receivePacket(cat, data, id);
        
    }

}
