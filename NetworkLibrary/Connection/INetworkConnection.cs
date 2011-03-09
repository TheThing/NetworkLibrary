using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;
using NetworkLibrary.Exceptions;

namespace NetworkLibrary.Connection
{
	/// <summary>
	/// A public interface for the network plugin connection.
	/// </summary>
	public interface INetworkConnection : IExceptionHandler, IDisposable
	{
		/// <summary>
		/// Send a packet over the network using the currently loaded plugin.
		/// </summary>
		/// <param name="packet">The packet to transmit.</param>
		/// <param name="target">The target for the packet. Used by the host.</param>
		void SendPacket(NetworkPacket packet, object target);
		/// <summary>
		/// Get whether the connection is up and active.
		/// </summary>
		bool Connected { get; }
		/// <summary>
		/// Occurs when the network connection recieves a new packet from the network.
		/// </summary>
		event delegatePacketRecieved OnPacketRecieved;
		/// <summary>
		/// Occurs when the or a connection has been disconnected.
		/// </summary>
		event delegateDisconnected OnDisconnected;
	}
}
