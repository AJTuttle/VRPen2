using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace VRPen {

	public static class PersistantData {

		private static List<string> stampFileNames;
		private static List<Texture2D> stampTextures = new List<Texture2D>();

        #if UNITY_EDITOR

        [MenuItem("VRPen/UpdateStampPaths")]
		public static void openStampResources() {

			//check if resources folder exists
			bool generate = false;
			if (!Directory.Exists(Application.dataPath + "/Resources")) generate = true;
			else if (!Directory.Exists(Application.dataPath + "/Resources/VRPen")) generate = true;
			else if (!Directory.Exists(Application.dataPath + "/Resources/VRpen/Stamps")) generate = true;

			if (generate) generateStampResources();

			//clear file 
			string path = Application.dataPath + "/Resources/VRpen/Stamps/StampData.txt";
			File.Create(path).Close();

			//check resources folder for files
			string[] filePaths = Directory.GetFiles(Application.dataPath + "/Resources/VRpen/Stamps");
			Debug.Log("Updating stamp file resources: checking " + filePaths.Length + " files");
            int count = 0;

			//add pictures to stampdata file
			StreamWriter sw = new StreamWriter(path, true);
			for (int x = 0; x < filePaths.Length; x++) {
				string str = filePaths[x].Substring(Application.dataPath.Length + "\\Resources".Length + 1);
				if (str.Substring(str.Length - 4).Equals(".png") || str.Substring(str.Length - 4).Equals(".jpg")) {
					str = str.Substring(0, str.Length - 4);
					sw.WriteLine(str);
                    count++;
				}
			}
            Debug.Log("Updating stamp file resources: found " + count + " files");


            //end
            sw.Close();


		}

		[MenuItem("VRPen/GenerateResourcesStructure")]
		public static void generateStampResources() {
			Debug.Log("Generating resources structure");

			if (!Directory.Exists(Application.dataPath + "/Resources")) {
				Directory.CreateDirectory(Application.dataPath + "/Resources");
			} 
			if (!Directory.Exists(Application.dataPath + "/Resources/VRPen")) {
				Directory.CreateDirectory(Application.dataPath + "/Resources/VRPen");
			} 
			if (!Directory.Exists(Application.dataPath + "/Resources/VRpen/Stamps")) {
				Directory.CreateDirectory(Application.dataPath + "/Resources/VRpen/Stamps");
			} 
		}

        #endif

        public static void instantiate() {

			//stamp files
			TextAsset stampFile = Resources.Load<TextAsset>("VRPen/Stamps/StampData");
			stampFileNames = new List<string>(stampFile.ToString().Trim(new char[] { '\n' }).Split(new char[] { '\n' }));
			for (int x = 0; x < stampFileNames.Count; x++) {
                stampTextures.Add(Resources.Load<Texture2D>(getStampFileName(x)));
            }
			
		}

		public static string getStampFileName(int index) {

			if (stampFileNames == null) {
				Debug.LogError("Stamp files not found due to persistant data not being instantiated");
				return null;
			}

			else if (stampFileNames.Count <= index) {
				Debug.LogError("Stamp file not found due to persistant data not being updated");
				return null;
			}

			else {
				return stampFileNames[index].Trim(new char[] { '\r' });
			}

		}

		//-1 means not instantiated
		public static int getStampFileNameCount() {
            if (stampFileNames == null) instantiate();
            return stampFileNames.Count;
		}

        public static Texture2D getStampTexture(int index) {

            if (stampTextures == null) {
                Debug.LogError("Stamp files not found due to persistant data not being instantiated");
                return null;
            }

            else if (stampFileNames.Count <= index) {
                Debug.LogError("Stamp file not found due to persistant data not being updated");
                return null;
            }

            else {
                return stampTextures[index];
            }

        }
        
	}

}
