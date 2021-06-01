using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class AdditionalUIWindow : MonoBehaviour {

        //priv vars
        private RectTransform thisRect;
        private BoxCollider thisCol;
        
        //pub vars
        public GameObject parent;
        public RectTransform bg;
        public RectTransform topBar;
        public RectTransform topBarGrab;
        public BoxCollider topBarGrabCol;
        public RectTransform contentParent;
        
        //consts
        private const float topBarThickness = 25;
        private const float contentPadding = 10;
        
        void Awake() {
            thisRect = GetComponent<RectTransform>();
            thisCol = bg.GetComponent<BoxCollider>();
            
        }


        void enable() {
            parent.SetActive(true);
        }

        void disable() {
            parent.SetActive(true);
        }

        public void setContent(GameObject prefab) {
            
            //destory old content
            while (contentParent.childCount > 0) {
                GameObject.Destroy(contentParent.GetChild((0)));
            }

            //add new contnnt
            GameObject content = GameObject.Instantiate(prefab, contentParent);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(.5f, .5f);
            contentRect.anchorMax = new Vector2(.5f, .5f);
            contentRect.localPosition = Vector3.zero;
            
            //set size
            setWindowSize(contentRect.sizeDelta.x + contentPadding, contentRect.sizeDelta.y + contentPadding);
            
            //en
            enable();

        }

        void setWindowSize(float width, float height) {
            
            //main window
            thisRect.sizeDelta = new Vector2(width, height);
            thisCol.size = new Vector3(width, height, 0.001f);
            
            //top bar
            topBar.localPosition = new Vector3(0, height/2f + topBarThickness/2, 0);
            topBarGrab.sizeDelta = new Vector2(width, topBarThickness);
            topBarGrabCol.size = new Vector3(width, topBarThickness, .001f);
            
        }
        
    }

}
