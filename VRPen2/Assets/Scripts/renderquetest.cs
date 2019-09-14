using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class renderquetest : MonoBehaviour
{
    public GameObject redG;
    public GameObject blueG;

    Material red;
    Material blue;

    public int redVal;
    public int blueVal;

    // Start is called before the first frame update
    void Start()
    {

        red = redG.GetComponent<Renderer>().material;
        blue = blueG.GetComponent<Renderer>().material;

        

        redVal = red.renderQueue;
        blueVal = blue.renderQueue;

        Debug.Log(redVal);
        Debug.Log(blueVal);

    }

    // Update is called once per frame
    void Update()
    {
        blue.renderQueue = blueVal;
        red.renderQueue = redVal;
    }
}
