using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Connection
{
	/// <summary>
	/// Provides methods for host connection to start listening on new connections.
	/// </summary>
	public interface INetworkConnectionHost
	{
		/// <summary>
		/// Initiate the listener and start listening on new connections.
		/// </summary>
		/// <param name="worker">A background worker that reports the progress of the stage. Not mandatory.</param>
		/// <param name="port">The port to bind the server to.</param>
		void StartBroadcasting(Action<int, string> callback, int port);

		/// <summary>
		/// Stop the currently active listener if such is active.
		/// </summary>
		void StopBroadcasting();

		/// <summary>
		/// Force a connection to disconnect.
		/// </summary>
		/// <param name="connection">The source connection to disconnect.</param>
		void Disconnect(object connection);
	}
}
