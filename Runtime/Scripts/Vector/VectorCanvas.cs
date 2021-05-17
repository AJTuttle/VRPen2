 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class VectorCanvas : MonoBehaviour {

        VectorDrawing drawingMan;
        NetworkManager network;
        
        [System.NonSerialized]
        public List<VectorGraphic> graphics = new List<VectorGraphic>();

        public byte canvasId;
        public int renderQueueCounter = 1;
        public float aspectRatio;
        
        public bool isPublic;

        //render texture stuff
        public RenderTexture renderTexturePresets;
        public GameObject renderingPrefab;
        Camera renderCam;
        public Color32 bgColor;
        public Transform vectorParent;
		public RenderTexture renderTexture;

        const float PRESSURE_MULTIPLIER = 0.01f;


		public void instantiate(VectorDrawing man, NetworkManager net, byte id, int width, int height) {

            //set vars
            network = net;
            drawingMan = man;
            canvasId = id;
            aspectRatio = ((float)width) / ((float)height);

            //set up the render texture stuff
            Transform renderArea = GameObject.Instantiate(renderingPrefab, drawingMan.renderAreaOrigin + new Vector3(0,0,canvasId*2), Quaternion.identity).transform;
            renderCam = renderArea.GetChild(0).GetComponent<Camera>();

            //instance material and make the render texture pipe to it
            Material mat = GetComponent<Renderer>().material;
			renderTexture = new RenderTexture(renderTexturePresets);
            renderTexture.width = width;
            renderTexture.height = height;
            mat.mainTexture = renderTexture;
            renderCam.targetTexture = renderTexture;
            vectorParent = renderArea.GetChild(1);

            //fill canvas bg color
            StartCoroutine(fillboard());
        }

        public VectorGraphic findGraphic(ulong ownerId, int localIndex) {
            return graphics.Find(x => x.ownerId == ownerId && x.localIndex == localIndex);
        }

        //think of the normal as if it was ribbon drawing (it is internal used to order the vertices such that they connect to previous ones properlly)
        public void addToLine(VectorLine currentLine, Vector3 pos, float pressure, bool rotateLastSegment) {

            //normal is constant since render area doesnt move
            Vector3 normal = Vector3.up;
            

            #region errors

            //graphic is edit locked
            if (currentLine.editLock) {
                VRPen.Debug.LogError("Tryed to edit a graphic that is edit-locked");
                return;
            }
            
            //if this isnt the first point, make sure that the direction of the line is not the same as the normal
            if (currentLine.pointCount != 0) {
                Vector3 dir = pos - currentLine.lastDrawPoint;
                if (dir.Equals(normal)) {
                    Debug.LogError("The normal of a line segment addition was in the same dirrection as the line which is not possible to render");
                    return;
                }
            }
            

            if (rotateLastSegment && currentLine.pointCount < 2) {
                Debug.LogWarning("Shouldnt try to rotate the last line segment if there are less than 2 segments");
                rotateLastSegment = false;
            }
            

            #endregion

            //compute angle data
            float angle = 0;
            if (currentLine.pointCount >= 2) angle = Vector3.Angle(pos - currentLine.lastDrawPoint, currentLine.lastDrawPoint - currentLine.secondLastDrawPoint);
            bool isCusp = angle > 90 && !rotateLastSegment;

            #region vertices

            Vector3[] oldVerts = currentLine.mesh.vertices;

            Vector3[] verts = new Vector3[currentLine.pointCount * 2 + 2];

            //if I need to rotate the last segment, do that
            if (rotateLastSegment) {
                //recalulate

                //get perpindicular vectors
                Vector3 dir1 = (pos - currentLine.lastDrawPoint).normalized;
                Vector3 dir2 = (currentLine.lastDrawPoint - currentLine.secondLastDrawPoint).normalized;
                Vector3 dir = dir1 + dir2;
                Vector3 perp = Vector3.Cross(dir, normal).normalized;
                if (currentLine.flipVerts) perp = -perp;

                //make a list of points that will be ordered into the vert array later
                List<Vector3> addedVerts = new List<Vector3>() {
                    currentLine.lastDrawPoint + perp * currentLine.lastPressure * PRESSURE_MULTIPLIER,
                    currentLine.lastDrawPoint + -perp * currentLine.lastPressure * PRESSURE_MULTIPLIER,
                };
                for (int x = 0; x < 2; x++) {
                    oldVerts[currentLine.pointCount * 2 - 2 + x] = addedVerts[x];
                }

            }
            
            for (int x = 0; x < currentLine.pointCount * 2; x++) {
                verts[x] = oldVerts[x];
            }
            
            //first point (all p[oints at same point
            if (currentLine.pointCount == 0) {
                for (int x = 0; x < 2; x++) {
                    verts[x] = pos;
                }
            }
            //not first point
            else {

                //get perpindicular vectors
                Vector3 dir = pos - currentLine.lastDrawPoint;
                Vector3 perp = Vector3.Cross(dir, normal).normalized;

                //when theres a 90+ degree turn we need to flip the verts
                if (isCusp) {
                    currentLine.flipVerts = !currentLine.flipVerts;
                    //Debug.Log("flip: " + angle);
                }
                if (currentLine.flipVerts) perp = -perp;



                //make a list of points that will be ordered into the vert array later
                List<Vector3> addedVerts = new List<Vector3>() {
                    pos + perp * pressure * PRESSURE_MULTIPLIER,
                    pos + -perp * pressure * PRESSURE_MULTIPLIER,
                };
                for (int x = 0; x < 2; x++) {
                    verts[currentLine.pointCount * 2 + x] = addedVerts[x];
                }


            }


            currentLine.mesh.SetVertices(verts);

            #endregion

            #region indices

            if (currentLine.pointCount == 0) {
                currentLine.indices = new List<int>();
            }
            else {

                if (!currentLine.flipVerts) {
                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 0);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);

                    currentLine.indices.Add(currentLine.pointCount * 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);
                }

                else {
                    currentLine.indices.Add(currentLine.pointCount * 2 + 0);
                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);

                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);

                }

            }


            currentLine.mesh.SetIndices(currentLine.indices.ToArray(), MeshTopology.Triangles, 0);

            #endregion


            //update vars
            currentLine.pointCount++;
            if (currentLine.pointCount > 1) currentLine.secondLastDrawPoint = currentLine.lastDrawPoint;
            currentLine.lastDrawPoint = pos;
            currentLine.lastPressure = pressure;
            

            //turn off prediction
            //toggleLinePrediction(line, false);

        }

        public void turnLineIntoDot(VectorLine currentLine) {

            //graphic is edit locked
            if (currentLine.editLock) {
                VRPen.Debug.LogError("Tryed to edit a graphic that is edit-locked");
                return;
            }
            
            //normal is constant since render area doesnt move
            Vector3 normal = Vector3.up;


            #region errors

            #endregion



            #region vertices

            Vector3[] oldVerts = currentLine.mesh.vertices;

            Vector3 midPoint;
            if (currentLine.pointCount == 1) {
                midPoint = currentLine.lastDrawPoint;
            }
            else {
                midPoint = currentLine.lastDrawPoint + (currentLine.secondLastDrawPoint-currentLine.lastDrawPoint)/ 2;
            }
            Vector3 side = Vector3.Cross(new Vector3(1, 1, 1), normal).normalized;
            Vector3 up = Vector3.Cross(side, normal).normalized;
            float pressure = currentLine.lastPressure * PRESSURE_MULTIPLIER;


            List<Vector3> verts = new List<Vector3>() {
                midPoint,
                midPoint + side * pressure,
                midPoint + side * pressure * 0.7071f + up * pressure * 0.7071f,
                midPoint + up * pressure,
                midPoint - side * pressure * 0.7071f + up * pressure * 0.7071f,
                midPoint - side * pressure,
                midPoint - side * pressure * 0.7071f - up * pressure * 0.7071f,
                midPoint - up * pressure,
                midPoint + side * pressure * 0.7071f - up * pressure * 0.7071f,
            };

            currentLine.mesh.SetVertices(verts);

            #endregion

            #region indices

            currentLine.indices = new List<int> {
                0,2,1,
                0,3,2,
                0,4,3,
                0,5,4,
                0,6,5,
                0,7,6,
                0,8,7,
                0,1,8
            };

            currentLine.mesh.SetIndices(currentLine.indices.ToArray(), MeshTopology.Triangles, 0);

            #endregion

        }

        public void updatePointThickness(VectorLine currentLine, float pressure) {

            //graphic is edit locked
            if (currentLine.editLock) {
                VRPen.Debug.LogError("Tryed to edit a graphic that is edit-locked");
                return;
            }
            
            //if its more than just a dot, recalculate the points
            if (currentLine.pointCount > 1) { 
                Vector3[] verts = currentLine.mesh.vertices;
                Vector3 point = currentLine.lastDrawPoint;

                //get perpindicular vectors
                Vector3 normal = Vector3.up;
                Vector3 dir = currentLine.lastDrawPoint - currentLine.secondLastDrawPoint;
                Vector3 perp = Vector3.Cross(dir, normal).normalized;


                if (currentLine.flipVerts) perp = -perp;
                verts[verts.Length - 2] = point + perp * pressure * PRESSURE_MULTIPLIER;
                verts[verts.Length - 2] = point - perp * pressure * PRESSURE_MULTIPLIER;
            }

            //update data
            currentLine.lastPressure = pressure;
        }
        
        public void updateLinePrediction(VectorLine currentLine, Vector3 pos, float pressure) {

            //graphic is edit locked
            if (currentLine.editLock) {
                VRPen.Debug.LogError("Tryed to edit a graphic that is edit-locked");
                return;
            }
            
            //normal is constant since render area doesnt move
            Vector3 normal = Vector3.up;


            #region errors

            //if this isnt the first point, make sure that the direction of the line is not the same as the normal
            if (currentLine.pointCount != 0) {
                Vector3 direction = pos - currentLine.lastDrawPoint;
                if (direction.Equals(normal)) {
                    Debug.LogError("The normal of a line segment addition was in the same dirrection as the line which is not possible to render");
                    return;
                }
            }
            else {
                Debug.LogError("Need one point before adding line prediction");
                return;
            }
            
            
            #endregion

            //compute angle data
            float angle = 0;
            if (currentLine.pointCount >= 2) angle = Vector3.Angle(pos - currentLine.lastDrawPoint, currentLine.lastDrawPoint - currentLine.secondLastDrawPoint);
            bool isCusp = angle > 90;

            #region vertices

            Vector3[] oldVerts = currentLine.mesh.vertices;

            Vector3[] verts = new Vector3[currentLine.pointCount * 2 + 2];

            
            for (int x = 0; x < currentLine.pointCount * 2; x++) {
                verts[x] = oldVerts[x];
            }
            
            //get perpindicular vectors
            Vector3 dir = pos - currentLine.lastDrawPoint;
            Vector3 perp = Vector3.Cross(dir, normal).normalized;

            //when theres a 90+ degree turn we need to flip the verts
            bool tempFlip = currentLine.flipVerts;
            if (isCusp) {
                tempFlip = !currentLine.flipVerts;
                if (tempFlip) perp = -perp;
            }
            
            //make a list of points that will be ordered into the vert array later
            List<Vector3> addedVerts = new List<Vector3>() {
                pos + perp * pressure * PRESSURE_MULTIPLIER,
                pos + -perp * pressure * PRESSURE_MULTIPLIER,
            };
            for (int x = 0; x < 2; x++) {
                verts[currentLine.pointCount * 2 + x] = addedVerts[x];
            }

            
            currentLine.mesh.SetVertices(verts);

            #endregion

            #region indices

            if (currentLine.pointCount == 0) {
                currentLine.indices = new List<int>();
            }
            else {
                if (!tempFlip) {
                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 0);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);

                    currentLine.indices.Add(currentLine.pointCount * 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);
                }

                else {
                    currentLine.indices.Add(currentLine.pointCount * 2 + 0);
                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);

                    currentLine.indices.Add(currentLine.pointCount * 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 1);

                }
            }


            currentLine.mesh.SetIndices(currentLine.indices.ToArray(), MeshTopology.Triangles, 0);

            #endregion

            

        }

        

        public IEnumerator rerenderCanvas() {

            //turn on objs
            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.backgroundColor = bgColor;
            int index = 0;
            while (index < vectorParent.childCount) {
                vectorParent.GetChild(index).gameObject.GetComponent<MeshRenderer>().enabled = true;
                index++;
            }

            yield return null;

            //turn off objs
            renderCam.clearFlags = CameraClearFlags.Nothing;
            index = 0;
            while (index < vectorParent.childCount) {
                vectorParent.GetChild(index).gameObject.GetComponent<MeshRenderer>().enabled = false;
                index++;
            }

            //turn back on the current Graphics for each user
            foreach (VectorGraphic graphic in graphics) {
                if (graphic.editLock) graphic.mr.enabled = true;
            }

        }

        public void placeStamp(VectorStamp stamp, Vector3 pos) {

            //graphic is edit locked
            if (stamp.editLock) {
                VRPen.Debug.LogError("Tryed to edit a graphic that is edit-locked");
                return;
            }
            
            stamp.obj.transform.localPosition = pos;
            StartCoroutine(renderGraphic(stamp));

        }

        

        public void clear(bool localInput) {

            //deal with players data structures
            foreach (VRPenInput input in FindObjectsOfType<VRPenInput>()){
                if (input.currentLine != null && input.currentLine.canvasId == canvasId) input.currentLine = null;
                input.undoStack.RemoveAll(x => x.canvasId == canvasId);
            }
            graphics.Clear();
            
            //delete Graphics
            int index = 0;
            while (index < vectorParent.childCount) {
                Destroy(vectorParent.GetChild(index).gameObject);
                index++;
            }

            //reset render queue counter
            renderQueueCounter = 1;

            //network
            if (localInput) network.sendClear(canvasId); 
				
            //fill board
            StartCoroutine(fillboard());

        }

        IEnumerator fillboard() {

            renderCam.clearFlags = CameraClearFlags.SolidColor;
            renderCam.backgroundColor = bgColor;
            yield return null;
            yield return null;
            renderCam.clearFlags = CameraClearFlags.Nothing;
			
			//set background
			if (drawingMan.canvasBackgrounds.Length > canvasId && drawingMan.canvasBackgrounds[canvasId] != null) {
				//TODO - bring back backgrounds
				//drawingMan.stamp(drawingMan.canvasBackgrounds[canvasId], -1, drawingMan.facilitativeDevice.owner, drawingMan.facilitativeDevice.deviceIndex, 0, 0, 1, 0.5f, canvasId, false);
			}

        }

        public IEnumerator renderGraphic(VectorGraphic graphic) {
			
            //turn on the edit lock since we dont want it to change now that its been / is being rendered
            graphic.editLock = true;

            //make sure mesh renderer is on
            graphic.mr.enabled = true;
            
            //wait
            yield return null;
            yield return null;
            
            //turn off graphic.mr if it still exists (it could have been deleted in the 2 frames, especially in catchup sequences)
            if (graphic.mr != null) graphic.mr.enabled = false;

        }

    }

}