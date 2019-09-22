using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace VRPen {

    public static class TextureSaver {


        


        /// <summary>
        /// The export method writes the texture in disc space as a png. 
        /// </summary>
        /// <param name="tex">takes in the texture to be exported</param>
        public static void export(Texture2D tex) {

            //make into png file
            byte[] png = tex.EncodeToPNG();

            //get directory for saving
			#if UNITY_ANDROID && !UNITY_EDITOR
			string saveDir = Application.persistentDataPath;
			#else
            string saveDir = Application.dataPath;
			#endif

			//vars for tracking each digit 
			byte d2 = 0;
            byte d1 = 0;
            byte d0 = 0;

            //find a file index that isnt already taken
            for (; d2 < 10; d2++) {
                d1 = 0;
                for (; d1 < 10; d1++) {
                    d0 = 0;
                    for (; d0 < 10; d0++) {
                        if (!File.Exists(saveDir + "/save" + d2 + "" + d1 + "" + d0 + ".png")) {
                            goto fileNumberFound;
                        }
                    }
                }
            }

			//exit for loop
			fileNumberFound:

            //save
            saveDir += "/save" + d2 + "" + d1 + "" + d0 + ".png";
            File.WriteAllBytes(saveDir, png);

        }
        
		public static void export(RenderTexture rt) {

			Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();

			export(tex);

		}
    }
}





