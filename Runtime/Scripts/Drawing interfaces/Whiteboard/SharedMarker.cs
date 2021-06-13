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
    [Header("Override values")]
    [Space(10)]
    [Tooltip("Any unique identifier for the device (only needs to be unique for the shared devices)")]
    public int uniqueIdentifier;

    //shared marker specific
    bool isOwner = false;
    
    private void Awake() {
        
        //set overrides
        localMarker.sendVisualUpdates = true;
        localMarker.ownerID = ulong.MaxValue; //reserved owner id for when there is no owner
        localMarker.uniqueIdentifier = uniqueIdentifier;
        remoteMarker.sendVisualUpdates = false;
        remoteMarker.ownerID = ulong.MaxValue; //reserved owner id for when there is no owner
        remoteMarker.uniqueIdentifier = uniqueIdentifier;
        
    }

    private void Start() {
        
        //add to input list
        VectorDrawing.s_instance.sharedDevices.Add(this);
        
        //turn off local marker (it should be on at start to give it a chance to add itself to the data structs)
        StartCoroutine(turnOffLocalAfterFrame());

    }

    IEnumerator turnOffLocalAfterFrame() {
        yield return null;
        localMarker.gameObject.SetActive(false);
    }
    

    public void takeOwnership() {
        
        //check if already in correct state
        if (isOwner) return;
        
        //set owner
        isOwner = true;

        //object
        localMarker.gameObject.SetActive(true);
        remoteMarker.gameObject.SetActive(false);

    }

    public void relinquishOwnership() {
        
        //check if already in correct state
        if (!isOwner) return;
        
        //set owner
        isOwner = false;
        
        //finish line
        localMarker.forceEndLine();
        
        //object
        localMarker.gameObject.SetActive(false);
        remoteMarker.gameObject.SetActive(true);
        
    }
    
}
