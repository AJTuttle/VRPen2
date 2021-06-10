using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using UnityEngine.Events;

namespace VRPen {

    /// <summary>
    /// Type of packet, used as a header for sending/recieving packets
    /// </summary>
    enum PacketType : byte {
        PenData,
        Clear,
        Connect,
        AddCanvas,
        Undo,
        Stamp,
		UIState,
		CanvasSwitch,
        InputVisualsEvent,
        AddStampFile,
        SharedDeviceOwnershipTransfer
    }

    public class NetworkManager : MonoBehaviour {

        //instance
        public static NetworkManager s_instance;

        //scripts
        [System.NonSerialized]
        public VectorDrawing vectorMan;
        UIManager UIMan;

        [Space(5)]
        [Header("       Important variables to set")]
        [Space(5)]
        [Tooltip("If enabled, this script will attempt to invoke an event for sending a connection packet shortly after start " +
            "instead of waited for the sendConnect method to be called")]
        public bool autoConnect;
        [Tooltip("If enabled, when one personn switches canvas, the canvas on the same display will auto switch for other users as well")]
        public bool syncCurrentCanvas;
        [Tooltip("If enabled, ui elements will sync")]
        public bool syncDisplayUIs;
        [Tooltip("Period of ui syncing (does not do anything if uisync is disabled")]
        public float UI_SYNC_PERIOD;
        [Tooltip("Where to put the spawned remote devices in global space")]
        public Vector3 spawnRemoteDevicesLocation;
        //[Tooltip("Should networkMan instantiate remote input device or simple make a data struct")]
        //public bool instantiateRemoteInputDevices;


        //other players data
        public List<NetworkedPlayer> players = new List<NetworkedPlayer>();
        private NetworkedPlayer localPlayer;
        bool localPlayerHasID = false;

        //other vars
        long instanceStartTime; //used to differentiate catchup packets and current instance packets
        [NonSerialized]
        public bool sentConnect = false;


        //cache packets
        private List<byte[]> packetCache = new List<byte[]>();
        private List<ulong> packetSenders = new List<ulong>();
        public bool cachePackets;

        //prefabs
        [Space(5)]
        [Header("       Variables that don't need to be changed")]
        [Space(15)]
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
		public delegate void remoteInputDeviceSpawned(GameObject obj, int deviceIndex);
		public event remoteInputDeviceSpawned remoteSpawn;
		
		public delegate void AddCanvasHandler();
		public event AddCanvasHandler AddedCanvas = delegate { };


        //
        Queue<byte[]> onConnectPacketQueue = new Queue<byte[]>();
        
        
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

            //scripts
            UIMan = FindObjectOfType<UIManager>();
            vectorMan = GetComponent<VectorDrawing>();

            //instantiate local player if it doesnt already exist
            getLocalPlayer();

            if(autoConnect) Invoke(nameof(sendConnect), 0.05f);


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

        public NetworkedPlayer getNetworkedPlayer(ulong connectionId) {
            NetworkedPlayer player = players.Find(p => p.connectionId == connectionId);
            return player;
        }

        public NetworkedPlayer getLocalPlayer() {
            if (localPlayer == null) {
                localPlayer = new NetworkedPlayer();
                localPlayer.connectionId = 0;
                players.Add(localPlayer);
            }
            return localPlayer;
        }

        public void setLocalId(ulong ID) {
            if (localPlayer == null) {
                Debug.Log("Local player needs to be setup before adding the ID");
                return;
            }
            localPlayer.connectionId = ID;
            localPlayerHasID = true;
            
            //find all graphics added before connection and set their owner id to be correct
            foreach (VectorCanvas canvas in vectorMan.canvases) {
                foreach (VectorGraphic graphic in canvas.graphics) {
                    if (graphic.createdLocally) graphic.ownerId = ID;
                }
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
            if (!sentConnect) {
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

            //laser state                                                                TO-DO
            sendBufferList.Add(0);

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

		void cachePacket(byte[] data, ulong connectionId) {

            if (cachePackets) {
				packetCache.Add(data);
				packetSenders.Add(connectionId);
			}
		}

		public byte[] getCache() {

			//size
			int size = 0;
			foreach (byte[] arr in packetCache) {
				size += arr.Length;
			}
			size += packetSenders.Count * 8;   //8 bytes for the sender id for each packet
			size += packetCache.Count * 4;     //4 bytes for the length of each packet

			//make the packet
			byte[] bigboy = new byte[size];
			int offset = 0;
			for (int x = 0; x < packetCache.Count; x++) {
				PackULong(packetSenders[x], bigboy, ref offset);
				PackInt32(packetCache[x].Length, bigboy, ref offset);
				for (int y = 0; y < packetCache[x].Length; y++) {
					PackByte(packetCache[x][y], bigboy, ref offset); 
				}
			}
			return bigboy;

		}

		public void loadCache(byte[] data) {

			int offset = 0;
			
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
				unpackPacket(connectionId, packet);

			}

		}


        /// <summary>
        /// Invokes and event for clearing the board and clears locally
        /// </summary>
        /// <param name="canvasId">The id of the canvas to clear</param>
        public void sendClear(byte canvasId) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!sentConnect) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Clear);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));

            //canvas id
            sendBufferList.Add(canvasId);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);
            

        }

        public void sendStampFile(string name, Texture2D tex, byte index) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!sentConnect) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.AddStampFile);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));

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
            if (!sentConnect) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Undo);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            
            //add data
            sendBufferList.AddRange(BitConverter.GetBytes(playerId));
            sendBufferList.AddRange(BitConverter.GetBytes(graphicIndex));
            sendBufferList.Add(canvasId);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send
            vrpenEvent?.Invoke(sendBuffer);

        }

        public void sendCanvasAddition(bool isPublic, byte originDisplayID, int width, int height, bool isPreset, byte canvasId) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.AddCanvas);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            sendBufferList.Add(isPublic?(byte)1:(byte)0);
            sendBufferList.Add(originDisplayID);
            sendBufferList.AddRange(BitConverter.GetBytes(width));
            sendBufferList.AddRange(BitConverter.GetBytes(height));
            sendBufferList.Add(isPreset ? (byte)1 : (byte)0);
            sendBufferList.Add(canvasId);


            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //send or queue
            if (!sentConnect) {
                onConnectPacketQueue.Enqueue(sendBuffer);
            }
            else {
                vrpenEvent?.Invoke(sendBuffer);
            }
        }

        public void sendCanvasChange(byte displayIndex, byte canvasIndex) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            if (!syncCurrentCanvas) {
                return;
            }

            //dont send if you havent connected to the other users yet
            if (!sentConnect) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }


            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.CanvasSwitch);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
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
            
            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Stamp);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
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
            if (!sentConnect) {
                onConnectPacketQueue.Enqueue(sendBuffer);
            }
            else {
                vrpenEvent?.Invoke(sendBuffer);
            }

        }
		

        public void sendConnect() {
            sendConnect(true);
        }

        public void sendConnect(bool requestReply) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //if local player has no id, send out warning
            if (!localPlayerHasID) {
                Debug.LogWarning("Connection packet is being sent without assigned a local player ID first, this could cause errors.");
            }
            
            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.Connect);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));

            //add data
            sendBufferList.Add(requestReply ? (byte)1 : (byte)0); //request reply?
            
            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();

            //if initial connection
            if (!sentConnect) {
                //start syncing displays
                if (syncDisplayUIs) {
                    foreach (Display dis in vectorMan.displays) {
                        dis.UIMan.startPackingState(UI_SYNC_PERIOD);
                    }
                }
            }

            //send
            sentConnect = true;
            vrpenEvent?.Invoke(sendBuffer);

            //send on connect packet queue
            sendOnConnectPacketQueue();
            
        }

        void sendOnConnectPacketQueue() {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            while (onConnectPacketQueue.Count > 0) {
                vrpenEvent?.Invoke(onConnectPacketQueue.Dequeue());
            }
        }

        public void sendInputVisualEvent(int uniqueDeviceIdentifier, Color32 color, VRPenInput.ToolState state) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.InputVisualsEvent);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));

            //add data
            sendBufferList.AddRange(BitConverter.GetBytes(uniqueDeviceIdentifier));
            sendBufferList.Add((byte)state);
            sendBufferList.Add(color.r);
            sendBufferList.Add(color.g);
            sendBufferList.Add(color.b);

            // convert to an array
            byte[] sendBuffer = sendBufferList.ToArray();
            

            //send
            sentConnect = true;
            vrpenEvent?.Invoke(sendBuffer);

        }

		public void sendUIState(byte displayId, byte[] data) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            //dont send if you havent connected to the other users yet
            if (!sentConnect) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.UIState);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));

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
            if (!sentConnect) {
                Debug.LogError("Attempted to send a data packet prior to sending a connect packet. Aborting.");
                return;
            }

            //mmake buffer list
            List<byte> sendBufferList = new List<byte>();

            // header
            sendBufferList.Add((byte)PacketType.SharedDeviceOwnershipTransfer);
            sendBufferList.AddRange(BitConverter.GetBytes(DateTime.Now.Ticks));
            
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
        public void unpackPacket(ulong connectionId, byte[] packet) {

            //dont do anything in offline mode
            if (VectorDrawing.OfflineMode) return;

            int offset = 0;

            PacketType header = (PacketType)ReadByte(packet, ref offset);
            long timeSent = ReadLong(packet, ref offset);

            //if local player has no id, send out warning
            if (!localPlayerHasID) {
                Debug.LogWarning("Packets are being recieved before the local player was assigned an ID, this could cause errors.");
            }

            //manage connection id                                        
            NetworkedPlayer player = players.Find(p => p.connectionId == connectionId);
            if (player == null && header != PacketType.Connect) {
                Debug.LogError("Player is null in a non-connection packet    " + connectionId + "   " + players.Count);
            }

            //ignore this packet
            bool ignorePacket = false;
            
            //if the packet is from the local player
            if (connectionId == localPlayer.connectionId) {
                //catchup packet
                if (timeSent < instanceStartTime) {
                }
                //non-catchup packet
                else {
                    //ignore packets sent by self unless it is a visual event (so that if you have a local-remote version it still works)
                    if (header != PacketType.InputVisualsEvent) ignorePacket = true;
                }
            }

            //if not ignored
            if (!ignorePacket) {

                if (header == PacketType.PenData) {
                    unpackPenData(player, packet, ref offset);
                }

                else if (header == PacketType.Clear) {
                    unpackClear(packet, ref offset);
                }

                else if (header == PacketType.AddCanvas) {
                    unpackCanvasAddition(packet, ref offset);
                    AddedCanvas.Invoke();
                }

                else if (header == PacketType.Connect) {
                    unpackConnect(player, packet, connectionId, ref offset);
                }

                else if (header == PacketType.Undo) {
                    unpackUndo(packet, ref offset);
                }

                else if (header == PacketType.UIState) {
                    unpackUIState(packet, ref offset);
                }

                else if (header == PacketType.Stamp) {
                    unpackStamp(player, packet, ref offset);
                }

                else if (header == PacketType.CanvasSwitch) {
                    unpackCanvasSwitch(packet, ref offset);
                }

                else if (header == PacketType.InputVisualsEvent) {
                    unpackInputVisualEvent(player, packet, ref offset);
                }
                
                else if (header == PacketType.SharedDeviceOwnershipTransfer) {
                    unpackSharedDeviceOwnershipTransfer(player, packet, ref offset);
                }

                else {
                    Debug.LogError("Packet type not recognized, ID = " + header);
                }

            }

        }


        void unpackSharedDeviceOwnershipTransfer(NetworkedPlayer player, byte[] packet, ref int offset) {

            //get data
            int uniqueIdentifier = ReadInt(packet, ref offset);
            
            //change ownership
            foreach (SharedMarker sharedMarker in vectorMan.sharedDevices) {
                if (uniqueIdentifier == sharedMarker.uniqueIdentifier) {
                    //assuming this is not the player that took ownership, ownership should be relinquished
                    if (player.connectionId != localPlayer.connectionId) {
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
        void unpackPenData(NetworkedPlayer player, byte[] packet, ref int offset) {

           
            //data
            bool laserState = ReadByte(packet, ref offset) != 0;

            //pen samples
            int length = ReadInt(packet, ref offset);
            for(int x = 0; x < length; x++) {


                byte endLine = ReadByte(packet, ref offset);
                Color32 color = new Color32(ReadByte(packet, ref offset), ReadByte(packet, ref offset), ReadByte(packet, ref offset), ReadByte(packet, ref offset));
                float xFloat = ReadFloat(packet, ref offset);
                float yFloat = ReadFloat(packet, ref offset);
                float pressure = ReadFloat(packet, ref offset);
                byte canvasId = ReadByte(packet, ref offset);
                int localGraphicIndex = ReadInt(packet, ref offset);

                //end line or draw
                if (endLine == 1) {
                    
                    vectorMan.endLineEvent(player, localGraphicIndex, canvasId, false);
                }
                else {

                    if (pressure > 0) {
                        vectorMan.draw(player, localGraphicIndex, (endLine == 1) ? true : false, color, xFloat, yFloat, pressure, canvasId, false);
                    }

                    else {
                        Debug.LogError("Received VRPen draw input packet with 0 pressure (not a newline event either)");
                    }

                    //update cursor at the last input
                    if (x == length - 1) {
                        //input.updateTabletCursor(player, xFloat, yFloat);
                    }

                }

            }
          
        }

        /// <summary>
        /// If the packet was a clear event, this method unpacks it and clears the correct board
        /// </summary>
        /// <param name="packet">data</param>
        void unpackClear(byte[] packet, ref int offset) {
     
            //get canvas
            byte canvasId = ReadByte(packet, ref offset);

            //clear
            vectorMan.getCanvas(canvasId).clear(false);

        }

        /// <summary>
        /// If the packet was a add canvas, this method accually does it
        /// </summary>
        /// <param name="packet">data</param>
        void unpackCanvasAddition(byte[] packet, ref int offset) {

            //get data
            bool isPublic = ReadByte(packet, ref offset) == 1;
            byte displayId = ReadByte(packet, ref offset);
            int width = ReadInt(packet, ref offset);
            int height = ReadInt(packet, ref offset);
            bool isPreset= ReadByte(packet, ref offset) == 1;
            byte canvasId = ReadByte(packet, ref offset);

            //add board
            vectorMan.addCanvas(false, isPublic, displayId, width, height, isPreset, canvasId);

        }

        void unpackStampFileAddition(byte[] packet, ref int offset) {

            //get index
            byte index = ReadByte(packet, ref offset);

            //return if we already have it
            if (PersistantData.doesStampExist(index)) {
                return;
            }

            //get stamp data
            int nameLength = ReadInt(packet, ref offset);
            byte[] nameBytes = ReadByteArray(packet, ref offset, nameLength);
            string name = Encoding.ASCII.GetString(nameBytes);

            int width = ReadInt(packet, ref offset);
            int height = ReadInt(packet, ref offset);
            int texLength = ReadInt(packet, ref offset);
            byte[] rawTexture = ReadByteArray(packet, ref offset, texLength);

            Texture2D texture = new Texture2D(width, height);
            texture.LoadRawTextureData(rawTexture);

            //add stamp
            PersistantData.addStamp(name, texture, index);

        }

        void unpackStamp(NetworkedPlayer player, byte[] packet, ref int offset) {
            

            //data
            int stampIndex = ReadInt(packet, ref offset);
            float x = ReadFloat(packet, ref offset);
            float y = ReadFloat(packet, ref offset);
            float size = ReadFloat(packet, ref offset);
            float rot = ReadFloat(packet, ref offset);
            byte canvasId = ReadByte(packet, ref offset);
            int graphicIndex = ReadInt(packet, ref offset);

            //get stamptexture
            Texture2D text = PersistantData.getStampTexture(stampIndex);

            //add stamp
            vectorMan.stamp(text, stampIndex, player.connectionId, graphicIndex, x, y, size, rot, canvasId, false);
            
        }

        void unpackUndo(byte[] packet, ref int offset) {
            
            //get data
            ulong playerId = ReadULong(packet, ref offset);
            int graphixIndex = ReadInt(packet, ref offset);
            byte canvasID = ReadByte(packet, ref offset);
            
            //apply undo
            vectorMan.undo(playerId, graphixIndex, canvasID, false);
        }

        void unpackCanvasSwitch(byte[] packet, ref int offset) {

            if (!syncCurrentCanvas) {
                return;
            }

            byte displayId = ReadByte(packet, ref offset);
            byte canvasId = ReadByte(packet, ref offset);

            vectorMan.getDisplay(displayId).swapCurrentCanvas(canvasId, false);
            
        }

        void unpackInputVisualEvent(NetworkedPlayer player, byte[] packet, ref int offset) {
            
            //get data
            int uniqueID = ReadInt(packet, ref offset);
            VRPenInput.ToolState state = (VRPenInput.ToolState)ReadByte(packet, ref offset);
            Color32 col = new Color32(ReadByte(packet, ref offset), ReadByte(packet, ref offset), ReadByte(packet, ref offset), 255);

            //update device
            foreach (InputVisuals device in vectorMan.inputDevices) {
                if (device.ownerID == player.connectionId && device.uniqueIdentifier == uniqueID) {
                    device.updateModel(state, false);
                    device.updateColor(col, false);
                }
            }
        }

        void unpackUIState(byte[] packet, ref int offset) {

            //return if not in sync state
            if (!syncDisplayUIs) return;

			//data
			byte displayId = ReadByte(packet, ref offset);
			Display display = vectorMan.getDisplay(displayId);
            byte[] choppedPacket = new byte[packet.Length - offset];
            for (int x = 0; x < choppedPacket.Length; x++) {
                choppedPacket[x] = packet[x + offset];
            }

			//update state
			display.UIMan.unpackState(choppedPacket);

        }

        void unpackConnect(NetworkedPlayer player, byte[] packet, ulong connectionId, ref int offset) {
            

            if (ReadByte(packet, ref offset) == 1) {
                sendConnect(false);
            }

            if (player != null) {
                Debug.Log("Player already initialized, this connect packet is ignored");
                return;
            }
            
            player = new NetworkedPlayer();
            player.connectionId = connectionId;
            players.Add(player);
            
            
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
