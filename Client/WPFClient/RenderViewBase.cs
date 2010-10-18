using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Dwarrowdelf.Client.TileControlD2D;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	interface IRenderView
	{
		IRenderer Renderer { get; }
		IntPoint3D CenterPos { get; set; }
		bool ShowVirtualSymbols { get; set; }
		Environment Environment { get; set; }
	}

	abstract class RenderViewBase<T> : IRenderView, IRenderResolver where T : struct
	{
		protected readonly RenderData<T> m_renderData;

		protected bool m_showVirtualSymbols = true;
		protected Environment m_environment;
		protected IntPoint3D m_centerPos;

		protected bool m_invalid;

		/* How many levels to show */
		const int MAXLEVEL = 4;

		protected RenderViewBase()
		{
			m_renderData = new RenderData<T>();
		}

		public abstract IRenderer Renderer { get; }

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
			}
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

		public Environment Environment
		{
			get { return m_environment; }

			set
			{
				if (m_environment == value)
					return;

				if (m_environment != null)
					m_environment.MapTileChanged -= MapChangedCallback;

				m_environment = value;
				m_invalid = true;

				if (m_environment != null)
					m_environment.MapTileChanged += MapChangedCallback;
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

			var columns = m_renderData.Size.Width;
			var rows = m_renderData.Size.Height;
			var grid = m_renderData.ArrayGrid.Grid;

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

		protected abstract void MapChangedOverride(IntPoint3D ml);

		protected static bool TileVisible(IntPoint3D ml, Environment env)
		{
			if (env.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			if (env.GetInterior(ml).ID == InteriorID.Undefined)
				return false;

			var controllables = env.World.Controllables;

			if (env.VisibilityMode == VisibilityMode.LOS)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != env || l.Location.Z != ml.Z)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange &&
						l.VisionMap[vp] == true)
						return true;
				}
			}
			else if (env.VisibilityMode == VisibilityMode.SimpleFOV)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != env || l.Location.Z != ml.Z)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange)
						return true;
				}
			}
			else
			{
				throw new Exception();
			}

			return false;
		}

		protected static byte GetDarknessForLevel(int level)
		{
			if (level == 0)
				return 0;
			else
				return (byte)((level + 2) * 255 / (MAXLEVEL + 2));
		}
	}
}