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
	public partial class SymbolEditorDialog : Window
	{
		System.Windows.Threading.DispatcherTimer m_timer;

		public SymbolEditorDialog()
		{
			this.InitializeComponent();

			m_timer = new System.Windows.Threading.DispatcherTimer();
			m_timer.Tick += new EventHandler(m_timer_Tick);
			m_timer.Interval = TimeSpan.FromMilliseconds(200);
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

			var symbols = cache.SymbolSet.Symbols;

			var chars = symbols.OfType<CharSymbol>().ToArray();
			var drawings = symbols.OfType<DrawingSymbol>().ToArray();

			dlg.charGrid.DataContext = chars;
			dlg.drawingGrid.DataContext = drawings;
		}

		void m_timer_Tick(object sender, EventArgs e)
		{
			m_timer.Stop();
			this.SymbolDrawingCache.Update();
		}

		private void charGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
		{
			m_timer.Start();
		}
	}
}