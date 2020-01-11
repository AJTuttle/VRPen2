using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

public class SampleServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        IPAddress ip = Dns.GetHostEntry("localhost").AddressList[0];
        TcpListener server = new TcpListener(6745);
        TcpClient client = default(TcpClient);

        try {
            server.Start();
            Debug.Log("server started");
        }
        catch (Exception ex) {
            Debug.Log(ex.ToString());
        }

        StartCoroutine(readRoutine(client, server));

    }

    IEnumerator readRoutine(TcpClient client, TcpListener server) {
        while (true) {

            client = server.AcceptTcpClient();

            Debug.Log(client);

            if (client != null) {

                byte[] receivedBuffer = new byte[100];
                NetworkStream stream = client.GetStream();

                stream.Read(receivedBuffer, 0, receivedBuffer.Length);

                Debug.Log(receivedBuffer[0]);

            }

            yield return null;

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
