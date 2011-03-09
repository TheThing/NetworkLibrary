using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;

namespace NetworkLibrary.Connection
{
	/// <summary>
	/// A delegate used when new packets have been recieved.
	/// </summary>
	/// <param name="source">The source of the packet.</param>
	/// <param name="packet">The packet itself.</param>
	public delegate void delegatePacketRecieved(object source, NetworkPacket packet);

	/// <summary>
	/// A delegate used when a connection has been disconnected.
	/// </summary>
	/// <param name="source">The source connection that was disconnected.</param>
	/// <param name="reason">The reason for the disconnection. Either this is string or an Exception.</param>
	public delegate void delegateDisconnected(object source, object reason);
}
