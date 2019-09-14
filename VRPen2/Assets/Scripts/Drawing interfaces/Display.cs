using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class Display : MonoBehaviour {

        public UIManager UIMan;
        public Transform canvasParent;


		[System.NonSerialized]
		public byte DisplayId;

        [System.NonSerialized]
        public VectorDrawing vectorMan;
        [System.NonSerialized]
        public NetworkManager network;
        [System.NonSerialized]
        public VectorCanvas currentLocalCanvas;

        private void Awake() {
            vectorMan = FindObjectOfType<VectorDrawing>();
            network = FindObjectOfType<NetworkManager>();
        }

        public void addCanvasPassthrough() {
            vectorMan.addCanvas(true);
            swapCurrentCanvas((byte)(vectorMan.canvases.Count - 1));
        }

        public void undoPassthrough() {
            vectorMan.undo(network.localPlayer, true);
        }

		public void savePassthrough() {
			vectorMan.saveImage(currentLocalCanvas.canvasId);
		}

       
        public void eyedropperPassthrough(VRPenInput input) {
            if (input.state != VRPenInput.MarkerState.EYEDROPPER) input.switchTool(VRPenInput.MarkerState.EYEDROPPER);
            
        }

        public void erasePassthrough(VRPenInput input) {
            if (input.state != VRPenInput.MarkerState.ERASE) input.switchTool(VRPenInput.MarkerState.ERASE);
        }

        public void markerPassthrough(VRPenInput input) {
            if (input.state != VRPenInput.MarkerState.NORMAL) input.switchTool(VRPenInput.MarkerState.NORMAL);
        }

        public void clearCanvas() {
            currentLocalCanvas.clear(true);
        }

        public void swapCurrentCanvas(byte canvasId) {

            

            if(currentLocalCanvas != null) canvasParent.GetChild(currentLocalCanvas.canvasId).GetComponent<Renderer>().enabled = false;
            currentLocalCanvas = vectorMan.getCanvas(canvasId);
            canvasParent.GetChild(canvasId).GetComponent<Renderer>().enabled = true;
            
        }

    }

}