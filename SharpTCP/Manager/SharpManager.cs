using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
//using NetworkPluginManager.Core;
using NetworkLibrary.Connection;
using SharpTCP.Core;

namespace SharpTCP.Manager
{
	public class SharpManager : INetworkConnectionManager
	{
		private bool _disposed;
		private string _name;
		private string _description;
		private string _pathLocation;

		public SharpManager()
		{
			_disposed = false;
			_name = "C# TCP/IP Connection";
			_description = "A plugin that initiates a TCP/IP socket pogrammed in C#.";
		}

		/// <summary>
		/// The desctructor for Connection
		/// </summary>
		~SharpManager()
		{
			//Check whether this object hasn't already been disposed
			if (_disposed)
				return;

			//Call Dispose
			Dispose();
		}

		/// <summary>
		/// Dispose this object and all data within.
		/// </summary>
		public void Dispose()
		{
			//Nothing to do here
		}

		public string Name
		{
			get { return _name; }
		}

		public string Description
		{
			get { return _description; }
		}

		public string PathLocation
		{
			get { return _pathLocation; }
			set { _pathLocation = value; }
		}

		public bool HasSettingsDialog
		{
			get { return false; }
		}

		public void ShowSettingsDialog()
		{
		}

		public INetworkConnection CreateConnection(NetworkLibrary.Core.NetworkType connectionType)
		{
			switch (connectionType)
			{
				case NetworkLibrary.Core.NetworkType.Client:
					return new SharpClient();

				case NetworkLibrary.Core.NetworkType.Host:
					return new SharpHost();
			}
			return null;
		}
	}
}
