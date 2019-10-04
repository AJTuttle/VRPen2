using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace VRPen {

    public class VectorDrawing : MonoBehaviour {


        public Texture stampTest;

        //scripts
        StarTablet tablet;
        NetworkManager network;
        
        //non serialized / private vars
        [System.NonSerialized]
        public List<VectorCanvas> canvases = new List<VectorCanvas>();

        //public vars
        [Space(5)]
        [Header("Important variables to set")]
        [Space(5)]
        [Tooltip("Make sure to add any displays in the scene here")]
        public List<Display> displays = new List<Display>();
        [Tooltip("Make sure to add any input devices in the scene here")]
        public List<InputDevice> localInputDevices = new List<InputDevice>();
        [Tooltip("In build, canvases will autosave on applicationQuit and scenchange event.")]
        public bool autoSaveOnExit;
        [Tooltip("Max number of unique canvases stored")]
        public int MAX_CANVAS_COUNT;
        [Tooltip("The render area is where the meshs for the drawing is constructed and rendered, this var sets its location in the scene")]
        public Vector3 renderAreaOrigin;
        


        [Space(5)]
        [Header("Line Smoothing and compression parameters")]
        [Space(15)]
        [Tooltip("Minimum distance from the last drawn point before a new one is registered, this primarilly is used for performance but also helps a bit with smoothing.")]
        [Range(0f, 0.1f)]
        public float minDistanceDelta;
        [Tooltip("This is used for smoothing and refers to the angle between the last line segment and the new one [0-180]. " +
            "Recommended values are between 135 (high performance) and 170 (high fidelity). Warning, values >= 180 will not work and may cause infinite loops.")]
        [Range(90, 175)]
        public float minCurveAngle;
        [Tooltip("For each new line segment, if its angle to the previous line segment is lower than this then it is a cusp. This means that it will not do slope smoothing.")]
        [Range(0, 90)]
        public float maxCuspAngle;

        [Space(5)]
        [Header("Optimization Parameters")]
        [Space(15)]
        [Tooltip("How many points are allocated for a line at a time (ie. if this is 50 then every 50 points in a line, new space in memory would be allocated for the next 50. " +
            "This is an alternative to doing it every time a new point is allocated)")]
        public int lineDataStepSize;


        [Space(5)]
        [Header("Variables that don't need to be changed")]
        [Space(15)]
        public GameObject quadPrefab;
        public Shader depthShaderColor;
        public Shader depthShaderTexture;
        public GameObject canvasPrefab;
        public Transform canvasParent;




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
            

            network.localPlayer.inputDevices = new Dictionary<byte, InputDevice>();
            for (byte x = 0; x < localInputDevices.Count; x++) {
                InputDevice device = localInputDevices[x];
                network.localPlayer.inputDevices.Add(x, device);
                device.owner = network.localPlayer;
                device.deviceIndex = x;
				
            }

            SceneManager.activeSceneChanged += OnSceneChange;

		}
        

        private void Update() {
            hotkeys();
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            #if !UNITY_EDITOR
            if (autoSaveOnExit) {
                foreach (VectorCanvas canvas in canvases) {
                    saveImage(canvas.canvasId);
                }
            }
			#endif
        }

		
		private void OnApplicationQuit() {
            #if !UNITY_EDITOR
            if (autoSaveOnExit) {
                foreach (VectorCanvas canvas in canvases) {
                    saveImage(canvas.canvasId);
                }
            }
            #endif
		}


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
            else if (Input.GetKeyDown(KeyCode.V)) {
                stamp(stampTest, network.localPlayer, 0, new Color32(0, 0, 0, 255), .5f, .5f, 0, true);
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
                VectorLine currentLine = getLine(player, device, color, canvas, ref newLine);

                //got vector pos
                Vector3 drawPoint = new Vector3(x, 0, y);

                //delta compression
                bool validInput = deltaCompression(device, newLine, drawPoint);

                //add to that mesh and network
                if (validInput) {

                    //if this is not the first or second point in the line, we need to smooth the slope
                    if (currentLine.mesh.vertexCount > 2) {

                        //get angle between last line and new line (negative for left turn, positive for right turn)
                        float angle = 180f - Vector3.Angle(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint);
                        if (Vector3.Cross(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint).y < 0) angle *= -1;

                        slopeSmoothing(canvas, device, currentLine, pressure, angle, device.lastDrawPoint, drawPoint);
                    }
                    else canvas.addToLine(device, currentLine, drawPoint, pressure);

                    if (localInput) network.addToDataOutbox(endLine, color, x, y, pressure, canvasId, deviceIndex);
                }
            }
        }

        void stamp(Texture stampTex, NetworkedPlayer player, byte deviceIndex, Color32 color, float x, float y, byte canvasId, bool localInput) {
            
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

            //make stamp
            VectorStamp stamp = createStamp(stampTex, canvas, device, player);

            //got vector pos
            Vector3 drawPoint = new Vector3(x, 0, y);

            //make sure it renders in
            canvas.placeStamp(stamp, device, drawPoint);


        }

        VectorStamp createStamp(Texture stampTex, VectorCanvas canvas, InputDevice device, NetworkedPlayer player) {

            //line end check needed
            if (device.currentGraphic != null && (device.currentGraphic is VectorLine)) {
				endLineData(device);
            }

            //make obj
            //GameObject obj = Instantiate(stampTest) ;
            GameObject obj = new GameObject();
            obj.transform.parent = canvas.vectorParent;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one * 0.01f;

            //add mesh
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            MeshFilter mf = obj.AddComponent<MeshFilter>();
			Mesh currentMesh = generateStampQuad(1, 1);
            mf.mesh = currentMesh;



            //stamp data struct and player data structs
            VectorStamp currentStamp = new VectorStamp();
            currentStamp.mesh = currentMesh;
            currentStamp.owner = player.connectionId;
            currentStamp.obj = obj;
            currentStamp.mr = mr;
            device.currentGraphic = currentStamp;
            player.graphicIndexer++;
            currentStamp.index = player.graphicIndexer;
            currentStamp.deviceIndex = device.deviceIndex;
            player.graphics.Add(currentStamp);


            //set shader
            mr.material = new Material(depthShaderTexture);
			mr.material.mainTexture = stampTex;
			//mr.material.color = color;
			mr.material.renderQueue = canvas.renderQueueCounter;
            canvas.renderQueueCounter++;


            return currentStamp;
            
        }

		Mesh generateStampQuad(float width, float height) {

			Mesh m = new Mesh();

			m.vertices = new Vector3[4] {
				new Vector3(0, 0, 0),
				new Vector3(width, 0, 0),
				new Vector3(0, height, 0),
				new Vector3(width, height, 0)
			};
			m.triangles = new int[6] {
				// lower left triangle
				0, 2, 1,
				// upper right triangle
				2, 3, 1
			};
			m.normals = new Vector3[4]{
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward,
				-Vector3.forward
			};
			m.uv = new Vector2[4] {
				new Vector2(0,0),
				new Vector2(1,0),
				new Vector2(0,1),
				new Vector2(1,1)
			};

			return m; 

		}

        void endLineData(InputDevice device) {

            //turn off last mesh for render texture
            if (device.currentGraphic != null) {

                //reset the renderqueue so that the depth is set when the line is finished drawing (so that it doesnt shift when undos happen for example)
                VectorCanvas canvas = getCanvas(device.currentGraphic.canvasId);
                device.currentGraphic.obj.GetComponent<Renderer>().material.renderQueue = canvas.renderQueueCounter;
                canvas.renderQueueCounter++;

                //turn off and remove from curr
                device.currentGraphic.obj.GetComponent<MeshRenderer>().enabled = false;
                device.currentGraphic = null;
            }
            else {
                Debug.LogError("Line end event but theres no line to end (this could also be caused by draw calls that have 0 pressure)");
            }

        }

        //recursive method that, depending on the angle between the last line and the new line, will split the new line into two new lines to make the slope more gradual
        void slopeSmoothing(VectorCanvas canvas, InputDevice device, VectorLine currentLine, float pressure, float angle, Vector3 start, Vector3 end) {
            
            if (minCurveAngle >= 179.9f) {
                Debug.LogError("Error: Unresonable/impossible target smoothing angle, please adgust public variable that controls this");
                return;
            }

            //if angle is too large
            if (Mathf.Abs(angle) < minCurveAngle && Mathf.Abs(angle) > maxCuspAngle && currentLine.pointCount > 1) {

                //get the new angle that represents both angles in for the 2 new lines
                float newAngle = (3 * Mathf.Abs(angle) + 360) / 5 * (angle > 0 ? 1: -1);

                //get the new midpoint
                float length = Vector3.Distance(start, end);
                float midPointLength = length / (1 + Mathf.Tan(Mathf.Deg2Rad * Mathf.Abs(newAngle - angle)) / Mathf.Tan(Mathf.Deg2Rad * Mathf.Abs(newAngle - angle) / 2));
                Vector3 notSmoothedDir = (end - start).normalized;
                Vector3 midpointDisplacment = new Vector3(-notSmoothedDir.z, 0, notSmoothedDir.x) * midPointLength * (angle > 0 ? 1 : -1) * Mathf.Tan(Mathf.Deg2Rad * Mathf.Abs(newAngle - angle));
                Vector3 middle = start + (end - start).normalized * midPointLength + midpointDisplacment;

                //recursively call for each new line segment now that we split this one
                slopeSmoothing(canvas, device, currentLine, pressure, newAngle, start, middle);
                slopeSmoothing(canvas, device, currentLine, pressure, newAngle, middle, end);

            }

            //actually add triangles to the mesh
            else {
                canvas.addToLine(device, currentLine, end, pressure);
            }

        }

        VectorLine getLine(NetworkedPlayer player, InputDevice device, Color32 color, VectorCanvas canvas, ref bool newLine) {

            VectorLine currentLine;

            //if new line needed
            if (device.currentGraphic == null || !(device.currentGraphic is VectorLine)) {

                //make obj
                GameObject obj = new GameObject();
                obj.transform.parent = canvas.vectorParent;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                //add mesh
                MeshRenderer mr = obj.AddComponent<MeshRenderer>();
                MeshFilter mf = obj.AddComponent<MeshFilter>();
                Mesh currentMesh = new Mesh();
                mf.mesh = currentMesh;

                

                //vector line data struct and player data structs
                currentLine = new VectorLine();
                currentLine.mesh = currentMesh;
                currentLine.owner = player.connectionId;
                currentLine.obj = obj;
                currentLine.mr = mr;
                device.currentGraphic = currentLine;
                player.graphicIndexer++;
                currentLine.index = player.graphicIndexer;
                currentLine.deviceIndex = device.deviceIndex;
                player.graphics.Add(currentLine);


                //set shader
                mr.material = new Material(depthShaderColor);
                mr.material.color = color;
                mr.material.renderQueue = canvas.renderQueueCounter;
                canvas.renderQueueCounter++;

                newLine = true;

            }
            //if addition to existing line
            else {

                currentLine = (VectorLine)device.currentGraphic;

                newLine = false;
            }

            return currentLine;

        }

        bool deltaCompression(InputDevice device, bool newLine, Vector3 drawPoint) {

            bool validDistance = Vector3.Distance(drawPoint, device.lastDrawPoint) > minDistanceDelta;

            return newLine || validDistance;

        }
       

        public void undo(NetworkedPlayer player, bool localInput) {

            //get graphic
            if (player.graphics.Count == 0) return;
            VectorGraphic undid = player.graphics.Last();

            //remove from data
            player.graphics.RemoveAt(player.graphics.Count - 1);

            byte deviceIndex = undid.deviceIndex;

            //get canvas
            VectorCanvas canvas = getCanvas(undid.canvasId);

            //destroy graphic
            Destroy(undid.obj);

            //reset curr graphic
            InputDevice device;
            if ((device = player.inputDevices[deviceIndex]) == null) {
                Debug.LogError("Failed retreiving input device from undo event");
                return;
            }
            device.currentGraphic = null;

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


