using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Connection
{
	/// <summary>
	/// Provides methods for client connection to connect to another host.
	/// </summary>
	public interface INetworkConnectionClient : INetworkConnection
	{
		/// <summary>
		/// Initiate an active connection to another host or server.
		/// </summary>
		/// <param name="worker">A background worker that reports the progress of the stage. Not mandatory.</param>
		/// <param name="ip">The ip of the host or server to connect to.</param>
		/// <param name="port">The port for the server.</param>
		void Connect(Action<int, string> callback, string ip, int port);

		/// <summary>
		/// Close currently opened connection if such connection is open and release all associated resources.
		/// </summary>
		void Disconnect();
	}
}
