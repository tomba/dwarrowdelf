﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	sealed class MapControlElementsService
	{
		MapControl m_mapControl;
		Canvas m_canvas;

		Dictionary<IAreaElement, FrameworkElement> m_elementMap;
		ScaleTransform m_scaleTransform;
		TranslateTransform m_translateTransform;

		EnvironmentObject m_env;

		public MapControlElementsService(MapControl mapControl, Canvas canvas)
		{
			m_mapControl = mapControl;
			m_canvas = canvas;

			m_scaleTransform = new ScaleTransform();
			m_translateTransform = new TranslateTransform();

			var group = new TransformGroup();
			group.Children.Add(m_scaleTransform);
			group.Children.Add(m_translateTransform);
			m_canvas.RenderTransform = group;

			m_elementMap = new Dictionary<IAreaElement, FrameworkElement>();

			m_mapControl.EnvironmentChanged += OnEnvironmentChanged;
			m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
			m_mapControl.ScreenCenterPosChanged += OnScreenCenterPosChanged;

			OnEnvironmentChanged(m_mapControl.Environment);
		}

		void OnEnvironmentChanged(EnvironmentObject env)
		{
			if (m_env != null)
				((INotifyCollectionChanged)m_env.AreaElements).CollectionChanged -= OnElementCollectionChanged;

			m_env = env;

			if (m_env != null)
				((INotifyCollectionChanged)m_env.AreaElements).CollectionChanged += OnElementCollectionChanged;

			UpdateElements();
		}

		void OnScreenCenterPosChanged(object mc, DoublePoint3 center, IntVector3 diff)
		{
			UpdateTranslateTransform();
			UpdateScaleTransform();

			// XXX doesn't work with different map orientations

			if (diff.Z != 0)
			{
				int z = MyMath.Round(center.Z);

				foreach (FrameworkElement child in m_canvas.Children)
				{
					if (GetElementZ(child) != z)
						child.Visibility = System.Windows.Visibility.Hidden;
					else
						child.Visibility = System.Windows.Visibility.Visible;
				}
			}
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			UpdateTranslateTransform();
			UpdateScaleTransform();
		}

		void UpdateTranslateTransform()
		{
			var p = m_mapControl.ScreenToRenderPoint(new Point(-0.5, -0.5));
			m_translateTransform.X = p.X;
			m_translateTransform.Y = p.Y;
		}

		void UpdateScaleTransform()
		{
			m_scaleTransform.ScaleX = m_mapControl.TileSize / 10;
			m_scaleTransform.ScaleY = m_mapControl.TileSize / 10;
		}

		void AddElement(IAreaElement element)
		{
			var shape = new Rectangle();

			if (element is Stockpile)
				shape.Stroke = Brushes.Gray;

			shape.StrokeThickness = 1;
			shape.IsHitTestVisible = false;
			shape.Width = element.Area.Columns * 10;
			shape.Height = element.Area.Rows * 10;

			var r = element.Area;
			Canvas.SetLeft(shape, r.X * 10);
			Canvas.SetTop(shape, r.Y * 10);
			SetElementZ(shape, r.Z);

			m_canvas.Children.Add(shape);
			m_elementMap[element] = shape;
		}

		void RemoveElement(IAreaElement element)
		{
			var e = m_elementMap[element];
			m_canvas.Children.Remove(e);
			m_elementMap.Remove(element);
		}

		void UpdateElements()
		{
			m_canvas.Children.Clear();
			m_elementMap.Clear();

			if (m_env != null)
			{
				foreach (IAreaElement element in m_env.AreaElements)
				{
					if (element.Environment == m_env)
						AddElement(element);
				}
			}
		}

		void OnElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (IAreaElement b in e.NewItems)
						if (b.Environment == m_env)
							AddElement(b);
					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (IAreaElement b in e.OldItems)
						if (b.Environment == m_env)
							RemoveElement(b);

					break;

				case NotifyCollectionChangedAction.Reset:
					foreach (IAreaElement b in m_elementMap.Keys.ToArray())
						RemoveElement(b);

					break;

				default:
					throw new Exception();
			}
		}


		public static int GetElementZ(DependencyObject obj)
		{
			return (int)obj.GetValue(ElementZProperty);
		}

		public static void SetElementZ(DependencyObject obj, int value)
		{
			obj.SetValue(ElementZProperty, value);
		}

		public static readonly DependencyProperty ElementZProperty =
			DependencyProperty.RegisterAttached("ElementZ", typeof(int), typeof(MapControlElementsService), new UIPropertyMetadata(0));
	}
}
