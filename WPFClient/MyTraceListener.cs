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

				if (m_textBox == null)
					return;

				if (m_sb != null && m_sb.Length > 0)
				{
					Write(m_sb.ToString());
					m_sb = null;
				}
			}
		}

		public override void Write(string value)
		{
			if (m_textBox != null)
			{
				if (m_textBox.Dispatcher.CheckAccess())
					AppendText(value);
				else
					m_textBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
						new AppendTextDelegate(AppendText), value);
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
			Write(System.Environment.NewLine);
		}
	}
}
