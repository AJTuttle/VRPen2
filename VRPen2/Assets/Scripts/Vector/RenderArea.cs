using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class RenderArea : MonoBehaviour {

        public RenderTexture renderTexturePresets;
        public Transform layerRenderParent;
        public Transform mainRenderParent;
        public GameObject renderLayerPrefab;
        public Shader canvasShader;

        [System.NonSerialized]
        public byte layerCount = 0;

        [System.NonSerialized]
        public List<Camera> layerCameras = new List<Camera>();
        Camera mainCamera;

        Color bgColor;

        public void instantiate(out Material mat, Color bgColor) {

            //main render layer
            GameObject layer = Instantiate(renderLayerPrefab, mainRenderParent);
            layer.transform.localPosition = new Vector3(0, 0, 0);
            layer.transform.localRotation = Quaternion.identity;
            layer.transform.GetChild(1).localPosition = Vector3.zero;

            //get camera
            mainCamera = layer.transform.GetChild(0).GetComponent<Camera>();
			mainCamera.clearFlags = CameraClearFlags.Color;
			mainCamera.backgroundColor = bgColor;

            //set up mat/camera
            RenderTexture renderTexture = new RenderTexture(renderTexturePresets);
            mat = new Material(Shader.Find("Unlit/Texture"));
            mat.mainTexture = renderTexture;
            this.bgColor = bgColor;
            mat.SetColor("BG_Color", bgColor);
            mainCamera.targetTexture = renderTexture;

            //instantiate default layer(s)
            addLayer();
            addLayer();

        }
        public void addLayer() {

            //layer index
            byte index = (byte)layerCameras.Count;

            //layer creat
            GameObject layer = Instantiate(renderLayerPrefab, layerRenderParent);
            layer.transform.localPosition = new Vector3(0, 0, index * 2f);
            layer.transform.localRotation = Quaternion.identity;
            layer.name = "Layer " + index;

            //get camera
            Camera cam = layer.transform.GetChild(0).GetComponent<Camera>();
            layerCameras.Add(cam);

            //set up canvas for main camera
            GameObject LayerRendered = GameObject.CreatePrimitive(PrimitiveType.Quad);
            LayerRendered.name = "Layer " + index;
            LayerRendered.transform.parent = mainRenderParent.GetChild(0).GetChild(1);
            LayerRendered.transform.rotation = Quaternion.Euler(90,0,0);
            LayerRendered.transform.localPosition = new Vector3(0, 0.001f * index, 0); //make sure layer are layer in the correct order
            LayerRendered.transform.localScale = new Vector3(renderTexturePresets.width/(float)renderTexturePresets.height,1, 1);
            Destroy(LayerRendered.GetComponent<Collider>());




            //instance material and make the render texture pipe to it
            Material mat = LayerRendered.GetComponent<Renderer>().material;
            mat.shader = canvasShader;
            RenderTexture renderTexture = new RenderTexture(renderTexturePresets);
            renderTexture.filterMode = FilterMode.Point;
            mat.mainTexture = renderTexture;
            mat.SetColor("BG_Color", bgColor);
            cam.targetTexture = renderTexture;

            //iterate layer count
            layerCount++;
            
        }

        public Transform getVectorParent(byte layerIndex, byte canvasIndex) {
            return layerRenderParent.GetChild(layerIndex).GetChild(1);
        }

    }

}
