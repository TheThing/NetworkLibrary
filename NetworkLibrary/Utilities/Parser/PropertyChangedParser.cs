using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StructureMap;
using NetworkLibrary.Core;
using NetworkLibrary.Exceptions;

namespace NetworkLibrary.Utilities.Parser
{
	/// <summary>
	/// Public parser that takes care of parsing PropertyChanged packets both to and from.
	/// </summary>
	public class PropertyChangedParser : IParser
	{
		/// <summary>
		/// Initialise a new instance of PropertyChangedParser.
		/// </summary>
		public PropertyChangedParser()
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
		/// Parse a property changed data to a special string that will be the source of a network packet message.
		/// </summary>
		/// <param name="item">The item that had it's property changed..</param>
		/// <param name="arguments">Argument containing the name of the property in a string form.</param>
		/// <returns>The object parsed.</returns>
		public string ParseObject(object item, object[] arguments)
		{
			//Preliminary checks, checking if the arguments are there, of correct type
			//if the property exists and so on.

			//Check to see if we have enough arguments.
			if (arguments.Length < 1)
			{
				RunExceptionOccured(new Exception("Property changed parser was called with incomplete arguments. Expected mininum 1 argument, received " + arguments.Length));
				return null;
			}
			//Check to see if the argument is of correct type.
			if (!(arguments[0] is string))
			{
				RunExceptionOccured(new Exception("Property changed parser was called with unknown argument. Expected an argument of System.String type, received " + arguments[0].GetType().FullName));
				return null;
			}
			//Check to see if we are working with network data object. Propert changed works only
			//on INetorkData objects.
			if (!(item is INetworkData))
			{
				RunExceptionOccured(new Exception("Property changed parser was called with an object that was not of INetworkData type. The object that did not implement INetworkData was of type " + item.GetType().FullName));
				return null;
			}

			//Grab the property name from the argument
			string propertyName = arguments[0] as string;

			//Check whether the property actually exist. Sometimes a grammar error
			//or a simple mistake can lead to the property name being misspelled.
			if (item.GetType().GetProperty(propertyName) == null)
			{
				if (OnWarningOccured != null)
					OnWarningOccured(item, new Warning(string.Format("The property '{0}' was not found in the object of type '{1}'", propertyName, item.GetType().FullName)));
				return null;
			}

			//Preliminary checks complete. Time to parse the property

			//Create a local value of the new value from the property.
			object newValue = item.GetType().GetProperty(propertyName).GetValue(item, null);

			//The format of a property changed is the following: "NAMEOFOBJECT:NAMEOFPROPERTY:NEWVALUE"
			//The new value is retrieved using the Serialiser. 
			string message = string.Format("{0}:{1}:", (item as INetworkData).NetworkId, propertyName);

			try
			{
				//Serialise the new value
				message += new Serialiser().Serialise(newValue);
			}
			catch (Exception error)
			{
				RunExceptionOccured(new Exception(string.Format("Error while preparing a network packet of PropertyChanged. {0}", error.Message), error));
				return null;
			}
			return message;
		}

		/// <summary>
		/// Parse an incoming string from an incoming packet and return a specialised NetworkEventArgs containg property changed information.
		/// </summary>
		/// <param name="item">The packet to parse.</param>
		/// <returns>Specialised NetworkEventArgs.</returns>
		public NetworkEventArgs ParsePacket(NetworkPacket packet)
		{
			//Split the message into maximum 3 parts because of the property changed packet format:
			//networkid:property_name:value
			string[] data = packet.Message.Split(new char[] { ':' }, 3);

			//Create the holder for our results.
			NetworkEventArgs result = new NetworkEventArgs(packet);

			//Make sure the message was correct.
			if (data.Length == 3)
			{
				INetworkDataHandler dataHandler = ObjectFactory.GetInstance<INetworkDataHandler>();

				//Check to see if we have the object that had it's property changed.
				if (dataHandler.RegisteredObjects.ContainsKey(data[0]))
				{
					//We have the object so now we extract the information we want from the packet.
					result.NetworkDataId = data[0];
					result.PropertyName = data[1];

					try
					{
						//Deserialise the property value.
						result.Data = new Serialiser().Deserialise(data[2]);

						//Return our results.
						return result;
					}
					catch (Exception e)
					{
						//We received a valid packet but encountered an error while deserialising.
						//We pass this on as a warning.
						if (OnWarningOccured != null)
							OnWarningOccured(packet, new Warning(string.Format("Error while deserialising a property changed packet. Error message received: {0}.", e.Message), e));
					}
				}
				else if (OnNotificationOccured != null)
						OnNotificationOccured(packet, string.Format("Error while processing an incoming property changed packet. Network object name of '{0}' was not found in the collection", data[0]));
			}
			else if (OnNotificationOccured != null)
				OnNotificationOccured(packet, "Property updated was not in a correct format. Acepting a message in format of 'name:property:value'");

			//If we ever get here it means we encountered an error. We therefore return a null value.
			return null;
		}
	}
}
