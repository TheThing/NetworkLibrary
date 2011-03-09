using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;
using NetworkLibrary.Exceptions;
using StructureMap;
using System.Threading;

namespace NetworkLibrary.Utilities
{
	/// <summary>
	/// Public class to serialise or deserialise objects.
	/// </summary>
	public class Serialiser
	{
		private object _data;
		private Type _dataType;
		private bool _ignoreLibrary;
		private List<object> _serialisedObjects;
		private static Dictionary<Type, PropertyInfo[]> _cachedProperties;

		/// <summary>
		/// Initialise a new instance of Serialiser.
		/// </summary>
		public Serialiser()
		{
			_serialisedObjects = new List<object>();
			_ignoreLibrary = false;
			if (_cachedProperties == null)
				_cachedProperties = new Dictionary<Type, PropertyInfo[]>();
		}

		/// <summary>
		/// Initialise a new instance of Serialiser with specific values.
		/// </summary>
		/// <param name="serialisedObjects">A list of already serialised values. This contains reference to a
		/// collection of already serliased objects so if this serialiser encounters instances to serialise
		/// already serialised values it will link to it.</param>
		public Serialiser(List<object> serialisedObjects)
			: this()
		{
			_serialisedObjects = serialisedObjects;
		}

		/// <summary>
		/// Initialise a new instance of Serialiser with specific values.
		/// </summary>
		/// <param name="ignoreLibrary">Specifies whether the serialiser should ignore the library when parsing.
		/// This will result in all properties referencing other objects in the library will be ignored and those
		/// objects will also be serialised along.</param>
		public Serialiser(bool ignoreLibrary)
			: this()
		{
			_ignoreLibrary = ignoreLibrary;
		}

		/// <summary>
		/// Initialise a new instance of Serialiser with specific values.
		/// </summary>
		/// <param name="serialisedObjects"></param>
		/// <param name="ignoreLibrary">Specifies whether the serialiser should ignore the library when parsing.
		/// This will result in all properties referencing other objects in the library will be ignored and those
		/// objects will also be serialised along.</param>
		public Serialiser(List<object> serialisedObjects, bool ignoreLibrary)
			: this(serialisedObjects)
		{
			_ignoreLibrary = ignoreLibrary;
		}

		/// <summary>
		/// Seralise an object.
		/// </summary>
		/// <param name="data">The object itself that is being serialised.</param>
		/// <returns>A string representing a sreialised value of the object.</returns>
		public string Serialise(object data)
		{
			/*
			 * This serialiser works a little different that normal serialiser. All objects are formatted as 
			 * following and recursively:
			 *	Basic value types:
			 *		FullNameOfTheObject:TheValue
			 *		
			 *			If we are trying to parse an integer of value 4, the output would be:
			 *				Int32:4
			 *			If we were trying to parse a double value of 4.3645 the output would be:
			 *				Double:4.3645
			 *			Before the semicolon is the name of the type without the "System." in front
			 *			and after the semicolon comes the value parsed using the ToString() method.
			 *	
			 *	Arrays:
			 *		FullNameOfTheObjectPlusNamespace:[...]
			 *		
			 *			The dots are comma seperated values that contain the serialised value of the objects
			 *			inside the array. If we have an array of integers containing the values of 4, 6 and 3
			 *			the output would be like so:
			 *				System.Int32[]:[Int32:4,Int32:6,Int32:3]
			 *			The value of each contains the name of the type and it is so because then we
			 *			can have an aray of objects which contain an integer of 5, a double value of 6.43 and
			 *			a custom object. With this the result would be like so:
			 *				System.Object[]:[Int32:5,Double:6.43,NameOfNamespace.OurObject:{...}]
			 *			For more information on how objects are parsed look below.
			 *		
			 *	Custom objects and such:
			 *		FullNameOfTheObjectPlusNamespace:{...}
			 *		
			 *			Inside the brackets comes all properties that are supported in the following serialised format:
			 *				NameOfProperty=xxx;
			 *			The xxx represent the value run through the serialiser recursively, so if we have a property
			 *			called 'NumberOfUnits' and has an integer value of 2, the output would be following:
			 *				NumberOfUnits=Int32:2;
			 *				
			 *	Objects registered in the library:
			 *		{NetworkId}
			 *		
			 *			If you have an object which implements INetwordData, the story is different. Instead of the
			 *			object being recursively serialised like above, INetworkData that have been registered are
			 *			treated differently. The output of an object which implements INetworkData and contains the
			 *			NetworkId of 'Tile03' would be parsed as following:
			 *				{Tile03}
			 *			No type name or nothing like that. This is why the Serialiser is million times faster than
			 *			normal serialiser when working with registered objects. Instead of parsing and serialising
			 *			each property and so on, all it does is send a name with '{' and '}' around it.
			 *			If we have an Array which contain a reference to an object that has been registered into
			 *			the Library, it also get's the same treatment.
			 *		
			 */

			//Basic check to see if the data really is data.
			if (data != null)
			{
				//Create a local copy of the data.
				_data = data;

				//Check to see if the object implements INetworkData
				if (_data is INetworkData)
					//Make sure the NetworkId is not empty
					if (!string.IsNullOrEmpty((_data as INetworkData).NetworkId))
						//If we are set to ignore the library, the next statement will be skipped and the
						//the object will be parsed like every other object.
						if (!_ignoreLibrary)
							if (ObjectFactory.TryGetInstance<INetworkDataHandler>() != null)
								//Check to see if the object has been registered in the library.
								if (ObjectFactory.GetInstance<INetworkDataHandler>().RegisteredObjects.ContainsKey((_data as INetworkData).NetworkId))
									//The object implements INetworkData and is registered. Return the name and stop
									//with the serialiser.
									return "{" + (_data as INetworkData).NetworkId + "}";

				//Check to make sure we are not serialising an already serialised objects
				if (_serialisedObjects.Contains(data))
				{
					//We have already serialised this object.
					//Then we only need to save a link to it and exit.
					if (data is INetworkData)
					{
						if (!string.IsNullOrEmpty((_data as INetworkData).NetworkId))
						{
							//Return a link to the object
							return "{" + (_data as INetworkData).NetworkId + "}";
						}
						else
							//The object had not been registered into our collection and 
							//contains a reference to itself. Let know about it.
							throw new SerialiserException("A valid INetworkData object contained a reference to itself but had not yet been registered.", data);
					}
					else
						//Object contained a reference to itself but was not an INetworkData object.
						//In order to fully support circular reference, the object must be INetworkData.
						//This can also happen if 2 different objects contain a reference to the same object.
						throw new SerialiserException("A circular reference was detected on an object that was not of type INetworkData. Make sure all circular reference objects are of type INetworkData. This also applies if 2 different objects contain a reference to the same object then that object must also be an INetworkData object.", data);
				}

				//Create a local copy of the type of the object.
				_dataType = _data.GetType();

				//The serialised value of the object
				string output = "";

				//If the object being serialised is a basic System type we don't need to write
				//the full name of it. Instead for example 'System.Int32' we get 'Int32', short
				//and simple.
				if (_dataType.FullName == "System." + _dataType.Name)
					output += _dataType.Name + ":";
				else
					if (_dataType.UnderlyingSystemType != null && _dataType.UnderlyingSystemType.Name.Contains("ReadOnly"))
						output += _dataType.DeclaringType.FullName + ":";
					else
						output += _dataType.FullName + ":";

				//If the object is a basic value type, all we have to do is run ToString method
				//and be done with it.
				if (_data is string)
					return output + "\"" + (_data as string).Replace("\"", "\\\"") + "\"";
				else if (_data is ValueType)
					return output + (_data).ToString();

				//Check to see if the object is an array or a collection of some sort.
				if (_data is Array)
				{
					//We are parsing an array, prepare a new serialiser and parse all of it's values.
					output += "[";
					Serialiser ser = new Serialiser();
					for (int i = 0; i < (_data as IList).Count; i++)
						//The contents of an array is a comma seperated value of the contents.
						if (i > 0)
							output += "," + ser.Serialise((_data as IList)[i]);
						else
							output += ser.Serialise((_data as IList)[i]);
					return output + "]";
				}
				else
				{
					//We are parsing a normal object. Parse all supported properties inside the object.
					output += "{";

					//We create a cache of all valid properties of a given type due to the nature that
					//checking whether a property is supported or not is a slow process.
					if (!_cachedProperties.ContainsKey(_dataType))
					{
						CachePropertiesFromType(_dataType);
					}

					//Save our object into the collection. If any property contains a reference to itself
					//then we will know about it by checking if it exists in the collection.
					_serialisedObjects.Add(data);

					//If the object is of INetworkData type then we add that property first.
					if (data is INetworkData)
					{
						//Find the property
						foreach (var propInfo in _cachedProperties[_dataType])
							//The only time the INetworkData property is hardcoded
							if (propInfo.Name == "NetworkId")
							{
								//Add it first to the output
								output += new PropertySerialiser(_data, propInfo).Serialise() + ";";
								break;
							}
					}

					//Run through each property of the object and parse it's value
					foreach (var propInfo in _cachedProperties[_dataType])
					{
						if (propInfo.Name == "NetworkId" && data is INetworkData)
							continue;

						//PropertSerialiser takes care of this for us.
						PropertySerialiser serProp = new PropertySerialiser(_data, propInfo);

						if (_dataType == typeof(RequestNetworkData))
							//Pass forward the rule of whether to ignore the Library
							serProp.IgnoreLibrary = this._ignoreLibrary;

						//Pass in a reference to already serialised objects
						serProp.SerialisedObjects = this._serialisedObjects;
						output += serProp.Serialise() + ";";
					}
					return output + "}";
				}
			}
			else
				return "null";
		}

		/// <summary>
		/// Cache all supported properties for a specific type.
		/// </summary>
		/// <param name="type">The type to cache all supported properties for.</param>
		private void CachePropertiesFromType(Type type)
		{
			//We have yet to create a local cache of all supported properties of 
			//the type of the object being serialised. Prepare a new list and
			//prepare to fill it with all properties we can support.
			List<PropertyInfo> listProp = new List<PropertyInfo>();
			foreach (var propInfo in type.GetProperties())
				//Check to see if the property can be serialised or not
				if (PropertySerialiser.CanSerialiseProperty(propInfo))
					//The property can be seralised, add it to the list.
					listProp.Add(propInfo);
			//Add the list of supported properties into our local cache.
			_cachedProperties.Add(type, listProp.ToArray());
		}

		/// <summary>
		/// Deserialise a string value into it's given object that it represents.
		/// </summary>
		/// <param name="data">A serialised object that should be deserialised.</param>
		/// <returns>The original object the serialised string represented.</returns>
		/// <exception cref="System.NullReferenceException" />
		/// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
		/// <exception cref="NetworkLibrary.Exceptions.NetworkDataCollectionException" />
		public object Deserialise(string data)
		{
			return this.Deserialise(null, data);
		}

		/// <summary>
		/// Deserialise a string value into it's given object that it represents.
		/// </summary>
		/// <param name="t">The type of the data that is being deserialised.</param>
		/// <param name="data">A serialised object that should be deserialised.</param>
		/// <returns>The original object the serialised string represented.</returns>
		/// <exception cref="System.NotSupportedException" />
		/// <exception cref="System.NullReferenceException" />
		/// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
		/// <exception cref="NetworkLibrary.Exceptions.NetworkDataCollectionException" />
		protected object Deserialise(Type dataType, string data)
		{
			object output = null;
			//Check to see if the data is longer than zero letters. this is done to prevent unnecessary exceptions
			//that could be thrown by the system.
			if (data.Length > 0)
			{
				//Check to see if we are parsing a serialised value of null
				if (data == "null")
					return null;

				//Retreave the local instance of NetworkHandler. We use this to get types registered
				//into the library as well as sending requests and such.
				INetworkDataHandler networkHandler = ObjectFactory.GetInstance<INetworkDataHandler>();
				
				//Check to see if the object is a reference to an INetworkData object
				if (data[0] == '{')
				{
					//Get the name of the NetworkId of the object being referenced here.
					string name = data.Substring(1, data.Length - 2);

					//Check to see if we have it in our collection.
					if (networkHandler.RegisteredObjects.ContainsKey(name))
						//Return a reference to the object that is being contained in our collection.
						return networkHandler.RegisteredObjects[name];
					else
					{
						//It might be this object we are trying to deserialise has already been deserialised
						//and this is only a reference to it. Check to see if we have maybe already deserialised it.
						for (int i = 0; i < _serialisedObjects.Count; i++)
						{
							if (_serialisedObjects[i] is INetworkData)
								if (((INetworkData)_serialisedObjects[i]).NetworkId == name)
									return _serialisedObjects[i];
						}

						//The object was not found in our library. Send request to the source and ask
						//for the object that was being referenced.
						RequestNetworkData request = new RequestNetworkData(RequestNetworkDataType.RequestData, name);

						lock (request)
						{
							networkHandler.RequestSend(request);
							//The request has been sent. Now we lock this thread until the reply has
							//been received. Once an reply has been recieved, it will send a pulse to
							//the request waking this thread and allows it to finish it's job.
							Monitor.Wait(request);
						}
						//Make sure the object is finally in our collection.
						if (networkHandler.RegisteredObjects.ContainsKey(name))
							return networkHandler.RegisteredObjects[name];
						else
							//The object was not found even after sending an request to the source.
							throw new NetworkDataCollectionException("The data contained a reference to a network id but was not found in the collection and was neither found at the source of the packet.", name);
					}
				}
				//Create a reference to the type being deserialised here.
				Type t;
				string[] split;

				if (dataType == null)
				{
					//Split the name of the type and the value which is being seperated by a semicolon.
					split = data.Split(new char[] { ':' }, 2);

					//Check to see if we were able to split the value.
					if (split.Length == 2)
					{
						//Check to see if the type has been registered in our library.
						if (networkHandler.RegisteredTypes.ContainsKey(split[0]))
							t = networkHandler.RegisteredTypes[split[0]];
						else
						{
							//If the type is a basic value type, the namespace 'System' is ommitted to save space.
							//Here we check to see if such ommitation has been done and if so, add the System into
							//the front of the name before we search for the type using name search.
							if (split[0].IndexOf('.') == -1)
								t = Type.GetType("System." + split[0]);

							else if (split[0].EndsWith("[]"))
							{
								//Because this is an array we could be dealing with an array of special objects.
								//These kinds of arrays objects can't be created directly but need to be passed
								//through the Array.CreateInstance.
								if (networkHandler.RegisteredTypes.ContainsKey(split[0].Remove(split[0].Length - 2, 2)))
									//Create an array of special objects.
									t = Array.CreateInstance(networkHandler.RegisteredTypes[split[0].Remove(split[0].Length - 2, 2)], 0).GetType();
								else
									//If it's an array of basic value types then we can create those directly.
									t = Type.GetType(split[0]);
							}
							else
								t = Type.GetType(split[0]);
						}
					}
					else
						throw new ParsingException("A serialised string was of an invalid format. Expecting a value of 'type:content'.");
				}
				else
				{
					t = dataType;
					split = new string[] { "", data};
				}

				//If we still don't have the type of the object there is nothing to be done.
				//Most likely cause for this is if the programmer forgot to pre-register the type
				//of the object.
				if (t == null)
					throw new NullReferenceException(string.Format("Unable to create an object of name '{0}'. Did you forget to register the type?.", split[0]));

				//Check to see if the type is array
				if (t.IsArray)
					//Because it's an array we can't dynamicly add to it, we therefore create
					//a list which we can add and remove at will and later, copy it into an array.
					output = new List<object>();
				//If you try to run Activator and create an instance of string it will fail.
				else if (t != typeof(string))
					//Create a default instance of the object being deserialised, we fill it with
					//data later on.
					output = Activator.CreateInstance(t);
				else
					//The type is string, since we only need a basic value in the correct format,
					//we make our output "become" string like so
					output = "";

				int skipIdentifier = 0, length = split[1].Length - 1;
				bool insideParenthis = false;
				string identifiers = "{[]}", buffer = "", property = "";

				//Check to see if we are working with basic value type
				if (output is ValueType || output is string)
					//Since this is just a basic value type, all we have to do is parse the value
					//into it's correct format.
					return ParseValueToType(t, split[1]);
				//Check to see if the Activator was succesfull in creating an instance for us.
				else if (output == null)
					throw new NullReferenceException(string.Format("While creating an instance of '{0}', the activator returned null value.", t.FullName));
				else if (output is IList)
					length++;
				
				//Add our new object to the collection
				_serialisedObjects.Add(output);

				//Checkmark for enabling listener.
				bool enableListener = false;

				//Run through the value, feed the buffer and parse if necessary.
				for (int i = 1; i < length; i++)
				{
					switch (split[1][i])
					{
						//The following is checking whether we have reached an endmark that resembles
						//an end when parsing objects and a next step is necessary.
						case ';':
						case '=':
							//Here we check if the endmark is really an endmark and not a character inside the value
							if (skipIdentifier == 0 && !insideParenthis)
								//Check to see if we have the name of the property or not.
								if (string.IsNullOrEmpty(property))
								{
									//The buffer contains the name of the property, retrieve it and flush
									//the buffer so it can grab the value of the property.
									property = buffer;
									buffer = "";
								}
								else
								{
									//We have the name of the property we are trying to fill and the value
									//is inside the buffer. Prepare to deserialise the value and feed it into
									//the property of the object.

									//Grap a local info copy of the property.
									PropertyInfo info = t.GetProperty(property);

									//Deserialise the value
									object value;
									if (buffer[0] == '[')
										value = this.Deserialise(t, buffer);
									else
										value = this.Deserialise(buffer);

									//Check if the property is of an array or collection that can't be overriden.
									if (info.PropertyType.IsArray || !info.CanWrite)
										//Make sure the collection or array inside the object is not null.
										if (info.GetValue(output, null) == null)
										{
											//Because the collection or array inside is null and we can't override
											//it we stop what we are doing and continue with the rest of the deserialising.
											property = buffer = "";
											break;
										}

									//Check if we are working with INetworkData object and if we haven't disabled the listener already.
									if (output is INetworkData && !enableListener)
										//Check to see if the object has it's INetworkId
										if (!string.IsNullOrEmpty((output as INetworkData).NetworkId))
											//Check if we have it registered in our collection
											if (networkHandler.RegisteredObjects.ContainsKey((output as INetworkData).NetworkId))
											//Since we have it in our collection there is a chance that changing properties can cause
											//NotifyPropertyChanged to run. We disable any listeners while we finish adding all the properties.
											{
												networkHandler.ListenerDisable((INetworkData)output);
												enableListener = true;
											}

									//If we have direct write access, we can just pass the value directly
									//into the object and be done with it.
									if (info.CanWrite)
									{
										//Check to see if the property has index parameter
										if (info.GetIndexParameters().Length > 0)
										{
											//The property requires index parameter. This usually means we are working
											//with the contents of a collection. If so, then the value should also be a
											//collection of the contents.
											if (output is IList && !t.IsArray && value is IList)
												//Add each item into the collection
												for (int addRange = 0; addRange < (value as IList).Count; addRange++)
													(output as IList).Add((value as IList)[addRange]);
											else
												//It required an index parameter but it wasn't a collection
												throw new NotSupportedException("Encountered a property that required index parameter but either the object or the value was not of an IList type.");
										}
										else
										{
											//No index property, then we just write the value directly
											info.SetValue(output, value, null);

											//Check to see if the property is NetworkData and if the object is NetworkId
											if (property == "NetworkId" && output is INetworkData && !string.IsNullOrEmpty((string)value))
												//Since the incoming object is INetworkData, we check to see
												//if we already have it. If we don't, we register it.
												if (!networkHandler.RegisteredObjects.ContainsKey((string)value))
													networkHandler.Register((INetworkData)output);
										}
									}
									else if (value is IList)
										//We don't have direct write access but it is a collection
										//so instead of overwriting the array with new array, we fill
										//the array with new information from the value.
										if (info.PropertyType.IsArray)
										{
											//Grab the array from the collection.
											IList objectArray = info.GetValue(output, null) as IList;

											//Go over both arraies and make sure we don't go overboard.
											for (int arrayIndex = 0; arrayIndex < objectArray.Count && arrayIndex < (value as IList).Count; arrayIndex++)
												//Assign the value to the array in the object.
												objectArray[arrayIndex] = (value as IList)[arrayIndex];
										}
										else
										{
											//Grab the collection from the object.
											IList collection = info.GetValue(output, null) as IList;

											//Check to see if we have all the properties for this object cached.
											if (!_cachedProperties.ContainsKey(value.GetType()))
												//We don't have the properties for this object cached. We need to cache it
												//before we continue.
												CachePropertiesFromType(value.GetType());

											//Go over all the properties and merge them with the one inside the object.
											//By doing this, properties such as NetworkId will be passed along.
											foreach (var propInfo in _cachedProperties[value.GetType()])
												//Check if it's a basic property which we can write to.
												if (propInfo.GetIndexParameters().Length == 0 && propInfo.CanWrite)
													//Write the property from our network packet collection
													//into the collection inside the object.
													propInfo.SetValue(collection, propInfo.GetValue(value, null), null);

											//Add the contents into the collection in the object.
											for (int item = 0; item < (value as IList).Count; item++)
												collection.Add((value as IList)[item]);

											if (collection is INetworkData && value is INetworkData)
												//This is an important step. If the collection is INetworkData then the collection
												//from the network packet has been registered into the RegisteredObjects instead of
												//of the collection from the object. We therefore have to unregister the incorrect one
												//and register the correct one from the object.
												if (networkHandler.RegisteredObjects.ContainsKey((value as INetworkData).NetworkId))
												{
													//Unregister the incorrect collection that we just now deserialised.
													networkHandler.Unregister(value as INetworkData);
													//Register the correct one from inside the object we are assign the values to.
													networkHandler.Register(collection as INetworkData);
												}
										}
									else
										throw new ParsingException("Encountered a property that is unsupported.");
									property = buffer = "";
								}
							else
								//Since this is not an endmark, make sure we add it to the buffer.
								buffer += split[1][i];
							break;
						//The following is checking whether we have reached an endmark that resembles
						//an end when parsing arrays and such.
						case ']':
						case ',':
							//Here we check if the endmark is really an endmark and not a character inside the value
							if (skipIdentifier == 0 && output is IList && !insideParenthis)
							{
								if (buffer != "")
									//Since this is an array, we add the current value inside the buffer
									//into the output array and continue on.
									(output as IList).Add(this.Deserialise(buffer));
								buffer = "";
							}
							else
							{
								if (!insideParenthis && split[1][i] == ']')
									skipIdentifier--;

								//Since this is not an endmark, make sure we add it to the buffer.
								buffer += split[1][i];
							}
							break;
						//This checks to see if we have reached a string value, if so, all characters
						//that resemble an endmark are automatically ignored. This is done by marking the
						//insideParenthis boolean.
						case '"':
							//Check to see if the parenthis has been escaped or not.
							if (split[1][i - 1] != '\\')
								//The parenthis has not been escaped, this means it's a start or an end
								//of a string value, mark this by changing the marked.
								insideParenthis = !insideParenthis;

							//Add the current character to the buffer.
							buffer += split[1][i];
							break;
						default:
							//Check to see if we have to skip some identifiers because we are adding
							//items into the buffer that contain recursive objects.
							if (identifiers.IndexOf(split[1][i]) > -1 && !insideParenthis)
								//Check to see if we have to skip an extra identifier/endmark of if we
								//are finishing skipping an identifier/endmark
								if (identifiers.IndexOf(split[1][i]) < identifiers.Length / 2)
									skipIdentifier += 1;
								else
									skipIdentifier -= 1;
							buffer += split[1][i];
							break;
					}
				}
				//Since the output is array, here we copy the content of the collection into an array
				if (output is IList && t.IsArray)
				{
					object temp = Activator.CreateInstance(t, (output as IList).Count);
					Array.Copy((output as List<object>).ToArray(), temp as Array, (output as IList).Count);
					return temp;
				}
				//Check to see if we need to reenable the listener.
				if (enableListener)
					//We need to reenable the listener since we disabled it during the deserialisation.
					networkHandler.ListenerEnable((INetworkData)output);
			}
			
			return output;
		}

		/// <summary>
		/// Parse a string value from it's string type into a specific type. Uses Parse if such a method is available
		/// automatically parses enums from it's string values to it's original value.
		/// </summary>
		/// <param name="type">The type the string should be parsed into.</param>
		/// <param name="value">The string that represent the data that is being parsed.</param>
		/// <returns>The original value parsed into it's correct format.</returns>
		/// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
		public static object ParseValueToType(Type type, string value)
		{
			//Create a basic holder for our parsed value
			object output = null;

			//If the value should be parsed into string, there is nothing for us to do.
			if (type == typeof(string))
				//We remove the first and last letter due to the fact that Serialiser parses
				//all strings with a " in front of it and back to distinguish it from other types.
				return value.Substring(1, value.Length - 2);

			//Check whether the type has a method called Parse and accepts string as a parameter.
			//All basic values, like integers and such all support this.
			else if (type.GetMethod("Parse", new Type[] { typeof(string) }) != null)
			{
				try
				{
					//Parse string using the internal Parse method the type supports.
					output = type.GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { value });
				}
				catch (Exception e)
				{
					throw new ParsingException(string.Format("Error while parsing '{0}' to the following type: {1}. Error message received: {2}", value, type.FullName, e.Message));
				}
			}
			//If the type is a basic enum, we have to do a parse on it using naming convenience.
			//Fortunately C# supports this.
			else if (type.BaseType == typeof(Enum))
				return Enum.Parse(type, value);

			//Return the value parsed into correct format.
			return output;
		}

		/// <summary>
		/// Get or set whether the Serialiser should ignore the library. This will result in all
		/// properties referencing other objects in the library will be ignored and those objects
		/// will also be serialised along.
		/// </summary>
		public bool IgnoreLibrary
		{
			get { return _ignoreLibrary; }
			set { _ignoreLibrary = value; }
		}
	}
}
