using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Win32
{
	public static class SavedGamesFolder
	{
		public static string GetSavedGamesPath()
		{
			if (Environment.OSVersion.Version.Major < 6)
				throw new NotSupportedException();

			IntPtr pathPtr;
			int hresult = SHGetKnownFolderPath(ref SavedGamesFolderGUID, 0, IntPtr.Zero, out pathPtr);
			if (hresult != 0)
				throw new IOException("Saved Games folder not found");

			string path = Marshal.PtrToStringUni(pathPtr);
			Marshal.FreeCoTaskMem(pathPtr);
			return path;
		}

		private static Guid SavedGamesFolderGUID = new Guid("4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4");

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);
	}
}
