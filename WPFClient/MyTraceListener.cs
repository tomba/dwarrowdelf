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
		StringBuilder m_sb;

		delegate void AppendTextDelegate(string s);

		public MyTraceListener()
		{
		}

		void AppendText(string s)
		{
			m_textBox.AppendText(s);
			m_textBox.ScrollToEnd();
		}

		public TextBox TextBox
		{
			get { return m_textBox; }
			set 
			{
				m_textBox = value;
				if (m_sb != null && m_sb.Length > 0)
				{
					m_textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
						new AppendTextDelegate(AppendText), m_sb.ToString());
//					m_textBox.AppendText(m_sb.ToString());
//					m_textBox.ScrollToEnd();
					m_sb = null;
				}
			}
		}

		public override void Write(string value)
		{
			if (m_textBox != null)
			{
				m_textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
					new AppendTextDelegate(AppendText), value);
//				m_textBox.AppendText(value);
//				m_textBox.ScrollToEnd();
			}
			else
			{
				if (m_sb == null)
					m_sb = new StringBuilder();
				m_sb.Append(value);
			}
		}

		public override void WriteLine(string message)
		{
			Write(message);
			Write(Environment.NewLine);
		}
	}
}
