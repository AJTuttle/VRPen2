using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class Display : MonoBehaviour {

        public UIManager UIMan;
        public Transform canvasParent;

        [Tooltip("This sets the intial canvas of this whiteboard LOCALLY, if the canvas index does not exist a new one will be made LOCALLY. " +
            "WARNING: this does not sync over the network so its a good idea to make everyone have the same initial canvases at start.")]
        public byte initialCanvasId;

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

        private void Start() {
            switchToInitialCanvas();
        }

        void switchToInitialCanvas() {

            //make sure the initial canvas isnt too high
            if (initialCanvasId >= vectorMan.MAX_CANVAS_COUNT) {
                Debug.LogError("Initial canvas id for display was set the a value higher than possible.");
                return;
            }
            
            //spawn local canvases if there are no other
            while (initialCanvasId >= vectorMan.canvases.Count) {
                vectorMan.addCanvas(false);
            }

            int x = canvasParent.childCount;
            //switch to that canvas
            swapCurrentCanvas(initialCanvasId, false);

        }

        public void addCanvasPassthrough() {
            vectorMan.addCanvas(true);
            swapCurrentCanvas((byte)(vectorMan.canvases.Count - 1), true);
        }

        public void undoPassthrough() {
            vectorMan.undo(network.getLocalPlayer(), true);
        }

		public void savePassthrough() {
			vectorMan.saveImage(currentLocalCanvas.canvasId);
		}

       
        public void eyedropperPassthrough(VRPenInput input) {
            if (input.state != VRPenInput.ToolState.EYEDROPPER) input.switchTool(VRPenInput.ToolState.EYEDROPPER);
            
        }

        public void erasePassthrough(VRPenInput input) {
            if (input.state != VRPenInput.ToolState.ERASE) input.switchTool(VRPenInput.ToolState.ERASE);
        }

        public void markerPassthrough(VRPenInput input) {
            if (input.state != VRPenInput.ToolState.NORMAL) input.switchTool(VRPenInput.ToolState.NORMAL);
        }

        public void stampPassthrough(VRPenInput input, Transform parent, int stampIndex) {
            input.newStamp(parent, this, stampIndex);
        }
        
        public void clearCanvas() {
            currentLocalCanvas.clear(true);
        }

        public void swapCurrentCanvas(byte canvasId, bool localInput) {

            //end local drawing if it is drawing
            foreach (KeyValuePair<byte, InputDevice> device in network.getLocalPlayer().inputDevices) {
                if (device.Value.currentGraphic != null) {
                    vectorMan.endLineEvent(network.getLocalPlayer(), device.Value.deviceIndex, true);
                }
            }

            //swap canvas
            if(currentLocalCanvas != null) canvasParent.GetChild(currentLocalCanvas.canvasId).GetComponent<Renderer>().enabled = false;
            currentLocalCanvas = vectorMan.getCanvas(canvasId);
            canvasParent.GetChild(canvasId).GetComponent<Renderer>().enabled = true;

            //sync
            if (localInput) network.sendCanvasChange(DisplayId, canvasId);
            
        }

    }

}