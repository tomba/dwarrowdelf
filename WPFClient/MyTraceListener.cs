using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;


namespace MyGame
{
	class MyTraceListener : TraceListener
	{
		TextBox m_textBox;
		StringBuilder m_sb = new StringBuilder();

		public MyTraceListener()
		{
		}

		~MyTraceListener()
		{
			Console.WriteLine("gone");
		}

		void AppendText()
		{
			lock (m_sb)
			{
				if (m_sb.Length > 0)
				{
					m_textBox.AppendText(m_sb.ToString());
					m_textBox.ScrollToEnd();
					m_sb.Length = 0;
				}
			}
		}

		public TextBox TextBox
		{
			get { return m_textBox; }
			set 
			{
				m_textBox = value;

				if (m_textBox == null)
					return;

				AppendText();
			}
		}

		public override void Write(string value)
		{
			lock (m_sb)
				m_sb.Append(value);

			if (m_textBox != null)
					m_textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
						new Action(AppendText));
		}

		public override void WriteLine(string message)
		{
			lock (m_sb)
				m_sb.AppendLine(message);

			if (m_textBox != null)
				m_textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
					new Action(AppendText));
		}
	}
}
