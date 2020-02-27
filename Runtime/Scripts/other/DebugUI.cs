using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{


    public GameObject obj;

    public Text errorText;

    public bool enabled;

    public void display(string str) {
        if (!enabled) return; 
        obj.SetActive(true);
        errorText.text = str;
    }


}
