using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace VRPen {

	public static class PersistantData {

		private static string[] stampFileNames;

        #if UNITY_EDITOR

        [MenuItem("VRPen/UpdateStampPaths")]
		public static void openStampResources() {

			//clear file 
			string path = Application.dataPath + "/Resources/StampData.txt";
			File.Create(path).Close();

			//check resources folder for files
			string[] filePaths = Directory.GetFiles(Application.dataPath + "/Resources");
			Debug.Log("Updated stamp file resources");

			//add pictures to stampdata file
			StreamWriter sw = new StreamWriter(path, true);
			for (int x = 0; x < filePaths.Length; x++) {
				string str = filePaths[x].Substring(Application.dataPath.Length + "/Resources".Length + 1);
				if (str.Substring(str.Length - 4).Equals(".png") || str.Substring(str.Length - 4).Equals(".jpg")) {
					str = str.Substring(0, str.Length - 4);
					sw.WriteLine(str);
				}
			}

			//end
			sw.Close();


		}

        #endif

        public static void instantiate() {

			//stamp files
			TextAsset stampFile = Resources.Load<TextAsset>("StampData");
			stampFileNames = stampFile.ToString().Trim(new char[] { '\n' }).Split(new char[] { '\n' });
			
		}

		public static string getStampFileName(int index) {

			if (stampFileNames == null) {
				Debug.LogError("Stamp files not found due to persistant data not being instantiated");
				return null;
			}

			else if (stampFileNames.Length <= index) {
				Debug.LogError("Stamp file not found due to persistant data not being updated");
				return null;
			}

			else {
				return stampFileNames[index].Trim(new char[] { '\r' });
			}

		}

		//-1 means not instantiated
		public static int getStampFileNameCount() {
			if (stampFileNames == null) return -1;
			else return stampFileNames.Length;
		}
	}
}
