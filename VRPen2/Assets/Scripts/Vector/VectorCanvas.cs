﻿using System.Collections;
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


        public void addToLine(InputDevice device, VectorLine currentLine, Vector3 drawPoint, float pressure) {


            //get mesh
            Mesh currentMesh = currentLine.mesh;

            //if start of line then setup up arrays
            if (currentLine.vertices == null) {
                currentLine.vertices = new Vector3[drawingMan.lineDataStepSize*2];
                currentLine.normals = new Vector3[drawingMan.lineDataStepSize*2];
            }

            //new quad points
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



            //new normals
            Vector3[] quadNormals = new Vector3[2];
            for (int i = 0; i < 2; i++) {
                quadNormals[i] = vectorParent.up;
            }


            

            //if we need to add more memery space for the data
            if (currentLine.vertices.Length < currentLine.pointCount * 2 + 2) {

                Vector3[] newVertices = new Vector3[currentLine.vertices.Length + drawingMan.lineDataStepSize*2];
                Vector3[] newNormals = new Vector3[currentLine.vertices.Length + drawingMan.lineDataStepSize*2];

                for (int i = 0; i < currentLine.vertices.Length; i++) {
                    newVertices[i] = currentLine.vertices[i];
                    newNormals[i] = currentLine.normals[i];
                }
                for (int i = 0; i < 2; i++) {
                    newNormals[currentLine.vertices.Length + i] = quadNormals[i];
                    newVertices[currentLine.vertices.Length + i] = quadPoints[i];
                }

                currentLine.vertices = newVertices;
                currentLine.normals = newNormals;

                currentMesh.vertices = newVertices;
                currentMesh.normals = newNormals;

            }

            //if we already have open memory space
            else {

                for (int i = 0; i < 2; i++) {
                    currentLine.normals[currentLine.pointCount * 2 + i] = quadNormals[i];
                    currentLine.vertices[currentLine.pointCount*2 + i] = quadPoints[i];
                }

                currentMesh.vertices = currentLine.vertices;
                currentMesh.normals = currentLine.normals;

            }


            if (currentLine.pointCount == 25) {
                int x = 0;
            }


            //indices
            if (currentLine.indices == null) currentLine.indices = new List<int>();

            //if its not the first point, make triangles
            if (currentLine.pointCount > 0) {

                //check if it is a cusp. This will require slightly different indices so that no tris are facing the wrong way
                bool cusp = Vector3.Angle(device.lastDrawPoint - device.secondLastDrawPoint, drawPoint - device.lastDrawPoint) > 90f;

                if (cusp) {
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 4);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 3);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 3);

                }
                else {
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 4);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 3);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 2);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 1);
                    currentLine.indices.Add(currentLine.pointCount * 2 + 2 - 3);

                }

            }


            //update mesh
            currentMesh.SetIndices(currentLine.indices.ToArray(), MeshTopology.Triangles, 0);
            //currentMesh.RecalculateNormals();
            currentMesh.RecalculateBounds();

            //set last draw points
            device.secondLastDrawPoint = device.lastDrawPoint;
            device.lastDrawPoint = drawPoint;

            //set point count
            currentLine.pointCount++;
            
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
            foreach (NetworkedPlayer player in network.players) {
                foreach (KeyValuePair<byte, InputDevice> device in player.inputDevices) {
                    if (device.Value.currentGraphic != null) device.Value.currentGraphic.mr.enabled = true;
                }
            }

        }

        public void clear(bool localInput) {

            //deal with players data structures
            foreach (NetworkedPlayer player in network.players) {
                foreach (KeyValuePair<byte,InputDevice> device in player.inputDevices) {
                    if (device.Value.currentGraphic != null && device.Value.currentGraphic.canvasId == canvasId) device.Value.currentGraphic = null;
                }
                int length = player.graphics.Count;
                for (int x = 0; x < length; x++) {
                    if (player.graphics[x].canvasId == canvasId) {
                        player.graphics.Remove(player.graphics[x]);
                        length--;
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

        }

        

    }

}