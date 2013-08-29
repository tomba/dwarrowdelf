using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	public sealed partial class World
	{
		public Task WaitTickEnded()
		{
			VerifyAccess();

			if (this.IsTickOnGoing == false)
				return Task.FromResult<bool>(true);

			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

			var action = new Action(() => tcs.SetResult(true));

			this.TickEnded += action;

			tcs.Task.ContinueWith(t => this.TickEnded -= action);

			return tcs.Task;
		}
	}
}
