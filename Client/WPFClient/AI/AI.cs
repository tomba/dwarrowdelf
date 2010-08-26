using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

namespace MyGame.Client
{

	class AI
	{
		Living m_living;
		IActionJob m_currentJob;

		public AI(Living living)
		{
			m_living = living;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			MyDebug.WriteLine("[AI] [{0}]: {1}", m_living, String.Format(format, args));
		}

		public void ActionRequired()
		{
			//if (m_living == GameData.Data.CurrentObject)
			//	return;

			var jm = m_living.World.JobManager;

			while (true)
			{
				if (m_currentJob == null)
				{
					m_currentJob = jm.FindAndAssignJob(m_living);

					if (m_currentJob == null)
					{
						D("no job to do");
						m_living.DoSkipAction();
						return;
					}
					else
					{
						m_currentJob.PropertyChanged += OnJobPropertyChanged;
					}
				}

				var progress = m_currentJob.PrepareNextAction();

				switch (progress)
				{
					case Progress.Ok:
						var action = m_currentJob.CurrentAction;
						if (action == null)
							throw new Exception();

						m_living.DoAction(action);
						return;

					case Progress.Done:
					case Progress.Fail:
					case Progress.Abort:
						D("ActionRequired: {0} in {1}, looking for new job", progress, m_currentJob);
						m_currentJob = null;
						break;

					case Progress.None:
					default:
						throw new Exception();
				}
			}
		}

		public void ActionProgress(ActionProgressEvent e)
		{
			if (m_currentJob == null)
				return;

			var progress = m_currentJob.ActionProgress(e);

			switch (progress)
			{
				case Progress.None:
					throw new Exception();
				// break;

				case Progress.Ok:
					D("Job progressing");
					break;

				case Progress.Done:
				case Progress.Fail:
				case Progress.Abort:
					D("ActionProgress: {0} in {1}", progress, m_currentJob);
					m_currentJob = null;
					break;

				default:
					throw new Exception();
			}
		}

		void OnJobPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Debug.Assert(sender == m_currentJob);

			IJob job = (IJob)sender;
			if (e.PropertyName == "Progress")
			{
				if (job.Progress == Progress.Abort)
				{
					job.PropertyChanged -= OnJobPropertyChanged;
					m_currentJob = null;
				}
			}
		}
	}
}
