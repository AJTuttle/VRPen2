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
        public Vector3[] normals;    //array so we only need to copy every x items (if we increase the size by x each time)
        public Vector3[] vertices;   //array so we only need to copy every x items (if we increase the size by x each time)
        public List<int> indices;    //list since we cant have empty values like we can for normals or verts


    }
}