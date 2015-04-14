//#define NONPARALLEL
//#define PERF_MEASURE
//#define RESOLV_MEASURE

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
	static class RenderResolver
	{
		/* How many levels to show */
		const int MAXLEVEL = 4;

		public static void Resolve(EnvironmentObject env, DataGrid2D<TileControl.RenderTile> renderData,
			bool isVisibilityCheckEnabled,
			IntVector3 baseLoc, IntVector3 xInc, IntVector3 yInc, IntVector3 zInc, bool symbolToggler,
			bool showMultipleLevels)
		{
			//Debug.WriteLine("RenderView.Resolve");

#if PERF_MEASURE
			var sw = Stopwatch.StartNew();
#endif
			var columns = renderData.Width;
			var rows = renderData.Height;

			// render everything when using LOS
			if (env != null && env.VisibilityMode == VisibilityMode.LivingLOS)
				renderData.Invalid = true;

			if (renderData.Invalid)
			{
				//Debug.WriteLine("RenderView.Resolve All");
				renderData.Clear();
				renderData.Invalid = false;
			}

			if (env == null)
				return;

#if RESOLV_MEASURE
			int count = 0;
#endif

#if NONPARALLEL
			for (int y = 0; y < rows; ++y)
#else
			// Note: we cannot access WPF stuff from different threads
			Parallel.For(0, rows, y =>
#endif
			{
				int idx = renderData.GetIdx(0, y);
				var ml = baseLoc + yInc * y;

				for (int x = 0; x < columns; ++x, ++idx, ml += xInc)
				{
					if (renderData.Grid[idx].IsValid)
						continue;

#if RESOLV_MEASURE
					count++;
#endif
					ResolveDetailed(out renderData.Grid[idx], env, ml, isVisibilityCheckEnabled, symbolToggler, zInc, showMultipleLevels);
				}
			}
#if !NONPARALLEL
);
#endif

#if PERF_MEASURE
			sw.Stop();
			Trace.WriteLine(String.Format("Resolve {0} ms", sw.ElapsedMilliseconds));
#endif
#if RESOLV_MEASURE
			Trace.WriteLine(String.Format("Resolved {0} tiles", count));
#endif
		}

		static void ResolveDetailed(out RenderTile tile, EnvironmentObject env, IntVector3 ml, bool isVisibilityCheckEnabled,
			bool symbolToggler, IntVector3 zInc, bool showMultipleLevels)
		{
			tile = new RenderTile();
			tile.IsValid = true;

			if (!env.Contains(ml))
				return;

			bool visible;

			if (isVisibilityCheckEnabled == false)
				visible = true;
			else
				visible = TileVisible(ml, env);

			int maxLevel = showMultipleLevels ? MAXLEVEL : 1;

			for (int i = 0; i < maxLevel; ++i)
			{
				bool seeThrough;

				var p = ml - zInc * i;

				if (env.Contains(p) == false)
					break;

				byte darkness = GetDarknessForLevel(i + (visible ? 0 : 1));

				if (tile.Layer3.SymbolID == SymbolID.Undefined)
				{
					GetTopTile(p, env, ref tile.Layer3, symbolToggler);

					if (tile.Layer3.SymbolID != SymbolID.Undefined)
						tile.Layer3DarknessLevel = darkness;
				}

				if (tile.Layer2.SymbolID == SymbolID.Undefined)
				{
					GetObjectTile(p, env, ref tile.Layer2);

					if (tile.Layer2.SymbolID != SymbolID.Undefined)
						tile.Layer2DarknessLevel = darkness;
				}

				GetTerrainTile(p, env, ref tile.Layer0, ref tile.Layer1, out seeThrough, i);

				if (tile.Layer1.SymbolID != SymbolID.Undefined)
					tile.Layer1DarknessLevel = darkness;

				if (tile.Layer0.SymbolID != SymbolID.Undefined)
					tile.Layer0DarknessLevel = darkness;

				if (!seeThrough)
					break;
			}

			if (tile.Layer2DarknessLevel == 0)
				tile.Layer2DarknessLevel = tile.Layer3DarknessLevel;

			if (tile.Layer1DarknessLevel == 0)
				tile.Layer1DarknessLevel = tile.Layer2DarknessLevel;

			if (tile.Layer0DarknessLevel == 0)
				tile.Layer0DarknessLevel = tile.Layer1DarknessLevel;
		}

		static void GetTerrainTile(IntVector3 ml, EnvironmentObject env, ref RenderTileLayer floorTile, ref RenderTileLayer tile,
			out bool seeThrough, int currentLevel)
		{
			var td = env.GetTileData(ml);

			MaterialInfo matInfo = null;
			if (td.MaterialID != MaterialID.Undefined)
			{
				matInfo = Materials.GetMaterial(td.MaterialID);
				tile.Color = matInfo.Color;
				tile.BgColor = GameColor.None;
			}

			switch (td.ID)
			{
				case TileID.Undefined:
					tile.SymbolID = SymbolID.Hidden;
					tile.Color = GameColor.DimGray;
					tile.BgColor = GameColor.None;
					seeThrough = false;
					return;

				case TileID.Empty:
					tile.SymbolID = SymbolID.Empty;
					tile.Color = GameColor.None;
					tile.BgColor = GameColor.None;
					seeThrough = true;
					break;

				case TileID.NaturalWall:
					floorTile.SymbolID = SymbolID.Wall;
					floorTile.Color = matInfo.Color;
					floorTile.BgColor = GameColor.None;

					var secondaryMatInfo = Materials.GetMaterial(td.SecondaryMaterialID);

					switch (secondaryMatInfo.Category)
					{
						case MaterialCategory.Gem:
							tile.SymbolID = SymbolID.GemOre;
							tile.Color = secondaryMatInfo.Color;
							break;

						case MaterialCategory.Mineral:
							tile.SymbolID = SymbolID.ValuableOre;
							tile.Color = secondaryMatInfo.Color;
							break;

						default:
							break;
					}

					seeThrough = false;
					return;

				case TileID.Slope:
					tile.SymbolID = currentLevel == 0 ? SymbolID.SlopeUp : SymbolID.SlopeDown;

					// ZZZ: IsGreen doesn't work
					// If the interior is "green", override the color to make the terrain greenish
					if (td.IsGreen)
					{
						// override the material color
						tile.Color = GameColor.DarkGreen;
						tile.BgColor = GameColor.Green;
					}
					else
					{
						tile.BgColor = GetTerrainBackgroundColor(matInfo);
					}
					seeThrough = false;
					return;

				case TileID.Stairs:
					// ZZZ we should use StairsUp/Down/UpDown
					tile.SymbolID = SymbolID.StairsUpDown;
					seeThrough = false;
					break;

				case TileID.Sapling:
					switch (td.MaterialID)
					{
						case MaterialID.Fir:
							tile.SymbolID = SymbolID.ConiferousSapling;
							break;

						case MaterialID.Pine:
							tile.SymbolID = SymbolID.ConiferousSapling2;
							break;

						case MaterialID.Birch:
							tile.SymbolID = SymbolID.DeciduousSapling;
							break;

						case MaterialID.Oak:
							tile.SymbolID = SymbolID.DeciduousSapling2;
							break;

						default:
							throw new Exception();
					}

					tile.Color = GameColor.ForestGreen;
					seeThrough = true;
					break;

				case TileID.Tree:
					switch (td.MaterialID)
					{
						case MaterialID.Fir:
							tile.SymbolID = SymbolID.ConiferousTree;
							break;

						case MaterialID.Pine:
							tile.SymbolID = SymbolID.ConiferousTree2;
							break;

						case MaterialID.Birch:
							tile.SymbolID = SymbolID.DeciduousTree;
							break;

						case MaterialID.Oak:
							tile.SymbolID = SymbolID.DeciduousTree2;
							break;

						default:
							throw new Exception();
					}

					tile.Color = GameColor.ForestGreen;
					seeThrough = true;
					break;

				case TileID.Grass:
					switch (matInfo.ID)
					{
						case MaterialID.ReedGrass:
							tile.SymbolID = SymbolID.Grass4;
							break;

						case MaterialID.RyeGrass:
							tile.SymbolID = SymbolID.Grass2;
							break;

						case MaterialID.MeadowGrass:
							tile.SymbolID = SymbolID.Grass3;
							break;

						case MaterialID.HairGrass:
							tile.SymbolID = SymbolID.Grass;
							break;

						default:
							throw new Exception();
					}

					// Grass color should come from the symbol definition
					tile.Color = GameColor.None;
					seeThrough = true;
					break;

				case TileID.DeadTree:
					tile.SymbolID = SymbolID.DeadTree;
					seeThrough = true;
					break;

				case TileID.Shrub:
					tile.SymbolID = SymbolID.Shrub;
					seeThrough = true;
					break;

				default:
					throw new Exception();
			}

			// ZZZ: do we have a floor here, i.e. wall below, and we can see it?
			if (seeThrough && td.HasFloor)
			{
				// If the interior is "green", override the color to make the terrain greenish
				if (td.IsGreen)
				{
					floorTile.SymbolID = SymbolID.Empty;
					floorTile.BgColor = GameColor.Green;
				}
				else
				{
					var matInfoDown = env.GetMaterial(ml.Down);

					if (matInfoDown.ID != MaterialID.Undefined)
					{
						if (matInfoDown.Category == MaterialCategory.Soil)
							floorTile.SymbolID = SymbolID.Sand;
						else
							floorTile.SymbolID = SymbolID.Floor;

						floorTile.BgColor = GetTerrainBackgroundColor(matInfoDown);
					}
				}

				seeThrough = false;
			}
		}

		static GameColor GetTerrainBackgroundColor(MaterialInfo matInfo)
		{
			if (matInfo.Category == MaterialCategory.Rock)
				return GameColor.DarkSlateGray;
			else if (matInfo.Category == MaterialCategory.Soil)
				return GameColor.Sienna;
			else
				throw new Exception();
		}

		static void GetObjectTile(IntVector3 ml, EnvironmentObject env, ref RenderTileLayer tile)
		{
			var ob = (ConcreteObject)env.GetFirstObject(ml);

			if (ob == null)
				return;

			tile.SymbolID = ob.SymbolID;
			tile.Color = ob.EffectiveColor;
			tile.BgColor = GameColor.None;
		}

		static void GetTopTile(IntVector3 ml, EnvironmentObject env, ref RenderTileLayer tile, bool symbolToggler)
		{
			SymbolID id;

			if (symbolToggler)
			{
				id = GetDesignationSymbolAt(env.Designations, ml);
				if (id != SymbolID.Undefined)
				{
					tile.SymbolID = id;
					tile.BgColor = GameColor.DimGray;
					return;
				}
			}

			id = GetConstructSymbolAt(env.ConstructManager, ml);
			if (id != SymbolID.Undefined)
			{
				tile.SymbolID = id;
				if (symbolToggler)
					tile.Color = GameColor.DarkGray;
				else
					tile.Color = GameColor.LightGray;
				return;
			}

			if (!symbolToggler)
			{
				id = GetInstallSymbolAt(env.InstallItemManager, ml);
				if (id != SymbolID.Undefined)
				{
					tile.SymbolID = id;
					tile.Color = GameColor.DarkGray;
					return;
				}
			}

			int wl = env.GetWaterLevel(ml);

			if (wl > 0)
			{
				if (env.GetTileFlags(ml, TileFlags.WaterStatic))
					id = SymbolID.WaterDouble;
				else
					id = SymbolID.Water;

				switch (wl)
				{
					case 7:
						tile.Color = GameColor.Aqua;
						break;
					case 6:
					case 5:
						tile.Color = GameColor.DodgerBlue;
						break;
					case 4:
					case 3:
						tile.Color = GameColor.Blue;
						break;
					case 2:
					case 1:
						tile.Color = GameColor.MediumBlue;
						break;
				}
			}

			tile.BgColor = GameColor.DarkBlue;

			tile.SymbolID = id;
		}

		static byte GetDarknessForLevel(int level)
		{
			if (level == 0)
				return 0;
			else
				return (byte)((level + 2) * 127 / (MAXLEVEL + 2));
		}


		static bool TileVisible(IntVector3 ml, EnvironmentObject env)
		{
			switch (env.VisibilityMode)
			{
				case VisibilityMode.AllVisible:
					return true;

				case VisibilityMode.GlobalFOV:
					return !env.GetHidden(ml);

				case VisibilityMode.LivingLOS:

					var controllables = env.World.Controllables;

					foreach (var l in controllables)
					{
						if (l.Sees(env, ml))
							return true;
					}

					return false;

				default:
					throw new Exception();
			}
		}

		static SymbolID GetDesignationSymbolAt(Designation designation, IntVector3 p)
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
					return SymbolID.DesignationChannel;

				case DesignationType.FellTree:
					return SymbolID.Log;

				default:
					throw new Exception();
			}
		}

		static SymbolID GetConstructSymbolAt(ConstructManager mgr, IntVector3 p)
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

		static SymbolID GetInstallSymbolAt(InstallItemManager mgr, IntVector3 p)
		{
			var item = mgr.ContainsPoint(p);

			if (item == null)
				return SymbolID.Undefined;

			return item.SymbolID;
		}
	}
}
