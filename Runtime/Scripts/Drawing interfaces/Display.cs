using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class Display : MonoBehaviour {

        [Tooltip("Full access means that the display can see all canvases whether or not they are private")]
        public bool fullAccess;

        public Shader shaderOverride;

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

        public void init() {
            StartCoroutine(nameof(createInitialCanvas));
        }


        IEnumerator createInitialCanvas() {

            //wait one frame so that canvas0 can be spawned first if this diplay is in the scene at start
            yield return null;

            //there is no initial canvas made as a remote client
            if (!VectorDrawing.actSynchronously) {
                yield break;
            }


            if (initialCanvasIsPrivate) {

                //make sure the initial canvas isnt too high
                if (vectorMan.canvases.Count == vectorMan.MAX_CANVAS_COUNT) {
                    Debug.LogError("Initial was set to private but this would exceed the max number of canvases.");
                    yield break;
                }

                //spawn local canvase
                vectorMan.addCanvas(true, false, DisplayId, pixelWidth, pixelHeight, true);


                //switch to that canvas
                swapCurrentCanvas((byte)(vectorMan.canvases.Count - 1), false);
            }
            else {

                // if this isnt the first public canvas
                if (vectorMan.initialPublicCanvasId != -1) {
                    swapCurrentCanvas((byte)vectorMan.initialPublicCanvasId, false);
                }

                //if this is the first public canvas
                else {
                    //make sure the initial canvas isnt too high
                    if (vectorMan.canvases.Count == vectorMan.MAX_CANVAS_COUNT) {
                        Debug.LogError("Initial was set to private but this would exceed the max number of canvases.");
                        yield break;
                    }
                    //spawn local canvase
                    vectorMan.addCanvas(true, true, VectorDrawing.INITIAL_PUBLIC_CANVAS_DISPLAY_ID, vectorMan.initalPublicCanvasPixelWidth, vectorMan.initalPublicCanvasPixelHeight, true);
                    
                    //switch to that canvas
                    swapCurrentCanvas((byte)(vectorMan.canvases.Count - 1), false);
                }

            }

        }

        public void addCanvasPassthrough(bool isPublic) {
            vectorMan.addCanvas(true, isPublic, DisplayId, pixelWidth, pixelHeight, false);
            swapCurrentCanvas((byte)(vectorMan.canvases.Count - 1), true);
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
            foreach (VRPenInput input in VectorDrawing.s_instance.localInputDevices){
                if (input.currentLine != null &&
                    input.currentLine.ownerId == network.getLocalPlayer().connectionId) {
                    vectorMan.endLineEvent(network.getLocalPlayer(), input.currentLine.localIndex, input.currentLine.canvasId,true);
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