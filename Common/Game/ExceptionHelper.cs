using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public static class ExceptionHelper
	{
		public static void DumpException(Exception e, string header)
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				"dwarrowdelf-crash.txt");

			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0}=== {1} ==={2}", Environment.NewLine, header, Environment.NewLine);

			sb.AppendLine(DateTime.Now.ToString());

			sb.AppendLine(e.ToString());

			File.AppendAllText(path, sb.ToString());
		}
	}
}
