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

				if (tile.Layer1.SymbolID == SymbolID.Undefined)
				{
					GetInteriorTile(p, env, ref tile.Layer1, out seeThrough);

					if (tile.Layer1.SymbolID != SymbolID.Undefined)
						tile.Layer1DarknessLevel = darkness;

					if (!seeThrough)
						break;
				}

				GetTerrainTile(p, env, ref tile.Layer0, out seeThrough);

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

		static void GetTerrainTile(IntVector3 ml, EnvironmentObject env, ref RenderTileLayer tile, out bool seeThrough)
		{
			seeThrough = false;

			var td = env.GetTileData(ml);

			if (td.TerrainID == TerrainID.Undefined)
			{
				tile.SymbolID = SymbolID.Hidden;
				tile.Color = GameColor.DimGray;
				tile.BgColor = GameColor.None;
				return;
			}

			if (td.TerrainID == TerrainID.Empty)
			{
				tile.SymbolID = SymbolID.Empty;
				tile.Color = GameColor.None;
				tile.BgColor = GameColor.None;
				seeThrough = true;
				return;
			}

			var matInfo = Materials.GetMaterial(td.TerrainMaterialID);
			tile.Color = matInfo.Color;
			tile.BgColor = GameColor.None;

			switch (td.TerrainID)
			{
				case TerrainID.NaturalFloor:
					tile.SymbolID = SymbolID.Floor;

					if (matInfo.Category == MaterialCategory.Soil)
						tile.SymbolID = SymbolID.Sand;

					tile.BgColor = GetTerrainBackgroundColor(matInfo);

					// If the interior is "green", override the color to make the terrain greenish
					if (td.IsGreen)
					{
						tile.SymbolID = SymbolID.Empty;
						tile.BgColor = GameColor.Green;
						return;
					}

					break;

				case TerrainID.BuiltFloor:
					tile.SymbolID = SymbolID.Floor;
					break;

				case TerrainID.StairsDown:
					tile.SymbolID = SymbolID.StairsDown;
					break;

				case TerrainID.Slope:
					tile.SymbolID = SymbolID.SlopeUp;

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

					break;

				default:
					throw new Exception();
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

		static void GetInteriorTile(IntVector3 ml, EnvironmentObject env, ref RenderTileLayer tile, out bool seeThrough)
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
					if (td.TerrainID == TerrainID.StairsDown)
					{
						tile.SymbolID = SymbolID.StairsUpDown;
						// disable seethrough so that the terrain's stairsdown are not visible
						seeThrough = false;
					}
					else
					{
						tile.SymbolID = SymbolID.StairsUp;
					}
					break;

				case InteriorID.BuiltWall:
					tile.SymbolID = SymbolID.Wall;
					seeThrough = false;
					break;

				case InteriorID.NaturalWall:
					switch (matInfo.Category)
					{
						case MaterialCategory.Gem:
							tile.SymbolID = SymbolID.GemOre;
							seeThrough = true;
							break;

						case MaterialCategory.Mineral:
							tile.SymbolID = SymbolID.ValuableOre;
							seeThrough = true;
							break;

						default:
							tile.SymbolID = SymbolID.Wall;
							seeThrough = false;
							break;
					}

					break;

				case InteriorID.Pavement:
					tile.SymbolID = SymbolID.Floor;
					seeThrough = false;
					break;

				case InteriorID.Empty:
					tile.SymbolID = SymbolID.Undefined;
					break;

				case InteriorID.Grass:
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
					break;

				case InteriorID.Sapling:
					{
						switch (td.InteriorMaterialID)
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
					}
					break;

				case InteriorID.Tree:
					{
						switch (td.InteriorMaterialID)
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
					}
					break;

				case InteriorID.DeadTree:
					{
						tile.SymbolID = SymbolID.DeadTree;
					}
					break;

				case InteriorID.Shrub:
					{
						tile.SymbolID = SymbolID.Shrub;
					}
					break;

				default:
					throw new Exception();
			}
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

					switch (env.World.LivingVisionMode)
					{
						case LivingVisionMode.LOS:
							foreach (var l in controllables)
							{
								if (l.Environment != env || l.Location.Z != ml.Z)
									continue;

								IntVector2 vp = new IntVector2(ml.X - l.Location.X, ml.Y - l.Location.Y);

								if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange &&
									l.VisionMap[vp] == true)
									return true;
							}

							return false;

						case LivingVisionMode.SquareFOV:
							foreach (var l in controllables)
							{
								if (l.Environment != env || l.Location.Z != ml.Z)
									continue;

								IntVector2 vp = new IntVector2(ml.X - l.Location.X, ml.Y - l.Location.Y);

								if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange)
									return true;
							}

							return false;

						default:
							throw new Exception();
					}

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
