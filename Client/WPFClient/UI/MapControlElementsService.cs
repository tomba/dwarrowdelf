using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dwarrowdelf.Client.UI
{
	class MapControlElementsService
	{
		MapControl m_mapControl;
		Canvas m_canvas;

		Dictionary<IDrawableElement, FrameworkElement> m_elementMap;
		ScaleTransform m_scaleTransform;
		TranslateTransform m_translateTransform;

		Environment m_env;

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

			m_elementMap = new Dictionary<IDrawableElement, FrameworkElement>();

			m_mapControl.EnvironmentChanged += OnEnvironmentChanged;
			m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
			m_mapControl.ZChanged += OnZChanged;

			OnEnvironmentChanged(m_mapControl.Environment);
		}

		void OnEnvironmentChanged(Environment env)
		{
			if (m_env != null)
			{
				m_env.Buildings.CollectionChanged -= OnElementCollectionChanged;
				((INotifyCollectionChanged)m_env.Stockpiles).CollectionChanged -= OnElementCollectionChanged;
				((INotifyCollectionChanged)m_env.ConstructionSites).CollectionChanged -= OnElementCollectionChanged;
			}

			m_env = env;

			if (m_env != null)
			{
				m_env.Buildings.CollectionChanged += OnElementCollectionChanged;
				((INotifyCollectionChanged)m_env.Stockpiles).CollectionChanged += OnElementCollectionChanged;
				((INotifyCollectionChanged)m_env.ConstructionSites).CollectionChanged += OnElementCollectionChanged;
			}

			UpdateElements();
		}

		void OnTileLayoutChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			UpdateTranslateTransform();
			UpdateScaleTransform();
		}

		void UpdateTranslateTransform()
		{
			var p = m_mapControl.MapLocationToScreenPoint(new Point(-0.5, -0.5));
			m_translateTransform.X = p.X;
			m_translateTransform.Y = p.Y;
		}

		void UpdateScaleTransform()
		{
			m_scaleTransform.ScaleX = m_mapControl.TileSize;
			m_scaleTransform.ScaleY = -m_mapControl.TileSize;
		}

		void OnZChanged(int z)
		{
			foreach (FrameworkElement child in m_canvas.Children)
			{
				if (GetElementZ(child) != z)
					child.Visibility = System.Windows.Visibility.Hidden;
				else
					child.Visibility = System.Windows.Visibility.Visible;
			}
		}

		void AddElement(IDrawableElement element)
		{
			var e = element.Element;

			if (e != null)
			{
				var r = element.Area;
				Canvas.SetLeft(e, r.X);
				Canvas.SetTop(e, r.Y);
				SetElementZ(e, r.Z);

				m_canvas.Children.Add(e);
				m_elementMap[element] = e;
			}
		}

		void RemoveElement(IDrawableElement element)
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
				var elements = m_env.Buildings.Cast<IDrawableElement>()
					.Concat(m_env.Stockpiles)
					.Concat(m_env.ConstructionSites);

				foreach (IDrawableElement element in elements)
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
					foreach (IDrawableElement b in e.NewItems)
						if (b.Environment == m_env)
							AddElement(b);
					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (IDrawableElement b in e.OldItems)
						if (b.Environment == m_env)
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
