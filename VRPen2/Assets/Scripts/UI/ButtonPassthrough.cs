using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class ButtonPassthrough : MonoBehaviour {

        public Display display;

        [System.NonSerialized]
        public VRPenInput clickedBy;

        public void passThrough(int id) {

            if(clickedBy == null) {
                Debug.LogError("No input device detected");
                return;
            }
            switch (id) {
                case 0:
                    display.markerPassthrough(clickedBy);
                    break;
                case 1:
                    display.eyedropperPassthrough(clickedBy);
                    break;
                case 2:
                    display.erasePassthrough(clickedBy);
                    break;
                default:
                    Debug.LogError("Undetected case");
                    break;
            }

        }

    }
}