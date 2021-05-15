using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class NetworkedPlayer{
       
        public ulong connectionId;

        //input devices
        public Dictionary<byte, InputDevice> inputDevices; 
        
        //used for cursor
        public GameObject cursor;


        //vector drawing
        public int localGraphicIndex = 0;


    }

}
