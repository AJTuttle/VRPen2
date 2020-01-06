﻿using System.Collections;
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
        

        

        public void instantiate(VRPenInput device, VectorDrawing man, NetworkedPlayer player, Display display, int stampIndex) {

            vectorMan = man;
            this.device = device;
            this.player = player;
            this.display = display;

            imageMat = image.GetComponent<Renderer>().material;


            string str = PlayerPrefs.GetString("Stamp#" + stampIndex);
            Debug.Log(str);

            Texture2D text = Resources.Load<Texture2D>(str);

            Debug.Log((text == null).ToString());

            setTexture(text);
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
            vectorMan.stamp(imageMat.mainTexture, player, device.deviceData.deviceIndex, -pos.x*display.canvasParent.transform.parent.localScale.x/display.canvasParent.transform.parent.localScale.y, -pos.y, size, rot, display.currentLocalCanvas.canvasId, true);
            close();
        }

        public void close() {
            device.currentStamp = null;
            Destroy(gameObject);
        }


    }

}