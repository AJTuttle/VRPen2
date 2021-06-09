using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPen;

public class SharedMarker : MonoBehaviour {
    
    //markers
    [Header("Markers")] [Space(10)]
    public MarkerInput localMarker;
    public RemoteMarker remoteMarker;
    
    //override parameters
    [Header("Override buttons")]
    [Space(10)]
    [Tooltip("The playerID of the person who owns the device (must match the one used in the networking system)")]
    public ulong ownerID;
    [Tooltip("Any unique identifier for the device (only needs to be unique for the owner's devices)")]
    public int uniqueIdentifier;

    //shared marker specific
    bool isOwner = false;
    
    private void Awake() {
        
        //set overrides
        localMarker.ownerID = ownerID;
        localMarker.uniqueIdentifier = uniqueIdentifier;
        remoteMarker.ownerID = ownerID;
        remoteMarker.uniqueIdentifier = uniqueIdentifier;
        
    }

    public void takeOwnership() {
        
        //check if already in correct state
        if (isOwner) return;

        //object
        localMarker.gameObject.SetActive(true);
        remoteMarker.gameObject.SetActive(false);

    }

    public void relinquishOwnership() {
        
        //check if already in correct state
        if (!isOwner) return;
        
        //finish line
        
        //object
        localMarker.gameObject.SetActive(false);
        remoteMarker.gameObject.SetActive(true);
        
    }
    
}
