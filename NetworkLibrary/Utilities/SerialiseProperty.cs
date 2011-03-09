using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using NetworkLibrary.Exceptions;
using NetworkLibrary.Core;

namespace NetworkLibrary.Utilities
{
	public class PropertySerialiser
	{
		private string _unserialised;
		private object _data;
		private bool _ignoreLibrary;
		private PropertyInfo _propertyInfo;
		private List<object> _serialisedObjects;

		public PropertySerialiser()
		{
			_serialisedObjects = new List<object>();
		}

		public PropertySerialiser(object data, string serialisedProperty)
			: this()
		{
			_data = data;
			_unserialised = serialisedProperty;
		}

		public PropertySerialiser(object data, PropertyInfo propInfo)
			: this()
		{
			_data = data;
			_propertyInfo = propInfo;
		}

		public string Serialise()
		{
			if (_propertyInfo == null)
				throw new NullReferenceException("An attempt was made serialise a null referenced property.");

			Serialiser ser = new Serialiser(_serialisedObjects, _ignoreLibrary);
			switch (_propertyInfo.GetIndexParameters().Length)
			{
				case 0:
					return _propertyInfo.Name + "=" + ser.Serialise(_propertyInfo.GetValue(_data, null));
				case 1:
					if (_data is IList)
					{
						string output = "";
						for (int i = 0; i < (_data as IList).Count; i++)
							if (i > 0)
								output += "," + ser.Serialise((_data as IList)[i]);
							else
								output += ser.Serialise((_data as IList)[i]);
						return _propertyInfo.Name + "=[" + output + "]";
					}
					throw new PropertySerialiseException("An attempt was made to serialise a property that required one index parameter but the object was not of an IList interface.", _propertyInfo);
				default:
					throw new PropertySerialiseException("An attempt was made to serialise a property that required more than one index parameter. This is currently unsupported.", _propertyInfo);
			}
		}

		public static bool CanSerialiseProperty(PropertyInfo info)
		{
			if (info.CanWrite)
				if (info.GetCustomAttributes(typeof(XmlIgnoreAttribute), true).Length == 0)
					return true;
				else
					return false;
			if (typeof(IList).IsAssignableFrom(info.PropertyType))
				if (info.GetCustomAttributes(typeof(XmlIgnoreAttribute), true).Length == 0)
					return true;
			return false;
		}
		public bool IgnoreLibrary
		{
			get { return _ignoreLibrary; }
			set { _ignoreLibrary = value; }
		}
		public List<object> SerialisedObjects
		{
			get { return _serialisedObjects; }
			set { _serialisedObjects = value; }
		}
	}
}

/*

PropertyInfo[] properties = type.GetProperties();
					for (int i = 0; i < properties.Length; i++)
					{
						if (properties[i].CanWrite || properties[i].PropertyType.FullName.Contains("Collections"))
						{
							if (properties[i].GetCustomAttributes(typeof(System.Xml.Serialization.XmlIgnoreAttribute), true).Length == 0)
							{
								output.AppendFormat("{0}=", properties[i].Name);

								if (properties[i].GetIndexParameters().Length > 0)
								{
									if (properties[i].GetIndexParameters().Length > 1)
										throw new NetworkLibrary.ParsingException(string.Format("A property of name '{0}' in object of name '{1}' contained more than one index parameter which is currently unsupported.", properties[i].Name, type.FullName));
									PropertyInfo count = type.GetProperty("Count");
									if (count != null)
									{
										output.Append("[");
										int numberItems = (int)count.GetValue(data, null);
										for (int itemNr = 0; itemNr < numberItems; itemNr++)
										{
											if (itemNr > 0)
												output.Append(",");
											output.Append(SerialiseObject(registeredData, properties[i].GetValue(data, new object[] { itemNr })));
										}
										output.Append("]");
									}
									else
									{
										throw new NetworkLibrary.ParsingException(string.Format("A property of name '{0}' in object of name '{1}' contained an index parameter but the object did not contain the property Count.", properties[i].Name, type.FullName));
									}
								}
								else
								{
									output.Append(SerialiseObject(registeredData, properties[i].GetValue(data, null)));
								}
								output.Append(";");
							}
						}
					}

 */
