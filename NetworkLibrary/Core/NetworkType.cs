using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using NetworkPluginManager.Core;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Specifies Identifiers to indicate the network connection type.
	/// </summary>
	public enum NetworkType
	{
		/// <summary>
		/// The network connection is a host and listens on new connections.
		/// </summary>
		Host,
		/// <summary>
		/// The network connection is a client to connect or join another host.
		/// </summary>
		Client
	}
}
