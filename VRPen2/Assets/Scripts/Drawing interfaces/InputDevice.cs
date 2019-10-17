using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class InputDevice : MonoBehaviour {

        public enum InputDeviceType : byte {
            Marker,
            Tablet,
            Mouse,
			Facilitative
		}

        public InputDeviceType type;

        [System.NonSerialized]
        public NetworkedPlayer owner;
        [System.NonSerialized]
        public byte deviceIndex;
        [System.NonSerialized]
        public VectorGraphic currentGraphic;
        [System.NonSerialized]
        public Vector3 lastDrawPoint;
        [System.NonSerialized]
        public Vector3 secondLastDrawPoint;

    }

}
