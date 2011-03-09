using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
//using NetworkPluginManager.Core;
using NetworkLibrary.Connection;

namespace SharpTCP.Core
{
	public class SharpClient : SharpConnection, INetworkConnectionClient
	{
		public SharpClient()
			: base()
		{
		}

		public void Connect(Action<int, string> callback, string ip, int port)
		{
			IPAddress address;
			if (IPAddress.TryParse(ip, out address))
			{
				if (callback != null)
					callback(33, "Attempting to connect to server " + address + " on port " + port);
				_connection.Connect(new IPEndPoint(address, port));
				if (callback != null)
					callback(66, "Connection established, beginning receaving new packets.");
				_connection.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(OnDataReceive), _connection);
				if (callback != null)
					callback(100, "Connection established succesfully.");
			}
			else
				throw new FormatException("IP address was in an incorrect format.");
		}

		/// <summary>
		/// Close currently opened connection if such connection is open and release all associated resources.
		/// </summary>
		public void Disconnect()
		{
			//Close and allow a reuse of the socket.
			_connection.Close();
		}
	}
}
