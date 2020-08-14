﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace VRPen {

    public class VectorDrawing : MonoBehaviour {



        //scripts
        StarTablet tablet;
        NetworkManager network;

        //non serialized / private vars
        [System.NonSerialized]
        public List<VectorCanvas> canvases = new List<VectorCanvas>();

        [System.NonSerialized]
        public int initialPublicCanvasId = -1;
        [System.NonSerialized]
        public const byte INITIAL_PUBLIC_CANVAS_DISPLAY_ID = 255;

        //public vars
        [Space(5)]
        [Header("       Important variables to set")]
        [Space(5)]
        [Tooltip("Make sure to add any displays in the scene here")]
        public List<Display> displays = new List<Display>();
        [Tooltip("Input devices here will automatically be added to code base, any local devices not here will need to be added using addLocalInputDevice()")]
        public List<VRPenInput> localInputDevices = new List<VRPenInput>();
        [Tooltip("A constant background that gets spawned with certain canvas IDs (the canvas ID is the index of this array). Don't fret if this list doesnt match the length of canvases.")]
        public Texture[] canvasBackgrounds;
        [Tooltip("In build, canvases will autosave on applicationQuit and scenechange event.")]
        public bool autoSavePNGOnExit;
        [Tooltip("Max number of unique canvases stored")]
        public int MAX_CANVAS_COUNT;
        [Tooltip("The render area is where the meshs for the drawing is constructed and rendered, this var sets its location in the scene")]
        public Vector3 renderAreaOrigin;
        [Tooltip("Width of inital public canvas")]
        public int initalPublicCanvasPixelWidth;
        [Tooltip("Height of initial public canvas")]
        public int initalPublicCanvasPixelHeight;
        public float initalPublicCanvasAspectRatio {
            get {
                return ((float)initalPublicCanvasPixelWidth / (float)initalPublicCanvasPixelHeight);
            }
        }


        [Space(5)]
        [Header("       Line Smoothing and compression parameters")]
        [Space(15)]
        [Tooltip("Minimum distance from the last drawn point before a new one is registered, this primarilly is used for performance but also helps a bit with smoothing.")]
        [Range(0f, 0.1f)]
        public float minDistanceDelta;
        [Tooltip("For each new line segment, if its angle to the previous line segment is higher than this then it is a cusp. This means that it will not rotate the last point. Recommended values between 110 and 160.")]
        [Range(90, 180)]
        public float minCuspAngle;

        [Space(5)]
        [Header("       Optimization Parameters")]
        [Space(15)]
        [Tooltip("How many points are allocated at a time in the line data structure (ie. if this is 50 then every 50 points in a line, 50 new spaces will be allocated. " +
            "This is used to avoid having to copy array values into a new longer array every single time a new point is allocated)")]
        public int lineDataStepSize;


        [Space(5)]
        [Header("       Variables that don't need to be changed")]
        [Space(15)]
        public GameObject quadPrefab;
        public Shader depthShaderColor;
        public Shader depthShaderTexture;
        public GameObject canvasPrefab;
        public Transform canvasParent;

        [System.NonSerialized]
        public InputDevice facilitativeDevice;

        //acting as a remote client restricts the things that the client can do. Things like making new canvases.
        public static bool actSynchronously = true;
        private static bool offlineMode = false;
        public static bool OfflineMode {
            get {
                return offlineMode;
            }
            set {
                if (offlineMode && !value) {
                    Debug.LogError("Cannot return to online mode after being in offline mode");
                }
                else offlineMode = value;
            }
        } 




        private void Start() {
            

            //get scripts
            tablet = GetComponent<StarTablet>();
            network = GetComponent<NetworkManager>();


			//set up deisplay ids
			for(byte x = 0; x < displays.Count; x++) {
				displays[x].DisplayId = x;
                displays[x].init();
			}
            

			//setup input devices
            network.getLocalPlayer().inputDevices = new Dictionary<byte, InputDevice>();
			
            //add local devices in list
            foreach(VRPenInput device in localInputDevices) {
                addLocalInputDevice(device);
            }

			//set up input device for fascilitative inputs that shouldnt be editable (import background etc.)
			facilitativeDevice = new InputDevice();
			facilitativeDevice.type = InputDevice.InputDeviceType.Facilitative;
			facilitativeDevice.owner = null;
			facilitativeDevice.deviceIndex = 255;

            SceneManager.activeSceneChanged += OnSceneChange;

		}
        

        private void Update() {
            hotkeys();
        }

        //Todo: add networking to this so that they can be added after connecting
        public void addLocalInputDevice(VRPenInput inputDevice) {
            if (network.sentConnect) {
                Debug.LogError("Input device addition denied: Adding a local input device after NetworkManager.sendConnect() has been called will cause errors for multiplayer (other users dont get the instantiation)");
                return;
            }
            else {
                Debug.Log("Adding local input device (this must happen before NetworkManager.sendConnect() is called)");

                NetworkedPlayer localPlayer = network.getLocalPlayer();
                InputDevice device = new InputDevice();
                byte deviceIndex = (byte)localPlayer.inputDevices.Count;

                inputDevice.deviceData = device;
                device.type = inputDevice.deviceType;
                localPlayer.inputDevices.Add(deviceIndex, device);
                device.owner = network.getLocalPlayer();
                device.deviceIndex = deviceIndex;
                device.visuals = inputDevice;
                
            }
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            #if !UNITY_EDITOR
            if (autoSavePNGOnExit) {
                foreach (VectorCanvas canvas in canvases) {
                    saveImage(canvas.canvasId);
                }
            }
			#endif
        }

		
		private void OnApplicationQuit() {
            #if !UNITY_EDITOR
            if (autoSavePNGOnExit) {
                foreach (VectorCanvas canvas in canvases) {
                    saveImage(canvas.canvasId);
                }
            }
            #endif
		}


        void hotkeys() {
            //hotkeys
            if (Input.GetKeyDown(KeyCode.U)) {
                undo(network.getLocalPlayer(), true);
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
			if (player == null && deviceIndex == facilitativeDevice.deviceIndex) {
				device = facilitativeDevice;
			}
			else if ((device = player.inputDevices[deviceIndex]) == null) {
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

                //add to that mesh and network or just show a line prediction
                if (validInput) {
                    
                    //get angle between last line and new line (negative for left turn, positive for right turn)
                    float angle = Vector3.Angle(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint);
                    bool isCusp = angle >= minCuspAngle;

                    if (currentLine.pointCount < 2) isCusp = true;

                    //add to line
                    canvas.addToLine(device, currentLine, drawPoint, pressure, !isCusp);

                    //network it
                    if (localInput) network.addToDataOutbox(endLine, color, x, y, pressure, canvasId, deviceIndex);

                }
                else {
                    //to do add line prediction stuff
                    if (currentLine.pointCount > 0) canvas.updateLinePrediction(device, currentLine, drawPoint, pressure);
                }
            }
        }

        //non networked stamps use a stamp index of -1 (for example when stamping is used for the background)
        public void stamp(Texture stampTex, int stampIndex, NetworkedPlayer player, byte deviceIndex, float x, float y, float size, float rotation, byte canvasId, bool localInput) {
            
            //get canvas
            VectorCanvas canvas = getCanvas(canvasId);
            if (canvas == null) {
                Debug.LogError("No canvas found for draw input");
                return;
            }

            //get device
            InputDevice device;
			if (player == null && deviceIndex == facilitativeDevice.deviceIndex) {
				device = facilitativeDevice;
			}
            else if ((device = player.inputDevices[deviceIndex]) == null) {
                Debug.LogError("Failed retreiving input device");
                return;
            }

            //make stamp
            VectorStamp stamp = createStamp(stampTex, canvas, device, player, size, rotation);

            //got vector pos
            Vector3 drawPoint = new Vector3(x, 0, y);

            //make sure it renders in
            canvas.placeStamp(stamp, device, drawPoint);

            //network
            if (localInput) {
                if (stampIndex == -1) Debug.LogError("Tried to network a non-networkable stamp (stamp index == -1)");
                else network.sendStamp(stampIndex, x, y, size, rotation, canvasId, deviceIndex);
            }

        }

        VectorStamp createStamp(Texture stampTex, VectorCanvas canvas, InputDevice device, NetworkedPlayer player, float size, float rotation) {

            //line end check needed
            if (device.currentGraphic != null && (device.currentGraphic is VectorLine)) {
				endLineData(device);
            }

            //make obj
            //GameObject obj = Instantiate(stampTest) ;
            GameObject obj = new GameObject();
            obj.transform.parent = canvas.vectorParent;
            obj.transform.localPosition = Vector3.zero;

			//convert from [0,1] to [-180,180]
			float rotValue = rotation* 360;
			rotValue -= 180;

			obj.transform.localRotation = Quaternion.Euler(0,rotValue,0);
            obj.transform.localScale = obj.transform.localScale * size;

            //add mesh
            MeshRenderer mr = obj.AddComponent<MeshRenderer>();
            MeshFilter mf = obj.AddComponent<MeshFilter>();
			Mesh currentMesh = generateStampQuad((float)stampTex.width/stampTex.height, 1);
            mf.mesh = currentMesh;



            //stamp data struct and player data structs
            VectorStamp currentStamp = new VectorStamp();
            currentStamp.mesh = currentMesh;
			if (player != null) {
				currentStamp.owner = player.connectionId;
				player.graphicIndexer++;
				currentStamp.index = player.graphicIndexer;
				player.graphics.Add(currentStamp);
			}
			currentStamp.obj = obj;
            currentStamp.mr = mr;
            device.currentGraphic = currentStamp;
            
            currentStamp.deviceIndex = device.deviceIndex;


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
                new Vector3(-width/2, 0, -height/2),    //bot left
				new Vector3(width/2, 0, -height/2),     //bot right
				new Vector3(-width/2, 0, height/2),     //top left
				new Vector3(width/2, 0, height/2)       //top right
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

                //get canvas
                VectorCanvas canvas = getCanvas(device.currentGraphic.canvasId);

                //if line only has one point turn it into a dot
                if (((VectorLine)device.currentGraphic).pointCount == 1) {
                    canvas.turnLineIntoDot(device, (VectorLine)device.currentGraphic);
                }

                //reset the renderqueue so that the depth is set when the line is finished drawing (so that it doesnt shift when undos happen for example)
                device.currentGraphic.obj.GetComponent<Renderer>().material.renderQueue = canvas.renderQueueCounter;
                canvas.renderQueueCounter++;

                //turn off and remove from curr after a frame
                //StartCoroutine(canvas.renderGraphic(device.currentGraphic, device));

                //instead of just rendering the 1 line, we rerender the whole canvas so that we can make use of various post processed effects
                device.currentGraphic = null;
                StartCoroutine(canvas.rerenderCanvas());

            }
            else {
                Debug.LogError("Line end event but theres no line to end (this could also be caused by draw calls that have 0 pressure)");
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
                currentLine.canvasId = canvas.canvasId;
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

        //if originDisplay is null, this is the initial public canvas
        //if origindisplayID is 255, it is the initial public canvas
        //the canvas id should only be used for remote additions (255 is null state)
        public void addCanvas(bool localInput, bool isPublic, byte originDisplayID, int pixelWidth, int pixelHeight, bool isPreset, byte canvasId) {

            //if there is no canvasId, give it the most recent
            if (canvasId == 255) canvasId = (byte)canvases.Count;

            if (getCanvas(canvasId) != null) return;

            //if the canvas addition is a remote preset, ignore it if not acting async.
            if (!localInput && isPreset && actSynchronously) {
                return;
            }

            //ERROR CASES
            //if too many canvses
            if (getCanvasListCount() >= MAX_CANVAS_COUNT) {
                Debug.LogWarning("Failed to add a canvas when max canvas count reached already");
                return;
            }
            //if a canvas is added as the inital public canvs, but one has already been instantiated
            if (originDisplayID == INITIAL_PUBLIC_CANVAS_DISPLAY_ID && initialPublicCanvasId != -1) {
                Debug.LogWarning("Failed to add a canvas when there is no originDisplay (the only time this works is when the initial public canvas is added)");
                return;
            }
            

            //if this is the inital public canvas
            if (originDisplayID == INITIAL_PUBLIC_CANVAS_DISPLAY_ID) {
                initialPublicCanvasId = canvasId;
            }

            //make canvas
            VectorCanvas temp = GameObject.Instantiate(canvasPrefab, canvasParent).GetComponent<VectorCanvas>();
            temp.instantiate(this, network, canvasId, pixelWidth, pixelHeight);
            temp.GetComponent<Renderer>().enabled = false;
            temp.isPublic = isPublic;

            
            //add to list
            canvases.Add(temp);
            
            //make copys of texture
            foreach (Display display in displays) {
                
                if (!isPublic && display.DisplayId != originDisplayID && !display.fullAccess) continue;

	            GameObject quad = Instantiate(quadPrefab);
				Destroy(quad.GetComponent<Collider>());
                quad.transform.parent = display.canvasParent;
                quad.transform.localPosition = Vector3.zero;
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = Vector3.one;
                quad.name = "" + canvasId;
                quad.GetComponent<Renderer>().material = temp.GetComponent<Renderer>().material;
                if (display.shaderOverride != null) quad.GetComponent<Renderer>().material.shader = display.shaderOverride;

                //turn off renderer
                quad.GetComponent<Renderer>().enabled = false;

                //update ui
                display.UIMan.addCanvas(canvasId);

                //add the actual obj to display's list
                display.canvasObjs.Add(canvasId, quad);

                //if theres not canvas on the display, switch
                if (display.currentLocalCanvas == null) display.swapCurrentCanvas(canvasId, false);
                
            }

            //if local
            if (localInput) {

                //network it (make sure you dont send the default board)
                network.sendCanvasAddition(isPublic, originDisplayID, pixelWidth, pixelHeight, isPreset,canvasId);
            }
            
        }

        public void addCanvas(bool localInput, bool isPublic, byte originDisplayID, int pixelWidth, int pixelHeight, bool isPreset) {
            addCanvas(localInput, isPublic, originDisplayID, pixelWidth, pixelHeight, isPreset, 255);
        }

        public void saveImage(byte canvasId) {
			TextureSaver.export(getCanvas(canvasId).renderTexture);
		}


    }

}


