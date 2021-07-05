using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.Events;

namespace VRPen {

    /// <summary>
    /// Type of packet, used as a header for sending/recieving packets
    /// </summary>
    public enum PacketType : byte {
        PenData,
        Clear,
        AddCanvas,
        Undo,
        Stamp,
		UIState,
		CanvasSwitch,
        InputDeviceState,
        AddStampFile,
        SharedDeviceOwnershipTransfer,
        CacheRequest
    }

    public class NetworkManager : MonoBehaviour {

        //instance
        public static NetworkManager s_instance;


        //other players data
        bool localPlayerHasID = false;
        private ulong localPlayerId;
        int localPacketIndex = 0;

        //other vars
        long instanceStartTime; //used to differentiate catchup packets and current instance packets
        [NonSerialized]
        public bool connectedAndCaughtUp = false;
        private bool cacheDownloaded = false; //warning, if a cache is not needed (ie first player in server) this will be false always


        //vrpenpacket data struct
        public class VRPenPacket {
            public byte[] data;
            public ulong sender;
            public int senderPacketIndex;
            public PacketType type;
            public int byteSize = 0;
            
            public bool additionalDataIsSet = false;
            public List<object> additionalData;
            
            public VRPenPacket(byte[] data, ulong sender, int senderPacketIndex, PacketType type) {
                this.data = data;
                this.sender = sender;
                this.senderPacketIndex = senderPacketIndex;
                this.type = type;
                byteSize += data.Length + 12;
            }

            public void setAdditionalData(List<object> data, int byteSize) {
                if (additionalDataIsSet) {
                    Debug.LogError("Tried to set packet data that is already set");
                }
                else {
                    additionalData = data;
                    this.byteSize += byteSize;
                    additionalDataIsSet = true;
                }
            }
        }
        
        //cache packets
        //historical packets are for when the entire packet history is important (ie. draw events)
        private List<VRPenPacket> cacheHistoricalPackets = new List<VRPenPacket>(); 
        //non historical packets are for when the history doesnt matter (ie. UI state)
        private List<VRPenPacket> cacheNonHistoricalPackets = new List<VRPenPacket>(); 
        [Header("Optional")]
        [Space(10)]
        public List<TextMeshPro> cacheSizeDisplays;
        private const int cachePacketMaxSendingSize = 10000; //max ammount of bytes to be sent in one cache packet
        
        
        //on connect packet
        Queue<byte[]> onConnectPacketQueue = new Queue<byte[]>();
        
        //packets received before connected and caught up
        private List<VRPenPacket> packetsReceivedPreCatchup = new List<VRPenPacket>();

        //prefabs
        [Header("Variables that don't need to be changed")]
        [Space(10)]
        public GameObject remoteMarkerPrefab;
        public GameObject remoteTabletPrefab;
        public GameObject remoteMousePrefab;

        //data vars
        private Color32[] colors = new Color32[0];
        private bool[] endLines = new bool[0];
        private float[] xFloats = new float[0];
        private float[] yFloats = new float[0];
        private float[] pressures = new float[0];
        private byte[] canvasIds = new byte[0];
        private int[] localGraphicIndices = new int[0];


        //event
        public delegate void VRPenEvent(byte[] packet);
        public event VRPenEvent vrpenEvent;
        public delegate void VRPenCacheEvent(byte[] packet, ulong receiverID);
        public event VRPenCacheEvent cacheEvent;
		public delegate void remoteInputDeviceSpawned(GameObject obj, int deviceIndex);
		public event remoteInputDeviceSpawned remoteSpawn;
		
		public delegate void AddCanvasHandler();
		public event AddCanvasHandler AddedCanvas = delegate { };

        
        //local graphic index
        [System.NonSerialized]
        public int localGraphicIndex = 0;


        private void Awake() {
            //set instnace
            s_instance = this;
        }

        /// <summary>
		/// Start gets relevant scripts and also inits the local player's data
		/// </summary>
		private void Start() {

            //set the start time
            instanceStartTime = DateTime.Now.Ticks;

            //display cache size
            InvokeRepeating(nameof(displayCacheSize), 1f, 0.5f);

        }

        void displayCacheSize() {
            
            //return if no display
            if (cacheSizeDisplays.Count == 0) {
                return;
            }
            
            //get size
            long size = 0;
            foreach (VRPenPacket packet in cacheHistoricalPackets) {
                size += packet.byteSize;
            }
            foreach (VRPenPacket packet in cacheNonHistoricalPackets) {
                size += packet.byteSize;
            }
            
            //display
            foreach (TextMeshPro tmp in cacheSizeDisplays) {
                if (size >= 1000000) {
                    tmp.text = (size/1000000f).ToString(".###") + " MB";
                }
                else if (size >= 1000) {
                    tmp.text = (size/1000f).ToString(".###") + " KB";
                }
                else {
                    tmp.text = size + " Bytes";
                }
            }
            
        }

        public ulong getLocalPlayerID() {
            if (!localPlayerHasID) {
                Debug.LogError("Local player id used before being set");
            }
            return localPlayerId;
        }

        /// <summary>
        /// A method that input scripts can access to add the drawing data necesarry to be sent to other clients.
        /// </summary>
        /// <param name="endLine">Boolean value that is responsible for deciding if the space between points needs to be interpolated.</param>
        /// <param name="currentColor">The color of each input.</param>
        /// <param name="x">The x coordinate, 0 - 1.</param>
        /// <param name="y">The y coordinate, 0 - 1.</param>
        /// <param name="pressure">The value that determines the thickenss of the line, 0 - 1.</param>
        /// <param name="canvasId">index of board to write to</param>
        public void addToDataOutbox(bool endLine, Color32 currentColor, float x, float y, float pressure, byte canvas, int localGraphicIndex) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;
            
            //make new temp arrays with +1 length
            bool[] tempEL = new bool[endLines.Length + 1];
            Color32[] tempC = new Color32[colors.Length + 1];
            float[] tempX = new float[xFloats.Length + 1];
            float[] tempY = new float[yFloats.Length + 1];
            float[] tempP = new float[pressures.Length + 1];
            byte[] tempCanvasIds = new byte[canvasIds.Length + 1];
            int[] tempLocalGraphicIndices = new int[localGraphicIndices.Length + 1];

            //copy existing data to temp array
            for (int i = 0; i < pressures.Length; i++) {

                tempEL[i] = endLines[i];
                tempC[i] = colors[i];
                tempX[i] = xFloats[i];
                tempY[i] = yFloats[i];
                tempP[i] = pressures[i];
                tempCanvasIds[i] = canvasIds[i];
                tempLocalGraphicIndices[i] = localGraphicIndices[i];
            }

            //add the most recent data entry
            tempEL[tempEL.Length - 1] = endLine;
            tempC[tempC.Length - 1] = currentColor;
            tempX[tempX.Length - 1] = x;
            tempY[tempY.Length - 1] = y;
            tempP[tempP.Length - 1] = pressure;
            tempCanvasIds[tempCanvasIds.Length - 1] = canvas;
            tempLocalGraphicIndices[tempLocalGraphicIndices.Length - 1] = localGraphicIndex;

            //replace the array references with the temp array references
            endLines = tempEL;
            colors = tempC;
            xFloats = tempX;
            yFloats = tempY;
            pressures = tempP;
            canvasIds = tempCanvasIds;
            localGraphicIndices = tempLocalGraphicIndices;

        }
        
        public void connect(ulong localID, bool waitForCache) {
            
            //set id
            localPlayerId = localID;
            localPlayerHasID = true;
            
            //find all graphics added before connection and set their owner id to be correct
            foreach (VectorCanvas canvas in VectorDrawing.s_instance.canvases) {
                foreach (VectorGraphic graphic in canvas.graphics) {
                    if (graphic.createdLocally) graphic.ownerId = localID;
                }
            }
            
            //if the user is the first in the server, then no need to seek catch up packet
            if (!waitForCache) {
                connectedAndCaughtUp = true;
            }
        }

        /// <summary>
        /// Packe pen data take all data accumulted since the last time it was called and returns it in a packet
        /// </summary>
        /// <returns>a byte array packet</returns>
        public byte[] packPenData() {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return null;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return null;
            }

            //checks for data
            if (xFloats.Length < 1) {
                return null;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            //header
            sendBufferList.Add((byte)PacketType.PenData);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;
            

            //identify if this entire packet only effects one canvas
            //this will be used for quickly identifying which packets can be removed from cache on a canvas clear event
            bool onlyOneCanvasID = canvasIds.Distinct().Count() == 1;
            sendBufferList.Add(onlyOneCanvasID ? (byte) 1 : (byte) 0);
            if (onlyOneCanvasID) sendBufferList.Add(canvasIds[0]);
            

            //add list length
            sendBufferList.AddRange(BitConverter.GetBytes(xFloats.Length));

            //set values for byte array
            for (int i = 0; i < xFloats.Length; i++) {

                //endline
                sendBufferList.Add(endLines[i]? (byte)1 : (byte)0);

                //color
                sendBufferList.Add(colors[i].r);
                sendBufferList.Add(colors[i].g);
                sendBufferList.Add(colors[i].b);
                sendBufferList.Add(colors[i].a);

                //position, pressures, board indexes
                sendBufferList.AddRange(BitConverter.GetBytes(xFloats[i]));
                sendBufferList.AddRange(BitConverter.GetBytes(yFloats[i]));
                sendBufferList.AddRange(BitConverter.GetBytes(pressures[i]));
                sendBufferList.Add(canvasIds[i]);
                sendBufferList.AddRange(BitConverter.GetBytes(localGraphicIndices[i]));

            }

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();



            endLines = new bool[0];
            colors = new Color32[0];
            xFloats = new float[0];
            yFloats = new float[0];
            pressures = new float[0];
            canvasIds = new byte[0];
            localGraphicIndices = new int[0];

            //return packet
            return sendBuffer;

        }

		void addPacketToHistoricalCache(VRPenPacket packet) {
            cacheHistoricalPackets.Add(packet);
		}

        void addPacketToNonHistoricalCache(VRPenPacket packet) {
            
            //remove old packets
            cacheNonHistoricalPackets.RemoveAll(nonHistoricalPacket => {

                //input device state
                if (packet.type == PacketType.InputDeviceState &&
                    nonHistoricalPacket.type == PacketType.InputDeviceState) {

                    //make sure data is set
                    if (packet.additionalDataIsSet && nonHistoricalPacket.additionalDataIsSet) {

                        //remove if the packets are describing states of same input devices
                        if ((ulong)packet.additionalData[0] == (ulong)nonHistoricalPacket.additionalData[0] &&
                            (int)packet.additionalData[1] == (int)nonHistoricalPacket.additionalData[1]) {
                            return true;
                        }
                    }
                    else {
                        Debug.LogError("Input device packet does not have additional data var set");
                    }
                }
                
                //canvas change 
                else if (packet.type == PacketType.CanvasSwitch &&
                         nonHistoricalPacket.type == PacketType.CanvasSwitch) {
                    
                    //make sure data is set
                    if (packet.additionalDataIsSet && nonHistoricalPacket.additionalDataIsSet) {

                        //remove if the displayid is the same
                        if ((byte)packet.additionalData[0] == (byte)nonHistoricalPacket.additionalData[0]) {
                            return true;
                        }
                    }
                    else {
                        Debug.LogError("Display canvas change packet does not have additional data var set");
                    }
                    
                }
                
                //UI state change 
                else if (packet.type == PacketType.UIState &&
                         nonHistoricalPacket.type == PacketType.UIState) {
                    
                    //make sure data is set
                    if (packet.additionalDataIsSet && nonHistoricalPacket.additionalDataIsSet) {

                        //remove if the displayid is the same
                        if ((byte)packet.additionalData[0] == (byte)nonHistoricalPacket.additionalData[0]) {
                            return true;
                        }
                    }
                    else {
                        Debug.LogError("UI state change packet does not have additional data var set");
                    }
                    
                }

                return false;
                
            });
            
            //add new packet
            cacheNonHistoricalPackets.Add(packet);
        }
        
        void reduceCacheByCanvasClear(byte canvasId) {
            
            //NOTE: NOT ALL DRAWING DATA FROM THIS CANVAS WILL BE CLEARED
            //only drawing packets that have ONLY this canvas in the list of updates will be removed
            //updates with data from this canvas as well as other canvases will be kept
            cacheHistoricalPackets.RemoveAll(packet => {
                
                //check if draw packet
                if (packet.type == PacketType.PenData) {

                    //make sure data is set
                    if (packet.additionalDataIsSet) {
                        
                        //check if theres is only one canvas update in packet
                        if ((bool)packet.additionalData[0] == true) {
                        
                            //delete if that only canvas is the one cleared
                            if ((byte)packet.additionalData[1] == canvasId) {
                                return true;
                            }
                            
                        }
                        
                    }
                    else {
                        Debug.LogError("PenData packet does not have additional data var set");
                    }
                    
                } 
                return false;
                
            });
        }

        void sendCache(ulong receiverId) {
            
            //get cache
            List<byte[]> fullCache = getCache();
            
            //send
            for (int x = 0; x < fullCache.Count; x++) {
                cacheEvent?.Invoke(fullCache[x], receiverId);
            }
            
        }
        
		List<byte[]> getCache() {
            
            //list of packets
            List<List<byte>> allCachePacketBatches = new List<List<byte>>();
            List<byte[]> allCachePacketBatchesArrays = new List<byte[]>();
            
            //current batch - historic packets
            List<byte> currentCachePacketBatch = new List<byte>();
            allCachePacketBatches.Add(currentCachePacketBatch);
            currentCachePacketBatch.Add(1); //true: historic cache packets
            
            //iterate through packets
            for (int x = 0; x < cacheHistoricalPackets.Count; x++) {

                //start new list if past max size
                if (currentCachePacketBatch.Count != 0 &&
                    (currentCachePacketBatch.Count + cacheHistoricalPackets[x].data.Length) >= cachePacketMaxSendingSize) {
                    
                    //new packet
                    currentCachePacketBatch = new List<byte>();
                    allCachePacketBatches.Add(currentCachePacketBatch);
                    currentCachePacketBatch.Add(1); //true: historic cache packets
                }
                
                //add data
                currentCachePacketBatch.AddRange(BitConverter.GetBytes(cacheHistoricalPackets[x].sender));
                currentCachePacketBatch.AddRange(BitConverter.GetBytes(cacheHistoricalPackets[x].data.Length));
                currentCachePacketBatch.AddRange(cacheHistoricalPackets[x].data);
                
            }
            
            //current batch - non historic packets
            currentCachePacketBatch = new List<byte>();
            allCachePacketBatches.Add(currentCachePacketBatch);
            currentCachePacketBatch.Add(0); //true: non historic cache packets
            
            //iterate through packets
            for (int x = 0; x < cacheNonHistoricalPackets.Count; x++) {

                //start new list if past max size
                if (currentCachePacketBatch.Count != 0 &&
                    (currentCachePacketBatch.Count + cacheNonHistoricalPackets[x].data.Length) >= cachePacketMaxSendingSize) {
                    
                    //new packet
                    currentCachePacketBatch = new List<byte>();
                    allCachePacketBatches.Add(currentCachePacketBatch);
                    currentCachePacketBatch.Add(0); //true: historic cache packets
                }
                
                //add data
                currentCachePacketBatch.AddRange(BitConverter.GetBytes(cacheNonHistoricalPackets[x].sender));
                currentCachePacketBatch.AddRange(BitConverter.GetBytes(cacheNonHistoricalPackets[x].data.Length));
                currentCachePacketBatch.AddRange(cacheNonHistoricalPackets[x].data);
                
            }
            
            //add cache packet batch index to start of each batch
            for (int x = 0; x < allCachePacketBatches.Count; x++) {
                allCachePacketBatches[x].InsertRange(0, BitConverter.GetBytes(allCachePacketBatches.Count));
                allCachePacketBatches[x].InsertRange(0, BitConverter.GetBytes(x));
                
                //convert to byte array
                allCachePacketBatchesArrays.Add(allCachePacketBatches[x].ToArray());
            }

            return allCachePacketBatchesArrays;

        }

		public void loadCache(byte[] data) {

            //offset
            int offset = 0;

            //read batch index data
            int currentBatchIndex = ReadInt(data, ref offset);
            int batchCount = ReadInt(data, ref offset);
            bool isHistoricBatch = ReadByte(data, ref offset) == 1;
            Debug.Log("Reading "+ (isHistoricBatch? "historic" : "non-historic")+ " cache packet batch. Batch ["+
                      (currentBatchIndex + 1) +"/"+batchCount+"]. " + data.Length + " Bytes.");
            
            //read and compute batch
            while (offset < data.Length - 1) {
            
                // get header
                ulong connectionId = ReadULong(data, ref offset);
                int length = ReadInt(data, ref offset);
            
                //get data
                byte[] packet = new byte[length];
                for (int x = 0; x < length; x++) {
                	packet[x] = data[x + offset];
                }
                offset += length;
            
                //push data through
                unpackPacket(connectionId, packet, true);
            
            }
            
            //if this was the last batch, we are all caught up 
            if (currentBatchIndex == batchCount - 1) {
                
                //set flag
                cacheDownloaded = true; 
                
                //Debug
                Debug.Log("Cache fully downloaded. Now processing packets received during connection process. Packet count: "+packetsReceivedPreCatchup.Count);

                //unpack packets received before fully connected
                for (int x = 0; x < packetsReceivedPreCatchup.Count; x++) {
                    unpackPacket(packetsReceivedPreCatchup[x].sender, packetsReceivedPreCatchup[x].data);
                }
                //Debug
                Debug.Log("VRPen instance fully caught up.");
                
                //set flag
                connectedAndCaughtUp = true; 
                
            }

        }

        public void sendCacheRequest(ulong requestFromID) {
            
            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.CacheRequest);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;

            //canvas id
            sendBufferList.AddRange(BitConverter.GetBytes(requestFromID));

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);
        }

        /// <summary>
        /// Invokes and event for clearing the board and clears locally
        /// </summary>
        /// <param name="canvasId">The id of the canvas to clear</param>
        public void sendClear(byte canvasId) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Clear);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;

            //canvas id
            sendBufferList.Add(canvasId);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);
            
            //remove from cache
            reduceCacheByCanvasClear(canvasId);


        }

        public void sendStampFile(string name, Texture2D tex, byte index) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.AddStampFile);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;

            //data
            sendBufferList.Add(index);
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            sendBufferList.AddRange(BitConverter.GetBytes(nameBytes.Length));
            sendBufferList.AddRange(nameBytes);

            sendBufferList.AddRange(BitConverter.GetBytes(tex.width));
            sendBufferList.AddRange(BitConverter.GetBytes(tex.height));
            byte[] rawTex = tex.GetRawTextureData();
            sendBufferList.AddRange(BitConverter.GetBytes(rawTex.Length));
            sendBufferList.AddRange(rawTex);


            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);


        }

        public void sendUndo(ulong playerId, int graphicIndex, byte canvasId) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Undo);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;
            
            //add data
            sendBufferList.AddRange(BitConverter.GetBytes(playerId));
            sendBufferList.AddRange(BitConverter.GetBytes(graphicIndex));
            sendBufferList.Add(canvasId);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);

        }

        public void sendCanvasAddition(byte originDisplayID, int width, int height, byte canvasId) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }
            
            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.AddCanvas);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;
            
            //data
            sendBufferList.Add(originDisplayID);
            sendBufferList.AddRange(BitConverter.GetBytes(width));
            sendBufferList.AddRange(BitConverter.GetBytes(height));
            sendBufferList.Add(canvasId);


            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send or queue
            if (!connectedAndCaughtUp) {
                onConnectPacketQueue.Enqueue(sendBuffer);
            }
            else {
                vrpenEvent?.Invoke(sendBuffer);
            }
        }

        public void sendCanvasChange(byte displayIndex, byte canvasIndex) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }


            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.CanvasSwitch);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;
            
            //data
            sendBufferList.Add(displayIndex);
            sendBufferList.Add(canvasIndex);


            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);

        }

        public void sendStamp(int stampIndex, float x, float y, float size, float rot, byte canvasId, int graphicIndex) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;
            
            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }
            
            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Stamp);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;
            
            //data
            sendBufferList.AddRange(BitConverter.GetBytes(stampIndex));
            sendBufferList.AddRange(BitConverter.GetBytes(x));
            sendBufferList.AddRange(BitConverter.GetBytes(y));
            sendBufferList.AddRange(BitConverter.GetBytes(size));
            sendBufferList.AddRange(BitConverter.GetBytes(rot));
            sendBufferList.Add(canvasId);
            sendBufferList.AddRange(BitConverter.GetBytes(graphicIndex));
            
            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send or queue
            if (!connectedAndCaughtUp) {
                onConnectPacketQueue.Enqueue(sendBuffer);
            }
            else {
                vrpenEvent?.Invoke(sendBuffer);
            }

        }
		
        

        //for when the user draws before connecting, potentially deprecating as i dont believe the user should be able
        //to draw without being connected
        //todo figure out what to do wiht
        void sendOnConnectPacketQueue() {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            while (onConnectPacketQueue.Count > 0) {
                vrpenEvent?.Invoke(onConnectPacketQueue.Dequeue());
            }
        }

        public void sendInputVisualEvent(ulong ownerId, int uniqueDeviceIdentifier, Color32 color, VRPenInput.ToolState state) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }
            
            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.InputDeviceState);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;

            //add data
            sendBufferList.AddRange(BitConverter.GetBytes(ownerId));
            sendBufferList.AddRange(BitConverter.GetBytes(uniqueDeviceIdentifier));
            sendBufferList.Add((byte)state);
            sendBufferList.Add(color.r);
            sendBufferList.Add(color.g);
            sendBufferList.Add(color.b);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();
            

            //send
            vrpenEvent?.Invoke(sendBuffer);

        }

		public void sendUIState(byte displayId, byte[] data) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.UIState);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;

            //add data
            sendBufferList.Add(displayId);
			sendBufferList.AddRange(data);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);

        }

        public void sendSharedDeviceOwnershipTransfer(int uniqueIdentifier) {
            
            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!connectedAndCaughtUp) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.SharedDeviceOwnershipTransfer);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            //add packet index to header
            sendBufferList.AddRange(BitConverter.GetBytes(localPacketIndex));
            localPacketIndex++;
            
            //add data
            sendBufferList.AddRange(BitConverter.GetBytes(uniqueIdentifier));
            
            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);
            
        }

        /// <summary>
        /// Packets recieved get pushed into here to be sorted out to the correct methods
        /// </summary>
        /// <param name="connectionId">who sent it, must be unique</param>
        /// <param name="packet">the data</param>
        /// <param name="isCache">is this a catchup packet?</param>
        public void unpackPacket(ulong connectionId, byte[] packetData, bool isCache = false) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;
            
            //offset
            int offset = 0;

            //header data
            PacketType header = (PacketType)ReadByte(packetData, ref offset);
            long timeSent = ReadLong(packetData, ref offset);
            int packetIndex = ReadInt(packetData, ref offset);
            
            //make packet obj
            VRPenPacket packet = new VRPenPacket(packetData, connectionId, packetIndex, header);
            
            //Make sure this packet hasnt already been unpacked
            //Only needa do this when unpacking non-cache packets received during catchup sequence
            if (cacheDownloaded && !connectedAndCaughtUp) {
                //if packet was already received (stored in cache) dont continue to use it
                if (cacheHistoricalPackets.Exists(x => x.sender == connectionId && x.senderPacketIndex == packetIndex) || 
                    cacheNonHistoricalPackets.Exists(x => x.sender == connectionId && x.senderPacketIndex == packetIndex) ) {
                    Debug.Log("Packet already received... ignoring... [user=" + connectionId + ", index=" + packetIndex +"]");
                    return;
                }
            }
            
            //if cache not downloaded yet, add to list of packets to process after fully connected
            //the !connectedAndCaughtUp is needed for if when no cache is needed to be downloaded
            if (!connectedAndCaughtUp && !cacheDownloaded && !isCache) {
                packetsReceivedPreCatchup.Add(packet);
                return;
            }

            
            //if local player has no id, send out warning
            if (!localPlayerHasID) {
                Debug.LogWarning("Packets are being recieved before the local player was assigned an ID, this could cause errors.");
            }

            //ignore this packet
            bool ignorePacket = false;
            
            //if the packet is from the local player
            if (connectionId == localPlayerId) {
                //catchup packet
                if (timeSent < instanceStartTime) {
                }
                //non-catchup packet
                else {
                    //ignore packets sent by self unless it is a visual event (so that if you have a local-remote version it still works)
                    if (header != PacketType.InputDeviceState) ignorePacket = true;
                }
            }

            //unpack
            if (header == PacketType.PenData) {
                unpackPenData(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.Clear) {
                unpackClear(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.AddCanvas) {
                unpackCanvasAddition(packet, ref offset, ignorePacket);
                AddedCanvas.Invoke();
            }
            else if (header == PacketType.Undo) {
                unpackUndo(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.UIState) {
                unpackUIState(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.Stamp) {
                unpackStamp(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.CanvasSwitch) {
                unpackCanvasSwitch(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.InputDeviceState) {
                unpackInputVisualEvent(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.SharedDeviceOwnershipTransfer) {
                unpackSharedDeviceOwnershipTransfer(packet, ref offset, ignorePacket);
            }
            else if (header == PacketType.CacheRequest) {
                unpackCacheRequest(packet, ref offset, ignorePacket);
            }
            else {
                Debug.LogError("Packet type not recognized, ID = " + header);
            }
            
            //add packet to caches
            if (header == PacketType.PenData || header == PacketType.Clear || header == PacketType.AddCanvas ||
                header == PacketType.Undo || header == PacketType.Stamp ) {
                addPacketToHistoricalCache(packet);
            }
            else if (header == PacketType.InputDeviceState || header == PacketType.CanvasSwitch || header == PacketType.UIState) {
                addPacketToNonHistoricalCache(packet);
            }
            
        }

        void unpackCacheRequest(VRPenPacket packet, ref int offset, bool ignorePacket) {
            
            //ignore
            if (ignorePacket) return;
            
            //get data
            ulong responderId = ReadULong(packet.data, ref offset);
            
            //send cache if this user is the responder
            if (responderId == localPlayerId) {
                sendCache(packet.sender);
            }
            
        }

        void unpackSharedDeviceOwnershipTransfer(VRPenPacket packet, ref int offset, bool ignorePacket) {

            //ignore
            if (ignorePacket) return;
            
            //get data
            int uniqueIdentifier = ReadInt(packet.data, ref offset);
            
            //change ownership
            foreach (SharedMarker sharedMarker in VectorDrawing.s_instance.sharedDevices) {
                if (uniqueIdentifier == sharedMarker.uniqueIdentifier) {
                    //assuming this is not the player that took ownership, ownership should be relinquished
                    if (packet.sender != localPlayerId) {
                        sharedMarker.relinquishOwnership();
                    }
                }
            }
            
        }

        /// <summary>
        /// If the packet was pen data, this method unpacks it and converts it into drawings
        /// </summary>
        /// <param name="player">netowrked player who sent data</param>
        /// <param name="packet">data</param>
        void unpackPenData(VRPenPacket packet, ref int offset, bool ignorePacket) {

            //identify if this entire packet only effects one canvas
            //this will be used for quickly identifying which packets can be removed from cache on a canvas clear event
            bool onlyOneCanvasID = ReadByte(packet.data, ref offset) == 1;
            byte singularCanvasID = 0;
            if (onlyOneCanvasID) singularCanvasID = ReadByte(packet.data, ref offset);

            //set additional data for use in the cache
            packet.setAdditionalData(new List<object>(){onlyOneCanvasID, singularCanvasID}, 2);
            
            //ignore (only after we make sure to set additional data)
            if (ignorePacket) return;
            
            //pen samples
            int length = ReadInt(packet.data, ref offset);
            for(int x = 0; x < length; x++) {


                byte endLine = ReadByte(packet.data, ref offset);
                Color32 color = new Color32(ReadByte(packet.data, ref offset), ReadByte(packet.data, ref offset), ReadByte(packet.data, ref offset), ReadByte(packet.data, ref offset));
                float xFloat = ReadFloat(packet.data, ref offset);
                float yFloat = ReadFloat(packet.data, ref offset);
                float pressure = ReadFloat(packet.data, ref offset);
                byte canvasId = ReadByte(packet.data, ref offset);
                int localGraphicIndex = ReadInt(packet.data, ref offset);

                //end line or draw
                if (endLine == 1) {
                    
                    VectorDrawing.s_instance.endLineEvent(packet.sender, localGraphicIndex, canvasId, false);
                }
                else {

                    if (pressure > 0) {
                        VectorDrawing.s_instance.draw(packet.sender, localGraphicIndex, (endLine == 1) ? true : false, color, xFloat, yFloat, pressure, canvasId, false);
                    }

                    else {
                        Debug.LogError("Received VRPen draw input packet with 0 pressure (not a endline event either)");
                    }

                }

            }
          
        }

        /// <summary>
        /// If the packet was a clear event, this method unpacks it and clears the correct board
        /// </summary>
        /// <param name="packet">data</param>
        void unpackClear(VRPenPacket packet, ref int offset, bool ignorePacket) {
     
            //ignore
            if (ignorePacket) return;

            //get canvas
            byte canvasId = ReadByte(packet.data, ref offset);

            //clear
            VectorDrawing.s_instance.getCanvas(canvasId).clear(false);
            
            //clear from cache
            reduceCacheByCanvasClear(canvasId);

        }

        /// <summary>
        /// If the packet was a add canvas, this method accually does it
        /// </summary>
        /// <param name="packet">data</param>
        void unpackCanvasAddition(VRPenPacket packet, ref int offset, bool ignorePacket) {

            
            //ignore
            if (ignorePacket) return;

            //get data
            byte displayId = ReadByte(packet.data, ref offset);
            int width = ReadInt(packet.data, ref offset);
            int height = ReadInt(packet.data, ref offset);
            byte canvasId = ReadByte(packet.data, ref offset);

            //add board
            VectorDrawing.s_instance.addCanvas(false, displayId, width, height, canvasId);

        }

        void unpackStampFileAddition(VRPenPacket packet, ref int offset, bool ignorePacket) {

            //ignore
            if (ignorePacket) return;

            //get index
            byte index = ReadByte(packet.data, ref offset);

            //return if we already have it
            if (PersistantData.doesStampExist(index)) {
                return;
            }

            //get stamp data
            int nameLength = ReadInt(packet.data, ref offset);
            byte[] nameBytes = ReadByteArray(packet.data, ref offset, nameLength);
            string name = Encoding.ASCII.GetString(nameBytes);

            int width = ReadInt(packet.data, ref offset);
            int height = ReadInt(packet.data, ref offset);
            int texLength = ReadInt(packet.data, ref offset);
            byte[] rawTexture = ReadByteArray(packet.data, ref offset, texLength);

            Texture2D texture = new Texture2D(width, height);
            texture.LoadRawTextureData(rawTexture);

            //add stamp
            PersistantData.addStamp(name, texture, index);

        }

        void unpackStamp(VRPenPacket packet, ref int offset, bool ignorePacket) {
            
            //ignore
            if (ignorePacket) return;

            //data
            int stampIndex = ReadInt(packet.data, ref offset);
            float x = ReadFloat(packet.data, ref offset);
            float y = ReadFloat(packet.data, ref offset);
            float size = ReadFloat(packet.data, ref offset);
            float rot = ReadFloat(packet.data, ref offset);
            byte canvasId = ReadByte(packet.data, ref offset);
            int graphicIndex = ReadInt(packet.data, ref offset);

            //get stamptexture
            Texture2D text = PersistantData.getStampTexture(stampIndex);

            //add stamp
            //vectorMan.stamp(text, stampIndex, player.connectionId, graphicIndex, x, y, size, rot, canvasId, false);
            
        }

        void unpackUndo(VRPenPacket packet, ref int offset, bool ignorePacket) {
            
            //ignore
            if (ignorePacket) return;

            //get data
            ulong playerId = ReadULong(packet.data, ref offset);
            int graphixIndex = ReadInt(packet.data, ref offset);
            byte canvasID = ReadByte(packet.data, ref offset);
            
            //apply undo
            VectorDrawing.s_instance.undo(playerId, graphixIndex, canvasID, false);
        }

        void unpackCanvasSwitch(VRPenPacket packet, ref int offset, bool ignorePacket) {

            //unpack
            byte displayId = ReadByte(packet.data, ref offset);
            byte canvasId = ReadByte(packet.data, ref offset);
            
            //set additional data for use in the cache
            packet.setAdditionalData(new List<object>(){displayId}, 1);
            
            //ignore after setting additional data but before applying any change
            if (ignorePacket) return;

            VectorDrawing.s_instance.getDisplay(displayId).swapCurrentCanvas(canvasId, false);
            
        }

        void unpackInputVisualEvent(VRPenPacket packet, ref int offset, bool ignorePacket) {

            //get data
            ulong ownerID = ReadULong(packet.data, ref offset);
            int uniqueID = ReadInt(packet.data, ref offset);
            VRPenInput.ToolState state = (VRPenInput.ToolState)ReadByte(packet.data, ref offset);
            Color32 col = new Color32(ReadByte(packet.data, ref offset), ReadByte(packet.data, ref offset), ReadByte(packet.data, ref offset), 255);

            
            //set additional data for use in the cache
            packet.setAdditionalData(new List<object>(){ownerID, uniqueID}, 12);
            
            //ignore (only after we make sure to set additional data)
            if (ignorePacket) return;
            
            //update device
            foreach (InputVisuals device in VectorDrawing.s_instance.inputDevices) {
                if (device.ownerID == ownerID && device.uniqueIdentifier == uniqueID) {
                    device.updateModel(state, false);
                    device.updateColor(col, false);
                }
            }
        }

        void unpackUIState(VRPenPacket packet, ref int offset, bool ignorePacket) {

            

			//data
			byte displayId = ReadByte(packet.data, ref offset);
			Display display = VectorDrawing.s_instance.getDisplay(displayId);
            byte[] choppedPacket = new byte[packet.data.Length - offset];
            for (int x = 0; x < choppedPacket.Length; x++) {
                choppedPacket[x] = packet.data[x + offset];
            }
            
            //set additional data for use in the cache
            packet.setAdditionalData(new List<object>(){displayId}, 1);
            
            //ignore after getting additional data
            if (ignorePacket) return;

			//update state
			display.UIMan.unpackState(choppedPacket);

        }

        #region Serialization

        void PackByte(byte b, byte[] buf, ref int offset) {
            buf[offset] = b;
            offset += sizeof(byte);
        }

        byte ReadByte(byte[] buf, ref int offset) {
            byte val = buf[offset];
            offset += sizeof(byte);
            return val;
        }

        void PackFloat(float f, byte[] buf, ref int offset) {
            Buffer.BlockCopy(BitConverter.GetBytes(f), 0, buf, offset, sizeof(float));
            offset += sizeof(float);
        }

        public static float ReadFloat(byte[] buf, ref int offset) {
            float val = BitConverter.ToSingle(buf, offset);
            offset += sizeof(float);
            return val;
        }

        short ReadShort(byte[] buf, ref int offset) {
            short val = BitConverter.ToInt16(buf, offset);
            offset += sizeof(short);
            return val;
        }
		

        void PackULong(ulong u, byte[] buf, ref int offset) {
            Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(ulong));
            offset += sizeof(ulong);
        }

        ulong ReadULong(byte[] buf, ref int offset) {
            ulong val = BitConverter.ToUInt64(buf, offset);
            offset += sizeof(ulong);
            return val;
        }

        long ReadLong(byte[] buf, ref int offset) {
            long val = BitConverter.ToInt64(buf, offset);
            offset += sizeof(long);
            return val;
        }

        void PackUInt32(UInt32 u, byte[] buf, ref int offset) {
            Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(UInt32));
            offset += sizeof(UInt32);
        }

		void PackInt32(Int32 u, byte[] buf, ref int offset) {
			Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(Int32));
			offset += sizeof(Int32);
		}

		UInt32 ReadUInt32(byte[] buf, ref int offset) {
            UInt32 val = BitConverter.ToUInt32(buf, offset);
            offset += sizeof(UInt32);
            return val;
        }

        int ReadInt(byte[] buf, ref int offset) {
            int val = BitConverter.ToInt32(buf, offset);
            offset += sizeof(Int32);
            return val;
        }

        Vector3 ReadVector3(byte[] buf, ref int offset) {
            Vector3 vec;
            vec.x = ReadFloat(buf, ref offset);
            vec.y = ReadFloat(buf, ref offset);
            vec.z = ReadFloat(buf, ref offset);
            return vec;
        }

        Quaternion ReadQuaternion(byte[] buf, ref int offset) {
            Quaternion quat;
            quat.x = ReadFloat(buf, ref offset);
            quat.y = ReadFloat(buf, ref offset);
            quat.z = ReadFloat(buf, ref offset);
            quat.w = ReadFloat(buf, ref offset);
            return quat;
        }

        byte[] ReadByteArray(byte[] buf, ref int offset, int length) {
            byte[] temp = new byte[length];
            for (int x = 0; x <  length; x++) {
                temp[x] = buf[x + offset];
            }
            offset += length;
            return temp;
        }

        #endregion

    }

}
