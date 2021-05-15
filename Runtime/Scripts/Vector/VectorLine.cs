using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class VectorLine : VectorGraphic{

        

        //drawpoints
        public int pointCount;
        public Vector3[] normals;    //array so we only need to copy every x items (if we increase the size by x each time)
        //public Vector3[] vertices;   //array so we only need to copy every x items (if we increase the size by x each time)
        public List<int> indices;    //list since we cant have empty values like we can for normals or verts
        

        //line smoothing / compression
        public Vector3 lastDrawPoint;
        public Vector3 secondLastDrawPoint;
        public float lastPressure;
        public bool flipVerts = false; //this is for when the line forms a cusp (90+ degree turn). The verts need to be flipped to avoid intersection

        public Vector3 velocity {
            get {
                if (pointCount < 2) {
                    return Vector3.zero;
                }
                else {
                    return lastDrawPoint - secondLastDrawPoint;
                }
            }
        }

    }
}