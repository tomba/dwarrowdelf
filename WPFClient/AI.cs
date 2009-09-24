using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
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
				MyDebug.WriteLine("JOB DONE!");
				World.TheWorld.Jobs.Remove(m_job);
				m_job = null;
			}
			else if (progress == Progress.Fail)
			{
				MyDebug.WriteLine("JOB FAIL!!!");
				World.TheWorld.Jobs.Remove(m_job);
				m_job = null;
			}
			else
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

			var action = m_job.Do();
			if (action != null)
			{
				m_object.EnqueueAction(action);
			}
			else
			{
				throw new Exception();
			}
		}

		void Idle()
		{
			MyDebug.WriteLine("no job to do");
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
					MyDebug.WriteLine("JOB (already) DONE!");
					World.TheWorld.Jobs.Remove(m_job);
				}
			}

			return null;
		}
	}
}
