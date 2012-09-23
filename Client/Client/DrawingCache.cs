using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;
using System.Resources;
using System.IO;

namespace Dwarrowdelf.Client
{
	sealed class DrawingCache
	{
		/* [ name of the drawing -> [ color -> drawing ] ] */
		Dictionary<string, Dictionary<GameColor, Drawing>> m_drawingMap;

		public DrawingCache(string drawingsName)
		{
			ResourceDictionary drawingResources;

			var asm = System.Reflection.Assembly.GetExecutingAssembly();
			var path = Path.Combine(Path.GetDirectoryName(asm.Location), "Symbols", drawingsName);

			if (File.Exists(path))
			{
				using (var stream = File.OpenRead(path))
					drawingResources = (ResourceDictionary)System.Xaml.XamlServices.Load(stream);
			}
			else
			{
				var uri = new Uri("Symbols/" + drawingsName, UriKind.Relative);
				drawingResources = (ResourceDictionary)Application.LoadComponent(uri);
			}

			m_drawingMap = new Dictionary<string, Dictionary<GameColor, Drawing>>(drawingResources.Count);

			foreach (System.Collections.DictionaryEntry de in drawingResources)
			{
				Drawing drawing = ((DrawingBrush)de.Value).Drawing;
				string name = (string)de.Key;
				m_drawingMap[name] = new Dictionary<GameColor, Drawing>();
				m_drawingMap[name][GameColor.None] = drawing;
			}
		}

		public Drawing GetDrawing(string drawingName, GameColor color)
		{
			Dictionary<GameColor, Drawing> map;
			Drawing drawing;

			if (!m_drawingMap.TryGetValue(drawingName, out map))
				return null;

			if (!map.TryGetValue(color, out drawing))
			{
				drawing = m_drawingMap[drawingName][GameColor.None].Clone();
				Dwarrowdelf.Client.Symbols.TileSetHelpers.ColorizeDrawing(drawing, color.ToWindowsColor());
				drawing.Freeze();
				map[color] = drawing;
			}

			return drawing;
		}
	}
}