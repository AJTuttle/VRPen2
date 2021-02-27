using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {


    public class TouchInput : VRPenInput {
        // Start is called before the first frame update

        public Camera cam;
        public float pressure = 0;
        public Display display;

        public Renderer colorMat;

        //canvas tranlation vars
        public bool canvasMove = false;
        public GameObject displayObj;
        public bool firstCanvasMoveInput = true;
        Vector3 lastCanvasMovePos;

        //slider
        public bool useThicknessSlider;
        public Slider thicknessSlider

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
            //if we need to move th canvas
            else if (data.hover == HoverState.DRAW && canvasMove) {

                

                if (firstCanvasMoveInput) {
                    firstCanvasMoveInput = false;
                }
                else {
                    float zBefore = displayObj.transform.localPosition.z;
                    Vector3 delta = data.hit.point - lastCanvasMovePos;
                    displayObj.transform.position += delta;
                    displayObj.transform.localPosition = new Vector3(displayObj.transform.localPosition.x, displayObj.transform.localPosition.y, zBefore);
                }
                
                lastCanvasMovePos = data.hit.point;


                //we dont wanna add any draw points so deselect the canvas
                data.hover = HoverState.NONE;
                return data;
            }

            //pressure
            data.pressure = pressure;

            //find display (touch input assumes theres only 1 display, so we can just set it as  public param)
            data.display = display;
            
            
            

            //return
            return data;


        }
		

    }

}
