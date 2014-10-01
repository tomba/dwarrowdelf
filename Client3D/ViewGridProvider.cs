using Dwarrowdelf;
using SharpDX.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class ViewGridProvider
	{
		public ViewGridProvider(Game game)
		{
			var map = GlobalData.VoxelMap;

			this.ViewCorner1 = new IntVector3(0, 0, 0);
			this.ViewCorner2 = new IntVector3(map.Width - 1, map.Height - 1, map.Depth - 1);

			game.Services.AddService(typeof(ViewGridProvider), this);
		}

		IntVector3 m_viewCorner1;
		public IntVector3 ViewCorner1
		{
			get { return m_viewCorner1; }

			set
			{
				if (value == m_viewCorner1)
					return;

				if (GlobalData.VoxelMap.Size.Contains(value) == false)
					return;

				if (value.X > m_viewCorner2.X || value.Y > m_viewCorner2.Y || value.Z > m_viewCorner2.Z)
					return;

				var old = m_viewCorner1;
				m_viewCorner1 = value;

				if (this.ViewGridCornerChanged != null)
					this.ViewGridCornerChanged(old, value);

				/*

				var diff = value - old;
				if (diff.X == 0 && diff.Y == 0)
				{
					m_chunkManager.InvalidateChunksZ(Math.Min(old.Z, value.Z), Math.Max(old.Z, value.Z));
				}
				else
				{
					m_chunkManager.InvalidateChunks();
				}*/
			}
		}

		IntVector3 m_viewCorner2;
		public IntVector3 ViewCorner2
		{
			get { return m_viewCorner2; }

			set
			{
				if (value == m_viewCorner2)
					return;

				if (GlobalData.VoxelMap.Size.Contains(value) == false)
					return;

				if (value.X < m_viewCorner1.X || value.Y < m_viewCorner1.Y || value.Z < m_viewCorner1.Z)
					return;

				var old = m_viewCorner2;
				m_viewCorner2 = value;

				if (this.ViewGridCornerChanged != null)
					this.ViewGridCornerChanged(old, value);

				/*
				var diff = value - old;

				if (diff.X == 0 && diff.Y == 0)
				{
					m_chunkManager.InvalidateChunksZ(Math.Min(old.Z, value.Z), Math.Max(old.Z, value.Z));
				}
				else
				{
					m_chunkManager.InvalidateChunks();
				}*/
			}
		}

		public event Action<IntVector3, IntVector3> ViewGridCornerChanged;

		public IntGrid3 ViewGrid { get { return new IntGrid3(m_viewCorner1, m_viewCorner2); } }
	}
}
