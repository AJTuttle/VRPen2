using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
public class SampleClient : MonoBehaviour
{

    TcpClient client;
    NetworkStream stream;

    // Start is called before the first frame update
    void Start()
    {
        //Screen.fullScreen = false;
        Screen.SetResolution(500, 500, false);
        client = new TcpClient("localhost", 6745);
        stream = client.GetStream();
    }

    // Update is called once per frame
    void Update()
    {
        sendData();
    }

    void sendData() {

        

        byte[] sendData = new byte[100];
        sendData[0] = 23;

        

        stream.Write(sendData, 0, 100);
        


    }

}
