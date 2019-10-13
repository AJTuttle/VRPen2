using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class UIGrabbable : MonoBehaviour {

        public Transform parent;

        float x;
        float y;

        public void updatePos(float x, float y) {

            Debug.Log(x + "  " + y);

            parent.transform.localPosition += new Vector3(x-this.x,y-this.y,0);
            this.x = x;
            this.y = y;
        }

        public void grab(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public void unGrab() {

        }


    }

}
