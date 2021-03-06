﻿using Dwarrowdelf.Client.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dwarrowdelf.Client
{
	public class MapControlConfig : INotifyPropertyChanged
	{
		MapControlPickMode m_pickMode;
		CameraControlMode m_cameraControlMode;

		public MapControlPickMode PickMode
		{
			get { return m_pickMode; }
			set { m_pickMode = value; Notify("PickMode");  }
		}

		public CameraControlMode CameraControlMode
		{
			get { return m_cameraControlMode; }
			set { m_cameraControlMode = value; Notify("CameraControlMode"); }
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	public enum MapControlPickMode
	{
		Underground,
		AboveGround,
		Constant,
	}

	public class MapControl3D : SharpDXHost
	{
		MyGame m_game;
		ToolTipService m_toolTipService;

		public MapControlConfig Config { get; private set; }

		DebugWindow m_debugWindow;

		public MapControl3D()
		{
			this.Config = new MapControlConfig();

			this.HoverTileView = new TileAreaView();
			this.SelectionTileAreaView = new TileAreaView();

			m_game = new MyGame(this);

			m_game.CursorService.LocationChanged += OnCursorMoved;
			m_game.SelectionService.SelectionChanged += OnSelectionChanged;
			m_game.SelectionService.GotSelection += OnGotSelection;

			m_game.Start();

			m_toolTipService = new ToolTipService(this);
		}

		protected override void Dispose(bool disposing)
		{
			if (m_debugWindow != null)
				m_debugWindow.Close();

			m_game.Stop();
			m_game.Dispose();
			m_game = null;

			base.Dispose(disposing);
		}

		public EnvironmentObject Environment
		{
			get { return m_game.Environment; }
			set { m_game.Environment = value; }
		}

		public void OpenDebugWindow()
		{
			if (m_debugWindow != null || m_game == null)
				return;

			var dbg = new DebugWindow();
			dbg.Owner = System.Windows.Window.GetWindow(this);
			dbg.SetGame(m_game);
			dbg.Closed += (s, e) => m_debugWindow = null;
			dbg.Show();
			m_debugWindow = dbg;
		}

		public void CameraLookAt(MovableObject ob)
		{
			if (ob == null)
			{
				this.Environment = null;
			}
			else
			{
				var env = ob.Environment;
				CameraLookAt(env, ob.Location);
			}
		}

		public void CameraLookAt(EnvironmentObject env, IntVector3 p)
		{
			this.Environment = env;
			m_game.CameraLookAt(p);
		}

		public void CameraMoveTo(MovableObject ob)
		{
			if (ob == null)
			{
				this.Environment = null;
			}
			else
			{
				var env = ob.Environment;
				CameraMoveTo(env, ob.Location);
			}
		}

		public void CameraMoveTo(EnvironmentObject env, IntVector3 p)
		{
			this.Environment = env;
			m_game.CameraMoveTo(p);
		}

		public Point PointToDevice(Point p)
		{
			var source = PresentationSource.FromVisual(this);
			var matrix = source.CompositionTarget.TransformToDevice;
			return matrix.Transform(p);
		}

		public Point DeviceToPoint(Point p)
		{
			var source = PresentationSource.FromVisual(this);
			var matrix = source.CompositionTarget.TransformFromDevice;
			return matrix.Transform(p);
		}

		public Rect GetPlacementRect(IntVector3 ml)
		{
			// XXX
			var view = m_game.Surfaces[0].Views[0];

			SharpDX.Matrix matrix = view.Camera.View * view.Camera.Projection;

			var p1 = ml.ToVector3();
			var p2 = p1 + new SharpDX.Vector3(1, 1, 0);
			SharpDX.Vector3 out1, out2;

			var vp = view.ViewPort;

			vp.Project(ref p1, ref matrix, out out1);
			vp.Project(ref p2, ref matrix, out out2);

			Rect rect = new Rect(
				DeviceToPoint(new System.Windows.Point(out1.X, out1.Y)),
				DeviceToPoint(new System.Windows.Point(out2.X, out2.Y)));

			return rect;
		}

		public event Action<MapSelection> GotSelection;

		public MapSelectionMode SelectionMode
		{
			get
			{
				return m_game.SelectionService.SelectionMode;
			}
			set
			{
				m_game.SelectionService.SelectionMode = value;
			}
		}

		public MapSelection Selection
		{
			get
			{
				return m_game.SelectionService.Selection;
			}
			set
			{
				m_game.SelectionService.Selection = value;
			}
		}

		public TileAreaView HoverTileView { get; private set; }

		void OnCursorMoved(IntVector3? pos)
		{
			if (pos.HasValue == false)
			{
				this.HoverTileView.ClearTarget();
			}
			else
			{
				var ml = pos.Value;

				if (this.Environment != null && this.Environment.Contains(ml))
				{
					this.HoverTileView.SetTarget(this.Environment, ml);
				}
				else
				{
					this.HoverTileView.ClearTarget();
				}
			}
		}

		public TileAreaView SelectionTileAreaView { get; private set; }

		void OnSelectionChanged(MapSelection selection)
		{
			if (!selection.IsSelectionValid)
			{
				this.SelectionTileAreaView.ClearTarget();
			}
			else
			{
				this.SelectionTileAreaView.SetTarget(this.Environment, selection.SelectionBox);
			}
		}

		void OnGotSelection(MapSelection selection)
		{
			if (this.GotSelection != null)
				this.GotSelection(selection);
		}
	}
}
