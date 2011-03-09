using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Exceptions
{
	public class NetworkDataCollectionException : Exception
	{
		public NetworkDataCollectionException(string message, string networkDataName)
			: base(message)
		{
			_networkDataName = networkDataName;
		}

		private string _networkDataName;

		public string NetworkDataName
		{
			get { return _networkDataName; }
		}

		public override string ToString()
		{
			return string.Format("{0}. Network data name: {1}.\n\tFull Traceback:{2}", base.Message, _networkDataName, base.ToString());
		}
	}
}
