using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {


    public class TouchInput : VRPenInput {
        // Start is called before the first frame update

        public Camera cam;
        public float pressure = 0;

        public Renderer colorMat;

        //canvas tranlation vars
        public bool canvasMove = false;
        public bool firstCanvasMoveInput = true;
        Vector3 lastCanvasMovePos;

        //slider
        public bool useThicknessSlider;
        public Slider thicknessSlider;

        new void Start() {
            
            base.Start();
        }

        // Update is called once per frame
        void Update() {

            pressure = 1;
            if (Input.touchSupported && Input.touchCount > 0) pressure = Mathf.Clamp(Input.GetTouch(0).pressure, 0, 1);
            if (useThicknessSlider) pressure *= thicknessSlider.value;

            //VRPen.Debug.LogError(Input.touchSupported + "  " + pressure + "  " + Input.GetMouseButtonDown(0) + "  " + Input.GetMouseButton(0) + "  " + Input.GetMouseButtonUp(0));

            //check for marker input
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) input();
            else if (Input.GetMouseButtonUp(0)) {
                UIClickDown = true;
                input();
            }
            else {
                firstCanvasMoveInput = true;
                idle();
            }
            
            //reset click value at end of frame
            UIClickDown = false;

            //base
            base.Update();

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
            
            //if we need to move th canvas
            if (data.hover == HoverState.DRAW && canvasMove) {

                

                if (firstCanvasMoveInput) {
                    firstCanvasMoveInput = false;
                }
                else {
                    float zBefore = data.display.transform.localPosition.z;
                    Vector3 delta = data.hit.point - lastCanvasMovePos;
                    data.display.transform.position += delta;
                    data.display.transform.localPosition = new Vector3(data.display.transform.localPosition.x, data.display.transform.localPosition.y, zBefore);
                }
                
                lastCanvasMovePos = data.hit.point;


                //we dont wanna add any draw points so deselect the canvas
                data.hover = HoverState.NONE;
                return data;
            }

            //pressure
            data.pressure = pressure;




            //return
            return data;


        }
		

    }

}
