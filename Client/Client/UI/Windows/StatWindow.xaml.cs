using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Dwarrowdelf.Client.UI
{
	public partial class StatWindow : Window
	{
		World m_world;

		int m_fpsCounter;
		int m_turnCounter;
		int m_tickCounter;

		DispatcherTimer m_timer;
		DateTime m_prevTime;

		TimeSpan m_lastRender;

		public StatWindow()
		{
			this.Initialized += StatWindow_Initialized;
			this.Closed += StatWindow_Closed;

			InitializeComponent();
		}

		void StatWindow_Initialized(object sender, EventArgs e)
		{
			GameData.Data.WorldChanged += Data_WorldChanged;
			Data_WorldChanged(GameData.Data.World);

			CompositionTarget.Rendering += CompositionTarget_Rendering;

			m_timer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromSeconds(1),
			};
			m_timer.Tick += OnTimerCallback;
			m_timer.Start();

			m_prevTime = DateTime.UtcNow;

			OnTimerCallback(null, null);
		}

		void StatWindow_Closed(object sender, EventArgs e)
		{
			if (m_world != null)
			{
				m_world.TurnEnded -= OnTurnEnded;
				m_world.TickStarted -= OnTickStarted;
			}

			m_world = null;

			CompositionTarget.Rendering -= CompositionTarget_Rendering;
		}

		void Data_WorldChanged(World world)
		{
			if (m_world != null)
			{
				m_world.TurnEnded -= OnTurnEnded;
				m_world.TickStarted -= OnTickStarted;
			}

			m_world = world;

			if (m_world != null)
			{
				m_world.TurnEnded += OnTurnEnded;
				m_world.TickStarted += OnTickStarted;
			}
		}

		void OnTurnEnded()
		{
			m_turnCounter++;
		}

		void OnTickStarted()
		{
			m_tickCounter++;
		}

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			var args = (RenderingEventArgs)e;

			if (args.RenderingTime == m_lastRender)
				return;

			m_lastRender = args.RenderingTime;

			m_fpsCounter++;
		}

		void OnTimerCallback(object ob, EventArgs args)
		{
			var now = DateTime.UtcNow;
			var diff = now - m_prevTime;
			m_prevTime = DateTime.UtcNow;

			var fps = m_fpsCounter / diff.TotalSeconds;
			fpsTextBox.Text = fps.ToString("F2");

			var turns = m_turnCounter / diff.TotalSeconds;
			turnsTextBox.Text = turns.ToString("F2");

			var ticks = m_tickCounter / diff.TotalSeconds;
			ticksTextBox.Text = ticks.ToString("F2");

			m_fpsCounter = m_turnCounter = m_tickCounter = 0;

		}
	}
}
