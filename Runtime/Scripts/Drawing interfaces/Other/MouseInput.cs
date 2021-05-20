﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {


    public class MouseInput : VRPenInput {
        // Start is called before the first frame update

        [Header("Mouse Parameters")]        
        [Space(10)]
        [Tooltip("If not assigned, the main camera will be used")]
        public Camera cam;
        
        
        [Header("Extra Values")]
        [Space(10)]
        public float pressure = 0;

        new void Start() {
            
            base.Start();
        }

        // Update is called once per frame
        void Update() {
            
            
            //find cam
            if (cam == null) cam = Camera.main;

            pressure = 1;
            
            
            //check for marker input
            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse0)) input();
            else if (Input.GetKeyUp(KeyCode.Mouse0)) {
                UIClickDown = true;
                input();
            }
            else {
                idle();
            }
            
            
            //reset click value at end of frame
            UIClickDown = false;


        }

        protected override InputData getInputData() {

            //init returns
            InputData data = new InputData();

            //raycast
            RaycastHit[] hits;
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            hits = Physics.RaycastAll(ray, 10);
            UnityEngine.Debug.DrawRay(ray.origin, ray.direction, Color.blue);
            
            //detect hit based off priority
            raycastPriorityDetection(ref data, hits);
            

            //no hit found
            if (data.hover == HoverState.NONE) {
                return data;
            }

            //pressure
            data.pressure = pressure;

            //find display
            Display localDisplay = null;
            Display[] displays = FindObjectsOfType<Display>();
            foreach(Display display in displays) {
                if (data.hit.collider.transform.IsChildOf(display.transform)) {
                    localDisplay = display;
                    break;
                }
            }
            if (localDisplay == null) {
                Debug.LogError("could not find display");
            }
            else {
                data.display = localDisplay;
            }
            
            

            //return
            return data;


        }
		

    }

}
