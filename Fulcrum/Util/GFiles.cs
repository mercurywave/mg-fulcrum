using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fulcrum;
public static class GFiles
	{
		public static string AppDirectory;
		public static string ContentDirectory;

		public static DirectoryInfo GetDirectory(string directory)
		{
			string fullPath = directory;
			if (AppDirectory != "") fullPath = Path.Combine(AppDirectory, directory);
			return new DirectoryInfo(fullPath);
		}

		public static IEnumerable<FileInfo> FilesInFolderByExtension(string directory, string extension)
		{
			if (AppDirectory != "") directory = Path.Combine(AppDirectory, directory);
			return _FilesInFolderByExtension(directory, extension);
		}

		public static IEnumerable<FileInfo> ContentFilesInFolderByExtension(string directory, string extension)
		{
			if (AppDirectory != "") directory = Path.Combine(ContentDirectory, directory);
			return _FilesInFolderByExtension(directory, extension);
		}

		private static IEnumerable<FileInfo> _FilesInFolderByExtension(string fullPath, string extension)
		{
			DirectoryInfo dir = new DirectoryInfo(fullPath);
			FileInfo[] files = dir.GetFiles("*." + extension);
			return files;
		}

		//loads files and scales in a folder that are named like 1.5x.xnb
		// returns scale, file handle, and file name without extensions
		public static IEnumerable<Tuple<float, FileInfo, string>> ScaledFiles(string directory, string extension = "xnb")
		{
			foreach (FileInfo file in FilesInFolderByExtension(directory, extension))
			{
				string key = Path.GetFileNameWithoutExtension(file.Name);
				string mult = GUtil.Piece(key, "x", 1);
				if (mult == "") continue;
				float scale = float.Parse(mult, System.Globalization.CultureInfo.InvariantCulture);
				yield return new Tuple<float, FileInfo, string>(scale, file, key);
			}
		}

		//write some text to a file, overwrite if it exists
		//intended for dumping errors, not complex serialization
		//universal apps don't have the right permissions for the application folder by default
		public static void WriteLogFile(string fileName, string text)
		{
			try
			{
				if (File.Exists(fileName))
					File.Delete(fileName);
				using (var fs = File.Create(fileName))
				{
					var data = new UTF8Encoding(true).GetBytes(text);
					fs.Write(data, 0, data.Length);
				}
			}
			catch (Exception) { }
		}
	}