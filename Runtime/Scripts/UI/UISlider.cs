using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {

    public class UISlider : MonoBehaviour {

        public UIManager UIMan;

        [Tooltip("The slidable area above the list of menu items")]
        public int defaultSizeAboveScalingMenu;
        [Tooltip("The slidable area below the list of menu items")]
        public int defaultSizeBelowScalingMenu;
        [Tooltip("The size (height) of each menu item")]
        public int sizePerScalingMenuItem;
        [Tooltip("Max size (height) of the entire sliding area that is visable at any one time (window size)")]
        public int maxSize;
        [Tooltip("sync over network")]
        public bool sync;


        public GameObject scalingParent;
        public Slider slider;

        void Awake() {
            slider = GetComponent<Slider>();

            //set pos to turn off culled objects
            setPos(0, false);
        }


        public void setPos(float pos, bool localInput) {

            //get scale distance
            int totalMenuSize = defaultSizeAboveScalingMenu + defaultSizeBelowScalingMenu + scalingParent.transform.childCount * sizePerScalingMenuItem;
            int potentialMovement = totalMenuSize - maxSize;

            if (potentialMovement > 0) {

                //get movement
                int menuPos = (int)(pos * potentialMovement);

                //get amount of empty space visable
                int aboveMenuVisable = defaultSizeAboveScalingMenu - menuPos;
                if (aboveMenuVisable < 0) aboveMenuVisable = 0;
                int belowMenuVisable = -totalMenuSize + defaultSizeBelowScalingMenu + menuPos + maxSize;
                if (belowMenuVisable < 0) belowMenuVisable = 0;

                Debug.Log(aboveMenuVisable + "  " + belowMenuVisable);

                //turn off culled menu items
                int topCulledCount = (int)((menuPos - defaultSizeAboveScalingMenu + sizePerScalingMenuItem/2) / sizePerScalingMenuItem);
                if (topCulledCount < 0) topCulledCount = 0;
                int nonCulledCount = (int)(maxSize - aboveMenuVisable - belowMenuVisable + sizePerScalingMenuItem) / sizePerScalingMenuItem;

                for (int x = 0; x < scalingParent.transform.childCount; x++) {
                    if (x < topCulledCount) scalingParent.transform.GetChild(x).gameObject.SetActive(false);
                    else if (x >= topCulledCount + nonCulledCount) scalingParent.transform.GetChild(x).gameObject.SetActive(false);
                    else scalingParent.transform.GetChild(x).gameObject.SetActive(true);
                }

                //move scalingparent
                float menuPosWithDisabledObjects = menuPos - topCulledCount * sizePerScalingMenuItem;
                scalingParent.transform.localPosition = new Vector3(scalingParent.transform.localPosition.x, menuPosWithDisabledObjects, scalingParent.transform.localPosition.z);

            }

            //set slider
            slider.value = pos;

            if (localInput && sync) {
                UIMan.queueState();

            }

        }
    }

}
