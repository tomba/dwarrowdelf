using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	public delegate void MapChanged(ObjectID mapID, IntPoint3D l, int terrainID);

	struct TileData
	{
		public int m_terrainID;
		public List<ServerGameObject> m_contentList;
	}

	class TileGrid : Grid3DBase<TileData>
	{
		public TileGrid(int width, int height, int depth)
			: base(width, height, depth)
		{
		}

		public void SetTerrainType(IntPoint3D l, int terrainType)
		{
			base.Grid[GetIndex(l)].m_terrainID = terrainType;
		}

		public int GetTerrainID(IntPoint3D l)
		{
			return base.Grid[GetIndex(l)].m_terrainID;
		}

		public List<ServerGameObject> GetContentList(IntPoint3D l)
		{
			return base.Grid[GetIndex(l)].m_contentList;
		}

		public void SetContentList(IntPoint3D l, List<ServerGameObject> list)
		{
			base.Grid[GetIndex(l)].m_contentList = list;
		}
	}

	public class Environment : ServerGameObject 
	{
		public event MapChanged MapChanged;

		TileGrid m_tileGrid;

		public uint Version { get; private set; }

		public VisibilityMode VisibilityMode { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public int Depth { get; private set; }

		public Environment(World world, int width, int height, VisibilityMode visibilityMode)
			: base(world)
		{
			this.Version = 1;
			base.Name = "map";
			this.VisibilityMode = visibilityMode;

			this.Width = width;
			this.Height = height;
			this.Depth = 1;

			m_tileGrid = new TileGrid(this.Width, this.Height, this.Depth);
		}

		public IntRect Bounds2D
		{
			get { return new IntRect(0, 0, this.Width, this.Height); }
		}

		public IntCube Bounds
		{
			get { return new IntCube(0, 0, 0, this.Width, this.Height, this.Depth); }
		}

		public int GetTerrainID(IntPoint3D l)
		{
			return m_tileGrid.GetTerrainID(l);
		}

		public void SetTerrain(IntPoint3D l, int terrainID)
		{
			this.Version += 1;

			m_tileGrid.SetTerrainType(l, terrainID);

			if (MapChanged != null)
				MapChanged(this.ObjectID, l, terrainID);
		}

		public bool IsWalkable(IntPoint3D l)
		{
			return this.World.AreaData.Terrains[GetTerrainID(l)].IsWalkable;
		}

		public IList<ServerGameObject> GetContents(IntPoint3D l)
		{
			return m_tileGrid.GetContentList(l);
		}

		protected override void ChildAdded(ServerGameObject child)
		{
			IntPoint3D l = child.Location;

			if (m_tileGrid.GetContentList(l) == null)
				m_tileGrid.SetContentList(l, new List<ServerGameObject>());

			Debug.Assert(!m_tileGrid.GetContentList(l).Contains(child));
			m_tileGrid.GetContentList(l).Add(child);
		}

		protected override void ChildRemoved(ServerGameObject child)
		{
			IntPoint3D l = child.Location;
			Debug.Assert(m_tileGrid.GetContentList(l) != null);
			bool removed = m_tileGrid.GetContentList(l).Remove(child);
			Debug.Assert(removed);
		}

		protected override bool OkToAddChild(ServerGameObject child, IntPoint3D p)
		{
			if (!this.Bounds.Contains(p))
				return false;

			if (!this.IsWalkable(p))
				return false;

			return true;
		}

		protected override void ChildMoved(ServerGameObject child, IntPoint3D oldLocation, IntPoint3D newLocation)
		{
			Debug.Assert(m_tileGrid.GetContentList(oldLocation) != null);
			bool removed = m_tileGrid.GetContentList(oldLocation).Remove(child);
			Debug.Assert(removed);

			if (m_tileGrid.GetContentList(newLocation) == null)
				m_tileGrid.SetContentList(newLocation, new List<ServerGameObject>());

			Debug.Assert(!m_tileGrid.GetContentList(newLocation).Contains(child));
			m_tileGrid.GetContentList(newLocation).Add(child);
		}

		public override string ToString()
		{
			return String.Format("Environment({0})", this.ObjectID);
		}
	}
}
