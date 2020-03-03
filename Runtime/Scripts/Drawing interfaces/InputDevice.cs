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
