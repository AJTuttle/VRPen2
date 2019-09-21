using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace VRPen {

    public class VectorDrawing : MonoBehaviour {

        //canvas
        public List<Display> displays = new List<Display>();
        List<InputDevice> localInputDevices = new List<InputDevice>();
        [System.NonSerialized]
        public List<VectorCanvas> canvases = new List<VectorCanvas>();
        public GameObject canvasPrefab;
        public int MAX_CANVAS_COUNT;
        public Shader lineShader;

        //rendertexture area
        public Vector3 renderAreaOrigin;

        //scripts
        StarTablet tablet;
        NetworkManager network;


        //public vars
        public Material lineMaterial;

        [Header("Line Smoothing and compression parameters")]
        public float minDistanceDelta;
        [Tooltip("This is used for smoothing and refers to the angle between the last line segment and the new one [0-180]. " +
            "Recommended values are between 135 (high performance) and 175 (high fidelity). Warning, values >= 180 will not work and may cause infinite loops.")]
        public float minCurveAngle;
        public float maxCuspAngle;
        public Transform canvasParent;


		public delegate void InputDeviceCreatedEvent(InputDevice pen, int deviceIndex);
		public event InputDeviceCreatedEvent InputDeviceCreated;
        
		public GameObject quadPrefab;


		private void Start() {
            

            //get scripts
            tablet = GetComponent<StarTablet>();
            network = GetComponent<NetworkManager>();

            //spawn preset canvases
            addCanvas(true);

			//set up deisplay ids
			for(byte x = 0; x < displays.Count; x++) {
				displays[x].DisplayId = x;
			}

            //set up input devices
            localInputDevices = new List<InputDevice>(FindObjectsOfType<InputDevice>());

			//removes old devices (patch) TODO
			localInputDevices.RemoveAll(x => x.owner != null);

            network.localPlayer.inputDevices = new Dictionary<byte, InputDevice>();
            for (byte x = 0; x < localInputDevices.Count; x++) {
                InputDevice device = localInputDevices[x];
                network.localPlayer.inputDevices.Add(x, device);
                device.owner = network.localPlayer;
                device.deviceIndex = x;
				InputDeviceCreated?.Invoke(device, device.deviceIndex);
				
            }

            SceneManager.activeSceneChanged += OnSceneChange;

		}
        

        private void Update() {
            hotkeys();
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
			#if !UNITY_EDITOR
	        foreach (VectorCanvas canvas in canvases) {
		        saveImage(canvas.canvasId);
	        }
			#endif
        }

		#if !UNITY_EDITOR
		private void OnApplicationQuit() {
			foreach (VectorCanvas canvas in canvases) {
				saveImage(canvas.canvasId);
			}
		}
		#endif

		void hotkeys() {
            //hotkeys
            if (Input.GetKeyDown(KeyCode.U)) {
                undo(network.localPlayer, true);
            }
            else if (Input.GetKeyDown(KeyCode.C)) {
                canvases[0].clear(true);
            }
            else if (Input.GetKeyDown(KeyCode.S)) {
                saveImage(0);
            }
        }

        
        public VectorCanvas getCanvas(byte canvasId) {
            return canvases.Find(x => x.canvasId == canvasId);
        }
		public Display getDisplay(byte displayId) { 
            return displays.Find(x => x.DisplayId == displayId);
        }

        public int getCanvasListCount() {
            return canvases.Count;
        }

        public void endLineEvent(NetworkedPlayer player, byte deviceIndex, bool localInput) {
            draw(player, deviceIndex, true, Color.black, 0, 0, 0, 0, localInput);
        }

        public void draw(NetworkedPlayer player, byte deviceIndex, bool endLine, Color32 color, float x, float y, float pressure, byte canvasId, bool localInput) {

           
            //get canvas
            VectorCanvas canvas = getCanvas(canvasId);
            if (canvas == null) {
                Debug.LogError("No canvas found for draw input");
                return;
            }

            //get device
            InputDevice device;
            if ((device = player.inputDevices[deviceIndex]) == null) {
                Debug.LogError("Failed retreiving input device");
                return;
            }

            //end line or draw
            if (endLine || pressure == 0) {

                endLineData(device);
                if (localInput) network.addToDataOutbox(endLine, color, x, y, pressure, canvasId, deviceIndex);
                
            }
            else {

                //newline bool, useful for delta compression
                bool newLine = false;

                //get or create mesh
                Mesh currentMesh = getMesh(player, device, color, canvas, ref newLine);

                //got vector pos
                Vector3 drawPoint = new Vector3(x, 0, y);

                //delta compression
                bool validInput = deltaCompression(device, newLine, drawPoint);

                //add to that mesh and network
                if (validInput) {

                    //if this is not the first or second point in the line, we need to smooth the slope
                    if (currentMesh.vertexCount > 2) {

                        //get angle between last line and new line (negative for left turn, positive for right turn)
                        float angle = 180f - Vector3.Angle(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint);
                        if (Vector3.Cross(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint).y < 0) angle *= -1;

                        Debug.Log(slopeSmoothing(canvas, device, currentMesh, pressure, angle, device.lastDrawPoint, drawPoint));
                    }
                    else canvas.addToLine(device, currentMesh, drawPoint, pressure);

                    if (localInput) network.addToDataOutbox(endLine, color, x, y, pressure, canvasId, deviceIndex);
                }
            }
        }

        void endLineData(InputDevice device) {

            //turn off last mesh for render texture
            if (device.currentLine != null) {

                //reset the renderqueue so that the depth is set when the line is finished drawing (so that it doesnt shift when undos happen for example)
                VectorCanvas canvas = getCanvas(device.currentLine.canvasId);
                device.currentLine.obj.GetComponent<Renderer>().material.renderQueue = canvas.renderQueueCounter;
                canvas.renderQueueCounter++;

                //turn off and remove from curr
                device.currentLine.obj.GetComponent<MeshRenderer>().enabled = false;
                device.currentLine = null;
            }
            else {
                Debug.LogError("Line end event but theres no line to end (this could also be caused by draw calls that have 0 pressure)");
            }

        }

        //recursive method that, depending on the angle between the last line and the new line, will split the new line into two new lines to make the slope more gradual
        int slopeSmoothing(VectorCanvas canvas, InputDevice device, Mesh currentMesh, float pressure, float angle, Vector3 start, Vector3 end) {

            int counter = 1;
            //if angle is too large
            if (Mathf.Abs(angle) < minCurveAngle && Mathf.Abs(angle) > maxCuspAngle) {

                //Debug.Log("angle smoothing needed  ");

                //get the new angle that represents both angles in for the 2 new lines
                float newAngle = (3 * Mathf.Abs(angle) + 360) / 5 * (angle > 0 ? 1: -1);

                //get the new midpoint
                float length = Vector3.Distance(start, end);
                float midPointLength = length / (1 + Mathf.Tan(Mathf.Deg2Rad * Mathf.Abs(newAngle - angle)) / Mathf.Tan(Mathf.Deg2Rad * Mathf.Abs(newAngle - angle) / 2));
                Vector3 notSmoothedDir = (end - start).normalized;
                Vector3 midpointDisplacment = new Vector3(-notSmoothedDir.z, 0, notSmoothedDir.x) * midPointLength * (angle > 0 ? 1 : -1) * Mathf.Tan(Mathf.Deg2Rad * Mathf.Abs(newAngle - angle));
                Vector3 middle = start + (end - start).normalized * midPointLength + midpointDisplacment;




                //recursively call for each new line segment now that we split this one
                counter += slopeSmoothing(canvas, device, currentMesh, pressure, newAngle, start, middle);
                counter += slopeSmoothing(canvas, device, currentMesh, pressure, newAngle, middle, end);
                //canvas.addToLine(device, currentMesh, end, pressure);

            }

            //actually add triangles to the mesh
            else {
                canvas.addToLine(device, currentMesh, end, pressure);
            }
            return counter;

        }

        Mesh getMesh(NetworkedPlayer player, InputDevice device, Color32 color, VectorCanvas canvas, ref bool newLine) {

            Mesh currentMesh;

            //if new line needed
            if (device.currentLine == null) {

                //make obj
                GameObject obj = new GameObject();
                obj.transform.parent = canvas.vectorParent;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                //add mesh
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                currentMesh = new Mesh();
                mf.mesh = currentMesh;

                

                //vector line data struct and player data structs
                VectorLine vl = new VectorLine();
                vl.mesh = currentMesh;
                vl.owner = player.connectionId;
                vl.obj = obj;
                vl.mr = mr;
                device.currentLine = vl;
                player.lineIndexer++;
                vl.index = player.lineIndexer;
                vl.deviceIndex = device.deviceIndex;
                player.lines.Add(vl);
                

                //set shader
                mr.material = lineMaterial;
                mr.material.color = color;
                
                //set shader
                Material mat = obj.GetComponent<Renderer>().material;
                mat.shader = lineShader;
                mat.renderQueue = canvas.renderQueueCounter;
                canvas.renderQueueCounter++;

                newLine = true;

            }
            //if addition to existing line
            else {
                currentMesh = device.currentLine.mesh;

                newLine = false;
            }

            return currentMesh;

        }

        bool deltaCompression(InputDevice device, bool newLine, Vector3 drawPoint) {

            bool validDistance = Vector3.Distance(drawPoint, device.lastDrawPoint) > minDistanceDelta;

            return newLine || validDistance;

        }
       

        public void undo(NetworkedPlayer player, bool localInput) {

			//get line
			if (player.lines.Count == 0) return;
            VectorLine undid = player.lines.Last();

            //remove from data
            player.lines.RemoveAt(player.lines.Count - 1);

            byte deviceIndex = undid.deviceIndex;

            //get canvas
            VectorCanvas canvas = getCanvas(undid.canvasId);

            //destroy line
            Destroy(undid.obj);

            //reset curr line
            InputDevice device;
            if ((device = player.inputDevices[deviceIndex]) == null) {
                Debug.LogError("Failed retreiving input device from undo event");
                return;
            }
            device.currentLine = null;

            //rerender rest
            StartCoroutine(canvas.rerenderCanvas());

            //network
            if (localInput) network.sendUndo();
        }

        public void addCanvas(bool localInput) {

            if (getCanvasListCount() >= MAX_CANVAS_COUNT) {
                Debug.LogWarning("Failed to add a canvas when max canvas count reached already");
                return;
            }

            byte canvasId = (byte)canvases.Count;

            //make canvas
            VectorCanvas temp = GameObject.Instantiate(canvasPrefab, canvasParent).GetComponent<VectorCanvas>();
            temp.instantiate(this, network, canvasId);
            temp.GetComponent<Renderer>().enabled = false;
            
            //add to list
            canvases.Add(temp);
            
            //make copys of texture
            foreach (Display display in displays) {

	            GameObject quad = Instantiate(quadPrefab);
				Destroy(quad.GetComponent<Collider>());
                quad.transform.parent = display.canvasParent;
                quad.transform.localPosition = Vector3.zero;
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = Vector3.one;
                quad.name = "" + canvasId;
                quad.GetComponent<Renderer>().material = temp.GetComponent<Renderer>().material;

                //turn off renderer
                quad.GetComponent<Renderer>().enabled = false;

                //update ui
                display.UIMan.addCanvas(canvasId);

                //swap to the canvas on all displays if its the initial canvas
                if (canvasId == 0) display.swapCurrentCanvas(0);

            }
            

            //if local
            if (localInput) {

                //network it (make sure you dont send the default board)
                if (canvasId != 0) network.sendCanvasAddition();
            }
            
        }

		public void saveImage(byte canvasId) {
			TextureSaver.export(getCanvas(canvasId).renderTexture);
		}


    }

}


