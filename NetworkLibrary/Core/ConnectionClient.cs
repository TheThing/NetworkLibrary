using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
//using NetworkPluginManager.Core;
using StructureMap;
using NetworkLibrary.Connection;

namespace NetworkLibrary.Core
{
	struct ClientConnectionInfo
	{
		public ClientConnectionInfo(string ip, int port)
		{
			Ip = ip;
			Port = port;
		}
		public string Ip;
		public int Port;
	}

	/// <summary>
	/// Represent the connection as a client in the NetworkLibrary and includes methods to connect or join another host.
	/// </summary>
	public class ConnectionClient : Connection
	{
		/// <summary>
		/// Initialise a new instance of ConnectionClient. Overrides the connection type to the value of client.
		/// </summary>
		public ConnectionClient()
			: base()
		{
			_connectionType = NetworkType.Client;
		}

		/// <summary>
		/// Try to connect or join another host.
		/// </summary>
		/// <param name="ip">The Ip of the host.</param>
		/// <param name="port">The port for the connection.</param>
		public void Connect(string ip, int port)
		{
			Connect(null, ip, port);
		}

		/// <summary>
		/// Try to connect or join another host.
		/// </summary>
		/// <param name="worker">An instance of a BackgrounWorker. Used to report progress in a form of messages as to the progress of the connection initialisation.</param>
		/// <param name="ip">The Ip of the host.</param>
		/// <param name="port">The port for the connection.</param>
		public void Connect(Action<int, string> callback, string ip, int port)
		{
			_networkConnection = base._connectionManager.CreateConnection(NetworkType.Client);
			(_networkConnection as INetworkConnectionClient).Connect(callback, ip, port);
			ConnectionEstablished();
			SendEvent(new NetworkPacket((int)CorePacketCode.NewClientConnected, "", null), false);
		}

		/// <summary>
		/// Try to connect or join another host asynchronised. Initiates a new instance of Background worker that does the work.
		/// </summary>
		/// <param name="ip">The Ip of the host.</param>
		/// <param name="port">The port for the connection.</param>
		/// <returns>The instance of the Background worker currently trying to connect to the host.</returns>
		public BackgroundWorker ConnectAsync(string ip, int port)
		{
			BackgroundWorker worker = new BackgroundWorker();
			worker.DoWork += new DoWorkEventHandler(worker_DoWork);
			worker.WorkerReportsProgress = true;
			worker.RunWorkerAsync(new ClientConnectionInfo(ip, port));
			return worker;
		}

		/// <summary>
		/// Send a packet over the network.
		/// </summary>
		/// <param name="packet">The source packet to transmit over.</param>
		/// <param name="excludeList">A list of all the clients who will not recieve the packet. Only used for the host.</param>
		protected override void SendPacket(NetworkPacket packet, params object[] excludeList)
		{
			_networkConnection.SendPacket(packet, null);
		}

        /// <summary>
        /// Send a packet over the network to a single client. Used only by the host.
        /// </summary>
        /// <param name="packet">The source packet to transmit over.</param>
        /// <param name="excludeList">A list of all the clients who will not recieve the packet. Only used for the host.</param>
        protected override void SendSinglePacket(NetworkPacket packet, object target)
        {
            SendPacket(packet);
        }

		/// <summary>
		/// Close currently opened connection if such connection is open and release all associated resources.
		/// </summary>
		public override void Disconnect()
		{
			//Close and clean resources
			(_networkConnection as INetworkConnectionClient).Disconnect();
		}

		void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				Connect((x, y) => { (sender as BackgroundWorker).ReportProgress(x, y); }, ((ClientConnectionInfo)e.Argument).Ip, ((ClientConnectionInfo)e.Argument).Port);
			}
			catch (Exception error)
			{
				e.Result = error;
				return;
			}
			e.Result = true;
		}

		protected override void Disconnected(object source)
		{
			Disconnect();
		}
	}
}
