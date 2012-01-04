using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace Dwarrowdelf.Client.UI
{
	sealed class TileView : INotifyPropertyChanged
	{
		EnvironmentObject m_environment;
		IntPoint3D m_location;

		public TileView()
		{
			this.Objects = new MovableObjectCollection();
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
					m_environment.MapTileTerrainChanged -= MapTerrainChanged;
					m_environment.MapTileObjectChanged -= MapObjectChanged;
				}

				m_environment = value;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged += MapTerrainChanged;
					m_environment.MapTileObjectChanged += MapObjectChanged;
				}

				Notify("Environment");
				NotifyTileChanges();
			}
		}

		public IntPoint3D Location
		{
			get { return m_location; }

			set
			{
				if (m_location == value)
					return;

				m_location = value;

				Notify("Location");
				NotifyTileChanges();
			}
		}

		void NotifyTileChanges()
		{
			NotifyTileTerrainChanges();
			NotifyTileObjectChanges();
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interior");
			Notify("Terrain");
			Notify("TerrainMaterial");
			Notify("InteriorMaterial");
			Notify("WaterLevel");
			Notify("Flags");
			Notify("MapElement");
		}

		void NotifyTileObjectChanges()
		{
			this.Objects.Clear();

			if (this.Environment != null)
			{
				var list = this.Environment.GetContents(this.Location);
				foreach (var o in list)
					this.Objects.Add(o);
			}

			Notify("Objects");
		}

		void MapTerrainChanged(IntPoint3D l)
		{
			if (l != this.Location)
				return;

			NotifyTileTerrainChanges();
		}

		void MapObjectChanged(MovableObject ob, IntPoint3D l, MapTileObjectChangeType changeType)
		{
			if (l != this.Location)
				return;

			NotifyTileObjectChanges();
		}

		public MovableObjectCollection Objects { get; private set; }

		public InteriorInfo Interior
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetInterior(this.Location);
			}
		}

		public MaterialInfo InteriorMaterial
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetInteriorMaterial(this.Location);
			}
		}

		public MaterialInfo TerrainMaterial
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetTerrainMaterial(this.Location);
			}
		}

		public TerrainInfo Terrain
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetTerrain(this.Location);
			}
		}

		public byte WaterLevel
		{
			get
			{
				if (this.Environment == null)
					return 0;
				return this.Environment.GetWaterLevel(this.Location);
			}
		}

		public TileFlags Flags
		{
			get
			{
				if (this.Environment == null)
					return TileFlags.None;
				return this.Environment.GetTileFlags(this.Location);
			}
		}

		public IDrawableElement MapElement
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetElementAt(this.Location);
			}
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
