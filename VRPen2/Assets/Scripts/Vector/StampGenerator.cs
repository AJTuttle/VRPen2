using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {

    public class StampGenerator : MonoBehaviour {

        VectorDrawing vectorMan;
        VRPenInput device;
        NetworkedPlayer player;
        Display display;

        public Transform image;
        Material imageMat;

        float size = .1f; //default value (not necesarrlly synced with ui slider on start)
        float rot = .5f; //default value (not necesarrlly synced with ui slider on start)

        public Texture2D defaultTexture;

        

        public void instantiate(VRPenInput device, VectorDrawing man, NetworkedPlayer player, Display display) {

            vectorMan = man;
            this.device = device;
            this.player = player;
            this.display = display;

            imageMat = image.GetComponent<Renderer>().material;

            setTexture(defaultTexture);
            setSize(size);
            setRot(rot);
        }

        void setTexture(Texture2D tex) {
            imageMat.mainTexture = tex;
        }

        public void setSize(float value) {
            size = value;
            image.localScale = new Vector3(600, 600, 600) * value;
            
        }

        public void setRot(float value) {

			rot = value;
			
			//convert from [0,1] to [-180,180]
			value *= 360;
            value -= 180;

            image.localRotation = Quaternion.Euler(0, 0, -value);
        }

        public void confirmStamp() {
            Vector3 pos = display.canvasParent.InverseTransformPoint(transform.position);
            vectorMan.stamp(imageMat.mainTexture, player, device.deviceData.deviceIndex, (5f/3*pos.x)+0.5f, pos.y+0.5f, size, rot, display.currentLocalCanvas.canvasId, display.currentLocalCanvas.currentLocalLayerIndex, true);
            close();
        }

        public void close() {
            device.currentStamp = null;
            Destroy(gameObject);
        }


    }

}