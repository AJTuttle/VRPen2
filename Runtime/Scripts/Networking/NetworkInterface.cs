using UnityEngine;

namespace VRPen {

	public abstract class NetworkInterface : MonoBehaviour {

		//scripts
		VRPen.NetworkManager vrpenNetwork;


		//timer
		const float PACKET_SEND_TIMER = 0.1f;

		//piping inputData
		bool pipingData = false;

		public enum PacketCategory : byte {
			normalPacket,
			cachePacket
		}


		protected void Start() {

			//get scripts
			vrpenNetwork = NetworkManager.s_instance;

			//make sure not offline
			if (VRPen.VectorDrawing.OfflineMode) return;

			//listen for events
			vrpenNetwork.vrpenEvent += normalPacketListener;
			vrpenNetwork.cacheEvent += cachePacketListener;

		}

		private void Update() {

			//start piping data
			if (!pipingData && vrpenNetwork.connectedAndCaughtUp) beginPipingData();

		}

		/// <summary>
		/// Once connected to server, call this to establish vrpen connection.
		/// </summary>
		/// <param name="connectionID">id of this local user</param>
		/// <param name="requestCache">request catch up packets?</param>
		/// <param name="userToRequestCacheFrom">if catchup packets are needed, choose a currently connected user to request it from. it doesnt matter who</param>
		public void connectedToServer(ulong connectionID, bool requestCache, ulong userToRequestCacheFrom = ulong.MaxValue) {

			//tell vrpen that its connected
			vrpenNetwork.connect(connectionID, requestCache);

			//request catch up cache
			if (requestCache) vrpenNetwork.sendCacheRequest(userToRequestCacheFrom);

		}

		void beginPipingData() {

			//set bool
			pipingData = true;

			//start send timer
			InvokeRepeating(nameof(sendPenData), 0f, PACKET_SEND_TIMER);
		}

		void sendPenData() {

			//get packet
			byte[] vrpenPacket = vrpenNetwork.packPenData();
			
			//check if there was data to send
			if (vrpenPacket == null) return;

			//send
			sendPacketToAll(PacketCategory.normalPacket, vrpenPacket);

		}

		void normalPacketListener(byte[] packet) {
			sendPacketToAll(PacketCategory.normalPacket, packet);
		}

		void cachePacketListener(byte[] packet, ulong receiverID) {
			sendPacketToIndividual(PacketCategory.cachePacket, packet, receiverID);
		}

		protected abstract void sendPacketToAll(PacketCategory category, byte[] packet);

		protected abstract void sendPacketToIndividual(PacketCategory category, byte[] packet, ulong receiverID);

		public void receivePacket(PacketCategory category, byte[] packet, ulong senderID) {
			switch (category) {
				case PacketCategory.normalPacket:
					receiveNormalPacket(packet, senderID);
					break;
				case PacketCategory.cachePacket:
					receiveCache(packet);
					break;
			}
		}

		void receiveCache(byte[] packet) {

			//make sure not offline
			if (VRPen.VectorDrawing.OfflineMode) return;

			//unpack
			vrpenNetwork.loadCache(packet);
		}

		void receiveNormalPacket(byte[] packet, ulong senderID) {

			//make sure not offline
			if (VRPen.VectorDrawing.OfflineMode) return;
			
			//move to unpacking
			vrpenNetwork.unpackPacket(senderID, packet);
		}

	}

}