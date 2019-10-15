using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class NetworkedPlayer{
       
        public ulong connectionId;

        //input devices
        public Dictionary<byte, InputDevice> inputDevices;
		public byte fascilitativeDeviceIndex; //also included in the dictionary
        
        //used for cursor
        public GameObject cursor;


        //vector drawing
        public List<VectorGraphic> graphics = new List<VectorGraphic>();
        public int graphicIndexer = 0;
        



    }

}
