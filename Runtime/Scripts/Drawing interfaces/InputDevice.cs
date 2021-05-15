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
        public InputVisuals visuals;

        

    }

}
