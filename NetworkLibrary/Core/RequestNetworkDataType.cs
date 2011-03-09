using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Specifies the type of the network request.
	/// </summary>
	public enum RequestNetworkDataType
	{
		/// <summary>
		/// Requesting data from the client or host. Usually used when requesting data with specific NetworkId where
		/// the NetworkId is known but the data itself has not been registered here.
		/// </summary>
		RequestData = 0,
		/// <summary>
		/// Requesting name for object whose NetworkId has not been specified. Only the host has the privilege to
		/// assign NetworkId for objects. Clients have to request the NetworkId from the host.
		/// </summary>
		RequestName = 1,
		/// <summary>
		/// Requesting data of type specified. This is handy when retrieving data from host to client. The data type
		/// name has to be fully specified.
		/// </summary>
		RequestType = 2
	}
}
