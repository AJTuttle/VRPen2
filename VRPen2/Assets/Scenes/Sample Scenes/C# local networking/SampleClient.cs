using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
public class SampleClient : MonoBehaviour
{

    TcpClient client;

    // Start is called before the first frame update
    void Start()
    {
        client = new TcpClient("localhost", 6745);
    }

    // Update is called once per frame
    void Update()
    {
        sendData();
    }

    void sendData() {

        

        byte[] sendData = new byte[100];
        sendData[0] = 23;

        NetworkStream stream = client.GetStream();

        stream.Write(sendData, 0, 100);

        stream.Close();


    }

}
