using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NetworkLibrary.Connection;
using NetworkLibrary.Utilities;
//using NetworkPluginManager.Core;
using StructureMap;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Represent the connection as a host in the NetworkLibrary and includes methods to host a game.
	/// </summary>
	public class ConnectionHost : Connection
	{
		private List<object> _clientConnections;
		private int _limit;
		private int _port;

		/// <summary>
		/// Initialise a new instance of ConnectionHost with no limit to the number of clients that can connect.
		/// </summary>
		/// <param name="port">The port to host the connection on.</param>
		public ConnectionHost(int port)
			: base()
		{
			_limit = -1;
			_port = port;
			_connectionType = NetworkType.Host;
			_clientConnections = new List<object>();
			this.RegisterEvent((int)CorePacketCode.NewClientConnected, ClientConnected);
			//this.RegisterEvent((int)CorePacketCode.PropertyUpdated, PropertyUpdated);
		}

		/// <summary>
		/// Dispose this object and if specified, all managed resources.
		/// </summary>
		/// <param name="disposeManagedResources">Specify whether managed resources need to be disposed or not</param>
		protected override void Dispose(bool disposeManagedResources)
		{
			base.Dispose(disposeManagedResources);

			if (disposeManagedResources)
			{
				for (int i = 0; i < _clientConnections.Count; i++)
				{
					(_networkConnection as INetworkConnectionHost).Disconnect(_clientConnections[i]);
				}
				_clientConnections.Clear();
				_clientConnections = null;
			}
		}

		void ConnectionHost_OnDisconnected(object source, object reason)
		{
			(_networkConnection as INetworkConnectionHost).Disconnect(source);
		}

		/// <summary>
		/// Initialise a new instance of ConnectionHost with a limit on how many clients can simultaneously connect.
		/// </summary>
		/// <param name="port">The port to host the connection on.</param>
		/// <param name="limitConnectionAmount">The limit on the number of clients allowed to connect.</param>
		public ConnectionHost(int port, int limitConnectionAmount)
			: this(port)
		{
			_limit = limitConnectionAmount;
		}

		/// <summary>
		/// Start the host connection and start listening for incoming connections.
		/// </summary>
		public void StartBroadcasting()
		{
			StartBroadcasting(null);
		}

		/// <summary>
		/// Start the host connection and start listening for incoming connections.
		/// </summary>
		/// <param name="worker">An instance of a BackgrounWorker. Used to report progress in a form of messages as to the progress of the connection initialisation.</param>
		public void StartBroadcasting(Action<int, string> callback)
		{
			_networkConnection = base._connectionManager.CreateConnection(NetworkType.Host);
			(_networkConnection as INetworkConnectionHost).StartBroadcasting(callback, _port);
			ConnectionEstablished();
		}

		/// <summary>
		/// Disconnect/kick a specific client
		/// </summary>
		/// <param name="connection"></param>
		public void Disconnect(object connection)
		{
			(_networkConnection as INetworkConnectionHost).Disconnect(connection);
		}

		/// <summary>
		/// Close the host connection, stop the listener and close all connected connections.
		/// </summary>
		public override void Disconnect()
		{
			(_networkConnection as INetworkConnectionHost).StopBroadcasting();
		}

		/// <summary>
		/// Start the host connection and start listening for incoming connections asynchronised.
		/// </summary>
		/// <returns>The instance of the Background worker currently trying to create a listener.</returns>
		public BackgroundWorker StartBroadcastingAsync()
		{
			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			worker.RunWorkerAsync();
			return worker;
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				StartBroadcasting((x, y) => { (sender as BackgroundWorker).ReportProgress(x, y); });
			}
			catch (Exception error)
			{
				e.Result = error;
				return;
			}
			e.Result = true;
		}

		/// <summary>
		/// Send a packet over the network.
		/// </summary>
		/// <param name="packet">The source packet to transmit over.</param>
		/// <param name="excludeList">A list of all the clients who will not recieve the packet.</param>
		protected override void SendPacket(NetworkPacket packet, params object[] excludeList)
		{
			for (int i = 0; i < _clientConnections.Count; i++)
			{
				bool skip = false;
				for (int exclude = 0; exclude < excludeList.Length; exclude++)
					if (_clientConnections[i] == excludeList[exclude])
					{
						skip = true;
						break;
					}
				if (!skip)
					_networkConnection.SendPacket(packet, _clientConnections[i]);
			}
		}

        /// <summary>
        /// Send a packet over the network to a single client. Used only by the host.
        /// </summary>
        /// <param name="packet">The source packet to transmit over.</param>
        /// <param name="excludeList">A list of all the clients who will not recieve the packet. Only used for the host.</param>
        protected override void SendSinglePacket(NetworkPacket packet, object target)
        {
            _networkConnection.SendPacket(new NetworkPacket(packet.Id, packet.Message, null), target);
        }

		/// <summary>
		/// Event callback method to handle when a new client has been connected.
		/// </summary>
		/// <param name="packet">The source network packet.</param>
		/// <param name="args">The NetworkEventArgs containing all the data of the packet.</param>
		private void ClientConnected(NetworkPacket packet, NetworkEventArgs args)
		{
			//Check to see if the version of the network library matches.
			if (args.BasePacket.Header.GetValue<int>("ver") != this._header.GetValue<int>("ver"))
			{
				//The client is using a different version. We therefore disconnect him.
				Disconnect(args.SourceConnection);
				//Set handled to true so other events don't get called.
				args.Handled = true;
			}
			else
				_clientConnections.Add(args.SourceConnection);
		}

		/// <summary>
		/// Method that is called when a client got disconnected. If such were to happen
		/// then this takes care of removing it from our client list.
		/// </summary>
		/// <param name="source"></param>
		protected override void Disconnected(object source)
		{
			_clientConnections.Remove(source);
		}

		private void PropertyUpdated(NetworkPacket packet, NetworkEventArgs args)
		{
			SendPacket(packet, packet.Source);
		}
	}
}
