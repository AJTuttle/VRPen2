 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class VectorCanvas : MonoBehaviour {

        VectorDrawing drawingMan;
        NetworkManager network;

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

        

        //think of the normal as if it was ribbon drawing (it is internal used to order the vertices such that they connect to previous ones properlly)
        public void addToLine(InputDevice device, VectorLine currentLine, Vector3 pos, float pressure, bool rotateLastSegment) {

            //normal is constant since render area doesnt move
            Vector3 normal = Vector3.up;
            

            #region errors

            //if this isnt the first point, make sure that the direction of the line is not the same as the normal
            if (currentLine.pointCount != 0) {
                Vector3 dir = pos - device.lastDrawPoint;
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
            if (currentLine.pointCount >= 2) angle = Vector3.Angle(pos - device.lastDrawPoint, device.lastDrawPoint - device.secondLastDrawPoint);
            bool isCusp = angle > 90 && !rotateLastSegment;

            #region vertices

            Vector3[] oldVerts = device.currentGraphic.mesh.vertices;

            Vector3[] verts = new Vector3[currentLine.pointCount * 2 + 2];

            //if I need to rotate the last segment, do that
            if (rotateLastSegment) {
                //recalulate

                //get perpindicular vectors
                Vector3 dir1 = (pos - device.lastDrawPoint).normalized;
                Vector3 dir2 = (device.lastDrawPoint - device.secondLastDrawPoint).normalized;
                Vector3 dir = dir1 + dir2;
                Vector3 perp = Vector3.Cross(dir, normal).normalized;
                if (device.flipVerts) perp = -perp;

                //make a list of points that will be ordered into the vert array later
                List<Vector3> addedVerts = new List<Vector3>() {
                    device.lastDrawPoint + perp * device.lastPressure * PRESSURE_MULTIPLIER,
                    device.lastDrawPoint + -perp * device.lastPressure * PRESSURE_MULTIPLIER,
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
                Vector3 dir = pos - device.lastDrawPoint;
                Vector3 perp = Vector3.Cross(dir, normal).normalized;

                //when theres a 90+ degree turn we need to flip the verts
                if (isCusp) {
                    device.flipVerts = !device.flipVerts;
                    Debug.Log("flip: " + angle);
                }
                if (device.flipVerts) perp = -perp;



                //make a list of points that will be ordered into the vert array later
                List<Vector3> addedVerts = new List<Vector3>() {
                    pos + perp * pressure * PRESSURE_MULTIPLIER,
                    pos + -perp * pressure * PRESSURE_MULTIPLIER,
                };
                Debug.Log("hurr");
                for (int x = 0; x < 2; x++) {
                    verts[currentLine.pointCount * 2 + x] = addedVerts[x];
                }


            }


            device.currentGraphic.mesh.SetVertices(verts);

            #endregion

            #region indices

            if (currentLine.pointCount == 0) {
                currentLine.indices = new List<int>();
            }
            else {

                if (!device.flipVerts) {
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


            device.currentGraphic.mesh.SetIndices(currentLine.indices.ToArray(), MeshTopology.Triangles, 0);

            #endregion


            //update vars
            currentLine.pointCount++;
            if (currentLine.pointCount > 1) device.secondLastDrawPoint = device.lastDrawPoint;
            device.lastDrawPoint = pos;
            device.lastPressure = pressure;
            

            //turn off prediction
            //toggleLinePrediction(line, false);

        }
        
        /*
        public void updateLinePrediction(InputDevice device, VectorLine currentLine, Vector3 pos, float pressure) {

            //normal is constant since render area doesnt move
            Vector3 normal = Vector3.up;


            #region errors

            //if this isnt the first point, make sure that the direction of the line is not the same as the normal
            if (currentLine.pointCount != 0) {
                Vector3 direction = pos - device.lastDrawPoint;
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
            if (currentLine.pointCount >= 2) angle = Vector3.Angle(pos - device.lastDrawPoint, device.lastDrawPoint - device.secondLastDrawPoint);
            bool isCusp = angle > 90;

            #region vertices

            Vector3[] oldVerts = device.currentGraphic.mesh.vertices;

            Vector3[] verts = new Vector3[currentLine.pointCount * 2 + 2];

            
            for (int x = 0; x < currentLine.pointCount * 2; x++) {
                verts[x] = oldVerts[x];
            }
            
            //get perpindicular vectors
            Vector3 dir = pos - device.lastDrawPoint;
            Vector3 perp = Vector3.Cross(dir, normal).normalized;

            //when theres a 90+ degree turn we need to flip the verts
            bool tempFlip = device.flipVerts;
            if (isCusp) {
                tempFlip = !device.flipVerts;
                if (tempFlip) perp = -perp;
            }
            
            //make a list of points that will be ordered into the vert array later
            List<Vector3> addedVerts = new List<Vector3>() {
                pos + perp * pressure * PRESSURE_MULTIPLIER,
                pos + -perp * pressure * PRESSURE_MULTIPLIER,
            };
            Debug.Log("hurr");
            for (int x = 0; x < 2; x++) {
                verts[currentLine.pointCount * 2 + x] = addedVerts[x];
            }

            
            device.currentGraphic.mesh.SetVertices(verts);

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


            device.currentGraphic.mesh.SetIndices(currentLine.indices.ToArray(), MeshTopology.Triangles, 0);

            #endregion

            

        }

        **/
        
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
            foreach (NetworkedPlayer player in network.players) {
                foreach (KeyValuePair<byte, InputDevice> device in player.inputDevices) {
                    if (device.Value.currentGraphic != null) device.Value.currentGraphic.mr.enabled = true;
                }
            }

        }

        public void placeStamp(VectorStamp stamp, InputDevice device, Vector3 pos) {

            stamp.obj.transform.localPosition = pos;
            StartCoroutine(renderGraphic(stamp, device));

        }

        

        public void clear(bool localInput) {

            //deal with players data structures
            foreach (NetworkedPlayer player in network.players) {
                foreach (KeyValuePair<byte,InputDevice> device in player.inputDevices) {
                    if (device.Value.currentGraphic != null && device.Value.currentGraphic.canvasId == canvasId) device.Value.currentGraphic = null;
                }
                int length = player.graphics.Count;
                for (int x = length-1; x >=0; x--) {
                    if (player.graphics[x].canvasId == canvasId) {
                        player.graphics[x] = null;
                        player.graphics.Remove(player.graphics[x]);
                    }
                }
            }
            //delete Graphics
            int index = 0;
            while (index < vectorParent.childCount) {
                Destroy(vectorParent.GetChild(index).gameObject);
                index++;
            }

            renderQueueCounter = 1;

            //network
            if (localInput) network.sendClear(canvasId); 
				
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
				drawingMan.stamp(drawingMan.canvasBackgrounds[canvasId], -1, drawingMan.facilitativeDevice.owner, drawingMan.facilitativeDevice.deviceIndex, 0, 0, 1, 0.5f, canvasId, false);
			}

        }

        public IEnumerator renderGraphic(VectorGraphic graphic, InputDevice device) {
			
            //turn off the currentgraphic before since we dont want it to be edited while being rendered in
            if (graphic == device.currentGraphic) device.currentGraphic = null;

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