using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Exceptions
{
	class SerialiserException: Exception
	{
		object _data;

		public SerialiserException(string message, object data)
			: this(message, data, null)
		{
		}

		public SerialiserException(string message, object data, Exception innerException)
			: base(message, innerException)
		{
			_data = data;
		}

		public new object Data
		{
			get { return _data; }
		}
	}
}
