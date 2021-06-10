using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRPen {
    public enum StampType :byte {
        image,
        text
    }
    public class StampGenerator : MonoBehaviour {

        

        public StampType type;
        
        VectorDrawing vectorMan;
        NetworkedPlayer player;
        Display display;

        
        float size = .1f; //default value (not necesarrlly synced with ui slider on start)
        float rot = .5f; //default value (not necesarrlly synced with ui slider on start)
		
        
        //image
        public Transform image;
        Material imageMat;
        float aspectRatio;
        int stampIndex;

        
        //text
        public TextMeshPro textObj;
        

        //image instantiate
        public void instantiate(VectorDrawing man, NetworkedPlayer player, Display display, int stampIndex) {

            //set type
            type = StampType.image;
            image.gameObject.SetActive(true);
            
            vectorMan = man;
            this.player = player;
            this.display = display;
            this.stampIndex = stampIndex;

            imageMat = image.GetComponent<Renderer>().material;
            string str = PersistantData.getStampFileName(stampIndex);
			Debug.Log(str);
            Texture2D texture = PersistantData.getStampTexture(stampIndex);
			aspectRatio = (float)texture.width / texture.height;

            setTexture(texture);
            setSize(size);
            setRot(rot);
        }

        //text instantiate
        public void instantiate(VectorDrawing man, NetworkedPlayer player, Display display, string text) {
            
            //set type
            type = StampType.text;
            textObj.gameObject.SetActive(true);
            
            vectorMan = man;
            this.player = player;
            this.display = display;


            setText(text);
            setSize(size);
            setRot(rot);
        }
        

        void setTexture(Texture2D tex) {
            imageMat.mainTexture = tex;
        }

        void setText(string text) {
            textObj.text = text;
        }

        public void setSize(float value) {
            size = value;
            if (type == StampType.image) {
                image.localScale = new Vector3(600 * aspectRatio, 600, 600) * value;
            }
            else {
                textObj.transform.localScale = new Vector3(value, value, value) * 60;
            }
            
        }

        public void setRot(float value) {

			rot = value;
			
			//convert from [0,1] to [-180,180]
			value *= 360;
            value -= 180;
            if (type == StampType.image) {
                image.localRotation = Quaternion.Euler(0, 0, -value);
            }
            else {
                textObj.transform.localRotation = Quaternion.Euler(0, 0, -value);
            }
        }

        public void confirmStamp() {
            
            Vector3 pos = display.canvasParent.InverseTransformPoint(transform.position);
            VectorStamp stamp = null;
            stamp = vectorMan.stamp(type, textObj.text, imageMat != null ? imageMat.mainTexture: null, stampIndex, player.connectionId,
                NetworkManager.s_instance.localGraphicIndex,
                -pos.x * display.canvasParent.transform.parent.localScale.x /
                display.canvasParent.transform.parent.localScale.y, -pos.y, size, rot,
                display.currentLocalCanvas.canvasId, true);
            
            NetworkManager.s_instance.localGraphicIndex++;
            VectorDrawing.s_instance.undoStack.Add(stamp);

            close();
        }

        public void close() {
            display.currentStamp = null;
            Destroy(gameObject);
        }


    }

}