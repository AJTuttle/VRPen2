using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class InputDevice {

        public enum InputDeviceType : byte {
            Marker,
            Tablet,
            Mouse,
			Facilitative
		}

        public InputDeviceType type;
		
        public NetworkedPlayer owner;
        public byte deviceIndex;
        public VectorGraphic currentGraphic;
        public Vector3 lastDrawPoint;
        public Vector3 secondLastDrawPoint;
        public float lastPressure;
        public bool flipVerts = false; //this is for when the line forms a cusp (90+ degree turn). The verts need to be flipped to avoid intersection
        public InputVisuals visuals;

        public Vector3 velocity {
            get {
                if (currentGraphic != null && currentGraphic is VectorLine && ((VectorLine)currentGraphic).pointCount < 2) {
                    return Vector3.zero;
                }
                else {
                    return lastDrawPoint - secondLastDrawPoint;
                }
            }
        }

    }

}
