using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class UIGrabbable : MonoBehaviour {

        public bool notNetworked;
        public Transform parent;
        public UIManager man;

        public float x;
        public float y;
        float grabX;
        float grabY;

        void Awake() {
            initializePos();
        }

        public void initializePos() {

            //set stating values
            x = parent.transform.localPosition.x;
            y = parent.transform.localPosition.y;
        }

        private void Update() {
            
            //lerp to the networked pos if its not already pretty much on it
            if (Vector3.Distance(parent.transform.localPosition, new Vector3(x, y, 0)) > 0.1f) {
                parent.transform.localPosition = Vector3.Lerp(parent.transform.localPosition, new Vector3(x, y, 0), 0.12f);
            }
        }

        public void updatePosRelativeToGrab(float x, float y) {
            
            parent.transform.localPosition += new Vector3(x- grabX, y- grabY, 0);
            grabX = x;
            grabY = y;
            this.x = parent.transform.localPosition.x;
            this.y = parent.transform.localPosition.y;

            //network
           if (!notNetworked) man.queueState();

        }

        public void setExactPos(float x, float y) {
            Debug.Log("set "+x+" " +y + " " + gameObject.transform.parent.parent.name);
            //parent.transform.localPosition = new Vector3(x, y, 0);
            this.x = x;
            this.y = y;
        }

        public void grab(float x, float y) {
            grabX = x;
            grabY = y;
        }

        public void unGrab() {

        }


    }

}
