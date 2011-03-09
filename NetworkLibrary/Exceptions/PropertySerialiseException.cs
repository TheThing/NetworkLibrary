using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NetworkLibrary.Exceptions
{
	class PropertySerialiseException : Exception
	{
		private PropertyInfo _property;

		public PropertySerialiseException(string message, PropertyInfo property)
			: this(message, property, null)
		{
		}

		public PropertySerialiseException(string message, PropertyInfo property, Exception innerException)
			: base(message, innerException)
		{
			_property = property;
		}

		public PropertyInfo Property
		{
			get { return _property; }
		}
	}
}
