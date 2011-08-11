using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	public class HistoryTextBox : TextBox
	{
		public delegate void TextEnteredDelegate(string str);
		public event TextEnteredDelegate TextEntered;

		List<string> m_stringList;
		int m_historyPos;
		int m_maxSize = 100;
		string m_searchString;
		bool m_changingText = false;

		public HistoryTextBox()
		{
			m_stringList = new List<string>(m_maxSize);
			m_stringList.Add("asd");
			m_historyPos = m_stringList.Count - 1;
		}

		public string[] History
		{
			get
			{
				string[] arr = new string[m_stringList.Count - 1];
				m_stringList.CopyTo(0, arr, 0, m_stringList.Count - 1);
				return arr;
			}
			set
			{
				m_stringList = new List<string>(value);
				m_stringList.Add("");
				m_historyPos = m_stringList.Count - 1;
			}
		}

		public void EnterPressed()
		{
			if (TextEntered != null)
				TextEntered(base.Text);

			if (base.Text.Length == 0)
				return;

			if (m_stringList.Count < 2 || m_stringList[m_stringList.Count - 2] != base.Text)
			{
				m_stringList[m_stringList.Count - 1] = base.Text;
				m_stringList.Add("");

				if (m_stringList.Count > m_maxSize)
				{
					m_stringList.RemoveAt(0);
				}
			}

			m_historyPos = m_stringList.Count - 1;
			base.Clear();
		}

		void MessageBeep(int x)
		{
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				EnterPressed();
				e.Handled = true;
			}
			else if (e.Key == Key.Up)
			{
				if (m_historyPos == 0)
				{
					MessageBeep(0);
				}
				else
				{
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
					{
						if (m_historyPos == m_stringList.Count - 1)
						{
							m_stringList[m_stringList.Count - 1] = base.Text;
						}

						if (m_searchString == null)
						{
							m_searchString = base.Text;
						}

						int foundIdx = -1;

						for (int i = m_historyPos - 1; i >= 0; i--)
						{
							if (m_stringList[i].StartsWith(m_searchString))
							{
								foundIdx = i;
								break;
							}
						}

						if (foundIdx != -1)
						{
							m_changingText = true;
							base.Text = m_stringList[foundIdx];
							m_changingText = false;

							SelectionStart = base.Text.Length;
							SelectionLength = 0;
							this.ScrollToHome();

							m_historyPos = foundIdx;
						}
						else
						{
							MessageBeep(0);
						}
					}
					else
					{
						if (m_historyPos == m_stringList.Count - 1)
						{
							m_stringList[m_stringList.Count - 1] = base.Text;
						}

						m_historyPos--;
						base.Text = m_stringList[m_historyPos];

						SelectionStart = base.Text.Length;
						SelectionLength = 0;
						this.ScrollToHome();
					}
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Down)
			{
				if (m_historyPos == m_stringList.Count - 1)
				{
					MessageBeep(0);
				}
				else
				{
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
					{
						int foundIdx = -1;

						for (int i = m_historyPos + 1; i < m_stringList.Count - 1; i++)
						{
							if (m_stringList[i].StartsWith(m_searchString))
							{
								foundIdx = i;
								break;
							}
						}

						if (foundIdx != -1)
						{
							m_changingText = true;
							base.Text = m_stringList[foundIdx];
							m_changingText = false;

							SelectionStart = base.Text.Length;
							SelectionLength = 0;
							this.ScrollToHome();

							m_historyPos = foundIdx;
						}
						else
						{
							MessageBeep(0);
						}
					}
					else
					{
						m_historyPos++;

						base.Text = m_stringList[m_historyPos];

						SelectionStart = base.Text.Length;
						SelectionLength = 0;
						this.ScrollToHome();
					}
				}

				e.Handled = true;
			}

			if (e.Handled)
				return;

			base.OnPreviewKeyDown(e);
		}

		protected override void OnTextChanged(TextChangedEventArgs e)
		{
			if (m_changingText == false)
				m_searchString = null;

			base.OnTextChanged(e);
		}
	}
}
