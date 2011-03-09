using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		/// <summary>
		/// Event called when any client or host connection has a change in a network data collection.
		/// </summary>
		/// <param name="packet">The base packet containing the raw data.</param>
		/// <param name="data">NetworkEvent data containing the contents of the NetworkPacket in a parsed format.</param>
		private void ConnectionCollectionChanged(NetworkPacket packet, NetworkEventArgs data)
		{
			try
			{
				//Make sure the data is of the correct type.
				if (data.DataType == typeof(NotifyCollectionChangedEventArgs))
				{
					//Grab the collection changed information from the data.
					NotifyCollectionChangedEventArgs args = data.Data as NotifyCollectionChangedEventArgs;

					//Check if we have the source collection whose items was changed.
					if (_registeredObjects.ContainsKey(data.NetworkDataId))
					{
						//Get the source collection we want to manipulate
						IList collection = _registeredObjects[data.NetworkDataId] as IList;

						//Disable the listener for this object while we do our changes.
						ListenerDisable((INetworkData)collection);

						//apply the changes that occured at the client/host to our collection.
						switch (args.Action)
						{
							case NotifyCollectionChangedAction.Add:
								for (int i = 0; i < args.NewItems.Count; i++)
									collection.Insert(args.NewStartingIndex + i, args.NewItems[i]);
								break;
							case NotifyCollectionChangedAction.Move:
								for (int i = 0; i < args.NewItems.Count; i++)
								{
									object temp = collection[args.OldStartingIndex + (args.OldStartingIndex > args.NewStartingIndex ? i : 0)];
									collection.Remove(temp);
									collection.Insert(args.NewStartingIndex + i, temp);
								}
								break;
							case NotifyCollectionChangedAction.Remove:
								for (int i = 0; i < args.OldItems.Count; i++)
									collection.RemoveAt(args.OldStartingIndex + i);
								break;
							case NotifyCollectionChangedAction.Replace:
								for (int i = 0; i < args.NewItems.Count; i++)
									collection[args.OldStartingIndex + i] = args.NewItems[i];
								break;
							case NotifyCollectionChangedAction.Reset:
								collection.Clear();
								break;
						}
						//Enable the listener for our collection.
						ListenerEnable((INetworkData)collection);

						//Forward if necessary.
						data.Forward();
					}
					else if (OnNotificationOccured != null)
						OnNotificationOccured(data, string.Format("A CollectionChanged packet was received for a collection on a network id but the network id object was not found in registered objects."));
				}
				else if (OnNotificationOccured != null)
					OnWarningOccured(data, new Warning(string.Format("A network packet for collection changed was received but that data was of an invalid type. Expected a type of '{0}' but received '{1}'.", typeof(NotifyCollectionChangedEventArgs).FullName, data.DataType.FullName)));
			}
			catch (Exception e)
			{
				ThrowException(data, e);
			}
		}

		/// <summary>
		/// Event called when any client or host connection has had any of its registered network data changed property.
		/// </summary>
		/// <param name="packet">The base packet containing the raw data.</param>
		/// <param name="data">NetworkEvent data containing the contents of the NetworkPacket in a parsed format.</param>
		/// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
		/// <exception cref="System.Exception" />
		private void ConnectionPropertyChanged(NetworkPacket packet, NetworkEventArgs data)
		{
			try
			{
				//Check to see if the property changed packet we recieved was found in the registered objects collection.
				if (_registeredObjects.ContainsKey(data.NetworkDataId) && data.PropertyName != "Count")
				{
					try
					{
						//Disable listening on the object while we pass in the value.
						//This is done so that it doesn't end in an infinite loop between the host and the client.
						this.ListenerDisable(_registeredObjects[data.NetworkDataId]);
						//Set the value of the property on the object
						_registeredObjects[data.NetworkDataId].GetType().GetProperty(data.PropertyName).SetValue(_registeredObjects[data.NetworkDataId], data.Data, null);
						this.ListenerEnable(_registeredObjects[data.NetworkDataId]);
					}
					catch (Exception e)
					{
						throw new ParsingException(string.Format("Error while passing the value '{0}' of type '{1}' to the following property '{2}' on an object of type '{3}'. Error message received: {4}", data.Data, data.DataType.FullName, data.PropertyName, _registeredObjects[data.NetworkDataId].GetType().FullName, e.Message));
					}
				}
			}
			catch (Exception e)
			{
				ThrowException(this, e);
			}
		}

		/// <summary>
		/// Event that is called automatically when a NetworkRequest was recieved.
		/// </summary>
		/// <param name="packet">The base packet containing the raw data.</param>
		/// <param name="data">NetworkEvent data containing the contents of the NetworkPacket in a parsed format.</param>
		private void ReceivedNetworkRequest(NetworkPacket packet, NetworkEventArgs data)
		{
			//Prelimenary checking on whether the data is of correct type
			if (data.DataType == typeof(RequestNetworkData))
			{
				// The data passed over the network is a real Request. Processing the request.
				try
				{
					//Create the response for the request and pass in all data of the request over to the
					//the response. This is done so the InternalName as well as other data is passed over.
					RequestNetworkData response = new RequestNetworkData(data.Data as RequestNetworkData);

					//Start processing the request.
					switch (response.RequestType)
					{
						case RequestNetworkDataType.RequestType:
							//The connection is requesting a data of specific type. Search the registered
							//objects for any objects that fit the description.
							for (int i = 0; i < _registeredObjects.Count; i++)
								if (_registeredObjects.ElementAt(i).Value.GetType().FullName == response.ClassTypeName)
								{
									response.Data = _registeredObjects.ElementAt(i).Value;
									break;
								}
							break;
						case RequestNetworkDataType.RequestName:
							//The connection is requesting name for it's object. Pass in a random
							//generated identifier.
							response.NetworkId = GetNewNetworkId(response.ClassTypeName);
							break;
						case RequestNetworkDataType.RequestData:
							//The connection is requesting a data with specific NetworkId.
							//Return the object containing that NetworkId.
							response.Data = this._registeredObjects[response.NetworkId];
							break;
					}
					//Send the response over the network to the client/host.
					_network.SendSingleRawEvent((int)CorePacketCode.NetworkDataRequestResponse, new Serialiser(true).Serialise(response), packet.Source);
				}
				catch (Exception e)
				{
					//An unknown error occured. Since this is not critical part of the NetworkLibrary we
					//pass it on as a Warning.
					if (OnWarningOccured != null)
						OnWarningOccured(this, new Warning("A network request was received but an unknown error occured while processing the request.", e));
				}
			}
			//Received a packet containing the code for a Request but the data was of an invalid type.
			//Since this is not a critical part of the NetworkLibrary we pass it on as a Warning.
			else if (OnWarningOccured != null)
				OnWarningOccured(this, new Warning("A network request was received but was of an invalid format. Request was truncated."));
		}

		/// <summary>
		/// Event that is called automatically when receiving a response to a Network Request.
		/// </summary>
		/// <param name="packet">The base packet containing the raw data.</param>
		/// <param name="data">NetworkEvent data containing the contents of the NetworkPacket in a parsed format.</param>
		private void ReceivedNetworkRequestResponse(NetworkPacket packet, NetworkEventArgs data)
		{
			//Prelimenary checking on whether the data is of correct type
			if (data.DataType == typeof(RequestNetworkData))
			{
				// The data passed over the network is a real Response. Processing the response.
				try
				{
					//Retreave the original request that was the source of the response.
					RequestNetworkData internalRequest = _requestedData[(data.Data as RequestNetworkData).InternalName];

					//Pass all data retreived from the response into the request.
					internalRequest.NetworkId = (data.Data as RequestNetworkData).NetworkId;
					internalRequest.Data = (data.Data as RequestNetworkData).Data;
					//Check whether the request was requesting data or type of an object.
					//If it is, we register into our local NetworkLibrary automatically.
					if ((internalRequest.RequestType == RequestNetworkDataType.RequestData ||
						internalRequest.RequestType == RequestNetworkDataType.RequestType) &&
						internalRequest.Data != null)
						//The request was requesting data. Registering it automatically.
						RegisterRecursive(internalRequest.Data as INetworkData);

					lock (internalRequest)
					{
						//This is important. In many cases when processing data in the NetworkLibrary and data
						//needs to be requested, the request is sent and the thread that is doing the processing
						//is put on sleep. With this we send a pulse into the thread to wake it up so it can continue
						//on the processing and finish the process it started.
						Monitor.Pulse(internalRequest);
					}
					//Remove the request from our collection. Since the request has been sent, processed and finished
					//we don't need it anymore.
					_requestedData.Remove(internalRequest.InternalName);
				}
				catch (Exception e)
				{
					//An unknown error occured. Since this could result in a method or a thread being paused forever
					//we pass it on as a real Exception.
					ThrowException(this, new Exception("A network response was received but an unknown error occured while processing the request.", e));
				}
			}
			//Received a packet containing the code for a Request but the data was of an invalid type.
			//Since this could be random junk we pass it on as a Warning.
			else if (OnWarningOccured != null)
				OnWarningOccured(this, new Warning("A network request response was received but was of an invalid format. Request was truncated."));
		}

		/// <summary>
		/// Event callback that is called when a local collection has it's items changed.
		/// </summary>
		/// <param name="sender">The sender object.</param>
		/// <param name="e">The collection changed information.</param>
		void NetworkData_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			_network.SendEvent((int)CorePacketCode.CollectionChanged, sender, false, new object[] { }, e);
		}

		/// <summary>
		/// Event callback that is called when a local object has it's property changed.
		/// </summary>
		/// <param name="sender">The sender object.</param>
		/// <param name="e">Property changed data.</param>
		void NetworkData_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			_network.SendEvent((int)CorePacketCode.PropertyUpdated, sender, false, new object[] { }, e.PropertyName);
		}
	}
}
