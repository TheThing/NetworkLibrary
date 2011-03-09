using System;
using System.Collections.Generic;
using System.Reflection;
using NetworkLibrary.Exceptions;

namespace NetworkLibrary.Core
{
	public interface INetworkDataHandler : IExceptionHandler, IDisposable
	{
		/// <summary>
		/// Register an object to the registered collection and returns whether it was already registered or not. Also checks and listens on the object if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The data to add to the registered collection.</param>
		/// <returns>The </returns>
		/// <exception cref="NetworkLibrary.Exceptions.NetworkDataException" />
		/// <exception cref="NetworkLibrary.Exceptions.NetworkDataWarning" />
		bool Register(INetworkData data);
		/// <summary>
		/// Registers an object and checks all properties on the specified object for other INetworkData objects to register along with it. Also checks and listens on any of the INetworkData object found if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The data to add to the register recursively on the collection.</param>
		/// <exception cref="NetworkLibrary.Exceptions.Warning" />
		void RegisterRecursive(INetworkData data);
		/// <summary>
		/// Register a type of an object to the library. This allows the transmit of that type of packets over the network.
		/// </summary>
		/// <param name="type">The type of an object to add.</param>
		void RegisterType(Type type);
		/// <summary>
		/// Register all types of all available objects into the library from an assembly. Scans the assembly and automatically registers
		/// all types found into the Library.
		/// </summary>
		/// <param name="assembly">The assembly to scan.</param>
		void RegisterTypeFromAssembly(Assembly assembly);
		/// <summary>
		/// Unregister an object from the collection. Also shuts down the listener on the object if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The object to remove from the collection.</param>
		void Unregister(INetworkData data);
		/// <summary>
		/// Unregisters an object and checks all properties on the specified object for other INetworkData objects to unregister along with it. Also checks and shuts down all listeners on events on any of the INetworkData object found if the object is IValuePropertyChanged or INetworkCollection.
		/// </summary>
		/// <param name="data">The data to unregister recursively from the collection.</param>
		/// <exception cref="NetworkLibrary.Exceptions.Warning" />
		void UnregisterRecursive(INetworkData data);
		/// <summary>
		/// A dictionary containing all registered objects in the library.
		/// </summary>
		Dictionary<string, INetworkData> RegisteredObjects { get; }
		/// <summary>
		/// A dictionary containing all registered types in the library.
		/// </summary>
		Dictionary<string, Type> RegisteredTypes { get; }
		/// <summary>
		/// Generate a new random identifier for object whose identifier has not been  manually specified.
		/// Uses the internal Guid class.
		/// </summary>
		/// <returns>A unique identifier that can be used to assigned as a NetworkId on objects.</returns>
		string GetRandomIdentifier();
		/// <summary>
		/// Generate a new identifier for INetworkData object. Uses an incrementing value.
		/// </summary>
		/// <param name="t">The type of the INetworkData to assign a new NetworkId for.</param>
		/// <returns>An incremented unique NetworkId for the object with the type name in front.</returns>
		string GetNewNetworkId(Type t);
		/// <summary>
		/// Generate a new identifier for INetworkData object. Uses an incrementing value.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		string GetNewNetworkId(string typeName);
		/// <summary>
		/// Send a custom request over the network.
		/// </summary>
		/// <param name="request">The request to be sent.</param>
		void RequestSend(RequestNetworkData request);
		/// <summary>
		/// Request an instance of a specific type of an object from host or client and returns it asyncronised.
		/// </summary>
		/// <typeparam name="T">The type that is being requested.</typeparam>
		/// <param name="callback">A callback method that will be executed once the object has been retreaved.</param>
		void RequestInstanceAsyncronised<T>(Action<T> callback);
		/// <summary>
		/// Request an instance of a specific type of an object from host or client and returns it.
		/// </summary>
		/// <typeparam name="T">The type that is being requested.</typeparam>
		/// <returns>Returns an instance of an object of requested type that was registered at the client or host.</returns>
		T RequestInstance<T>();
		/// <summary>
		/// Execute specific code within a safe block where all changes done on objects speified will be ignored
		/// by the Network Library. This will temporary disable all listeners and bindings on the objects that
		/// have been specified.
		/// </summary>
		/// <param name="safeCall">A method where you can modify the objects specified at will with no worries
		/// that the changes will be sent, whether it is by a PropertyChanged event or CollectionChanged event.</param>
		/// <param name="disabledItems">A list of all objects and items to disable while the method is being run.</param>
		void Safe(Action safeCall, params INetworkData[] disabledItems);
		/// <summary>
		/// Disable all registered listeners on an INetworkData specified. Will disable events that have been
		/// registered on the object but will not remove the object from the Library.
		/// </summary>
		/// <param name="data">The object to disable all registered events on.</param>
		void ListenerDisable(INetworkData data);
		/// <summary>
		/// Enable all registered listeners on an INetworkData specified. Will reenable all events that have been
		/// disabled on the object but will not add a new instance of the object into the Library.
		/// </summary>
		/// <param name="data">The object to reenable all registered events on.</param>
		void ListenerEnable(INetworkData data);
	}
}
