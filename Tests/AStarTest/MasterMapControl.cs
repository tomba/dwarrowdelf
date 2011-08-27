using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;

using Dwarrowdelf;
using AStarTest;
using System.Diagnostics;
using Dwarrowdelf.AStar;

/*
 * Benchmark pitkän palkin oikeasta alareunasta vasempaan:
 * BinaryHeap 3D: mem 12698992, ticks 1289165
 * BinaryHeap 3D: mem 10269648, ticks 1155656 (short IntPoint3D)
 * SimpleList 3D: mem 12699376, ticks 88453781
 * 
 */
namespace AStarTest
{
	class MasterMapControl : UserControl, INotifyPropertyChanged
	{
		MapControl m_map;
		Canvas m_canvas;
		Polyline m_path1;

		ScaleTransform m_scaleTransform;
		TranslateTransform m_translateTransform;



		public static int GetZ(DependencyObject obj)
		{
			return (int)obj.GetValue(ZProperty);
		}

		public static void SetZ(DependencyObject obj, int value)
		{
			obj.SetValue(ZProperty, value);
		}

		public static readonly DependencyProperty ZProperty =
			DependencyProperty.RegisterAttached("Z", typeof(int), typeof(MasterMapControl), new UIPropertyMetadata(0));

		public MasterMapControl()
		{
			this.UseLayoutRounding = true;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var grid = new Grid();
			AddChild(grid);

			grid.ClipToBounds = true;

			m_map = new MapControl();
			m_map.SomethingChanged += new Action(m_map_SomethingChanged);
			m_map.AStarDone += new Action<AStarResult>(m_map_AStarDone);
			grid.Children.Add((UIElement)m_map);

			m_canvas = new Canvas();
			m_canvas.UseLayoutRounding = false;
			m_canvas.SnapsToDevicePixels = true;

			m_canvas.ClipToBounds = true;
			grid.Children.Add(m_canvas);

			m_scaleTransform = new ScaleTransform();
			m_translateTransform = new TranslateTransform();
			var canvasTransform = new TransformGroup();
			canvasTransform.Children.Add(m_scaleTransform);
			canvasTransform.Children.Add(m_translateTransform);
			m_canvas.RenderTransform = canvasTransform;

			m_path1 = new Polyline();
			m_path1.Stroke = System.Windows.Media.Brushes.SlateGray;
			m_path1.StrokeThickness = 0.1;
			m_path1.FillRule = FillRule.EvenOdd;
			m_canvas.Children.Add(m_path1);

			Canvas.SetLeft(m_path1, 0.5);
			Canvas.SetTop(m_path1, 0.5);

			var pl = new Polyline();
			pl.Stroke = System.Windows.Media.Brushes.SlateGray;
			pl.StrokeThickness = 0.1;
			pl.Points.Add(new Point(3.5, 3.5));
			pl.Points.Add(new Point(4.6, 4.5));
			pl.Points.Add(new Point(5.5, 3.5));
			m_canvas.Children.Add(pl);
		}

		void m_map_AStarDone(AStarResult res)
		{
			var l = res.LastNode.Loc;
			var dirs = res.GetPathReverse();

			m_path1.Points.Clear();
			IntPoint p = new IntPoint(l.X, l.Y);
			m_path1.Points.Add(new Point(p.X, p.Y));
			foreach (var d in dirs)
			{
				var v = IntVector.FromDirection(d);
				p += v;
				m_path1.Points.Add(new Point(p.X, p.Y));
			}

			SetZ(m_path1, l.Z);
		}

		void m_map_SomethingChanged()
		{
			CheckCanvas();
		}

		public MapControl TileControl
		{
			get { return m_map; }
		}

		public int Z
		{
			get { return m_map.Z; }
			set { m_map.Z = value; }
		}

		public double TileSize
		{
			get { return m_map.TileSize; }
			set
			{
				m_map.TileSize = value;
				CheckCanvas();
			}
		}

		public Point CenterPos
		{
			get { return m_map.CenterPos; }
			set
			{
				m_map.CenterPos = value;
				CheckCanvas();
			}
		}

		void CheckCanvas()
		{
			var p = m_map.MapTileToScreenPoint(new Point(-0.5, -0.5));

			m_scaleTransform.ScaleX = m_map.TileSize;
			m_scaleTransform.ScaleY = -m_map.TileSize;

			m_translateTransform.X = p.X;
			m_translateTransform.Y = p.Y;

			foreach (FrameworkElement child in m_canvas.Children)
			{
				if (GetZ(child) != this.Z)
					child.Visibility = System.Windows.Visibility.Hidden;
				else
					child.Visibility = System.Windows.Visibility.Visible;
			}
		}

		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

	}
}
