using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI ;

namespace VRPen {

    public class AdditionalUIWindow : MonoBehaviour {

        //priv vars
        public UIManager uiMan;
        private RectTransform thisRect;
        private BoxCollider thisCol;
        private RectTransform contentRect;
        
        //pub vars
        public GameObject parent;
        public RectTransform bg;
        public RectTransform topBar;
        public RectTransform topBarGrab;
        public RectTransform minimizeButton;
        public BoxCollider topBarGrabCol;
        public RectTransform contentParent;
        public UIGrabbable grabbable;
        
        //consts
        private const float topBarThickness = 25;
        private const float contentPadding = 10;
        
        void Awake() {

            
            thisRect = GetComponent<RectTransform>();
            thisCol = bg.GetComponent<BoxCollider>();
            
        }

        private void Start() {
            
            grabbable.man = uiMan;
        }

        public void enable() {
            parent.SetActive(true);
            uiMan.queueState();
        }

        public void disable() {
            parent.SetActive(false);
            uiMan.queueState();
        }

        public bool isEnabled() {
            return parent.activeInHierarchy;
        }

        public void setContent(GameObject prefab) {
            
            //destory old content
            while (contentParent.childCount > 0) {
                GameObject.Destroy(contentParent.GetChild((0)));
            }

            //add new contnnt
            GameObject content = GameObject.Instantiate(prefab, contentParent);
            contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(.5f, .5f);
            contentRect.anchorMax = new Vector2(.5f, .5f);
            
            //set pos to default
            setWindowPos(Vector3.zero);
            
            //set window size
            computeWindowSize();
            
            //add highlight listener to buttons
            foreach (Button b in content.GetComponentsInChildren<Button>()) {
                b.onClick.AddListener(delegate { uiMan.highlightTimer(.2f); });
            }


        }

        public void computeWindowSize() {
            
            //set size
            setWindowSize(contentRect.sizeDelta.x + contentPadding, contentRect.sizeDelta.y + contentPadding);
        }

        void setWindowSize(float width, float height) {
            
            //main window
            thisRect.sizeDelta = new Vector2(width, height);
            thisCol.size = new Vector3(width, height, 0.001f);
            
            //top bar
            topBar.localPosition = new Vector3(0, height/2f + topBarThickness/2, 0);
            topBarGrab.sizeDelta = new Vector2(width, topBarThickness);
            topBarGrabCol.size = new Vector3(width, topBarThickness, .001f);
            
            //minimize button
            minimizeButton.localPosition = new Vector3(width/ 2f - 16, 0, 0);

        }

        public void setWindowPos(Vector3 pos) {
            contentRect.localPosition = pos;
        }
        
    }

}
