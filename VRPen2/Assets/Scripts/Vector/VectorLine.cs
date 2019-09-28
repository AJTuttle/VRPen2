using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class VectorLine {

        //vars
        public ulong owner;
        public byte deviceIndex;
        public int index;
        public Mesh mesh;
        public MeshRenderer mr;
        public GameObject obj;
        public byte canvasId;

        //drawpoints
        public int pointCount;
        public Vector3[] normals;
        public Vector3[] vertices;
        public List<int> indices;

  
    }
}