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
			DataGrid2D<RenderTile> renderData = new DataGrid2D<RenderTile>();

			int frameCount = 0;
			var fpsClock = Stopwatch.StartNew();

			var form = new RenderForm();

			var tileset = new TileSet("TileSet.png");

			SceneHostHwnd renderer = new SceneHostHwnd(form.Handle);
			var scene = new TileMapScene();
			var cube = new TestScene();
			var list = new IScene[] { scene
			//	, cube
			};
			var slist = new SceneList(list);
			renderer.Scene = slist;

			scene.SetTileSet(tileset);

			float tilesize = 64;
			IntSize2 renderSize = new IntSize2();
			int columns = 0;
			int rows = 0;
			bool tileDataInvalid = true;

			scene.SetTileSize(tilesize);

			ArrayGrid2D<RenderTile> mapData = new ArrayGrid2D<RenderTile>(1024, 1024);
			CreateMapData(mapData);

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

				columns = MyMath.Ceiling((double)size.Width / tilesize + 1) | 1;
				rows = MyMath.Ceiling((double)size.Height / tilesize + 1) | 1;

				renderData.SetSize(new IntSize2(columns, rows));

				RecreateRenderData(renderData, mapData);

				scene.SetTileSize(tilesize);

				tileDataInvalid = true;
			};

			float xoffset = 0;
			float yoffset = 0;

			form.KeyPress += (s, e) =>
			{
				const float move = 1f;
				const float zoom = 1.333f;

				float ts = tilesize;

				switch (e.KeyChar)
				{
					case '+':
						ts += zoom;
						break;

					case '-':
						ts -= zoom;
						break;

					case 'a':
						xoffset += move;
						break;

					case 'd':
						xoffset -= move;
						break;

					case 'w':
						yoffset += move;
						break;

					case 's':
						yoffset -= move;
						break;

					default:
						return;
				}

				if (ts < 2)
					ts = 2;

				if (ts > 512)
					ts = 512;

				tilesize = ts;

				var size = new IntSize2(form.ClientSize.Width, form.ClientSize.Height);

				columns = MyMath.Ceiling((double)size.Width / tilesize + 1) | 1;
				rows = MyMath.Ceiling((double)size.Height / tilesize + 1) | 1;

				renderData.SetSize(new IntSize2(columns, rows));

				RecreateRenderData(renderData, mapData);

				scene.SetTileSize(tilesize);

				tileDataInvalid = true;
			};

			form.UserResized += (s, e) =>
			{
				var size = new IntSize2(form.ClientSize.Width, form.ClientSize.Height);

				renderer.SetRenderSize(size);

				int maxColumns = MyMath.Ceiling((double)size.Width / MINTILESIZE + 1) | 1;
				int maxRows = MyMath.Ceiling((double)size.Height / MINTILESIZE + 1) | 1;

				renderData.SetMaxSize(new IntSize2(maxColumns, maxRows));
				scene.SetupTileBuffer(new IntSize2(maxColumns, maxRows));

				columns = MyMath.Ceiling((double)size.Width / tilesize + 1) | 1;
				rows = MyMath.Ceiling((double)size.Height / tilesize + 1) | 1;

				renderData.SetSize(new IntSize2(columns, rows));

				RecreateRenderData(renderData, mapData);

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

				int offsetX = MyMath.RoundAway((renderSize.Width - renderData.Size.Width * tilesize) / 2);
				int offsetY = MyMath.RoundAway((renderSize.Height - renderData.Size.Height * tilesize) / 2);

				scene.SetRenderOffset(offsetX + (xoffset % tilesize), offsetY + (yoffset % tilesize));

				if (tileDataInvalid)
				{
					scene.SendMapData(renderData.Grid, renderData.Width, renderData.Height);
				}

				renderer.Render();

				tileDataInvalid = false;
			});

			renderer.Dispose();
		}

		static GameColor[] s_colors = (GameColor[])Enum.GetValues(typeof(GameColor));

		static SymbolID[] s_terrainSymbols = new SymbolID[]
		{
			SymbolID.Floor, SymbolID.Grass, SymbolID.Grass2, SymbolID.Grass3,SymbolID.Grass4
		};
		static SymbolID[] s_interiorSymbols = new SymbolID[]
		{
			SymbolID.Empty, SymbolID.ConiferousTree, SymbolID.ConiferousTree2, SymbolID.DeciduousTree
		};
		static SymbolID[] s_objectSymbols = new SymbolID[] { SymbolID.Player, SymbolID.Wolf, SymbolID.Sheep, SymbolID.Orc };
		static SymbolID[] s_topSymbols = new SymbolID[] { SymbolID.Empty, SymbolID.Water };

		static void RecreateRenderData(DataGrid2D<RenderTile> renderData, ArrayGrid2D<RenderTile> mapData)
		{
			var xo = mapData.Width / 2 - renderData.Width / 2;
			var yo = mapData.Height / 2 - renderData.Height / 2;

			for (int y = 0; y < renderData.Height; ++y)
			{
				for (int x = 0; x < renderData.Width; ++x)
				{
					int idx = renderData.GetIdx(x, y);

					int mx = x + xo;
					int my = y + yo;

					if (mx < 0 || mx >= mapData.Width || my < 0 || my >= mapData.Height)
					{
						renderData.Grid[idx] = new RenderTile();
						continue;
					}

					renderData.Grid[idx] = mapData[mx, my];
				}
			}
		}

		static void CreateMapData(ArrayGrid2D<RenderTile> mapData)
		{
			for (int y = 0; y < mapData.Height; ++y)
			{
				for (int x = 0; x < mapData.Width; ++x)
				{
					var r = new MWCRandom(new IntVector2(x, y), 123);

					mapData[x, y] = new RenderTile()
					{
						Layer0 = new RenderTileLayer()
						{
							SymbolID = s_terrainSymbols[r.Next(s_terrainSymbols.Length)],
							Color = s_colors[r.Next(s_colors.Length)],
							BgColor = s_colors[r.Next(s_colors.Length)],
						},

						Layer1 = new RenderTileLayer()
						{
							SymbolID = s_interiorSymbols[r.Next(s_interiorSymbols.Length)],
							Color = s_colors[r.Next(s_colors.Length)],
						},

						Layer2 = new RenderTileLayer()
						{
							SymbolID = s_objectSymbols[r.Next(s_objectSymbols.Length)],
							Color = s_colors[r.Next(s_colors.Length)],
						},

						Layer3 = new RenderTileLayer()
						{
							SymbolID = s_topSymbols[r.Next(s_topSymbols.Length)],
							Color = s_colors[r.Next(s_colors.Length)],
						},

						IsValid = true,
					};
				}
			}
		}
	}
}
