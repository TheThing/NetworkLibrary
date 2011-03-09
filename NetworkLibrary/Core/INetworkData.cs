using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Enables objects to be registered to the network library.
	/// </summary>
	public interface INetworkData
	{
		/// <summary>
		/// Get or set the id of the object.
		/// </summary>
		string NetworkId
		{
			get;
			set;
		}
	}
}
