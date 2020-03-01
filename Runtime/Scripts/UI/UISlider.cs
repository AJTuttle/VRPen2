using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISlider : MonoBehaviour
{

    public int defaultSize;
    public int sizePerScalingMenuItem;
    public int maxSize;

    public GameObject scalingParent;

    Slider slider;
    
    void Start(){
        slider = GetComponent<Slider>();
    }
    

    public void setPos(float pos, bool localInput) {

        //get scale distance
        float potentialMovement = (defaultSize + scalingParent.transform.childCount * sizePerScalingMenuItem) - maxSize;
        if (potentialMovement > 0) {
            float menuPos = pos * potentialMovement;
            scalingParent.transform.localPosition = new Vector3(scalingParent.transform.localPosition.x, menuPos, scalingParent.transform.localPosition.z);
        }
        
        //set slider
        slider.value = pos;

        if (localInput) {
            //TODO network
        }

    }
}
