using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;



namespace VRPen {


    //this is supplemental ui functions specifically for use with the touch use-case (phone/tablet)
    public class TouchUIFunctions : MonoBehaviour {
        
        public static TouchUIFunctions s_instance;
        
        public GameObject displayObj;
        public CanvasScaler canvasScale;
        public RectTransform canvasRect;
        public TouchInput touchInput;
        public UIManager uiMan;

        public GameObject moveIcon;
        public GameObject drawIcons;

        //waiting for canvas text
        public VRPen.Display display;
        public Text waitingForCanvas;
        bool canvasExists = false;

        public Transform stampUIParent;

        private void Awake() {
            s_instance = this;
        }

        void Start() {
            setStampUIParentSize();
        }

        public void setStampUIParentSize() {
            //fix for screenspace ui being different that world space
            stampUIParent.localScale = Vector3.one / canvasScale.scaleFactor * displayObj.transform.localScale.x * canvasRect.sizeDelta.y * canvasScale.scaleFactor / 1000f; 
        }

        private void Update() {

            //waiting for canvas text
            if (!canvasExists) {
                canvasExists = display.currentLocalCanvas != null;
                if (canvasExists) {
                    waitingForCanvas.gameObject.SetActive(false);
                }
            }
        }

        public void changeCanvasSize(bool increase) {
            float increment = increase ? 0.05f : -0.05f;
            displayObj.transform.localScale += new Vector3(increment, increment, increment);
            setStampUIParentSize();
        }

        public void changeUIScale(bool increase) {
            float increment = increase ? 0.05f : -0.05f;
            canvasScale.scaleFactor += increment;
            setStampUIParentSize();
        }

        public void toggleCanvasMove(bool toggle) {

            //if the move is already on, it should be turned off if the button is clicked
            if (touchInput.canvasMove && toggle) toggle = false;

            touchInput.canvasMove = toggle;
            moveIcon.SetActive(toggle);
            drawIcons.SetActive(!toggle);
        }
        
        public void turnOnCanvasMove() {

            touchInput.canvasMove = true;
            moveIcon.SetActive(true);
            drawIcons.SetActive(!true);
        }
    }

}
