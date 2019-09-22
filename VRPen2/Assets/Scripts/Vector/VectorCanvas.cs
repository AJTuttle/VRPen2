using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class VectorCanvas : MonoBehaviour {

        VectorDrawing drawingMan;
        NetworkManager network;

        public byte canvasId;
        public int renderQueueCounter = 1;
        


        //render texture stuff
        public RenderTexture renderTexturePresets;
        public GameObject renderingPrefab;
        Camera renderCam;
        public Color32 bgColor;
        public Transform vectorParent;
		public RenderTexture renderTexture;



		public void instantiate(VectorDrawing man, NetworkManager net, byte id) {

            //set vars
            network = net;
            drawingMan = man;
            canvasId = id;

            //set up the render texture stuff
            Transform renderArea = GameObject.Instantiate(renderingPrefab, drawingMan.renderAreaOrigin + new Vector3(0,0,canvasId), Quaternion.identity).transform;
            renderCam = renderArea.GetChild(0).GetComponent<Camera>();

            //instance material and make the render texture pipe to it
            Material mat = GetComponent<Renderer>().material;
			renderTexture = new RenderTexture(renderTexturePresets);
            mat.mainTexture = renderTexture;
            renderCam.targetTexture = renderTexture;
            vectorParent = renderArea.GetChild(1);

            //fill canvas bg color
            StartCoroutine(fillboard());
        }


        public void addToLine(InputDevice device, Mesh currentMesh, Vector3 drawPoint, float pressure) {

            //quad points
            Vector3[] quadPoints = new Vector3[2];


            //if start of line, the first 2 verts are at the draw point
            if (currentMesh.vertexCount == 0) {
                quadPoints[0] = drawPoint;
                quadPoints[1] = drawPoint;
            }

            //not start of line
            else {
                //get the thickness vectors
                float thickness = pressure * .01f;
                Vector3 dir = drawPoint - device.lastDrawPoint;
                Vector3 thicknessDisplacement = Vector3.Cross(dir, vectorParent.up).normalized * thickness;

                //get the 2 new points
                quadPoints[0] = drawPoint + thicknessDisplacement / 2;
                quadPoints[1] = drawPoint - thicknessDisplacement / 2;
            }



            //normals
            Vector3[] quadNormals = new Vector3[2];
            for (int i = 0; i < 2; i++) {
                quadNormals[i] = vectorParent.up;
            }



            //add vertices and normals to mesh
            Vector3[] vertices = currentMesh.vertices;
            Vector3[] normals = currentMesh.normals;

            Vector3[] newVertices = new Vector3[currentMesh.vertexCount + 2];
            Vector3[] newNormals = new Vector3[currentMesh.vertexCount + 2];

            for (int i = 0; i < currentMesh.vertexCount; i++) {
                newNormals[i] = normals[i];
                newVertices[i] = vertices[i];
            }
            for (int i = 0; i < 2; i++) {
                newNormals[normals.Length + i] = quadNormals[i];
                newVertices[vertices.Length + i] = quadPoints[i];
            }

            currentMesh.vertices = newVertices;
            currentMesh.normals = newNormals;




            //indices
            List<int> indices = new List<int>();
            currentMesh.GetIndices(indices, 0);



            if (newVertices.Length > 2) {

                //check if it is a cusp. This will require slightly different indices so that no tris are facing the wrong way
                bool cusp = Vector3.Angle(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint) > 90f;

                if (cusp) {
                    indices.Add(newVertices.Length - 4);
                    indices.Add(newVertices.Length - 3);
                    indices.Add(newVertices.Length - 1);
                    indices.Add(newVertices.Length - 2);
                    indices.Add(newVertices.Length - 1);
                    indices.Add(newVertices.Length - 3);
                }
                else {
                    indices.Add(newVertices.Length - 4);
                    indices.Add(newVertices.Length - 2);
                    indices.Add(newVertices.Length - 3);
                    indices.Add(newVertices.Length - 2);
                    indices.Add(newVertices.Length - 1);
                    indices.Add(newVertices.Length - 3);
                }

            }



            currentMesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            //currentMesh.RecalculateNormals();
            currentMesh.RecalculateBounds();

            device.secondLastDrawPoint = device.lastDrawPoint;
            device.lastDrawPoint = drawPoint;
            
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

            //turn back on the current lines for each user
            foreach (NetworkedPlayer player in network.players) {
                foreach (KeyValuePair<byte, InputDevice> device in player.inputDevices) {
                    if (device.Value.currentLine != null) device.Value.currentLine.mr.enabled = true;
                }
            }

        }

        public void clear(bool localInput) {

            //deal with players data structures
            foreach (NetworkedPlayer player in network.players) {
                foreach (KeyValuePair<byte,InputDevice> device in player.inputDevices) {
                    if (device.Value.currentLine != null && device.Value.currentLine.canvasId == canvasId) device.Value.currentLine = null;
                }
                int length = player.lines.Count;
                for (int x = 0; x < length; x++) {
                    if (player.lines[x].canvasId == canvasId) {
                        player.lines.Remove(player.lines[x]);
                        length--;
                    }
                }
            }
            //delete lines
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

        }

        

    }

}