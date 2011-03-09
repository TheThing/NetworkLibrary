using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary
{
	public class NetworkDataException : Exception
	{
		public NetworkDataException(string message, INetworkData data)
			: base(message)
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
			return string.Format("{0}. Source: {1}", base.Message, _data.NetworkId);
		}
	}
}
