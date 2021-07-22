using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VRPen {

    public class TabletInput : VRPenInput {


        //scripts
        [Header("Tablet Parameters")]
        [Space(10)]
        public StarTablet2 tablet;

        //public vars
        public Display localDisplay;
        public AnimationCurve pressureCurve;

        //private vars
        StarTablet2.PenSample currentSample = null;
        StarTablet2.PenSample lastSample = null;
        float raycastDistance = 0.05f;
        private const float maxNullPressure = 0.0001f;

        [Header("Tablet Button Parameters")]
        [Space(10)]
        public List<Renderer> buttonRenderers;
        public Transform wheelTransform;
        
        [Header("Tablet Button Events")]
        [Space(10)]
        public UnityEvent wheelLeftEvent;
        public UnityEvent wheelRightEvent;
        public UnityEvent button0Event;
        public UnityEvent button1Event;
        public UnityEvent button2Event;
        public UnityEvent button3Event;
        public UnityEvent button4Event;
        public UnityEvent button5Event;
        
        
        
        void Start() {
            
            //base
            base.Start();
            
            //add highligh listeners
            button0Event.AddListener(delegate { buttonHighlight(0); });
            button1Event.AddListener(delegate { buttonHighlight(1); });
            button2Event.AddListener(delegate { buttonHighlight(2); });
            button3Event.AddListener(delegate { buttonHighlight(3); });
            button4Event.AddListener(delegate { buttonHighlight(4); });
            button5Event.AddListener(delegate { buttonHighlight(5); });
            wheelLeftEvent.AddListener(delegate { turnWheel(-20); });
            wheelRightEvent.AddListener(delegate { turnWheel(20); });
        }

        void Update() {

            //how many pen inputs need to be computed this frame
            int numSamples = tablet.penSamples.Count;
            
            //idle
            bool idleThisFrame = true;
            if (numSamples == 0) idleThisFrame = false;
            
            //loop for each pen input
            for (int x = 0; x < numSamples; x++) {
                
                currentSample = tablet.penSamples[x];

                //update pressure
                if (currentSample.pressure <= maxNullPressure) currentSample.pressure = 0;
                else currentSample.pressure = pressureCurve.Evaluate(currentSample.pressure);
                
                //uiclick event
                UIClickDown = lastSample != null && currentSample.pressure == 0 && lastSample.pressure > 0;
                
                //updateCursor
                updatelocalCursor();

                //input
                if (currentSample.pressure > 0 || UIClickDown) {
                    input();
                    idleThisFrame = false;
                }

                lastSample = currentSample;
                
            }

            //idle
            if (idleThisFrame) {
                idle();
            }
            
            //clear data
            tablet.penSamples.Clear();
            
            //button events
            if (tablet.getButtonDown(0)) button0Event.Invoke();
            if (tablet.getButtonDown(1)) button1Event.Invoke();
            if (tablet.getButtonDown(2)) button2Event.Invoke();
            if (tablet.getButtonDown(3)) button3Event.Invoke();
            if (tablet.getButtonDown(4)) button4Event.Invoke();
            if (tablet.getButtonDown(5)) button5Event.Invoke();
            
            //wheel events
            int wheel = tablet.getWheel();
            if (wheel < -1) {
                wheelLeftEvent.Invoke();
                tablet.resetWheelMag();
            }
            else if (wheel > 1) {
                wheelRightEvent.Invoke();
                tablet.resetWheelMag();
            }


        }


        public void setNewDisplay(Display display) {
            //end line and turn off current cursor
            if (localDisplay != null) {
                localDisplay.displayCursor.SetActive(false);
                endLine();
            }
            //set display
            localDisplay = display;
        }
        
        void turnWheel(int angle) {
            wheelTransform.RotateAround(wheelTransform.forward, angle);
        }
        
        void buttonHighlight(int index) {
            StartCoroutine(buttonHighlightRoutine(index));
        }
        
        IEnumerator buttonHighlightRoutine(int index) {
            
            //highlight
            Material mat = buttonRenderers[index].material;
            Color col = mat.color;
            mat.color = Color.white;
            
            //wait 100 ms
            yield return new WaitForSeconds(.1f);
            
            //unhighlight
            mat.color = col;
        }

        void updatelocalCursor() {

            if (currentSample != null && localDisplay != null) {

                //turn on
                localDisplay.displayCursor.SetActive(true);

                //aspect rat
                float aspectRatio = localDisplay.currentLocalCanvas.aspectRatio;

                //get x and y
                float x = .5f * aspectRatio - aspectRatio * currentSample.point.x;
                float y = .5f - currentSample.point.y;
                
                //apply
                localDisplay.displayCursor.transform.localPosition = new Vector3(x, 0, y);

            }
            else {
                localDisplay.displayCursor.SetActive(false);
            }

        }


        protected override InputData getInputData() {

            //init returns
            InputData data = new InputData();

            //raycast
            RaycastHit[] hits;
            hits = Physics.RaycastAll(localDisplay.displayCursor.transform.position - raycastDistance/2 * localDisplay.displayCursor.transform.up, localDisplay.displayCursor.transform.up, raycastDistance);
            //Debug.DrawRay(localCursor.transform.position + raycastDistance / 2 * localCursor.transform.up, -localCursor.transform.up, Color.green);


            //detect hit based off priority
            raycastPriorityDetection(ref data, hits);
            
            

            //pressure
            data.pressure = currentSample.pressure;
            

            //display
            data.display = localDisplay;
            

            return data;
        }


    }

}
