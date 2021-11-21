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
        public static void export(Texture2D tex,  string name, int count = 0) {

            

            //get directory for saving
			#if UNITY_ANDROID && !UNITY_EDITOR
			string saveDir = Application.persistentDataPath;
			#else
            string saveDir = Application.dataPath;
			#endif
	        if (VectorDrawing.s_instance.savedPNGPathOverride.Length > 0)
		        saveDir = VectorDrawing.s_instance.savedPNGPathOverride;
	        saveDir += "\\"+name;
	        if (count > 0) saveDir += "(" + count +")";
	        saveDir += ".png";
	        
	        //if save dir exists, add one to counter
	        if (File.Exists(saveDir)) export(tex, name, count+1);
	        
	        //debug
	        Debug.Log("SAVING IMAGE: " +saveDir);
	        
	        //make into png file
	        byte[] png = tex.EncodeToPNG();
	        
	        //save
            File.WriteAllBytes(saveDir, png);

        }
        
		public static void export(RenderTexture rt, string name) {

			Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			tex.Apply();

			export(tex, name);

		}
    }
}





