using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class Display : MonoBehaviour {

        public int pixelWidth;
        public int pixelHeight;
        public float aspectRatio {
            get {
                return ((float)pixelWidth / (float)pixelHeight);
            }
        }

        [Tooltip("This sets the intial canvas of this whiteboard LOCALLY, if the canvas index does not exist a new one will be made LOCALLY. " +
            "WARNING: this does not sync over the network so its a good idea to make everyone have the same initial canvases at start.")]
        public bool initialCanvasIsPrivate;


        public UIManager UIMan;
        public Transform canvasParent;

        public Dictionary<byte, GameObject> canvasObjs = new Dictionary<byte, GameObject>();

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
            StartCoroutine(nameof(switchToInitialCanvas));
        }

        IEnumerator switchToInitialCanvas() {

            //wait one frame so that canvas0 can be spawned first if this diplay is in the scene at start
            yield return null;

            //dont switch if the initial canvas is public
            if (!initialCanvasIsPrivate) yield break;

            //make sure the initial canvas isnt too high
            if (vectorMan.canvases.Count == vectorMan.MAX_CANVAS_COUNT) {
                Debug.LogError("Initial was set to private but this would exceed the max number of canvases.");
                yield break;
            }

            //spawn local canvase
            vectorMan.addCanvas(false, false, this); ;
            

            //switch to that canvas
            swapCurrentCanvas((byte)(vectorMan.canvases.Count - 1), false);

        }

        public void addCanvasPassthrough(bool isPublic) {
            vectorMan.addCanvas(true, isPublic, this);
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
            if(currentLocalCanvas != null) canvasObjs[currentLocalCanvas.canvasId].GetComponent<Renderer>().enabled = false;
            currentLocalCanvas = vectorMan.getCanvas(canvasId);
            canvasObjs[currentLocalCanvas.canvasId].GetComponent<Renderer>().enabled = true;

            //sync
            if (localInput) network.sendCanvasChange(DisplayId, canvasId);
            
        }

    }

}