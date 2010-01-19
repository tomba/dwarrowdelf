using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyGame.Client
{
	enum JobGroupType
	{
		Parallel,
		Serial,
	}

	interface IJob
	{
		IJob Parent { get; }
		Progress Progress { get; }
	}

	interface IJobGroup : IJob
	{
		IList<IJob> SubJobs { get; }
		JobGroupType JobGroupType { get; }
	}

	interface IActionJob : IJob
	{
		Living Worker { get; }
		GameAction CurrentAction { get; }

		Progress Assign(Living worker);
		Progress PrepareNextAction();
		Progress ActionProgress(ActionProgressEvent e);
	}


	abstract class SerialActionJob : IActionJob, INotifyPropertyChanged
	{
		IActionJob m_currentSubJob;
		ObservableCollection<IActionJob> m_subJobs = new ObservableCollection<IActionJob>();

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			MyDebug.WriteLine("[AI S] [{0}]: {1}", this.Worker, String.Format(format, args));
		}

		protected SerialActionJob(IJob parent)
		{
			this.Parent = parent;
		}

		public IJob Parent { get; private set; }

		Progress m_progress;
		public Progress Progress
		{
			get { return m_progress; }
			private set { m_progress = value; Notify("Progress"); }
		}

		public IList<IActionJob> SubJobs { get { return m_subJobs; } }

		Living m_worker;
		public Living Worker
		{
			get { return m_worker; }
			protected set { m_worker = value; Notify("Worker"); }
		}

		public GameAction CurrentAction
		{
			get { return m_currentSubJob != null ? m_currentSubJob.CurrentAction : null; }
		}




		public Progress Assign(Living worker)
		{
			Debug.Assert(this.Worker == null);
			Debug.Assert(this.Progress == Progress.None);

			var progress = AssignOverride(worker);
			SetProgress(progress);
			if (progress != Progress.Ok)
				return progress;

			this.Worker = worker;

			m_currentSubJob = FindAndAssignJob(this.SubJobs, this.Worker, out progress);
			SetProgress(progress);
			return progress;
		}

		protected virtual Progress AssignOverride(Living worker)
		{
			return Progress.Ok;
		}



		public Progress PrepareNextAction()
		{
			var progress = DoPrepareNextAction();
			SetProgress(progress);
			return progress;
		}

		Progress DoPrepareNextAction()
		{
			while (true)
			{
				Progress progress;

				if (m_currentSubJob == null)
				{
					m_currentSubJob = FindAndAssignJob(this.SubJobs, this.Worker, out progress);
					if (progress != Progress.Ok)
						return progress;
				}

				progress = m_currentSubJob.PrepareNextAction();
				Notify("CurrentAction");

				switch (progress)
				{
					case Progress.Ok:
						return Progress.Ok;

					case Progress.Done:
						m_currentSubJob = null;
						continue;

					case Progress.Abort:
					case Progress.Fail:
						return progress;

					case Progress.None:
					default:
						throw new Exception();
				}
			}
		}



		public Progress ActionProgress(ActionProgressEvent e)
		{
			Debug.Assert(this.Worker != null);
			Debug.Assert(this.Progress == Progress.Ok);
			Debug.Assert(m_currentSubJob != null);

			var progress = DoActionProgress(e);
			SetProgress(progress);
			return progress;
		}

		Progress DoActionProgress(ActionProgressEvent e)
		{
			var progress = m_currentSubJob.ActionProgress(e);
			Notify("CurrentAction");

			switch (progress)
			{
				case Progress.Ok:
					return Progress.Ok;

				case Progress.Abort:
				case Progress.Fail:
					return progress;

				case Progress.Done:
					m_currentSubJob = null;
					if (this.SubJobs.All(j => j.Progress == Progress.Done))
						return Progress.Done;
					else
						return Progress.Ok;

				case Progress.None:
				default:
					throw new Exception();
			}
		}

		static IActionJob FindAndAssignJob(IEnumerable<IActionJob> jobs, Living worker, out Progress progress)
		{
			Debug.Assert(!jobs.Any(j => j.Progress == Progress.Abort || j.Progress == Progress.Fail || j.Progress == Progress.Ok));

			//D("looking for new job");

			foreach (var job in jobs)
			{
				if (job.Progress == Progress.Done)
					continue;

				var subProgress = job.Assign(worker);

				switch (subProgress)
				{
					case Progress.Ok:
						//D("new job found");
						progress = subProgress;
						return job;

					case Progress.Done:
						continue;

					case Progress.Abort:
					case Progress.Fail:
						progress = subProgress;
						return null;

					case Progress.None:
					default:
						throw new Exception();
				}
			}

			// All subjobs are done

			//D("all subjobs done");

			progress = Progress.Done;
			return null;
		}


		protected void SetProgress(Progress progress)
		{
			switch (progress)
			{
				case Progress.None:
					break;

				case Progress.Ok:
					break;

				case Progress.Done:
					Cleanup();
					this.Worker = null;
					m_currentSubJob = null;
					break;

				case Progress.Abort:
					this.Worker = null;
					m_currentSubJob = null;
					break;

				case Progress.Fail:
					Cleanup();
					this.Worker = null;
					m_currentSubJob = null;
					break;
			}

			this.Progress = progress;
		}

		protected virtual void Cleanup()
		{
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		protected void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}



	abstract class JobGroup : IJobGroup, INotifyPropertyChanged
	{
		ObservableCollection<IJob> m_subJobs = new ObservableCollection<IJob>();

		protected JobGroup(IJob parent)
		{
			this.Parent = parent;
			m_subJobs.CollectionChanged += SubJobsChanged;
		}

		void SubJobsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add)
				throw new Exception();

			foreach (INotifyPropertyChanged job in e.NewItems)
			{
				job.PropertyChanged += SubJobPropertyChanged;
			}
		}

		void SubJobPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Progress")
				Notify("Progress");
		}

		public IJob Parent { get; private set; }

		public virtual Progress Progress
		{
			get
			{
				if (this.SubJobs.All(j => j.Progress == Progress.Done))
					return Progress.Done;

				if (this.SubJobs.Any(j => j.Progress == Progress.Fail))
					return Progress.Fail;

				return Progress.None;
			}
		}

		public IList<IJob> SubJobs { get { return m_subJobs; } }

		public abstract JobGroupType JobGroupType { get; }

		// XXX not called
		protected virtual void Cleanup()
		{
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		protected void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}


	abstract class ParallelJobGroup : JobGroup
	{
		protected ParallelJobGroup(IJob parent)
			: base(parent)
		{
		}

		public override Progress Progress
		{
			get
			{
				var progress = base.Progress;

				if (progress != Progress.None)
					return progress;

				if (this.SubJobs.All(j => j.Progress == Progress.Ok || j.Progress == Progress.Done))
					return Progress.Ok;

				return Progress.None;
			}
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Parallel; } }
	}


	abstract class SerialJobGroup : JobGroup
	{
		protected SerialJobGroup(IJobGroup parent)
			: base(parent)
		{
		}

		public override Progress Progress
		{
			get
			{
				if (this.SubJobs.Any(j => j.Progress == Progress.Ok))
					return Progress.Ok;

				return base.Progress;
			}
		}

		public override JobGroupType JobGroupType { get { return JobGroupType.Serial; } }
	}



	class MoveMineJob : SerialActionJob
	{
		Environment m_environment;
		IntPoint3D m_location;

		public MoveMineJob(IJob parent, Environment environment, IntPoint3D location)
			: base(parent)
		{
			m_environment = environment;
			m_location = location;

			this.SubJobs.Add(new MoveActionJob(this, m_environment, m_location, true));
			this.SubJobs.Add(new MineActionJob(this, m_environment, m_location));
		}

		/*
		 * XXX checkvalidity tms
		protected override Progress AssignOverride(Living worker)
		{
			if (worker.Environment != m_environment)
				return Progress.Abort;

			if (m_environment.GetInterior(m_location).ID == InteriorID.Empty)
				return Progress.Done;

			return Progress.Ok;
		}
		*/

		protected override void Cleanup()
		{
			m_environment = null;
		}

		public override string ToString()
		{
			return "MoveMineJob";
		}
	}


	class MineAreaJob : SerialActionJob
	{
		public Environment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaJob(Environment env, IntRect rect, int z)
			: base(null)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.NaturalWall);

			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, env, new IntPoint3D(p, z));
				this.SubJobs.Add(job);
			}
		}

		protected override void Cleanup()
		{
			m_environment = null;
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaSerialSameJob";
		}
	}

	class MineAreaParallelJob : ParallelJobGroup
	{
		public Environment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaParallelJob(Environment env, IntRect rect, int z)
			: base(null)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.NaturalWall);

			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, env, new IntPoint3D(p, z));
				this.SubJobs.Add(job);
			}
		}

		protected override void Cleanup()
		{
			m_environment = null;
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaParallelJob";
		}
	}

	class MineAreaSerialJob : SerialJobGroup
	{
		public Environment m_environment;
		public IEnumerable<IntPoint> m_locs;

		public MineAreaSerialJob(Environment env, IntRect rect, int z)
			: base(null)
		{
			m_environment = env;

			m_locs = rect.Range().Where(p => env.GetInterior(new IntPoint3D(p, z)).ID == InteriorID.NaturalWall);

			foreach (var p in m_locs)
			{
				var job = new MoveMineJob(this, env, new IntPoint3D(p, z));
				this.SubJobs.Add(job);
			}
		}

		protected override void Cleanup()
		{
			m_environment = null;
			m_locs = null;
		}

		public override string ToString()
		{
			return "MineAreaSerialJob";
		}
	}

	class BuildItemJob : SerialJobGroup
	{
		public BuildItemJob(BuildingData workplace, ItemObject[] sourceObjects)
			: base(null)
		{
			var env = workplace.Environment;
			var location = new IntPoint3D(workplace.Area.TopLeft, workplace.Z);

			this.SubJobs.Add(new FetchMaterials(this, env, location, sourceObjects));
			this.SubJobs.Add(new BuildItem(this, workplace, sourceObjects));
		}

		public override string ToString()
		{
			return "BuildItemJob";
		}
	}

	class FetchMaterials : ParallelJobGroup
	{
		public FetchMaterials(IJob parent, Environment env, IntPoint3D location, ItemObject[] objects)
			: base(parent)
		{
			foreach (var item in objects)
			{
				this.SubJobs.Add(new FetchMaterial(this, env, location, item));
			}
		}

		public override string ToString()
		{
			return "FetchMaterials";
		}
	}

	class FetchMaterial : SerialActionJob
	{
		public FetchMaterial(IJob parent, Environment env, IntPoint3D location, ItemObject item)
			: base(parent)
		{
			this.SubJobs.Add(new MoveActionJob(this, item.Environment, item.Location, false));
			this.SubJobs.Add(new GetItemActionJob(this, item));
			this.SubJobs.Add(new MoveActionJob(this, env, location, false));
			this.SubJobs.Add(new DropItemActionJob(this, item));
		}

		public override string ToString()
		{
			return "FetchMaterial";
		}
	}

	class BuildItem : SerialActionJob
	{
		public BuildItem(IJob parent, BuildingData workplace, ItemObject[] items)
			: base(parent)
		{
			var env = workplace.Environment;
			var location = new IntPoint3D(workplace.Area.TopLeft, workplace.Z);

			this.SubJobs.Add(new MoveActionJob(this, env, location, false));
			this.SubJobs.Add(new BuildItemActionJob(this, items));
		}

		public override string ToString()
		{
			return "BuildItem";
		}
	}

}
