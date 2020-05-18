using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace VRPen {

    public static class PersistantData {

        private static Dictionary<byte, string> stampFileNames;
        private static Dictionary<byte, Texture2D> stampTextures;

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
                string str = filePaths[x].Substring(Application.dataPath.Length + "\\Resources/VRpen/Stamps\\".Length);
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
            stampTextures = new Dictionary<byte, Texture2D>();
            stampFileNames = new Dictionary<byte, string>();
            TextAsset stampFile = Resources.Load<TextAsset>("VRPen/Stamps/StampData");

            string[] tempNames = stampFile.ToString().Trim(new char[] { '\n' }).Split(new char[] { '\n' });
            for (int x = 0; x < tempNames.Length; x++) {
                string name = tempNames[x].Trim(new char[] { '\r' });
                addStamp(name, Resources.Load<Texture2D>("VRpen/Stamps\\"+name), (byte)x);
            }
            
        }

        public static void addStamp(string name, Texture2D tex, byte index) {
            
            if (stampFileNames == null) {
                Debug.LogError("Stamp could not be added due to persistant data not being instantiated");
                return;
            }

            if (stampTextures.ContainsKey(index)) {
                Debug.LogError("Stamp could not be added since key already exists");
            }

            stampFileNames.Add(index, name);
            stampTextures.Add(index, tex);

        }

		public static string getStampFileName(int index) {

			if (stampFileNames == null) {
				Debug.LogError("Stamp files not found due to persistant data not being instantiated");
				return null;
			}

			else if (!stampFileNames.ContainsKey((byte)index)) {
				Debug.LogError("Stamp file not found due to stamp index not existing");
				return null;
			}

			else {
				return stampFileNames[(byte)index];
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

            else if (!stampTextures.ContainsKey((byte)index)) {
                Debug.LogError("Stamp file not found due to stamp index not existing");
                return null;
            }

            else {
                return stampTextures[(byte)index];
            }

        }
        
	}

}
