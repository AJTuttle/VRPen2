using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {


    /// <summary>
    /// This is attached to buttons that change input device stuff. The purpose of this script is to allow the UI interface to know WHICH inputdevice clicked it.
    /// </summary>
    public class ButtonPassthrough : MonoBehaviour {

        public UIManager UI;

        [System.NonSerialized]
        public VRPenInput clickedBy;

        //used only for the stamp buttons in the stamp explorer
        [System.NonSerialized]
        public int stampIndex;

        private void Start() {
        }

        public void passThrough(int id) {

            if(clickedBy == null) {
                Debug.LogError("No input device detected");
                return;
            }
            switch (id) {
                case 0:
                    UI.markerPassthrough(clickedBy);
                    break;
                case 1:
                    UI.eyedropperPassthrough(clickedBy);
                    break;
                case 2:
                    UI.erasePassthrough(clickedBy);
                    break;
                case 3:
                    UI.stampPassthrough(clickedBy, stampIndex);
                    break;
                default:
                    Debug.LogError("Undetected case");
                    break;
            }

        }

    }
}