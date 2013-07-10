using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Dwarrowdelf;
using Dwarrowdelf.Client;
using SharpDX.Windows;
using System.Diagnostics;
using Dwarrowdelf.Client.TileControl;

namespace TileControlD3DWinFormsTest
{
	static class Program
	{
		const int MINTILESIZE = 2;

		[STAThread]
		static void Main()
		{
			RenderData<RenderTile> renderData = new RenderData<RenderTile>();

			int frameCount = 0;
			var fpsClock = Stopwatch.StartNew();

			var form = new RenderForm();

			RendererD3DSharpDXHwnd renderer = new RendererD3DSharpDXHwnd(renderData, form.Handle);

			var tileset = new TileSet(new Uri("/TileControlD3DWinFormsTest;component/TileSet.png", UriKind.Relative));
			renderer.SetTileSet(tileset);

			float tilesize = 64;
			IntSize2 renderSize = new IntSize2();
			int columns = 0;
			int rows = 0;
			bool tileDataInvalid = true;

			form.MouseWheel += (s, e) =>
			{
				float ts = tilesize;

				if (e.Delta > 0)
					ts *= 2;
				else
					ts /= 2;

				if (ts < 2)
					ts = 2;

				if (ts > 512)
					ts = 512;

				if (tilesize == ts)
					return;

				tilesize = ts;

				var size = new IntSize2(form.ClientSize.Width, form.ClientSize.Height);

				columns = (int)Math.Ceiling((double)size.Width / tilesize + 1) | 1;
				rows = (int)Math.Ceiling((double)size.Height / tilesize + 1) | 1;

				renderData.SetSize(new IntSize2(columns, rows));

				RecreateMap(renderData);

				tileDataInvalid = true;
			};

			form.KeyPress += (s, e) =>
			{
				float ts = tilesize;

				if (e.KeyChar == '+')
					ts += 2;
				else if (e.KeyChar == '-')
					ts -= 2;

				if (ts < 2)
					ts = 2;

				if (ts > 512)
					ts = 512;

				if (tilesize == ts)
					return;

				tilesize = ts;

				var size = new IntSize2(form.ClientSize.Width, form.ClientSize.Height);

				columns = (int)Math.Ceiling((double)size.Width / tilesize + 1) | 1;
				rows = (int)Math.Ceiling((double)size.Height / tilesize + 1) | 1;

				renderData.SetSize(new IntSize2(columns, rows));

				RecreateMap(renderData);

				tileDataInvalid = true;
			};

			form.Resize += (s, e) =>
			{
				var size = new IntSize2(form.ClientSize.Width, form.ClientSize.Height);

				int maxColumns = (int)Math.Ceiling((double)size.Width / MINTILESIZE + 1) | 1;
				int maxRows = (int)Math.Ceiling((double)size.Height / MINTILESIZE + 1) | 1;

				renderData.SetMaxSize(new IntSize2(maxColumns, maxRows));

				columns = (int)Math.Ceiling((double)size.Width / tilesize + 1) | 1;
				rows = (int)Math.Ceiling((double)size.Height / tilesize + 1) | 1;

				renderData.SetSize(new IntSize2(columns, rows));

				RecreateMap(renderData);

				renderSize = size;

				tileDataInvalid = true;
			};

			form.Width = 800;
			form.Height = 640;

			// Main loop
			RenderLoop.Run(form, () =>
			{
				frameCount++;

				if (fpsClock.ElapsedMilliseconds > 500.0f)
				{
					float fps = (float)frameCount * 1000 / fpsClock.ElapsedMilliseconds;

					form.Text = string.Format("{0:F2} FPS, tilesize {1}, {2}x{3}", fps, tilesize, columns, rows);
					frameCount = 0;
					fpsClock.Restart();
				}

				if (renderSize == new IntSize2())
					return;

				int offsetX = (int)Math.Ceiling((renderSize.Width - renderData.Size.Width * tilesize) / 2);
				int offsetY = (int)Math.Ceiling((renderSize.Height - renderData.Size.Height * tilesize) / 2);

				renderer.Render(renderSize, renderData.Size, tilesize, new IntPoint2(offsetX, offsetY), tileDataInvalid);
				tileDataInvalid = false;
			});

			renderer.Dispose();
		}

		static SymbolID[] s_symbols = (SymbolID[])Enum.GetValues(typeof(SymbolID));

		static void RecreateMap(RenderData<RenderTile> renderData)
		{
			for (int y = 0; y < renderData.Height; ++y)
			{
				for (int x = 0; x < renderData.Width; ++x)
				{
					int idx = renderData.GetIdx(x, y);

					var r = new MWCRandom(new IntPoint2(x, y), 123);

					renderData.Grid[idx] = new RenderTile()
					{
						Terrain = new RenderTileLayer()
						{
							SymbolID = s_symbols[r.Next(s_symbols.Length - 2) + 1],
							Color = GameColor.White,
						},

						IsValid = true,
					};
				}
			}
		}
	}
}
