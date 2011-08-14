using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControl;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class RenderViewDetailed : RenderViewBase<RenderTileDetailed>
	{
		/* How many levels to show */
		const int MAXLEVEL = 4;

		public RenderViewDetailed()
		{
		}

		/*
		ISymbolDrawingCache m_symbolDrawingCache;
		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }
			set
			{
				m_symbolDrawingCache = value;
				m_renderer.SymbolDrawingCache = value;
			}
		}

		public void InvalidateSymbols()
		{
			m_renderer.InvalidateSymbols();
		}
		*/

		protected override void MapChangedOverride(IntPoint3D ml)
		{
			// Note: invalidates the rendertile regardless of ml.Z
			// invalidate only if the change is within resolve limits (MAXLEVEL?)

			var x = ml.X - m_centerPos.X + m_renderData.Width / 2;
			var y = ml.Y - m_centerPos.Y + m_renderData.Height / 2;

			if (m_renderData.Contains(new IntPoint(x, y)))
				m_renderData.Grid[y, x].IsValid = false;
		}



		public override void Resolve()
		{
			//Debug.WriteLine("RenderView.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Width;
			var rows = m_renderData.Height;
			var grid = m_renderData.Grid;

			if (m_invalid || (m_environment != null && (m_environment.VisibilityMode != VisibilityMode.AllVisible || m_environment.VisibilityMode != VisibilityMode.GlobalFOV)))
			{
				//Debug.WriteLine("RenderView.Resolve All");
				m_renderData.Clear();
				m_invalid = false;
			}

			bool isSeeAll = GameData.Data.User.IsSeeAll;

			int offsetX = m_centerPos.X - columns / 2;
			int offsetY = m_centerPos.Y - rows / 2;
			int offsetZ = m_centerPos.Z;

			// Note: we cannot access WPF stuff from different threads
			Parallel.For(0, rows, y =>
			{
				for (int x = 0; x < columns; ++x)
				{
					var p = new IntPoint(x, y);

					if (m_renderData.Grid[y, x].IsValid)
						continue;

					var ml = new IntPoint3D(offsetX + x, offsetY + (rows - y - 1), offsetZ);

					ResolveDetailed(out m_renderData.Grid[y, x], this.Environment, ml, m_showVirtualSymbols, isSeeAll);
				}
			});

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void ResolveDetailed(out RenderTileDetailed tile, Environment env, IntPoint3D ml, bool showVirtualSymbols, bool isSeeAll)
		{
			tile = new RenderTileDetailed();
			tile.IsValid = true;

			if (env == null || !env.Contains(ml))
				return;

			bool visible;

			if (isSeeAll)
				visible = true;
			else
				visible = TileVisible(ml, env);

			for (int z = ml.Z; z > ml.Z - MAXLEVEL; --z)
			{
				bool seeThrough;

				var p = new IntPoint3D(ml.X, ml.Y, z);

				if (tile.Top.SymbolID == SymbolID.Undefined)
				{
					GetTopTile(p, env, ref tile.Top, showVirtualSymbols);

					if (tile.Top.SymbolID != SymbolID.Undefined)
						tile.TopDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Object.SymbolID == SymbolID.Undefined)
				{
					GetObjectTile(p, env, ref tile.Object, showVirtualSymbols);

					if (tile.Object.SymbolID != SymbolID.Undefined)
						tile.ObjectDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));
				}

				if (tile.Interior.SymbolID == SymbolID.Undefined)
				{
					GetInteriorTile(p, env, ref tile.Interior, showVirtualSymbols, out seeThrough);

					if (tile.Interior.SymbolID != SymbolID.Undefined)
						tile.InteriorDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));

					if (!seeThrough)
						break;
				}

				GetTerrainTile(p, env, ref tile.Terrain, showVirtualSymbols, out seeThrough);

				if (tile.Terrain.SymbolID != SymbolID.Undefined)
					tile.TerrainDarknessLevel = GetDarknessForLevel(ml.Z - z + (visible ? 0 : 1));

				if (!seeThrough)
					break;
			}

			if (tile.ObjectDarknessLevel == 0)
				tile.ObjectDarknessLevel = tile.TopDarknessLevel;

			if (tile.InteriorDarknessLevel == 0)
				tile.InteriorDarknessLevel = tile.ObjectDarknessLevel;

			if (tile.TerrainDarknessLevel == 0)
				tile.TerrainDarknessLevel = tile.InteriorDarknessLevel;
		}

		static void GetTerrainTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols, out bool seeThrough)
		{
			seeThrough = false;

			var flrID = env.GetTerrainID(ml);

			if (flrID == TerrainID.Undefined)
				return;

			if (flrID == TerrainID.Empty)
			{
				seeThrough = true;
				return;
			}

			var matInfo = env.GetTerrainMaterial(ml);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (flrID)
			{
				case TerrainID.NaturalFloor:
					if (env.GetGrass(ml))
					{
						tile.SymbolID = SymbolID.Grass;
						// Grass color should come from the symbol definition
						tile.Color = GameColor.None;
					}
					else
					{
						tile.SymbolID = SymbolID.Floor;
					}
					break;

				case TerrainID.NaturalWall:
					tile.SymbolID = SymbolID.Wall;
					break;

				case TerrainID.Hole:
					tile.SymbolID = SymbolID.Floor;
					break;

				case TerrainID.SlopeNorth:
				case TerrainID.SlopeNorthEast:
				case TerrainID.SlopeEast:
				case TerrainID.SlopeSouthEast:
				case TerrainID.SlopeSouth:
				case TerrainID.SlopeSouthWest:
				case TerrainID.SlopeWest:
				case TerrainID.SlopeNorthWest:
					switch (flrID.ToDir())
					{
						case Direction.North:
							tile.SymbolID = SymbolID.SlopeUpNorth;
							break;
						case Direction.NorthEast:
							tile.SymbolID = SymbolID.SlopeUpNorthEast;
							break;
						case Direction.East:
							tile.SymbolID = SymbolID.SlopeUpEast;
							break;
						case Direction.SouthEast:
							tile.SymbolID = SymbolID.SlopeUpSouthEast;
							break;
						case Direction.South:
							tile.SymbolID = SymbolID.SlopeUpSouth;
							break;
						case Direction.SouthWest:
							tile.SymbolID = SymbolID.SlopeUpSouthWest;
							break;
						case Direction.West:
							tile.SymbolID = SymbolID.SlopeUpWest;
							break;
						case Direction.NorthWest:
							tile.SymbolID = SymbolID.SlopeUpNorthWest;
							break;
						default:
							throw new Exception();
					}

					if (env.GetGrass(ml))
					{
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}

					break;

				default:
					throw new Exception();
			}
		}

		static void GetInteriorTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols, out bool seeThrough)
		{
			var intID = env.GetInteriorID(ml);
			var intID2 = env.GetInteriorID(ml + Direction.Down);

			seeThrough = true;

			if (intID == InteriorID.Undefined)
				return;

			var matInfo = env.GetInteriorMaterial(ml);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (intID)
			{
				case InteriorID.Stairs:
					tile.SymbolID = SymbolID.StairsUp;
					break;

				case InteriorID.Empty:
					tile.SymbolID = SymbolID.Undefined;
					break;

				case InteriorID.Sapling:
					tile.SymbolID = SymbolID.Sapling;
					tile.Color = GameColor.ForestGreen;
					break;

				case InteriorID.Tree:
					tile.SymbolID = SymbolID.Tree;
					tile.Color = GameColor.ForestGreen;
					break;

				case InteriorID.Ore:
					switch (matInfo.Category)
					{
						case MaterialCategory.Gem:
							tile.SymbolID = SymbolID.GemOre;
							break;

						case MaterialCategory.Mineral:
							tile.SymbolID = SymbolID.ValuableOre;
							break;

						default:
							tile.SymbolID = SymbolID.Undefined;
							break;
					}
					break;

				default:
					throw new Exception();
			}

			if (showVirtualSymbols)
			{
				if (intID2 == InteriorID.Stairs)
				{
					if (intID == InteriorID.Stairs)
					{
						tile.SymbolID = SymbolID.StairsUpDown;
					}
					else if (intID == InteriorID.Empty)
					{
						tile.SymbolID = SymbolID.StairsDown;
						var downMatInfo = env.GetInteriorMaterial(ml + Direction.Down);
						tile.Color = downMatInfo.Color;
					}
				}
			}
		}

		static void GetObjectTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			var ob = (LocatabletGameObject)env.GetFirstObject(ml);

			if (ob == null)
				return;

			tile.SymbolID = ob.SymbolID;
			tile.Color = ob.Color;
			tile.BgColor = GameColor.None;
		}

		static void GetTopTile(IntPoint3D ml, Environment env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			SymbolID id;

			DesignationType dt = env.Designations.ContainsPoint(ml);

			if (dt != DesignationType.None)
			{
				switch (dt)
				{
					case DesignationType.Mine:
						id = SymbolID.Rock;
						break;

					case DesignationType.CreateStairs:
						id = SymbolID.StairsUp;
						break;

					case DesignationType.FellTree:
						id = SymbolID.Log;
						break;

					default:
						throw new Exception();
				}
				tile.SymbolID = id;
				return;
			}

			int wl = env.GetWaterLevel(ml);

			if (wl == 0)
				return;

			wl = wl * 100 / TileData.MaxWaterLevel;

			id = SymbolID.Water;

			if (wl > 80)
			{
				tile.Color = GameColor.Aqua;
			}
			else if (wl > 60)
			{
				tile.Color = GameColor.DodgerBlue;
			}
			else if (wl > 40)
			{
				tile.Color = GameColor.Blue;
			}
			else if (wl > 20)
			{
				tile.Color = GameColor.Blue;
			}
			else
			{
				tile.Color = GameColor.MediumBlue;
			}

			tile.BgColor = GameColor.DarkBlue;

			tile.SymbolID = id;
		}
	}
}
