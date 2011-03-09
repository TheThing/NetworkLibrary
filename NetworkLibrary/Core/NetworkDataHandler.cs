using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using NetworkLibrary.Exceptions;
using NetworkLibrary.Utilities;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Handles all NetworkData events, callbacks and such.
	/// </summary>
	public partial class NetworkDataHandler : IExceptionHandler, INetworkDataHandler
	{
		protected Dictionary<string, INetworkData> _registeredObjects;
		protected Dictionary<string, Type> _registeredTypes;
		protected Dictionary<string, RequestNetworkData> _requestedData;
		protected INetwork _network;
		protected bool _disposed;
		protected ulong _nextId;
		public event delegateException OnExceptionOccured;
		public event delegateWarning OnWarningOccured;
		public event delegateNotification OnNotificationOccured;

		/// <summary>
		/// Initialise a new instance of NetworkDataHandler.
		/// </summary>
		/// <param name="network">Reference to an instance of INetwork.</param>
		public NetworkDataHandler(INetwork network)
		{
			//Reset our network id generator
			_nextId = 1;

			//Contains whether this object has been disposed or not.
			_disposed = false;

			//_registeredObjects contains all objects into the NetworkLibrary.
			_registeredObjects = new Dictionary<string, INetworkData>();

			//Contains all valid types. This is used by the seraliser. Since NetworkLibrary
			//doesn't have automatic access to the objects in the program, they have to be pre-registerd.
			_registeredTypes = new Dictionary<string, Type>();

			//Contains all requests that have been sent and are waiting reply, a local copy of the request.
			//This Dictionary is used rarely and should usually be empty.
			_requestedData = new Dictionary<string, RequestNetworkData>();

			//Register default types directly into the Dictionary.
			RegisterType(typeof(RequestNetworkData));
			RegisterType(typeof(RequestNetworkDataType));

			_network = network;

			//Listen 
			_network.OnDisconnected += OnNetworkDisconnected;

			//Register default events into the connection that the NetworkDataHandler handles by defaults.
			_network.RegisterEvent((int)CorePacketCode.PropertyUpdated, ConnectionPropertyChanged);
			_network.RegisterEvent((int)CorePacketCode.CollectionChanged, ConnectionCollectionChanged);
			_network.RegisterEvent((int)CorePacketCode.NetworkDataRequest, ReceivedNetworkRequest);
			_network.RegisterEvent((int)CorePacketCode.NetworkDataRequestResponse, ReceivedNetworkRequestResponse);
		}

		/// <summary>
		/// The desctructor for NetworkDataHandler
		/// </summary>
		~NetworkDataHandler()
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
			if (!_disposed)
			{
				//Call dispose and dispose all managed resources
				Dispose(true);

				//Suppress the GarbageCollector from calling the destroctur
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Dispose this object and if specified, all managed resources.
		/// </summary>
		/// <param name="disposeManagedResources">Specify whether managed resources need to be disposed or not</param>
		protected void Dispose(bool disposeManagedResources)
		{
			//Check to see if this object hasn't been disposed already.
			if (_disposed)
				return;

			//Make sure dispose is not called again.
			_disposed = true;

			if (disposeManagedResources)
			{
				//Clear and null out all collections and variables
				//This is done to invalidate all methods in this class.
				for (int i = 0; i < _registeredObjects.Count; i++)
					ListenerDisable(_registeredObjects.ElementAt(i).Value);
				_registeredObjects.Clear();
				_registeredObjects = null;
				_registeredTypes.Clear();
				_registeredTypes = null;
				for (int i = 0; i < _requestedData.Count; i++)
					lock (_requestedData.ElementAt(i).Value)
					{
						Monitor.Pulse(_requestedData.ElementAt(i).Value);
					}
				_requestedData.Clear();
				_requestedData = null;
				_network = null;
			}
		}

		/// <summary>
		/// Event callback called when the network gets disconnected.
		/// </summary>
		/// <param name="source">The source connection.</param>
		/// <param name="reason">The reason for the connection.</param>
		void OnNetworkDisconnected(object source, object reason)
		{
			//When a client or host disconnects we send a pulse to all our requests.
			//This is done so the source thread of the request won't stop forever, waiting 
			//for a response from a disconnected party.
			for (int i = 0; i < _requestedData.Count; i++)
			{
				lock (_requestedData.ElementAt(i).Value)
				{
					//Wake up the sleeping thread.
					Monitor.Pulse(_requestedData.ElementAt(i).Value);
				}
			}
		}

		/// <summary>
		/// A dictionary containing all registered objects in the library.
		/// </summary>
		public Dictionary<string, INetworkData> RegisteredObjects
		{
			get { return _registeredObjects; }
		}

		/// <summary>
		/// A dictionary containing all registered types in the library.
		/// </summary>
		public Dictionary<string, Type> RegisteredTypes
		{
			get { return _registeredTypes; }
		}

		/// <summary>
		/// Register a type of an object to the database. This allows the transmit of that type of packets over the network.
		/// </summary>
		/// <param name="type">The type of an object to add.</param>
		public void RegisterType(Type type)
		{
			if (!_registeredTypes.ContainsKey(type.FullName))
				_registeredTypes.Add(type.FullName, type);
		}

		/// <summary>
		/// Register all types of all available objects into the library from an assembly. Scans the assembly and automatically registers
		/// all types found into the Library.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		public void RegisterTypeFromAssembly(Assembly assembly)
		{
			foreach (Type t in assembly.GetTypes())
				RegisterType(t);
		}

		/// <summary>
		/// Unregister an object from the collection. Also shuts down the listener on the object if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The object to remove from the collection.</param>
		public void Unregister(INetworkData data)
		{
			//Disable the listener monitoring the object before we remove it from the library.
			ListenerDisable(data);

			if (_registeredObjects.ContainsKey(data.NetworkId))
				_registeredObjects.Remove(data.NetworkId);
		}

		/// <summary>
		/// Register an object to the registered collection. Also checks and listens on the object if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The data to add to the registered collection.</param>
		/// <exception cref="NetworkLibrary.Exceptions.NetworkDataException" />
		/// <exception cref="NetworkLibrary.Exceptions.NetworkDataWarning" />
		public bool Register(INetworkData data)
		{
			if (!_registeredTypes.ContainsKey(data.GetType().FullName))
				_registeredTypes.Add(data.GetType().FullName, data.GetType());
			if (data.NetworkId == null)
				data.NetworkId = "";
			//Check whether the object has already been registered.
			if (_registeredObjects.ContainsKey(data.NetworkId))
			{
				//The object has been registered. If the value is empty, someone has forcefully registered
				//an object into the library with empty identifier and overrided all checks.
				if (string.IsNullOrEmpty(data.NetworkId))
				{
					ThrowException(this, new NetworkDataException("An attempt was to register an empty name network object to the collection when an empty name object already existed in the collection.", data));
				}
				else if (_registeredObjects[data.NetworkId] != data)
				{
					//The identfier was already registered by another object. We overide the object in the library
					//with this new value and notify the programmer with a Warning since this is not supposed to
					//happen under normal circumstances.

					//Disable event monitoring on the old object.
					ListenerDisable(_registeredObjects[data.NetworkId]);

					_registeredObjects[data.NetworkId] = data;

					//Enable all listener monitoring supported by the object for the new value.
					ListenerEnable(data);

					if (OnWarningOccured != null)
						OnWarningOccured(this, new NetworkDataWarning(string.Format("While registering a new/changed object to the registered objects collection, a previous object of same name was detected in the collection containing different data. The previous object in collection was overridden with the new object. The name of the object was: '{0}'", data.NetworkId), data));
				}
				else
				{
					//If the object identifier already exists in the Library and the values are the same
					//then there is nothing to be done.
					if (OnNotificationOccured != null)
						OnNotificationOccured(data, string.Format("The object '{0}' of type '{1}'already existed in the library and had been registered.", data.NetworkId, data.GetType().FullName));
					//Let it be known that the object was already in the collection
					return true;
				}
			}
			else
			{
				//Check whether the identifier is empty.
				if (string.IsNullOrEmpty(data.NetworkId))
				{
					//The identifier is empty, because of this we have to assign a value to it.
					//Only the host can assign a random value on objects so if the current connection
					//is the host we naturally assign the identifier and continue on.
					if (_network.NetworkType == NetworkType.Host)
						//Assing a new random identifier on the object.
						data.NetworkId = GetNewNetworkId(data.GetType());
					else
					{
						//The connection is a client and has to send a request to the host and request
						//a new random identifier from the host.
						RequestNetworkData request = new RequestNetworkData(RequestNetworkDataType.RequestName);

						//Assign the type name.
						request.ClassTypeName = data.GetType().Name;

						lock (request)
						{
							//Send a request to the host and request a new random identifier.
							RequestSend(request);

							//Lock the thread from continuing until the request has been answered.
							Monitor.Wait(request);
						}
						//Assign the new random identifier we got from the host unto the object
						data.NetworkId = request.NetworkId;
					}
				}
				//Add the object to the library.
				_registeredObjects.Add(data.NetworkId, data);

				//Enable all listener monitoring supported by the object. Whether this is a
				//bindable object or a collection that supports notifying
				ListenerEnable(data);
			}
			//Since this is a new object on the collection, let know that it was not already inside in the collection
			return false;
		}

		/// <summary>
		/// Registers an object and checks all properties on the specified object for other INetworkData objects to register along with it. Also checks and listens on any of the INetworkData object found if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The data to add to the register recursively on the collection.</param>
		/// <exception cref="NetworkLibrary.Exceptions.Warning" />
		public void RegisterRecursive(INetworkData data)
		{
			RecursiveNetworkData(data, true);
		}

		/// <summary>
		/// Unregisters an object and checks all properties on the specified object for other INetworkData objects to unregister along with it. Also checks and shuts down all listeners on events on any of the INetworkData object found if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The data to unregister recursively from the collection.</param>
		/// <exception cref="NetworkLibrary.Exceptions.Warning" />
		public void UnregisterRecursive(INetworkData data)
		{
			RecursiveNetworkData(data, false);
		}

		/// <summary>
		/// Private method to search an object recursively for other objects that can be registered or unregistered
		/// by searching all properties for an INetworkData objects.
		/// </summary>
		/// <param name="data">The object to search for recursively.</param>
		/// <param name="register">Specify whether we should register the object or unregister the object.</param>
		protected void RecursiveNetworkData(INetworkData data, bool register)
		{
			try
			{
				//Register or unregister network data
				//The boolean value in parameter controls whether to register or unregister.
				if (register)
				{
					if (Register(data))
						//Since the object had already been added we skip unnecessary recursive checks
						//This is done so if an object references itself we won't end up with a stack overflow.
						return;
				}
				else
					Unregister(data);
			}
			catch (Exception e)
			{
				//An error occured while registering the network data to the collection.
				//Notify the program about that and skip this object
				if (OnWarningOccured != null)
					OnWarningOccured(data, new Warning(
						string.Format("An error occured while recursively registering network data. This object will not be recursively check for more network data to register. The error returned was: {0} Check the inner exception for more details.",
						e.Message), e));
			}

			//Create a shortcut to the object type
			var t = data.GetType();

			//Loop and check every property in the object
			foreach (var property in t.GetProperties())
			{
				//Check whether the property needs an index. This usually indicates
				//that the property is either an array or a collection of something
				if (property.GetIndexParameters().Length > 0)
				{
					//The property requires an index. Check whether the Length or Count property exist.
					//If the count property exists, it means it's a collection of some sort.
					//If the length property exists, it means it's an array of some sort.
					if (property.GetIndexParameters().Length == 1 && (t.GetProperty("Count") != null || t.GetProperty("Length") != null))
					{
						int count = 0;
						//Retreave the length of the collection/array
						if (t.GetProperty("Count") != null)
							count = (int)t.GetProperty("Count").GetValue(data, null);
						else
							count = (int)t.GetProperty("Length").GetValue(data, null);

						//Loop through the collection/array
						for (int indexParamenter = 0; indexParamenter < count; indexParamenter++)
							//Check whether the content of that index in the property collection/array is a network data.
							if (property.GetValue(data, new object[] { indexParamenter }) is INetworkData)
								//The content was a network data object. Register that object recursively too.
								RecursiveNetworkData(property.GetValue(data, new object[] { indexParamenter }) as INetworkData, register);
					}
					else
					{
						//The property was of a special case and could not be recursively checked.
						//In that case the property will be skipped and the program will be notified
						//about this warning.
						string message = string.Format("A property of name '{0}' was skipped while recursively registering object of type '{1}'. The reason: ", property.Name, t.FullName);
						if (property.GetIndexParameters().Length > 1)
							message += "The property required more than one index parameter which is currently unsupported.";
						else
							message += "The property required an index parameter and neither Count or Length properties were found on the object.";
						if (OnWarningOccured != null)
							OnWarningOccured(this, new Warning(message));
					}
				}
				//The parameter does not require a parameter. Check if the value of the property
				//Is a network data or something else.
				else
				{
					//Retrieve the object inside the property
					object propValue = property.GetValue(data, null);

					if (propValue is INetworkData)
						//The property contains a network data object. Register that object recursively too.
						RecursiveNetworkData(propValue as INetworkData, register);
					else if (propValue is IList)
						//The property contained a collection or array of some sort. Check each object inside it too
						for (int i = 0; i < (propValue as IList).Count; i++)
							if ((propValue as IList)[i] is INetworkData)
								//An object inside the collection/array was a network data object. Register that object recursively too.
								RecursiveNetworkData((propValue as IList)[i] as INetworkData, register);
				}
			}
		}

		/// <summary>
		/// Private method to automatically handle an exception by either bubbling it up the program or
		/// if that is not possible, throw it directly and stop the execution.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="exception"></param>
		void ThrowException(object source, Exception exception)
		{
			if (OnExceptionOccured != null)
				OnExceptionOccured(source, exception);
			else
				throw exception;
		}

		/// <summary>
		/// Generate a new random identifier for object whose identifier has not been  manually specified.
		/// Uses the internal Guid class.
		/// </summary>
		/// <returns>A unique identifier that can be used to assigned as a NetworkId on objects.</returns>
		public string GetRandomIdentifier()
		{
			return Guid.NewGuid().ToString();
		}

		/// <summary>
		/// Generate a new identifier for INetworkData object. Uses an incrementing value.
		/// </summary>
		/// <param name="t">The type of the INetworkData to assign a new NetworkId for.</param>
		/// <returns>An incremented unique NetworkId for the object with the type name in front.</returns>
		public string GetNewNetworkId(Type t)
		{
			return GetNewNetworkId(t.Name);
		}

		/// <summary>
		/// Generate a new identifier for INetworkData object. Uses an incrementing value.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public string GetNewNetworkId(string typeName)
		{
			return string.Format("{0}_{1}", typeName, _nextId++);
		}

		/// <summary>
		/// Send a custom request over the network.
		/// </summary>
		/// <param name="request">The request to be sent.</param>
		public void RequestSend(RequestNetworkData request)
		{
			request.InternalName = GetRandomIdentifier();
			_requestedData.Add(request.InternalName, request);
			_network.SendEvent((int)CorePacketCode.NetworkDataRequest, request);
		}

		/// <summary>
		/// Request an instance of a specific type of an object from host or client and returns it asyncronised.
		/// </summary>
		/// <typeparam name="T">The type that is being requested.</typeparam>
		/// <param name="callback">A callback method that will be executed once the object has been retreaved.</param>
		public void RequestInstanceAsyncronised<T>(Action<T> callback)
		{
			BackgroundWorker work = new BackgroundWorker();
			work.DoWork += (a, b) => 
			{
				callback(RequestInstance<T>());
			};
			work.RunWorkerAsync();
		}

		/// <summary>
		/// Request an instance of a specific type of an object from host or client and returns it.
		/// </summary>
		/// <typeparam name="T">The type that is being requested.</typeparam>
		/// <returns>Returns an instance of an object of requested type that was registered at the client or host.</returns>
		public T RequestInstance<T>()
		{
			//Create a new request.
			RequestNetworkData request = new RequestNetworkData(RequestNetworkDataType.RequestType);

			//Add the full name of the type we are requesting.
			request.ClassTypeName = typeof(T).FullName;

			//Send our request.
			RequestSend(request);

			lock (request)
			{
				//Lock the current thread until a response is delivered.
				Monitor.Wait(request);
			}

			//Make sure the response actually contains some data.
			if (request.Data != null)
			{
				//Make sure it's of the correct type.
				if (request.Data is T)
				{
					//return the object we requested from the host/client.
					return (T)request.Data;
				}
				else if (OnWarningOccured != null)
					OnWarningOccured(request, new Warning("A Request for an instance was sent but returned invalid results. The request is truncated"));
			}
			else if (OnWarningOccured != null)
				OnWarningOccured(request, new Warning("A Request for an instance was sent but returned a null value. The request is truncated"));
			
			//Since the request contained invalid results we return the default value for this type.
			return default(T);
		}

		/// <summary>
		/// Execute specific code within a safe block where all changes done on objects specified will be ignored
		/// by the Network Library. This will temporary disable all listeners and bindings on the objects that
		/// have been specified.
		/// </summary>
		/// <param name="safeCall">A method where you can modify the objects specified at will with no worries
		/// that the changes will be sent, whether it is by a PropertyChanged event or CollectionChanged event.</param>
		/// <param name="disabledItems">A list of all objects and items to disable while the method is being run.</param>
		public void Safe(Action safeCall, params INetworkData[] disabledItems)
		{
			//Disable the listener for every object specified.
			for (int i = 0; i < disabledItems.Length; i++)
				ListenerDisable(disabledItems[i]);

			safeCall();

			//Reenable all disabled listeners.
			for (int i = 0; i < disabledItems.Length; i++)
				ListenerEnable(disabledItems[i]);
		}

		/// <summary>
		/// Disable all registered listeners on an INetworkData specified. Will disable events that have been
		/// registered on the object but will not remove the object from the Library.
		/// </summary>
		/// <param name="data">The object to disable all registered events on.</param>
		public void ListenerDisable(INetworkData data)
		{
			if (data is INotifyCollectionChanged)
				(data as INotifyCollectionChanged).CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(NetworkData_CollectionChanged);
			else if (data is INotifyPropertyChanged)
				(data as INotifyPropertyChanged).PropertyChanged -= new PropertyChangedEventHandler(NetworkData_PropertyChanged);
		}

		/// <summary>
		/// Enable all registered listeners on an INetworkData specified. Will reenable all events that have been
		/// disabled on the object but will not add a new instance of the object into the Library.
		/// </summary>
		/// <param name="data">The object to reenable all registered events on.</param>
		public void ListenerEnable(INetworkData data)
		{
			if (data is INotifyCollectionChanged)
				(data as INotifyCollectionChanged).CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(NetworkData_CollectionChanged);
			else if (data is INotifyPropertyChanged)
				(data as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(NetworkData_PropertyChanged);
		}
	}
}
