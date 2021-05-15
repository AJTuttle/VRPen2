using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRPen {

    public class VectorGraphic {

        //id vars
        public ulong ownerId; //the user that added
        public int localIndex; //index of the owner's graphics
        
        //vars
        public Mesh mesh;
        public MeshRenderer mr;
        public GameObject obj;
        public byte canvasId;
        
    }
}