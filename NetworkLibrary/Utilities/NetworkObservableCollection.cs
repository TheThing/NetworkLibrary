using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;

namespace NetworkLibrary.Utilities
{
	/// <summary>
	/// Represents a dynamic network data collection that provides notifications when items get added, removed
	/// or are refreshed.
	/// </summary>
	public class NetworkObservableCollection<T> : ObservableCollection<T>, INetworkData
	{
		string _networkId;

		/// <summary>
		/// Initialise a new instance of a dynamic network data collection.
		/// </summary>
		public NetworkObservableCollection()
		: base()
		{
		}

		/// <summary>
		/// Get or set the network id of this object.
		/// </summary>
		public string NetworkId
		{
			get { return _networkId; }
			set { _networkId = value; }
		}
	}
}
