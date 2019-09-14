using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace VRPen {

    public static class TextureSaver {

        

        //importing vars
        //public List<Texture2D> importList;
        //List<Texture2D[,]> importedArraysList = new List<Texture2D[,]>();





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

    /*
    void importAll() {

        //add default image to pics
        Texture2D[,] defaultTex = canvas.planesTex;
        importedArraysList.Add(defaultTex);
        
        //add imported pics to list
        foreach (Texture2D texture in importList) {
            convertToTextureArray(texture);
        }

    }

    IEnumerator convertToTextureArray(Texture2D full) {

        //make the texture array and add to import list
        Texture2D[,] textureArray = new Texture2D[canvas.width,canvas.height];
        importedArraysList.Add(textureArray);

        //go through each sector
        for (int x = 0; x < canvas.width / canvas.sectorDimensions; x++) {
            for (int y = 0; y < canvas.height / canvas.sectorDimensions; y++) {

                //get pixels
                Color[] pixels = canvas.getPlaneTex(x, y).GetPixels(x*canvas.sectorDimensions, y*canvas.sectorDimensions, canvas.sectorDimensions, canvas.sectorDimensions);

                //apply to textures
                textureArray[x, y] = new Texture2D(canvas.sectorDimensions, canvas.sectorDimensions, TextureFormat.ARGB32, false);
                textureArray[x,y].SetPixels(0, 0, canvas.sectorDimensions, canvas.sectorDimensions, pixels);

                //apply
                textureArray[x, y].Apply(false);

                //wait untill next frame
                yield return null;

            }

        }

    }

    public void swapTextureArray(int index) {

        canvas.planesTex = importedArraysList[index];

    }
    
}*/




