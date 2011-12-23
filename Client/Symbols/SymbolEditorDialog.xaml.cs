using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.Symbols
{
	/// <summary>
	/// Interaction logic for SymbolEditorDialog.xaml
	/// </summary>
	public sealed partial class SymbolEditorDialog : Window
	{
		public SymbolEditorDialog()
		{
			this.InitializeComponent();
		}

		public SymbolDrawingCache SymbolDrawingCache
		{
			get { return (SymbolDrawingCache)GetValue(SymbolDrawingCacheProperty); }
			set { SetValue(SymbolDrawingCacheProperty, value); }
		}

		public static readonly DependencyProperty SymbolDrawingCacheProperty =
			DependencyProperty.Register("SymbolDrawingCache", typeof(SymbolDrawingCache), typeof(SymbolEditorDialog), new UIPropertyMetadata(OnSymbolDrawingCacheChanged));

		static void OnSymbolDrawingCacheChanged(DependencyObject ob, DependencyPropertyChangedEventArgs args)
		{
			
			var cache = (SymbolDrawingCache)args.NewValue;
			var dlg = (SymbolEditorDialog)ob;

			dlg.DataContext = cache.SymbolSet;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.SymbolDrawingCache.Update();
		}

		private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
				return;

			var ob = e.AddedItems[0];
			propGrid.SelectedObject = ob;
		}
	}
}