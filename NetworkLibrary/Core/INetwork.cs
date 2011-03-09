using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using NetworkLibrary.Exceptions;
using NetworkLibrary.Connection;
using NetworkLibrary.Utilities;
using NetworkLibrary.Utilities.Parser;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// A public interface for the connection, whether the connection is a client or host.
	/// </summary>
	public interface INetwork : IExceptionHandler, IDisposable
	{
		/// <summary>
		/// Occurs whenever a connection has been lost. For host this informes of when a client
		/// has been disconnected, for a client this informs when the connection has been lost.
		/// </summary>
		event delegateDisconnected OnDisconnected;

		/// <summary>
		/// Register a method to run if any packets recieved contain the specified code.
		/// </summary>
		/// <param name="code">The code on the packet that will initiate the method.</param>
		/// <param name="method">The method that will be run.</param>
		void RegisterEvent(int code, delegateNetworkEvent method);

		/// <summary>
		/// Unregister a method.
		/// </summary>
		/// <param name="code">The code the method was registered to.</param>
		/// <param name="method">The method to remove.</param>
		void UnregisterEvent(int code, delegateNetworkEvent method);

		/// <summary>
		/// Register a packet parser to parse all packets with specific packet code.
		/// </summary>
		/// <param name="code">The packet code of packets that will be parsed.</param>
		/// <param name="parser">The specified parser that will parse the packet specifically.</param>
		void RegisterPacketParser(int code, IParser parser);

		/// <summary>
		/// Remove any packet parser that was registered for the specified packet code.
		/// </summary>
		/// <param name="code">The parser for this packet code will be removed.</param>
		void RemovePacketParser(int code);

		/// <summary>
		/// Send an object over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		void SendEvent(int code, object data, params object[] excludeList);

		/// <summary>
		/// Send an object over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="sendItself">Also send the object to itself.</param>
		/// <param name="excludeList">A list of clients who will not receive the packet.</param>
		void SendEvent(int code, object data, bool sendItself, params object[] excludeList);

		/// <summary>
		/// Send a network packet over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The packet to send over the network.</param>
		/// <param name="sendItself">Also send the object to itself.</param>
		/// <param name="excludeList">A list of clients who will not receive the packet.</param>
		void SendEvent(NetworkPacket packet, bool sendItself, params object[] excludeList);

		/// <summary>
		/// Send an object over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="sendItself">Also send the object to itself.</param>
		/// <param name="excludeList">A list of clients who will not receive the packet.</param>
		void SendEvent(int code, object data, bool sendItself, object[] excludeList, params object[] arguments);

		/// <summary>
		/// Send an object over the network to a specific client with specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="target">The target client to recieve this event.</param>
		void SendSingleEvent(int code, object data, object target);

		/// <summary>
		/// Send a raw message over the network to a specific client with specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The content of the package itself. This will not be automatically parsed.</param>
		/// <param name="target">The target client to recieve this event.</param>
		void SendSingleRawEvent(int code, string message, object target);

		/// <summary>
		/// Close currently opened connection if such connection is open and release all associated resources.
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Get or set the Dispatcher used to run all registered events.
		/// </summary>
		Dispatcher Dispatcher
		{
			get;
			set;
		}

		/// <summary>
		/// Get the current active connection of a connection has been established.
		/// </summary>
		INetworkConnection NetworkConnection
		{
			get;
		}

		/// <summary>
		/// Get or set the connection manager that should be used once a connection is established.
		/// </summary>
		INetworkConnectionManager SelectedConnection
		{
			get;
			set;
		}

		/// <summary>
		/// Get the network data handler. This handles all registered types and registered objects.
		/// </summary>
		INetworkDataHandler NetworkDataHandler
		{
			get;
		}

		/// <summary>
		/// Get the default header that is passed for all packets.
		/// </summary>
		HeaderCollection Header
		{
			get;
		}

		/// <summary>
		/// Get the type of the connection, whether the connection is a host or a client.
		/// </summary>
		NetworkType NetworkType
		{
			get;
		}

		/// <summary>
		/// Get or set whether the Network Library should ignore the dispatcher and run all events directly.
		/// </summary>
		bool IgnoreDispatcher
		{
			get;
			set;
		}

		/// <summary>
		/// Get whether the network has been disposed off or not.
		/// </summary>
		bool IsDisposed
		{
			get;
		}
	}
}
