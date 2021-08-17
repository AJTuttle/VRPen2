using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPen;
using Debug = VRPen.Debug;

public class test : MonoBehaviour {
    public  SharedMarker marker;
    
    private void Update() {
        if (Input.GetKeyDown(KeyCode.T)) marker.takeOwnership();
        if (Input.GetKeyDown(KeyCode.R)) marker.relinquishOwnership();
    }

    private void OnTriggerEnter(Collider other) {
        
        Debug.Log(gameObject.name + " - enter - " + other.gameObject.name);
        
    }
    
}
