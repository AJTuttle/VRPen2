using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

namespace VRPen {

    public class VectorDrawing : MonoBehaviour {

        //instance
        public static VectorDrawing s_instance;

        //non serialized / private vars
        [System.NonSerialized]
        public List<VectorCanvas> canvases = new List<VectorCanvas>();
        
        //[Tooltip("Input devices here will automatically be added to code base, any local devices not here will need to be added using addLocalInputDevice()")]
        [System.NonSerialized]
        public List<InputVisuals> inputDevices = new List<InputVisuals>();
        [System.NonSerialized]
        public List<SharedMarker> sharedDevices = new List<SharedMarker>();
        [System.NonSerialized]
        public List<Display> displays = new List<Display>();
        [System.NonSerialized]
        public List<VectorGraphic> undoStack = new List<VectorGraphic>();
        

        const float PRESSURE_UPDATE_MINIMUM_DELTA = 0.01f; //the min amount that pressure has to change to be considered worth updated the mesh

        //public vars
        [Space(5)] [Header("Important variables to set")] [Space(5)]
        [Tooltip("StartInOfflineMode")]
        public bool startInOfflineMode;
        [Tooltip("A constant background that gets spawned with certain canvas IDs (the canvas ID is the index of this array). Don't fret if this list doesnt match the length of canvases.")]
        public Texture[] canvasBackgrounds;
        [Tooltip("In build, canvases will autosave on applicationQuit and scenechange event.")]
        public bool autoSavePNGOnExit;
        [Tooltip("Max number of unique canvases stored")]
        public int MAX_CANVAS_COUNT;
        [Tooltip("The render area is where the meshs for the drawing is constructed and rendered, this var sets its location in the scene")]
        public Vector3 renderAreaOrigin;
        [Tooltip("Pressure Curve")]
        public AnimationCurve pressureCurve;
        [Tooltip("Are default hotkeys enabled")]
        public bool enableHotkeys;


        [Space(5)]
        [Header("Line Smoothing and compression parameters")]
        [Space(15)]
        [Tooltip("Minimum distance from the last drawn point before a new one is registered, this primarilly is used for performance but also helps a bit with smoothing.")]
        [Range(0f, 0.1f)]
        public float minDistanceDelta;
        [Tooltip("For each new line segment, if its angle to the previous line segment is higher than this then it is a cusp. This means that it will not rotate the last point. Recommended values between 110 and 160.")]
        [Range(90, 180)]
        public float minCuspAngle;

        [Space(5)]
        [Header("Optimization Parameters")]
        [Space(15)]
        [Tooltip("How many points are allocated at a time in the line data structure (ie. if this is 50 then every 50 points in a line, 50 new spaces will be allocated. " +
            "This is used to avoid having to copy array values into a new longer array every single time a new point is allocated)")]
        public int lineDataStepSize;


        [Space(5)]
        [Header("Variables that don't need to be changed")]
        [Space(15)]
        public GameObject quadPrefab;
        public Shader depthShaderColor;
        public Shader depthShaderTexture;
        public GameObject canvasPrefab;
        public Transform canvasParent;
        public GameObject textStampPrefab;


        //acting as a remote client restricts the things that the client can do. Things like making new canvases.
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


        private void Awake() {
            //set instnace
            s_instance = this;
            
            //start in offline mode
            if (startInOfflineMode) offlineMode = true;
        }

        private void Start() {
            
            //scene management
            SceneManager.activeSceneChanged += OnSceneChange;

            //init displays
            StartCoroutine(initializeDisplays());

        }

        IEnumerator initializeDisplays() {
            
            //wait one frame to make sure that all displays have added themselves to display list
            yield return null;
            
            //do it in order based on display ID's to make sure that player's canvases allign with correct displays
            displays.Sort((a, b) => a.uniqueIdentifier.CompareTo(b.uniqueIdentifier));
            for (int x = 0; x < displays.Count; x++) {
                displays[x].init();
            }
            
        }
        

        private void Update() {
            if (enableHotkeys) hotkeys();
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
            //if (Input.GetKeyDown(KeyCode.U)) {
            //    undo(network.getLocalPlayer(), true);
            //}
            if (Input.GetKeyDown(KeyCode.C)) {
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
            return displays.Find(x => x.uniqueIdentifier == displayId);
        }

        public int getCanvasListCount() {
            return canvases.Count;
        }

        public void endLineEvent(ulong playerId, int graphicIndex, byte canvasId, bool localInput) {
            draw(playerId, graphicIndex, true, Color.black, 0, 0, 0, canvasId, localInput);
        }

        public void draw(ulong playerId, int graphicIndex, bool endLine, Color32 color, float x, float y, float pressure, byte canvasId, bool localInput) {

            //get canvas
            VectorCanvas canvas = getCanvas(canvasId);
            if (canvas == null) {
                Debug.LogError("No canvas found for draw input");
                return;
            }

            //fix pressure
            if (localInput) pressure = pressureCurve.Evaluate(pressure);
            
            //end line or draw
            if (endLine) {

                endLineData(playerId, graphicIndex, canvas);
                if (localInput) NetworkManager.s_instance.addToDataOutbox(endLine, color, x, y, pressure, canvasId, graphicIndex);
                
            }
            else {

                //get line
                VectorGraphic currentGraphic = canvas.findGraphic(playerId, graphicIndex);
                if (currentGraphic == null) currentGraphic = createNewLine(playerId, color, graphicIndex, canvas, localInput);
                if (!(currentGraphic is VectorLine)) {
                    Debug.LogError("Trying tp draw onto a non-line graphic");
                    return;
                }
                VectorLine currentLine = (VectorLine)currentGraphic;

                //got vector pos
                Vector3 drawPoint = new Vector3(x, 0, y);

                //delta compression
                bool validInput = deltaCompression(currentLine, drawPoint);

                //add to that mesh and network or just show a line prediction
                if (validInput) {
                    
                    //get angle between last line and new line (negative for left turn, positive for right turn)
                    float angle = Vector3.Angle(currentLine.lastDrawPoint - currentLine.secondLastDrawPoint, drawPoint - currentLine.lastDrawPoint);
                    bool isCusp = angle >= minCuspAngle;

                    if (currentLine.pointCount < 2) isCusp = true;

                    //add to line
                    canvas.addToLine(currentLine, drawPoint, pressure, !isCusp);

                    //network it
                    if (localInput) NetworkManager.s_instance.addToDataOutbox(endLine, color, x, y, pressure, canvasId, graphicIndex);

                }
                else {

                    //if the pressure is greater then thicken the last point
                    if (pressure > currentLine.lastPressure + PRESSURE_UPDATE_MINIMUM_DELTA) {
                        canvas.updatePointThickness(currentLine, pressure);

                        //network it
                        if (localInput) NetworkManager.s_instance.addToDataOutbox(endLine, color, x, y, pressure, canvasId, graphicIndex);
                    }

                    //to do add line prediction stuff
                    if (currentLine.pointCount > 0) canvas.updateLinePrediction(currentLine, drawPoint, pressure);
                }
            }
        }

        //non networked stamps use a stamp index of -1 (for example when stamping is used for the background)
        public VectorStamp stamp(StampType type, string text, Texture stampTex, int stampIndex, ulong ownerId, int graphicIndex, float x, float y, float size, float rotation, byte canvasId, bool localInput) {
            
            //get canvas
            VectorCanvas canvas = getCanvas(canvasId);
            if (canvas == null) {
                Debug.LogError("No canvas found for draw input");
                return null;
            }

            //make stamp
            VectorStamp stamp;
            if (type == StampType.image) {
                stamp = createStamp(stampTex, canvas, ownerId, graphicIndex, size, rotation, localInput);
            }
            else {
                stamp = createStamp(text, canvas, ownerId, graphicIndex, size, rotation, localInput);
            }

            //got vector pos
            Vector3 drawPoint = new Vector3(x, 0, y);

            //make sure it renders in
            canvas.placeStamp(stamp, drawPoint);

            //network
            if (localInput) {
                if (stampIndex == -1) Debug.LogError("Tried to network a non-networkable stamp (stamp index == -1)");
                else NetworkManager.s_instance.sendStamp(stampIndex, x, y, size, rotation, canvasId, graphicIndex);
            }
            
            //return
            return stamp;

        }

        VectorStamp createStamp(Texture stampTex, VectorCanvas canvas, ulong ownerId, int graphicIndex, float size, float rotation, bool isLocal) {

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
            currentStamp.type = StampType.image;
            currentStamp.mesh = currentMesh;
			currentStamp.obj = obj;
            currentStamp.mr = mr;
            currentStamp.createdLocally = isLocal;
            currentStamp.ownerId = ownerId;
            currentStamp.localIndex = graphicIndex;
            canvas.graphics.Add(currentStamp);


            //set shader
            mr.material = new Material(depthShaderTexture);
			mr.material.mainTexture = stampTex;
			//mr.material.color = color;
			mr.material.renderQueue = canvas.renderQueueCounter;
            canvas.renderQueueCounter++;


            return currentStamp;

        }

        VectorStamp createStamp(string text, VectorCanvas canvas, ulong ownerId,
            int graphicIndex, float size, float rotation, bool isLocal) {
            //make obj
            //GameObject obj = Instantiate(stampTest) ;
            GameObject obj = GameObject.Instantiate(textStampPrefab);
            obj.transform.parent = canvas.vectorParent;
            obj.transform.localPosition = Vector3.zero;

            //convert from [0,1] to [-180,180]
            float rotValue = rotation* 360;
            rotValue -= 180;

            obj.transform.localRotation = Quaternion.Euler(90,rotValue,0);
            obj.transform.localScale = obj.transform.localScale * size * 0.01f;

            
            //stamp data struct and player data structs
            VectorStamp currentStamp = new VectorStamp();
            currentStamp.type = StampType.text;
            currentStamp.mesh = null;
            currentStamp.obj = obj;
            currentStamp.mr = obj.GetComponent<MeshRenderer>();
            currentStamp.createdLocally = isLocal;
            currentStamp.ownerId = ownerId;
            currentStamp.localIndex = graphicIndex;
            canvas.graphics.Add(currentStamp);

            
            //tmp
            TextMeshPro tmp = obj.GetComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontMaterial.renderQueue = canvas.renderQueueCounter;
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

        void endLineData(ulong connectionId, int graphicIndex, VectorCanvas canvas) {
            
            //find graphic
            VectorGraphic currentGraphic = canvas.findGraphic(connectionId, graphicIndex);

            //turn off last mesh for render texture
            if (currentGraphic != null) {

                //if line only has one point turn it into a dot
                if (((VectorLine)currentGraphic).pointCount <= 2) {
                    canvas.turnLineIntoDot((VectorLine)currentGraphic);
                }

                //reset the renderqueue so that the depth is set when the line is finished drawing (so that it doesnt shift when undos happen for example)
                currentGraphic.obj.GetComponent<Renderer>().material.renderQueue = canvas.renderQueueCounter;
                canvas.renderQueueCounter++;

                //turn off and remove from curr after a frame
                //StartCoroutine(canvas.renderGraphic(device.currentGraphic, device));

                //instead of just rendering the 1 line, we rerender the whole canvas so that we can make use of various post processed effects
                //also make sure to edit lock the graphic so that after the render ends it cant be edited.
                currentGraphic.editLock = true;
                currentGraphic = null;
                StartCoroutine(canvas.rerenderCanvas());

            }
            else {
                Debug.LogError("Line end event but theres no line to end (this could also be caused by draw calls that have 0 pressure)");
            }

        }
        
        public VectorLine createNewLine(ulong playerId, Color32 color, int graphicIndex, VectorCanvas canvas, bool isLocal) {
            
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
            VectorLine currentLine = new VectorLine();
            currentLine.mesh = currentMesh;
            currentLine.ownerId = playerId;
            currentLine.createdLocally = isLocal;
            currentLine.localIndex = graphicIndex;
            currentLine.obj = obj;
            currentLine.mr = mr;
            currentLine.canvasId = canvas.canvasId;
            canvas.graphics.Add(currentLine);

            //set shader
            mr.material = new Material(depthShaderColor);
            mr.material.color = color;
            mr.material.renderQueue = canvas.renderQueueCounter;
            canvas.renderQueueCounter++;

            //return
            return currentLine;
            
        }

        bool deltaCompression(VectorLine currentLine, Vector3 drawPoint) {

            bool validDistance = Vector3.Distance(drawPoint, currentLine.lastDrawPoint) > minDistanceDelta;

            return currentLine.pointCount == 0 || validDistance;

        }
       

        public void undo(ulong playerId, int graphicIndex, byte canvasId, bool localInput) {

            //get canvas
            VectorCanvas canvas = getCanvas(canvasId);
            
            //get graphic
            VectorGraphic undid = canvas.graphics.Find(x => x.ownerId == playerId && x.localIndex == graphicIndex);
            canvas.graphics.Remove(undid);
            
            //if local undo then remove from undo queue
            if (localInput) {
                //foreach (InputVisuals input in inputDevices) {
                //    if (input is VRPenInput) ((VRPenInput)input).undoStack.Remove(undid);
                //}
                undoStack.Remove(undid);
            }

            //destroy graphic
            Destroy(undid.obj);

            //rerender rest
            StartCoroutine(canvas.rerenderCanvas());

            //network
            if (localInput) NetworkManager.s_instance.sendUndo(playerId, graphicIndex, canvasId);
            
        }

        
        public void addCanvas(bool localInput, byte originDisplayID, int pixelWidth, int pixelHeight, bool isPreset, byte canvasId) {
            
            //ERROR CASES
            //already exists error
            if (getCanvas(canvasId) != null) {
                Debug.LogError("Canvas addition event for canvas ID that already exists");
                return;
            }
            //if too many canvses
            if (getCanvasListCount() >= MAX_CANVAS_COUNT) {
                Debug.LogWarning("Failed to add a canvas when max canvas count reached already");
                return;
            }
            

            //make canvas
            VectorCanvas temp = GameObject.Instantiate(canvasPrefab, canvasParent).GetComponent<VectorCanvas>();
            temp.instantiate(this, NetworkManager.s_instance, canvasId, pixelWidth, pixelHeight);
            temp.GetComponent<Renderer>().enabled = false;

            //add to list
            canvases.Add(temp);
            
            //make copys of texture
            foreach (Display display in displays) {
                
                //determing if display can show canvas
                if (display.fullAccess || display.uniqueIdentifier == originDisplayID) {
                    
                    //make obj
                    GameObject quad = Instantiate(quadPrefab);
                    Destroy(quad.GetComponent<Collider>());
                    quad.transform.parent = display.canvasParent;
                    quad.transform.localPosition = Vector3.zero;
                    quad.transform.localRotation = Quaternion.identity;
                    quad.transform.localScale = Vector3.one;
                    quad.name = "" + canvasId;
                    quad.GetComponent<Renderer>().material = temp.GetComponent<Renderer>().material;
                    if (display.shaderOverride != null)
                        quad.GetComponent<Renderer>().material.shader = display.shaderOverride;

                    //turn off renderer
                    quad.GetComponent<Renderer>().enabled = false;

                    //update ui
                    display.UIMan.addCanvas(canvasId);

                    //add the actual obj to display's list
                    display.canvasObjs.Add(canvasId, quad);

                    //if theres not canvas on the display, switch
                    if (display.currentLocalCanvas == null) display.swapCurrentCanvas(canvasId, false);
                    
                }

            }

            //if local
            if (localInput && !isPreset) {

                //network it (make sure you dont send the default board)
                NetworkManager.s_instance.sendCanvasAddition(originDisplayID, pixelWidth, pixelHeight, isPreset, canvasId);
            }
            
        }

        public void addCanvas(bool localInput, byte originDisplayID, int pixelWidth, int pixelHeight, bool isPreset) {
            addCanvas(localInput, originDisplayID, pixelWidth, pixelHeight, isPreset, (byte)getCanvasListCount());
        }

        public void saveImage(byte canvasId) {
			TextureSaver.export(getCanvas(canvasId).renderTexture);
		}


    }

}


