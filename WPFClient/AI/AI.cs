using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum Progress
	{
		None,
		Ok,
		Fail,
		Done,
	}

	class AI
	{
		Living m_object;
		Job m_job;

		public AI(Living ob)
		{
			m_object = ob;
		}

		public void ActionProgress(ActionProgressEvent e)
		{
			if (m_job == null)
				return;

			var progress = m_job.ActionProgress(e);

			if (progress == Progress.Done)
			{
				MyDebug.WriteLine("[AI] JOB DONE ({0})!", m_job);
				World.TheWorld.Jobs.Remove(m_job);
				m_job = null;
			}
			else if (progress == Progress.Fail)
			{
				MyDebug.WriteLine("[AI] JOB FAIL ({0})!!!", m_job);
				World.TheWorld.Jobs.Remove(m_job);
				m_job.Quit();
				m_job = null;
			}
			else if (progress == Progress.Ok)
			{
				MyDebug.WriteLine("Job progressing");
			}
		}

		public void ActionRequired()
		{
			if (m_object == GameData.Data.CurrentObject)
				return;

			if (m_job == null)
			{
				var job = FindJob();

				if (job == null)
				{
					Idle();
					return;
				}

				m_job = job;
			}

			var action = m_job.ActionRequired();
			if (action != null)
			{
				m_object.EnqueueAction(action);
			}
			else
			{
				MyDebug.WriteLine("[AI] JOB ABORTED ({0})!!!", m_job);
				m_job.Quit();
				m_job = null;
			}
		}

		void Idle()
		{
			MyDebug.WriteLine("[AI] no job to do");
			var action = new WaitAction(1);
			m_object.EnqueueAction(action);
		}

		Job FindJob()
		{
			foreach (var job in m_object.World.Jobs.Where(j => j.Worker == null))
			{
				var res = job.Take(m_object);
				if (res == Progress.Ok)
				{
					return job;
				}
				else if (res == Progress.Done)
				{
					MyDebug.WriteLine("[AI] JOB (already) DONE!");
					World.TheWorld.Jobs.Remove(m_job);
				}
			}

			return null;
		}
	}
}
