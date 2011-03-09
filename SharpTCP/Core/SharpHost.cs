using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NetworkLibrary.Connection;
using NetworkLibrary.Exceptions;

namespace SharpTCP.Core
{
	public class SharpHost : SharpConnection, INetworkConnectionHost
	{
		public SharpHost()
			: base()
		{
		}

		protected override void Dispose(bool disposeManagedResources)
		{
			base.Dispose(disposeManagedResources);
		}

		/// <summary>
		/// Get whether the connection is up and active.
		/// </summary>
		public override bool Connected
		{
			get { return true; }
		}

		public void StartBroadcasting(Action<int, string> callback, int port)
		{
			IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
			_connection.Bind(ip);
			if (callback != null)
				callback(33, "Starting listening on port " + port);
			_connection.Listen(port);
			if (callback != null)
				callback(66, "Begin accepting new incoming connections");
			_connection.BeginAccept(new AsyncCallback(OnConnection), null);
			if (callback != null)
				callback(100, "Server started succesfully");
		}

		/// <summary>
		/// Stop the currently active listener if such is active.
		/// </summary>
		public void StopBroadcasting()
		{
			//Disconnect our listener but allow a reuse of the socket.
			_connection.Close();
		}

		/// <summary>
		/// Force a connection to disconnect.
		/// </summary>
		/// <param name="connection">The source connection to disconnect.</param>
		public void Disconnect(object connection)
		{
			//Close the connection and also dispose of it.
			(connection as Socket).Close();
		}

		private void OnConnection(IAsyncResult data)
		{
			try
			{
				Socket client = _connection.EndAccept(data);
				client.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(OnDataReceive), client);
				_connection.BeginAccept(new AsyncCallback(OnConnection), null);
			}
			catch (ObjectDisposedException)
			{
				//After a close has been called, beginaccept will receive it's last call
				//This is done to do all necessary cleanup. When this happens, a ObjectDisposedException
				//will be thrown by the EndAccept and we will ignore this since it's none of our business :)
				return;
			}
			catch (Exception e)
			{
				ThrowWarning(new Warning("Error while accepting incoming connection. Message received: " + e.Message, e));
			}
		}
	}
}
