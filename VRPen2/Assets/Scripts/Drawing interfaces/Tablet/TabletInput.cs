using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {

    public class TabletInput : VRPenInput {


        //scripts
        public StarTablet tablet;

        //public vars
        public GameObject localCursor;
        public Display localDisplay;
        public Material colorIndicator;

        //private vars
        StarTablet.PenSample currentSample = null;
        StarTablet.PenSample lastSample = null;
        float raycastDistance = 0.01f;

        void Start() {
            base.Start();
        }

        void Update() {

            //how many pen inputs need to be computed this frame
            int numSamples = tablet.penSamples.Count;

            //loop for each pen input
            for (int x = 0; x < numSamples; x++) {
                
                currentSample = tablet.penSamples[x];

                //uiclick event
                UIClickDown = lastSample != null && currentSample.pressure > 0 && lastSample.pressure == 0;

                //updateCursor
                updatelocalCursor();

                //check for marker input
                input();

                lastSample = currentSample;
                
            }


            //clear data
            tablet.penSamples.Clear();

            //reset click value at end of frame
            UIClickDown = false;

        }


        void updatelocalCursor() {

            if (currentSample != null) {

                //turn on
                localCursor.SetActive(true);

                //aspect rat
                float aspectRatio = (float)localDisplay.currentLocalCanvas.renderTexturePresets.width / localDisplay.currentLocalCanvas.renderTexturePresets.height;

                //get x and y
                float x = .5f * aspectRatio - aspectRatio * currentSample.point.x;
                float y = .5f - currentSample.point.y;

                //apply
                localCursor.transform.localPosition = new Vector3(x, 0, y);

            }
            else {
                localCursor.SetActive(false);
            }

        }


        protected override InputData getInputData() {

            //init returns
            InputData data = new InputData();

            //raycast
            RaycastHit[] hits;
            hits = Physics.RaycastAll(localCursor.transform.position + raycastDistance/2 * localCursor.transform.up, -localCursor.transform.up, raycastDistance);
            //Debug.DrawRay(localCursor.transform.position + raycastDistance / 2 * localCursor.transform.up, -localCursor.transform.up, Color.green);


            //detect hit based off priority
            raycastPriorityDetection(ref data, hits);
            

            //pressure
            data.pressure = currentSample.pressure;
            

            //display
            data.display = localDisplay;
            

            return data;
        }

        protected override void updateColorIndicator(Color32 color) {
            if (colorIndicator != null) colorIndicator.color = color;
        }


    }

}
