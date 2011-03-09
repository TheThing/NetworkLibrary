using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// The type of the packet that was sent.
	/// </summary>
	public enum NetworkEventType
	{
		/// <summary>
		/// The network event is of type when property has been changed on any client/host connection.
		/// </summary>
		PropertyChanged = 0,
		/// <summary>
		/// The network event is of type when collection has been changed on any client/host connection.
		/// </summary>
		CollectionChanged = 1,
		/// <summary>
		/// The network event is of a custome type.
		/// </summary>
		Custom = 2
	}
}
