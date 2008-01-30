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
using System.Windows.Shapes;

namespace MyGame
{
	/// <summary>
	/// Interaction logic for LOSDebugWindow.xaml
	/// </summary>
	public partial class LOSDebugWindow : Window
	{
		public LOSDebugWindow()
		{
			InitializeComponent();
		}

		TextBox[,] m_gridElements;

		public void SetGridSize(int n)
		{
			grid.Children.Clear();

			grid.Columns = n;
			grid.Rows = n;

			m_gridElements = new TextBox[n, n];
			for (int x = 0; x < n; x++)
				for (int y = 0; y < n; y++)
				{
					TextBox tb = new TextBox();
					m_gridElements[x, y] = tb;
					grid.Children.Add(tb);
				}
		}

		public void SetGridElementData(int x, int y, string str, bool visible, bool blocker)
		{

			TextBox tb = m_gridElements[y, x];
			tb.Text = str;
			if (blocker)
				tb.BorderThickness = new Thickness(4);
			else
				tb.BorderBrush = null;

			if (visible)
				tb.Background = null;
			else
				tb.Background = Brushes.LightGray;
		}
	}
}
