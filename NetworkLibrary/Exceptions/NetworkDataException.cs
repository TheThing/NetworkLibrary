using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;

namespace NetworkLibrary.Exceptions
{
	public class NetworkDataException : Exception
	{
		public NetworkDataException(string message)
			: this(message, null)
		{
		}

		public NetworkDataException(string message, INetworkData data)
			: this(message, data, null)
		{
		}

		public NetworkDataException(string message, INetworkData data, Exception innerException)
			: base(message, innerException)
		{
			_data = data;
		}

		private INetworkData _data;

		public INetworkData NetworkData
		{
			get { return _data; }
		}

		public override string ToString()
		{
			return string.Format("{0}. Source network data name: {1}", base.Message, _data.NetworkId);
		}
	}
}
