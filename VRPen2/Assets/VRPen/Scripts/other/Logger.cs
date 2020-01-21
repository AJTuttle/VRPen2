using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace VRPen {
	/// <summary>
	/// Logs any data to a file.
	/// </summary>
	public class Logger : MonoBehaviour {
		private static string logFolder = "Log";
		private static string fileExtension = ".tsv";
		private static string delimiter = "\t";

		private static StreamWriter[] writers = new StreamWriter[0];
		private static string[] writersPaths = new string[0];

		public static bool enableLogging = true;

		public static void LogRow(string fileName, IEnumerable<string> data) {

			if (!enableLogging) {
				return;
			}

			string filePath;

#if UNITY_ANDROID && !UNITY_EDITOR
			filePath = Path.Combine(Application.persistentDataPath, fileName+fileExtension);

#else
			// make directory
			if (logFolder[0] == '/') {
				logFolder = logFolder.Substring(1);
			}

			if (logFolder[logFolder.Length - 1] == '/') {
				logFolder = logFolder.Substring(0, logFolder.Length - 2);
			}

			string directoryPath = Application.dataPath + "/" + logFolder + "/";

			if (!Directory.Exists(directoryPath)) {
				Directory.CreateDirectory(directoryPath);
			}
			filePath = Path.Combine(directoryPath, fileName + fileExtension);
#endif

			//write vars
			StreamWriter writer;
			bool writerExists = false;
			int writerIndex = -1;


			//check if writer exists
			for (int x = 0; x < writers.Length; x++) {
				if (filePath.Equals(writersPaths[x])) {
					writerExists = true;
					writerIndex = x;
					break;
				}
			}


			//if writer exists, use that
			if (writerExists) {
				writer = writers[writerIndex];
			}

			//else make a new writer and add it to the list
			else {
				if (File.Exists(filePath)) {
					writer = new StreamWriter(filePath, true);
				}
				else {
					writer = new StreamWriter(filePath);
				}


				StreamWriter[] tempWriters = new StreamWriter[writers.Length + 1];
				string[] tempWritersPaths = new string[writersPaths.Length + 1];

				for (int x = 0; x < writers.Length; x++) {
					tempWriters[x] = writers[x];
					tempWritersPaths[x] = writersPaths[x];
				}


				tempWriters[writers.Length] = writer;
				tempWritersPaths[writersPaths.Length] = filePath;

				writers = tempWriters;
				writersPaths = tempWritersPaths;
			}




			// actually log data
			try {
				string output = "";
				output += DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + delimiter;
				foreach (var elem in data) {
					if (elem.Contains(delimiter)) {
						throw new Exception("Data contains delimiter: " + elem);
					}

					output += elem + delimiter;
				}


				writer.WriteLine(output);
			}
			catch (Exception e) {
				Debug.LogWarning(e.Message);
			}

			writer.Flush();

		}

		public void EnableLogging(bool enable) {
			enableLogging = enable;
		}

		//close writers
		private void OnApplicationQuit() {
			for (int x = 0; x < writers.Length; x++) {
				writers[x].WriteLine("-------------");
				writers[x].Close();
			}
		}

	}

}