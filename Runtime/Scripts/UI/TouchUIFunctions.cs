using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace VRPen {


    //this is supplemental ui functions specifically for use with the touch use-case (phone/tablet)
    public class TouchUIFunctions : MonoBehaviour {

        public GameObject displayObj;

        void Start() {

        }
        
        void Update() {

        }

        public void changeCanvasSize(bool increase) {
            float increment = increase ? 0.1f : -0.1f;
            displayObj.transform.localScale += new Vector3(increment, increment, increment);
        }
    }

}
