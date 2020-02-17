using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

public class SampleServer : MonoBehaviour
{

    List<TcpClient> clients = new List<TcpClient>();
    TcpListener server;

    // Start is called before the first frame update
    void Start()
    {

        server = new TcpListener(6745);
        
        try {
            server.Start();
            Debug.Log("server started");
        }
        catch (Exception ex) {
            Debug.Log(ex.ToString());
        }

    }

    private void Update() {
        
        //check for client connections
        if (server.Pending()) {
            Debug.Log("Added user: " + clients.Count);
            TcpClient client = server.AcceptTcpClient();
            clients.Add(client);
        }

        //packets
        foreach(TcpClient client in clients) {
            if (client.ReceiveBufferSize > 0) {
                
                //get buffer
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
                //Debug.Log("Read buffer of size: " + bytesRead);

                //count
                if (BitConverter.ToInt32(buffer, 0) == buffer.Length) {
                    
                    //remove buffer counter
                    byte[] cutBuffer = new byte[buffer.Length - 4];
                    for(int x = 0; x < cutBuffer.Length; x++) {
                        cutBuffer[x] = buffer[x + 4];
                    }

                    //pass through to other clients
                    foreach(TcpClient otherClient in clients) {
                        //make sure the packet isnt sent to the origin user
                        if (otherClient != client) {
                            otherClient.GetStream().Write(cutBuffer, 0, cutBuffer.Length);
                        }
                    }

                }
                else {
                    Debug.LogError("Recieved buffer contains more than one packet, they have been ignore (TO-DO split up the packets and use them)");
                }

            }
        }
        
    }

}
