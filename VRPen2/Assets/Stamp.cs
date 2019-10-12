using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {

    public class Stamp : MonoBehaviour {

        VectorDrawing vectorMan;
        VRPenInput device;
        NetworkedPlayer player;
        Display display;

        public Transform image;
        Material imageMat;

        float size = .1f; //default value (not necesarrlly synced with ui slider on start)

        public Texture2D defaultTexture;

        

        public void instantiate(VRPenInput device, VectorDrawing man, NetworkedPlayer player, Display display) {

            vectorMan = man;
            this.device = device;
            this.player = player;
            this.display = display;

            imageMat = image.GetComponent<Renderer>().material;

            setTexture(defaultTexture);
            setSize(.1f);
        }

        void setTexture(Texture2D tex) {
            imageMat.mainTexture = tex;
        }

        public void setSize(float value) {
            size = value;
            image.localScale = new Vector3(1000, 1000, 1000) * size;
        }

        public void confirmStamp() {
            vectorMan.stamp(imageMat.mainTexture, player, device.deviceData.deviceIndex, .5f, .5f, size, display.DisplayId, true);
        }

        public void close() {
            device.currentStamp = null;
            Destroy(gameObject);
        }


    }

}