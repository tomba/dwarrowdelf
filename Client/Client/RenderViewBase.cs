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
	interface IRenderView
	{
		IntPoint3D CenterPos { get; set; }
		bool Contains(IntPoint3D ml);
		bool ShowVirtualSymbols { get; set; }
		EnvironmentObject Environment { get; set; }
		IRenderData RenderData { get; }

		void Resolve();
	}

	abstract class RenderViewBase<T> : IRenderView where T : struct
	{
		protected readonly RenderData<T> m_renderData;

		protected bool m_showVirtualSymbols = true;
		protected EnvironmentObject m_environment;
		protected IntPoint3D m_centerPos;
		IntRectZ m_bounds;

		protected bool m_invalid;

		/* How many levels to show */
		const int MAXLEVEL = 4;

		protected RenderViewBase()
		{
			m_renderData = new RenderData<T>();
		}

		IRenderData IRenderView.RenderData { get { return m_renderData; } }
		public RenderData<T> RenderData { get { return m_renderData; } }

		public abstract void Resolve();

		public IntPoint3D CenterPos
		{
			get { return m_centerPos; }
			set
			{
				if (value == m_centerPos)
					return;

				var diff = value - m_centerPos;

				m_centerPos = value;

				if (!m_invalid)
				{
					if (diff.Z != 0)
						m_invalid = true;
					else
						ScrollTiles(diff.ToIntVector());
				}

				var cp = CenterPos;
				var s = m_renderData.Size;
				m_bounds = new IntRectZ(new IntPoint(cp.X - s.Width / 2, cp.Y - s.Height / 2), s, cp.Z);
			}
		}

		public bool Contains(IntPoint3D ml)
		{
			return m_bounds.Contains(ml);
		}

		public bool ShowVirtualSymbols
		{
			get { return m_showVirtualSymbols; }

			set
			{
				m_showVirtualSymbols = value;
				m_invalid = true;
			}
		}

		public EnvironmentObject Environment
		{
			get { return m_environment; }

			set
			{
				if (m_environment == value)
					return;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged -= MapChangedCallback;
					m_environment.MapTileObjectChanged -= MapObjectChangedCallback;
				}

				m_environment = value;
				m_invalid = true;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged += MapChangedCallback;
					m_environment.MapTileObjectChanged += MapObjectChangedCallback;
				}
			}
		}

		public void Invalidate()
		{
			m_invalid = true;
		}

		// Note: this is used to scroll the rendermap immediately when setting the centerpos. Could be used only when GetRenderMap is called
		void ScrollTiles(IntVector scrollVector)
		{
			//Debug.WriteLine("RenderView.ScrollTiles");

			var columns = m_renderData.Width;
			var rows = m_renderData.Height;
			var grid = m_renderData.Grid;

			var ax = Math.Abs(scrollVector.X);
			var ay = Math.Abs(scrollVector.Y);

			if (ax >= columns || ay >= rows)
			{
				m_invalid = true;
				return;
			}

			int srcIdx = 0;
			int dstIdx = 0;

			if (scrollVector.X >= 0)
				srcIdx += ax;
			else
				dstIdx += ax;

			if (scrollVector.Y >= 0)
				srcIdx += columns * ay;
			else
				dstIdx += columns * ay;

			var xClrIdx = scrollVector.X >= 0 ? columns - ax : 0;
			var yClrIdx = scrollVector.Y >= 0 ? rows - ay : 0;

			Array.Copy(grid, srcIdx, grid, dstIdx, columns * rows - ax - columns * ay);

			for (int y = 0; y < rows; ++y)
				Array.Clear(grid, y * columns + xClrIdx, ax);

			Array.Clear(grid, yClrIdx * columns, columns * ay);
		}

		void MapChangedCallback(IntPoint3D ml)
		{
			MapChangedOverride(ml);
		}

		void MapObjectChangedCallback(MovableObject ob, IntPoint3D ml, MapTileObjectChangeType changeType)
		{
			MapChangedOverride(ml);
		}

		protected abstract void MapChangedOverride(IntPoint3D ml);

		protected static bool TileVisible(IntPoint3D ml, EnvironmentObject env)
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

								IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

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

								IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

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

		protected static byte GetDarknessForLevel(int level)
		{
			if (level == 0)
				return 0;
			else
				return (byte)((level + 2) * 127 / (MAXLEVEL + 2));
		}
	}
}