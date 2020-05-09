using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace VRPen {


    //this is supplemental ui functions specifically for use with the touch use-case (phone/tablet)
    public class TouchUIFunctions : MonoBehaviour {

        public GameObject displayObj;
        public CanvasScaler canvasScale;
        public TouchInput touchInput;
        public UIManager uiMan;

        void Start() {
            if (!VectorDrawing.actSynchronously) uiMan.removeAddCanvasButtons();
        }
        
        void Update() {

        }

        public void changeCanvasSize(bool increase) {
            float increment = increase ? 0.05f : -0.05f;
            displayObj.transform.localScale += new Vector3(increment, increment, increment);
        }

        public void changeUIScale(bool increase) {
            float increment = increase ? 0.05f : -0.05f;
            canvasScale.scaleFactor += increment;
        }

        public void toggleCanvasMove(bool toggle) {
            touchInput.canvasMove = toggle;
        }
    }

}
