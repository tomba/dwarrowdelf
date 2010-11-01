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
using Dwarrowdelf.Client;
using AStarTest;
using System.Diagnostics;

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
		TransformGroup m_canvasTransform;
		Polyline m_path1;



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
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var grid = new Grid();
			AddChild(grid);

			m_map = new MapControl();
			m_map.SomethingChanged += new Action(m_map_SomethingChanged);
			m_map.AStarDone += new Action<Dwarrowdelf.AStar.AStar3DResult>(m_map_AStarDone);
			grid.Children.Add((UIElement)m_map);

			m_canvas = new Canvas();
			//m_canvas.ClipToBounds = true;
			grid.Children.Add(m_canvas);

			m_canvasTransform = new TransformGroup();
			m_canvasTransform.Children.Add(new ScaleTransform());
			m_canvasTransform.Children.Add(new TranslateTransform());
			m_canvas.RenderTransform = m_canvasTransform;

			m_path1 = new Polyline();
			m_path1.Stroke = System.Windows.Media.Brushes.SlateGray;
			m_path1.StrokeThickness = 0.1;
			m_path1.FillRule = FillRule.EvenOdd;
			m_canvas.Children.Add(m_path1);

			Canvas.SetLeft(m_path1, 0.5);
			Canvas.SetTop(m_path1, 0.5);
		}

		void m_map_AStarDone(Dwarrowdelf.AStar.AStar3DResult res)
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

		public int TileSize
		{
			get { return m_map.TileSize; }
			set
			{
				m_map.TileSize = value;
				CheckCanvas();
			}
		}

		public IntPoint CenterPos
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
			return;
			var p = m_map.MapLocationToScreenPoint(new IntPoint(0, -1));

			((ScaleTransform)m_canvasTransform.Children[0]).ScaleX = m_map.TileSize;
			((ScaleTransform)m_canvasTransform.Children[0]).ScaleY = -m_map.TileSize;

			((TranslateTransform)m_canvasTransform.Children[1]).X = p.X;
			((TranslateTransform)m_canvasTransform.Children[1]).Y = p.Y;

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
