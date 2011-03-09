using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Threading;
using NetworkLibrary.Connection;
using NetworkLibrary.Utilities;
using NetworkLibrary.Utilities.Parser;
using NetworkLibrary.Exceptions;
using System.IO;
using StructureMap;
using StructureMap.Graph;
using StructureMap.Configuration.DSL;

namespace NetworkLibrary.Core
{
	public delegate void delegateNetworkEvent(NetworkPacket packet, NetworkEventArgs args);
	public delegate void delegateNetworkPacket(object source, NetworkPacket packet); 
	public delegate void delegateEmpty();

	/// <summary>
	/// Basic skeleton of a connection in the Network Library. Provides and handles all basic configuration as well as packet handling.
	/// </summary>
	public abstract class Connection : INetwork
	{
		protected bool _disposed;
		protected bool _ignoreDispatcher;
		protected NetworkType _connectionType;
		protected Dispatcher _dispatcher;
		protected INetworkConnection _networkConnection;
		protected INetworkConnectionManager _connectionManager;
		protected HeaderCollection _header;
		protected Dictionary<int, List<delegateNetworkEvent>> _registeredEvents;
		protected Dictionary<int, IParser> _packetParsers;

		/// <summary>
		/// Occurs whenever a connection has been lost. For host this informes of when a client
		/// has been disconnected, for a client this informs when the connection has been lost.
		/// </summary>
		public event delegateDisconnected OnDisconnected;

		public event delegateException OnExceptionOccured;
		public event delegateWarning OnWarningOccured;
		public event delegateNotification OnNotificationOccured;

		public abstract void Disconnect();
		protected abstract void Disconnected(object source);
		protected abstract void SendPacket(NetworkPacket packet, params object[] excludeList);
		protected abstract void SendSinglePacket(NetworkPacket packet, object target);

		/// <summary>
		/// Initialise and configure the StructureMap
		/// </summary>
		public Connection()
		{
			this._disposed = false;
			this._ignoreDispatcher = true;
			this._packetParsers = new Dictionary<int, IParser>();
			this._registeredEvents = new Dictionary<int, List<delegateNetworkEvent>>();
			this._header = new HeaderCollection();
			this._header.Add(new Header("ver", 1));
			NetworkPacket.DefaultHeader = this._header;
			this.RegisterPacketParser((int)CorePacketCode.CollectionChanged, new CollectionChangedParser());
			this.RegisterPacketParser((int)CorePacketCode.PropertyUpdated, new PropertyChangedParser());
			this.RegisterEvent((int)CorePacketCode.AssignNewHeaderValue, AssignValueInHeader);
			
			ObjectFactory.Initialize(x =>
			{
				x.For<INetwork>().Singleton().Use(this);
				x.AddRegistry<NetworkRegistry>();
			});
			IList<INetworkConnectionManager> listManagers = ObjectFactory.GetAllInstances<INetworkConnectionManager>();
			if (listManagers.Count == 0)
				throw new NullReferenceException("No valid connection plugins were found in neither the plugin folder or the plugins.");
			else
				_connectionManager = listManagers[0];
		}

		/// <summary>
		/// The desctructor for Connection
		/// </summary>
		~Connection()
		{
			//Check whether this object hasn't already been disposed
			if (_disposed)
				return;

			//Call Dispose but let it know it doesn't need to dispose managed resources
			Dispose(false);
		}

		/// <summary>
		/// Dispose this object and all data within.
		/// </summary>
		public void Dispose()
		{
			//Check whether this object hasn't already been disposed
			if (_disposed)
				return;

			//Call dispose and dispose all managed resources
			Dispose(true);

			//Suppress the GarbageCollector from calling the destroctur
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose this object and if specified, all managed resources.
		/// </summary>
		/// <param name="disposeManagedResources">Specify whether managed resources need to be disposed or not</param>
		protected virtual void Dispose(bool disposeManagedResources)
		{
			//Check to see if this object hasn't been disposed already.
			if (_disposed)
				return;

			//Make sure dispose is not called again.
			_disposed = true;

			if (disposeManagedResources)
			{
				NetworkDataHandler.Dispose();
				_networkConnection.Dispose();
				_connectionManager.Dispose();

				//Clear and null out all collections and variables
				//This is done to invalidate all methods in this class and break it.
				_registeredEvents.Clear();
				_registeredEvents = null;
				_dispatcher = null;
				_header = null;
			}
		}

		/// <summary>
		/// Event callback to handle a new value assignment for the header.
		/// </summary>
		/// <param name="packet">The source network packet.</param>
		/// <param name="data">NetworkEventArgs containing all relevant data.</param>
		private void AssignValueInHeader(NetworkPacket packet, NetworkEventArgs data)
		{
			if (data.DataType == typeof(Header))
				this._header.Add(data.Data as Header);
		}

		/// <summary>
		/// Register default events in the network connection plugin.
		/// </summary>
		protected void ConnectionEstablished()
		{
			_networkConnection.OnPacketRecieved += ThrowOnPacketRecieved;
			_networkConnection.OnDisconnected += ThrowOnDisconnected;
			MonitorExceptionOnObject(_networkConnection);
		}

		protected void MonitorExceptionOnObject(IExceptionHandler item)
		{
			item.OnExceptionOccured += ThrowOnExceptionOccured;
			item.OnWarningOccured += ThrowOnWarningOccured;
			item.OnNotificationOccured += ThrowOnNotificationOccured;
		}

		/// <summary>
		/// Execute all registerd events for a specific packet code.
		/// </summary>
		/// <param name="packet">The source packet to send to the events.</param>
		private void ExecuteRegisteredEvents(NetworkPacket packet)
		{
			NetworkEventArgs args;
			if (_packetParsers.ContainsKey(packet.Id))
			{
				args = _packetParsers[packet.Id].ParsePacket(packet);
				if (args == null)
					return;
			}
			else
			{
				args = new NetworkEventArgs(packet);
				if (packet.Message != null)
				{
					try
					{
						args.Data = new Serialiser().Deserialise(packet.Message);
					}
					catch (Exception e)
					{
						ThrowOnExceptionOccured(packet, e);
						return;
					}
				}
				else
					args.Data = null;
			}
			ExecuteEventsWithId(args, (int)CorePacketCode.PreviewPacket);
			if (packet.Id == (int)CorePacketCode.PropertyUpdated)
				ExecuteEventsWithId(args, (int)CorePacketCode.PreviewPropertyUpdated);
			if (packet.Id == (int)CorePacketCode.CollectionChanged)
				ExecuteEventsWithId(args, (int)CorePacketCode.PreviewCollectionChanged);
			ExecuteEventsWithId(args, packet.Id);
		}

		/// <summary>
		/// Execute all registered event with specific code id in the dictionary.
		/// </summary>
		/// <param name="eventArgs">The event data containing the packet in a parsed format.</param>
		/// <param name="id">The id of the event to execute.</param>
		/// <exception cref="System.Exception" />
		private void ExecuteEventsWithId(NetworkEventArgs eventArgs, int id)
		{
			//Check to see if we have any registered events for that event id.
			if (_registeredEvents.ContainsKey(id))
				//Run all registered events in order.
				for (int i = 0; i < _registeredEvents[id].Count; i++)
				{
					//Check to see if we have a dispatcher that we can use to run the event
					//in a threadsafe enviroment
					if (_dispatcher != null)
						_dispatcher.Invoke((delegateEmpty)delegate
						{
							ExecuteEvent(_registeredEvents[id][i], eventArgs);
						});
					//Since we don't have a dispacher we check to see if we are allowed to ignoer it.
					else if (_ignoreDispatcher)
						//We are allowed to ignore the dispatcher. This will run the event directly in
						//an unsafe multithread form. The programmer will have to take care of all the locks
						//and such.
						ExecuteEvent(_registeredEvents[id][i], eventArgs);
					else
						//Throw an exception on the fact that dispatcher had not been properly
						//assigned.
						ThrowOnExceptionOccured(this, new Exception("Dispatcher had not been specified and ignoring it was disabled"));
					if (_disposed || eventArgs.Handled)
						return;
				}
		}

		/// <summary>
		/// Run a registered event with the relevant arguments.
		/// </summary>
		/// <param name="method">The method to be run.</param>
		/// <param name="eventArgs">Arguments to pass the the method.</param>
		private void ExecuteEvent(delegateNetworkEvent method, NetworkEventArgs eventArgs)
		{
			try
			{
				method(eventArgs.BasePacket, eventArgs);
			}
			catch (Exception e)
			{
				ThrowOnExceptionOccured(this, e);
			}
		}

		void ThrowOnExceptionOccured(object source, Exception exception)
		{
			if (OnExceptionOccured != null)
				OnExceptionOccured(source, exception);
			else
				throw new Exception(exception.Message, exception);
		}

		void ThrowOnNotificationOccured(object source, string message)
		{
			if (OnNotificationOccured != null)
				OnNotificationOccured(source, message);
		}

		void ThrowOnWarningOccured(object source, Warning warning)
		{
			if (OnWarningOccured != null)
				OnWarningOccured(source, warning);
		}

		void ThrowOnDisconnected(object source, object reason)
		{
			Disconnected(source);
			if (OnDisconnected != null)
				OnDisconnected(source, reason);
		}

		void ThrowOnPacketRecieved(object source, NetworkPacket packet)
		{
			ExecuteRegisteredEvents(packet);
		}

		/// <summary>
		/// Unregister a method.
		/// </summary>
		/// <param name="code">The code the method was registered to.</param>
		/// <param name="method">The method to remove.</param>
		public void UnregisterEvent(int code, delegateNetworkEvent method)
		{
			if (_registeredEvents.ContainsKey(code))
			{
				if (_registeredEvents[code].Contains(method))
					_registeredEvents[code].Remove(method);
			}
		}

		/// <summary>
		/// Register a method to run if any packets recieved contain the specified code.
		/// </summary>
		/// <param name="code">The code on the packet that will initiate the method.</param>
		/// <param name="method">The method that will be run.</param>
		public void RegisterEvent(int code, delegateNetworkEvent method)
		{
			if (!_registeredEvents.ContainsKey(code))
				_registeredEvents.Add(code, new List<delegateNetworkEvent>());

			_registeredEvents[code].Add(method);
		}

		/// <summary>
		/// Register a packet parser to parse all packets with specific packet code.
		/// </summary>
		/// <param name="code">The packet code of packets that will be parsed.</param>
		/// <param name="parser">The specified parser that will parse the packet specifically.</param>
		public void RegisterPacketParser(int code, IParser parser)
		{
			if (_packetParsers.ContainsKey(code))
			{
				//Only one parser can be assigned for each packet code. Here we overrided a previous Parser
				//and therefore inform about it as a Warning.
				if (OnWarningOccured != null)
					OnWarningOccured(_packetParsers[code], new Warning(string.Format("When registering an IParser of type {0} for packet code {1} a previous IParser was overriden of type {2}.",
						parser.GetType().FullName, code, _packetParsers[code].GetType().FullName)));

				//Override the previous parser.
				_packetParsers[code] = parser;
			}
			else
				_packetParsers.Add(code, parser);
			MonitorExceptionOnObject(parser);
		}

		/// <summary>
		/// Remove any packet parser that was registered for the specified packet code.
		/// </summary>
		/// <param name="code">The parser for this packet code will be removed.</param>
		public void RemovePacketParser(int code)
		{
			if (_packetParsers.ContainsKey(code))
				_packetParsers.Remove(code);
		}

		/// <summary>
		/// Send an object over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="excludeList">A list of clients who will not receive the packet.</param>
		public void SendEvent(int code, object data, params object[] excludeList)
		{
			SendEvent(code, data, false, excludeList);
		}

		/// <summary>
		/// Send an object over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="sendItself">Also send the object to itself.</param>
		/// <param name="excludeList">A list of clients who will not receive the packet.</param>
		public void SendEvent(int code, object data, bool sendItself, params object[] excludeList)
		{
			SendEvent(code, data, sendItself, excludeList, null);
		}

		/// <summary>
		/// Send an object over the network with a specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="sendItself">Also send the object to itself.</param>
		/// <param name="excludeList">A list of clients who will not receive the packet.</param>
		public void SendEvent(int code, object data, bool sendItself, object[] excludeList, params object[] arguments)
		{
			string message;

			//Check to see if we have a custom parser to handle this type of data.
			if (_packetParsers.ContainsKey(code))
				message = _packetParsers[code].ParseObject(data, arguments);
			else
				//No special parser so we serialise the object like we normally do.
				message = new Serialiser().Serialise(data);

			//In some rare cases the packet parser can fail and in those cases it
			//will return a null value. Here we check if the parser was successfull.
			if (message != null)
				SendEvent(new NetworkPacket(code, message, null), sendItself, excludeList);
		}

		/// <summary>
		/// Send a specific network packet specified over the network.
		/// </summary>
		/// <param name="code">The packet to send over the network.</param>
		/// <param name="sendItself">Also send the object to itself.</param>
		public void SendEvent(NetworkPacket packet, bool sendItself, params object[] excludeList)
		{
			SendPacket(packet, excludeList);
			if (sendItself)
				ExecuteRegisteredEvents(packet);
		}

		/// <summary>
		/// Send an object over the network to a specific client with specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="target">The target client to recieve this event.</param>
		public void SendSingleEvent(int code, object data, object target)
		{
			SendSinglePacket(new NetworkPacket(code, new Serialiser().Serialise(data), null), target);
		}

		/// <summary>
		/// Send raw data over the network to a specific client with specific packet code.
		/// </summary>
		/// <param name="code">The code for the packet.</param>
		/// <param name="data">The object itself to transmit over the network.</param>
		/// <param name="target">The target client to recieve this event.</param>
		public void SendSingleRawEvent(int code, string message, object target)
		{
			SendSinglePacket(new NetworkPacket(code, message, null), target);
		}

		/// <summary>
		/// Get or set the Dispatcher used for runinng all the events.
		/// </summary>
		public Dispatcher Dispatcher
		{
			get { return _dispatcher; }
			set { _dispatcher = value; }
		}

		/// <summary>
		/// Get the type of the connection, whether the connection is a host or a client.
		/// </summary>
		public NetworkType NetworkType
		{
			get { return (NetworkType)_connectionType; }
		}

		/// <summary>
		/// Get the default header that is passed for all packets.
		/// </summary>
		public HeaderCollection Header
		{
			get { return _header; }
		}

		/// <summary>
		/// Get the network data handler. This handles all registered types and registered objects.
		/// </summary>
		public INetworkDataHandler NetworkDataHandler
		{
			get { return ObjectFactory.GetInstance<INetworkDataHandler>(); }
		}

		/// <summary>
		/// Get the current active connection of a connection has been established.
		/// </summary>
		public INetworkConnection NetworkConnection
		{
			get { return _networkConnection; }
		}

		/// <summary>
		/// Get or set the connection manager that should be used once a connection is established.
		/// </summary>
		public INetworkConnectionManager SelectedConnection
		{
			get { return _connectionManager; }
			set { _connectionManager = value; }
		}

		/// <summary>
		/// Get or set whether the Network Library should ignore the dispatcher and run all events directly.
		/// </summary>
		public bool IgnoreDispatcher
		{
			get { return _ignoreDispatcher; }
			set { _ignoreDispatcher = value; }
		}

		/// <summary>
		/// Get whether the network has been disposed off or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _disposed; }
		}
	}

}
