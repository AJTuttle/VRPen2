using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class Display : MonoBehaviour {

        

        [Header("Required Vars")] 
        [Space(10)]
        [Tooltip("Any unique identifier for the device")]
        public byte uniqueIdentifier;
        [Tooltip("Sync canvas changes over network")]
        public bool syncCanvas;
        [Tooltip("Sync ui state over network")]
        public bool syncUIState;
        [Tooltip("Should spawn in with its own canvas. Spawns after fully connected (or offline mode). ONLY WORKS FOR DISPLAYS PRESENT AT START OF SCENE")]
        public bool spawnCanvasOnStart;
        [Tooltip("Full access means that the display can see all canvases")]
        public bool fullAccess;
        public int pixelWidth;
        public int pixelHeight;
        public float aspectRatio {
            get {
                return ((float)pixelWidth / (float)pixelHeight);
            }
        }
        
        [Header("Optional Vars")] 
        public Shader shaderOverride;

        [Header("Shouldn't need to change these")] 
        public UIManager UIMan;
        public Transform canvasParent;

        public Dictionary<byte, GameObject> canvasObjs = new Dictionary<byte, GameObject>();

        [System.NonSerialized]
        public VectorCanvas currentLocalCanvas;

        public Transform cursorParent;
        
        
        //stamp
        public GameObject stampPrefab;
        [System.NonSerialized]
        public StampGenerator currentStamp;

        private void Start() {
            
            //add to displays
            VectorDrawing.s_instance.displays.Add(this);
            
            //check if there are any canvases that already exist for the display
            //should only do something for displays added post-start
            foreach (VectorCanvas canvas in VectorDrawing.s_instance.canvases) {
                if (fullAccess || canvas.originDisplayId == uniqueIdentifier) {
                    addCanvasToDisplay(canvas);
                }
            }
            
            
        }

        private void OnDestroy() {
            VectorDrawing.s_instance.displays.Remove(this);
        }

        public void startOfSceneInit() {
            if (spawnCanvasOnStart) StartCoroutine(createInitialCanvas());
        }


        IEnumerator createInitialCanvas() {

            //wait for user to be connected if in online mode
            while (!VectorDrawing.OfflineMode && !NetworkManager.s_instance.connectedAndCaughtUp) {
                yield return null;
                //if a canvas was already added then return
                //full access whiteboards need to ensure that at least one of the canvases added was by this display
                if (fullAccess) {
                    foreach (KeyValuePair<byte, GameObject> canvasObj in canvasObjs) {
                        if (VectorDrawing.s_instance.getCanvas(canvasObj.Key).originDisplayId == uniqueIdentifier) {
                            yield break;
                        }
                    }
                }
                //non full access can just assume that the current canvas was added by this displayID
                else {
                    if (currentLocalCanvas != null) {
                        yield break;
                    }
                }
            }
            

            //make sure the initial canvas isnt too high
            if (VectorDrawing.s_instance.canvases.Count >= VectorDrawing.s_instance.MAX_CANVAS_COUNT) {
                Debug.LogError("Initial was set to private but this would exceed the max number of canvases.");
                yield break;
            }

            //spawn local canvase
            VectorDrawing.s_instance.addCanvas(true, uniqueIdentifier, pixelWidth, pixelHeight);


            //switch to that canvas
            swapCurrentCanvas((byte)(VectorDrawing.s_instance.canvases.Count - 1), false);

        }

        public void addCanvasToDisplay(VectorCanvas canvas) {
            
            //check if it already exists first (may not be much of an error, but could indicate one)
            if (canvasObjs.ContainsKey(canvas.canvasId)) {
                Debug.LogError("Display trying to add canvas that it already has.");
                return;
            }
            
            //make obj
            GameObject quad = Instantiate(VectorDrawing.s_instance.quadPrefab);
            Destroy(quad.GetComponent<Collider>());
            quad.transform.parent = canvasParent;
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = Vector3.one;
            quad.name = "" + canvas.canvasId;
            quad.GetComponent<Renderer>().material = canvas.GetComponent<Renderer>().material;
            if (shaderOverride != null)
                quad.GetComponent<Renderer>().material.shader = shaderOverride;

            //turn off renderer
            quad.GetComponent<Renderer>().enabled = false;

            //update ui
            UIMan.addCanvas(canvas.canvasId);

            //add the actual obj to display's list
            canvasObjs.Add(canvas.canvasId, quad);

            //if theres not canvas on the display, switch
            if (currentLocalCanvas == null) swapCurrentCanvas(canvas.canvasId, false);
            
        }
        
        public void addCanvasPassthrough() {
            VectorDrawing.s_instance.addCanvas(true, uniqueIdentifier, pixelWidth, pixelHeight);
            swapCurrentCanvas((byte)(VectorDrawing.s_instance.canvases.Count - 1), true);
        }

		public void savePassthrough() {
            VectorDrawing.s_instance.saveImage(currentLocalCanvas.canvasId);
		}

       
        public void eyedropperPassthrough(VRPenInput input) {
            if (input.state != VRPenInput.ToolState.EYEDROPPER) input.switchTool(VRPenInput.ToolState.EYEDROPPER);
            else input.switchTool(VRPenInput.ToolState.NORMAL);
        }

        public void erasePassthrough(VRPenInput input) {
            if (input.state != VRPenInput.ToolState.ERASE) input.switchTool(VRPenInput.ToolState.ERASE);
            else input.switchTool(VRPenInput.ToolState.NORMAL);
        }

        public void markerPassthrough(VRPenInput input) {
            if (input.state != VRPenInput.ToolState.NORMAL) input.switchTool(VRPenInput.ToolState.NORMAL);
        }

        
        
        public void newStamp(StampType type, Transform parent, int stampIndex, string text, Color textColor) {
            if (currentStamp != null) currentStamp.close();

            GameObject obj = Instantiate(stampPrefab, parent);
            currentStamp = obj.GetComponent<StampGenerator>();
            if (type == StampType.image) {
                currentStamp.instantiate(VectorDrawing.s_instance, NetworkManager.s_instance.getLocalPlayerID(), this,
                    stampIndex);
            }
            else {
                currentStamp.instantiate(VectorDrawing.s_instance, NetworkManager.s_instance.getLocalPlayerID(), this,
                    text, textColor);
            }
        }
        
        public void clearCanvas() {
            currentLocalCanvas.clear(true);
        }

        public void swapCurrentCanvas(byte canvasId, bool localInput) {

            //end local drawing if it is drawing
            foreach (InputVisuals input in VectorDrawing.s_instance.inputDevices){
                if (input is VRPenInput && ((VRPenInput)input).currentLine != null &&
                    ((VRPenInput)input).currentLine.ownerId == NetworkManager.s_instance.getLocalPlayerID()) {
                    VectorDrawing.s_instance.endLineEvent(NetworkManager.s_instance.getLocalPlayerID(), ((VRPenInput)input).currentLine.localIndex, ((VRPenInput)input).currentLine.canvasId,true);
                }
            }

            //swap canvas
            if(currentLocalCanvas != null) canvasObjs[currentLocalCanvas.canvasId].GetComponent<Renderer>().enabled = false;
            currentLocalCanvas = VectorDrawing.s_instance.getCanvas(canvasId);
            canvasObjs[currentLocalCanvas.canvasId].GetComponent<Renderer>().enabled = true;

            //sync
            if (localInput && syncCanvas) NetworkManager.s_instance.sendCanvasChange(uniqueIdentifier, canvasId);
            
        }

    }

}