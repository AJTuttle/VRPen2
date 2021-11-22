﻿using System.Collections;
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
        public GameObject cursor;
        public GameObject cursorMesh0;
        public GameObject cursorMesh1;
        public GameObject cursorMesh2;
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
        
        [Header("Tablet Gesture Events")]
        [Space(10)]
        public UnityEvent swipeGestureLeft;
        public UnityEvent swipeGestureRight;
        public UnityEvent swipeGestureUp;
        public UnityEvent swipeGestureDown;
        public UnityEvent doubleTapGesture;
        
        
        //gesture vars
        private const float minSwipeDistance = 0.06f;
        private const float doubleTapMaxTime = 0.3f;
        private Vector2 swipeStart;
        private float doubleTapStart = 0; //not set when swiped
        
        
        
        void Start() {
            
            //base
            base.Start();
            
            //set start display
            if (localDisplay != null) setNewDisplay(localDisplay);
            
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

                //gesture
                if (currentSample.state ==  StarTablet2.PenState.LBUTTON_TOUCH || currentSample.state ==  StarTablet2.PenState.LBUTTON_AIR) {
                    
                    //gets tart and end
                    bool start = lastSample.state != StarTablet2.PenState.LBUTTON_TOUCH &&
                                 currentSample.state == StarTablet2.PenState.LBUTTON_TOUCH;
                    bool release = currentSample.state == StarTablet2.PenState.LBUTTON_AIR &&
                                   lastSample.state == StarTablet2.PenState.LBUTTON_TOUCH;
                    
                    gestureInput(start, release);
                    idleThisFrame = true;
                    
                    //set cursor
                    setCursorMesh(1);
                }
                //vrpen press input
                else if (currentSample.pressure > 0 || UIClickDown) {
                    input();
                    idleThisFrame = false;
                }
                //vrpen hover input
                else {
                    //call get input data even if no pressure, since this will update the cursor
                    getInputData();
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

            //base
            base.Update();
            
        }

        void setCursorMesh(int x) {
            //set active
            cursorMesh0.SetActive(x == 0);
            cursorMesh1.SetActive(x == 1);
            cursorMesh2.SetActive(x == 2);
        }
        
        void gestureInput(bool start, bool release) {
            
            //press
            if (start) {
                
                //swipe start
                swipeStart = currentSample.point;
            }
            //release
            else if (release) {
                
                //swipe
                if (Vector2.Distance(currentSample.point, swipeStart) >= minSwipeDistance) {
                    Vector2 dir = currentSample.point - swipeStart;
                    //up or down
                    if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x)) {
                        //up
                        if (dir.y > 0) {
                            swipeGestureUp.Invoke();
                        }
                        //down
                        else {
                            swipeGestureDown.Invoke();
                        }
                    }
                    //left or right
                    else {
                        //right
                        if (dir.x > 0) {
                            swipeGestureRight.Invoke();
                        }
                        //left
                        else {
                            swipeGestureLeft.Invoke();
                        }
                    }
                } 
                //double tap end
                else if (Time.time <= doubleTapStart + doubleTapMaxTime) {
                    //reset double tap start
                    doubleTapStart = 0;
                    
                    //invoke
                    doubleTapGesture.Invoke();
                    
                }
                //double tap start
                else {
                    doubleTapStart = Time.time;
                }
            }
        }
        
        public void setNewDisplay(Display display) {
            //end line
            if (localDisplay != null) {
                endLine();
            }
            //set display
            localDisplay = display;
            //move cursor
            Vector3 cursorSize = cursor.transform.localScale;
            cursor.transform.parent = localDisplay.cursorParent;
            cursor.transform.localScale = cursorSize;
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

                //set active
                cursor.SetActive(true);
                
                //aspect rat
                float aspectRatio;
                //assume 5/3 aspect ratio if no canvas
                if (localDisplay.currentLocalCanvas == null) aspectRatio = 1.66667f;
                else aspectRatio = localDisplay.currentLocalCanvas.aspectRatio;

                //get x and y
                float x = .5f * aspectRatio - aspectRatio * currentSample.point.x;
                float y = .5f - currentSample.point.y;
                
                //apply
                cursor.transform.localPosition = new Vector3(x, 0, y);
                //cursor.transform.position = cursor.transform.parent.TransformPoint(localDisplay.cursorParent.InverseTransformPoint(new Vector3(x, 0, y)));
                cursor.transform.localRotation = Quaternion.identity;

            }
            else {
                cursor.SetActive(false);
            }

        }


        protected override InputData getInputData() {

            //init returns
            InputData data = new InputData();
            

            //raycast
            RaycastHit[] hits;
            hits = Physics.RaycastAll(cursor.transform.position + raycastDistance/2 * cursor.transform.up, -cursor.transform.up, raycastDistance);
            //Debug.DrawRay(localCursor.transform.position + raycastDistance / 2 * localCursor.transform.up, -localCursor.transform.up, Color.green);


            //detect hit based off priority
            raycastPriorityDetection(ref data, hits);
            
            
            //set cursor 
            if (data.hover == HoverState.DRAW) {
                
                setCursorMesh(0);
            }
            else if (data.hover == HoverState.PALETTE || data.hover == HoverState.GRABBABLE ||
                     data.hover == HoverState.SELECTABLE || data.hover == HoverState.AREAINPUT) {
                setCursorMesh(1);
            }
            else {
                setCursorMesh(2);
            }

            //pressure
            data.pressure = currentSample.pressure;
            

            //display
            data.display = localDisplay;
            

            return data;
        }


    }

}
