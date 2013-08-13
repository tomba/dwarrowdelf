using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf
{
	public interface IConnection : IDisposable
	{
		bool IsConnected { get; }

		bool TryGetMessage(out Message msg);
		Task<Message> GetMessageAsync();

		void Send(Message msg);

		void Disconnect();

		event Action NewMessageEvent;
	}

	public interface INetStatCollector
	{
		void OnMessageReceived(Type msgType, int bytes);
		void OnMessageSent(Type msgType, int bytes);
	}
}
