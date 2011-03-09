using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Specifies Identifiers of the packet code that is used by the network library.
	/// </summary>
	public enum CorePacketCode
	{
		/// <summary>
		/// Specifies a code of a packet with no data. Used to check for connectivity and such.
		/// </summary>
		NOOP = -777,
		/// <summary>
		/// Specifies a code of a packet where a client got disconnected.
		/// </summary>
		Disconnected = -776,
		/// <summary>
		/// Specifies a code of a packet from a newly connected client.
		/// </summary>
		NewClientConnected = -775,
		/// <summary>
		/// Specifies a code of a packet containing value from an object where the property got changed.
		/// </summary>
		PropertyUpdated = -774,
		/// <summary>
		/// Specifies a code of a packet containing a change from an object collection.
		/// </summary>
		CollectionChanged = -773,
		/// <summary>
		/// An event code to preview all packet that is recieved.
		/// </summary>
		PreviewPacket = -772,
		/// <summary>
		/// An event code to preview a property changed packet that was recieved before the property of the object is changed.
		/// </summary>
		PreviewPropertyUpdated = -771,
		/// <summary>
		/// An event code to preview a collection changed packet that was recieved before the collection is changed.
		/// </summary>
		PreviewCollectionChanged = -770,
		/// <summary>
		/// An event code when sending a network request.
		/// </summary>
		NetworkDataRequest = -769,
		/// <summary>
		/// An event code when receiving a network request response.
		/// </summary>
		NetworkDataRequestResponse = -768,
		/// <summary>
		/// This identifier is for the plugin. It means a packet being received is too long to be sent in a single
		/// packet and has to be split up.
		/// </summary>
		LongData = -767,
		/// <summary>
		/// An event code used to specify when a new header value needs to be assigner to the user's connection.
		/// All packets sent will contain that value by default.
		/// </summary>
		AssignNewHeaderValue = -765
	}
}
