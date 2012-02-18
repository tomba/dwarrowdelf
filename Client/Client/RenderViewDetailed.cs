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
	sealed class RenderViewDetailed : RenderViewBase<RenderTileDetailed>
	{
		/* How many levels to show */
		const int MAXLEVEL = 4;
		static bool m_symbolToggler;

		public RenderViewDetailed()
		{
			GameData.Data.Blink += OnBlink;
		}

		void OnBlink()
		{
			// XXX we should invalidate only the needed tiles
			Invalidate();
			m_symbolToggler = !m_symbolToggler;
		}

		protected override void MapChangedOverride(IntPoint3 ml)
		{
			if (Contains(ml))
			{
				var p = MapLocationToRenderDataLocation(ml);
				int idx = m_renderData.GetIdx(p);
				m_renderData.Grid[idx].IsValid = false;
			}
		}



		public override void Resolve()
		{
			//Debug.WriteLine("RenderView.Resolve");

			//var sw = Stopwatch.StartNew();

			var columns = m_renderData.Width;
			var rows = m_renderData.Height;

			// render everything when using LOS
			if (m_environment != null && m_environment.VisibilityMode == VisibilityMode.LivingLOS)
				m_invalid = true;

			if (m_invalid)
			{
				//Debug.WriteLine("RenderView.Resolve All");
				m_renderData.Clear();
				m_invalid = false;
			}

			bool isSeeAll = GameData.Data.User.IsSeeAll;

			// Note: we cannot access WPF stuff from different threads
			Parallel.For(0, rows, y =>
			{
				int idx = m_renderData.GetIdx(0, y);

				for (int x = 0; x < columns; ++x, ++idx)
				{
					if (m_renderData.Grid[idx].IsValid)
						continue;

					var ml = RenderDataLocationToMapLocation(x, y);

					ResolveDetailed(out m_renderData.Grid[idx], this.Environment, ml, m_showVirtualSymbols, isSeeAll);
				}
			});

			//sw.Stop();
			//Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
		}

		static void ResolveDetailed(out RenderTileDetailed tile, EnvironmentObject env, IntPoint3 ml, bool showVirtualSymbols, bool isSeeAll)
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

				var p = new IntPoint3(ml.X, ml.Y, z);

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

		static void GetTerrainTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile, bool showVirtualSymbols, out bool seeThrough)
		{
			seeThrough = false;

			var td = env.GetTileData(ml);

			if (td.TerrainID == TerrainID.Undefined)
				return;

			if (td.TerrainID == TerrainID.Empty)
			{
				seeThrough = true;
				return;
			}

			var matInfo = Materials.GetMaterial(td.TerrainMaterialID);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (td.TerrainID)
			{
				case TerrainID.NaturalFloor:
					if (td.HasGrass)
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

				case TerrainID.BuiltFloor:
					tile.SymbolID = SymbolID.Floor;
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
					switch (td.TerrainID.ToDir())
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

					if (td.HasGrass)
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

		static void GetInteriorTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile, bool showVirtualSymbols, out bool seeThrough)
		{
			var td = env.GetTileData(ml);

			seeThrough = true;

			if (td.InteriorID == InteriorID.Undefined)
				return;

			var matInfo = Materials.GetMaterial(td.InteriorMaterialID);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (td.InteriorID)
			{
				case InteriorID.Stairs:
					tile.SymbolID = SymbolID.StairsUp;
					break;

				case InteriorID.BuiltWall:
					tile.SymbolID = SymbolID.Wall;
					seeThrough = false;
					break;

				case InteriorID.Pavement:
					tile.SymbolID = SymbolID.Floor;
					seeThrough = false;
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
				var pdown = ml + Direction.Down;

				if (env.Contains(pdown))
				{
					var tddown = env.GetTileData(pdown);

					if (tddown.InteriorID == InteriorID.Stairs)
					{
						if (td.InteriorID == InteriorID.Stairs)
						{
							tile.SymbolID = SymbolID.StairsUpDown;
						}
						else if (td.InteriorID == InteriorID.Empty)
						{
							tile.SymbolID = SymbolID.StairsDown;
							var downMatInfo = Materials.GetMaterial(tddown.InteriorMaterialID);
							tile.Color = downMatInfo.Color;
						}
					}
				}
			}
		}

		static void GetObjectTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			var ob = (ConcreteObject)env.GetFirstObject(ml);

			if (ob == null)
				return;

			tile.SymbolID = ob.SymbolID;
			tile.Color = ob.EffectiveColor;
			tile.BgColor = GameColor.None;
		}

		static void GetTopTile(IntPoint3 ml, EnvironmentObject env, ref RenderTileLayer tile, bool showVirtualSymbols)
		{
			SymbolID id;

			id = GetDesignationSymbolAt(env.Designations, ml);
			if (id != SymbolID.Undefined)
			{
				tile.SymbolID = id;
				if (m_symbolToggler)
					tile.Color = GameColor.DarkGray;
				else
					tile.Color = GameColor.LightGray;
				return;
			}

			id = GetConstructSymbolAt(env.ConstructManager, ml);
			if (id != SymbolID.Undefined)
			{
				tile.SymbolID = id;
				if (m_symbolToggler)
					tile.Color = GameColor.DarkGray;
				else
					tile.Color = GameColor.LightGray;
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

		public static SymbolID GetDesignationSymbolAt(Designation designation, IntPoint3 p)
		{
			var dt = designation.ContainsPoint(p);

			switch (dt)
			{
				case DesignationType.None:
					return SymbolID.Undefined;

				case DesignationType.Mine:
					return SymbolID.DesignationMine;

				case DesignationType.CreateStairs:
					return SymbolID.StairsUp;

				case DesignationType.Channel:
					return SymbolID.DesignationMine;

				case DesignationType.FellTree:
					return SymbolID.Log;

				default:
					throw new Exception();
			}
		}

		public static SymbolID GetConstructSymbolAt(ConstructManager mgr, IntPoint3 p)
		{
			var dt = mgr.ContainsPoint(p);

			switch (dt)
			{
				case ConstructMode.None:
					return SymbolID.Undefined;

				case ConstructMode.Pavement:
					return SymbolID.Floor;

				case ConstructMode.Floor:
					return SymbolID.Floor;

				case ConstructMode.Wall:
					return SymbolID.Wall;

				default:
					throw new Exception();
			}
		}
	}
}
