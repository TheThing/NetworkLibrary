using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using StructureMap;
using NetworkLibrary.Core;
using NetworkLibrary.Exceptions;

namespace NetworkLibrary.Utilities.Parser
{
	/// <summary>
	/// Public parser that takes care of parsing collection changed packets both to and from.
	/// </summary>
	public class CollectionChangedParser : IParser
	{
		/// <summary>
		/// Initialise a new instance of CollectionChangedParser.
		/// </summary>
		public CollectionChangedParser()
		{
		}

		public event delegateException OnExceptionOccured;
		public event delegateWarning OnWarningOccured;
		public event delegateNotification OnNotificationOccured;

		protected void RunExceptionOccured(Exception e)
		{
			if (OnExceptionOccured != null)
				OnExceptionOccured(this, e);
			else
				throw e;
		}

		/// <summary>
		/// Parse an object to it's relevant string for an outgoing packet.
		/// </summary>
		/// <param name="item">The item to parse.</param>
		/// <param name="arguments">Arguments to pass to the parser.</param>
		/// <returns>The object parsed.</returns>
		public string ParseObject(object item, object[] arguments)
		{
			//Start preliminary checking

			//Check to see if we have enough arguments.
			if (arguments.Length < 1)
			{
				RunExceptionOccured(new Exception("Collection changed parser was called with incomplete arguments. Expected mininum 1 argument, received " + arguments.Length));
				return null;
			}
			//Check to see if the argument is of correct type.
			if (!(arguments[0] is NotifyCollectionChangedEventArgs))
			{
				RunExceptionOccured(new Exception("Property changed parser was called with unknown argument. Expected an argument of System.String type, received " + arguments[0].GetType().FullName));
				return null;
			}
			//Check to see if we are working with network data object. Propert changed works only
			//on INetworkData objects.
			if (!(item is INetworkData))
			{
				RunExceptionOccured(new Exception("Property changed parser was called with an object that was not of INetworkData type. The object that did not implement INetworkData was of type " + item.GetType().FullName));
				return null;
			}

			//Get the collection changed info.
			NotifyCollectionChangedEventArgs arg = arguments[0] as NotifyCollectionChangedEventArgs;

			//Save the necessary values from the list.
			IList list = arg.NewItems;
			int old = 0;
			if (arg.OldItems != null)
				old = arg.OldItems.Count;
			if (arg.Action == NotifyCollectionChangedAction.Remove)
				list = arg.OldItems;

			try
			{
				//Return a new string that will be the message for a network packet of collection changed type.
				return string.Format("{0}:{1}:{2}:{3}:{4}:{5}",
					(item as INetworkData).NetworkId,
					(int)arg.Action,
					arg.NewStartingIndex,
					arg.OldStartingIndex,
					old,
					new Serialiser().Serialise(list));
			}
			catch (Exception e)
			{
				RunExceptionOccured(new SerialiserException("Error while serialising CollectioChanges, message returned was: " + e.Message, arg.NewItems, e));
				return null;
			}
		}

		/// <summary>
		/// Parse an incoming string from an incoming packet and return a specialised NetworkEventArgs.
		/// </summary>
		/// <param name="item">The packet to parse.</param>
		/// <returns>Specialised NetworkEventArgs.</returns>
		public NetworkEventArgs ParsePacket(NetworkPacket packet)
		{
			//Split the message into maximum 6 parts so we can extract it's data. The format is as follows:
			//networkid:collectionChangedAction:startingIndexForNewItems:startingIndexForOldItems:ListOfItemsRemovedOrAdded
			string[] data = packet.Message.Split(new char[] { ':' }, 6);

			//Make sure the maximum length was archieved.
			if (data.Length == 6)
			{
				//Get the data handler.
				INetworkDataHandler dataHandler = ObjectFactory.GetInstance<INetworkDataHandler>();

				//Make sure we have the item whose collection was changed.
				if (dataHandler.RegisteredObjects.ContainsKey(data[0]))
				{
					//Create the holder for our results.
					NetworkEventArgs result = new NetworkEventArgs(packet);

					//Assign the relevant values.
					result.NetworkDataId = data[0];
					IList itemList;
					int newIndex, oldIndex, oldCount, intAction;
					try
					{
						//Deserialise and convert the data to it's correct form.
						itemList = new Serialiser().Deserialise(data[5]) as IList;
						intAction = Convert.ToInt32(data[1]);
						newIndex = Convert.ToInt32(data[2]);
						oldIndex = Convert.ToInt32(data[3]);
						oldCount = Convert.ToInt32(data[4]);
					}
					catch (Exception e)
					{
						if (OnWarningOccured != null)
							OnWarningOccured(packet, new Warning("An unknown error occured while deserialising a collection changed packet that was received. Message received: " + e.Message, e));
						return null;
					}

					NotifyCollectionChangedAction action = (NotifyCollectionChangedAction)intAction;

					//Create a new instance of notify collection changed that contains the relevant
					//information about the changes in the collection.
					switch (action)
					{
						case NotifyCollectionChangedAction.Add:
							result.Data = new NotifyCollectionChangedEventArgs(action, itemList, newIndex);
							break;
						case NotifyCollectionChangedAction.Move:
							result.Data = new NotifyCollectionChangedEventArgs(action, itemList, newIndex, oldIndex);
							break;
						case NotifyCollectionChangedAction.Remove:
							result.Data = new NotifyCollectionChangedEventArgs(action, itemList, oldIndex);
							break;
						case NotifyCollectionChangedAction.Replace:
							result.Data = new NotifyCollectionChangedEventArgs(action, itemList, new bool[oldCount], newIndex);
							break;
						case NotifyCollectionChangedAction.Reset:
							result.Data = new NotifyCollectionChangedEventArgs(action);
							break;
					}
					
					//Return our results.
					return result;
				}
				else if (OnNotificationOccured != null)
					OnNotificationOccured(packet, string.Format("Collection changed parser was called but the network id of '{0}' was not found in the registered objects.", data[0]));
				
			}
			else if (OnNotificationOccured != null)
				OnNotificationOccured(packet, "Collection changed packet was not in a correct format. Acepting a message in format of 'name:newstartindex:oldstartindex:value'");

			//If we ever get here it means we encountered an error. We therefore return a null value.
			return null;
		}
	}
}
