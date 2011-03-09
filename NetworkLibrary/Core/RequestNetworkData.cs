using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Class used to specify request sent over the network.
	/// </summary>
	public class RequestNetworkData
	{
		private RequestNetworkDataType _type;
		private string _internalName;
		private string _name;
		private string _typeName;
		private INetworkData _data;
		private object _target;

		/// <summary>
		/// Initialise a new instance of RequestNetworkData with no data specified. 
		/// </summary>
		public RequestNetworkData()
		{
		}

		/// <summary>
		/// Initialise a new instance of RequestNetworkData and copy all members of another specified request. 
		/// </summary>
		/// <param name="baseCopy">An instance of Request to copy all members over to this new request.</param>
		public RequestNetworkData(RequestNetworkData baseCopy)
			: this(baseCopy.RequestType, baseCopy.Data)
		{
			_internalName = baseCopy._internalName;
			_name = baseCopy._name;
			_typeName = baseCopy._typeName;
			_target = baseCopy._target;
		}

		/// <summary>
		/// Initialise a new instance of RequestNetworkData with specific parameters.
		/// </summary>
		/// <param name="type">The type of the request. Whether it's requesting data or NetworkId or something else.</param>
		public RequestNetworkData(RequestNetworkDataType type)
			: this(type, "")
		{
		}

		/// <summary>
		/// Initialise a new instance of RequestNetworkData with specific parameters.
		/// </summary>
		/// <param name="type">The type of the request. Whether it's requesting data or NetworkId or something else.</param>
		/// <param name="data">The Network data to submit with the Request.</param>
		public RequestNetworkData(RequestNetworkDataType type, INetworkData data)
			: this(type, "")
		{
			_data = data;
		}
		/// <summary>
		/// Initialise a new instance of RequestNetworkData with specific parameters.
		/// </summary>
		/// <param name="type">The type of the request. Whether it's requesting data or NetworkId or something else.</param>
		/// <param name="networkId">NetworkId to transmit with the Request.</param>
		public RequestNetworkData(RequestNetworkDataType type, string networkId)
		{
			_type = type;
			_name = networkId;
		}

		/// <summary>
		/// Get or set the type of the Request.
		/// </summary>
		public RequestNetworkDataType RequestType
		{
			get { return _type; }
			set { _type = value; }
		}

		/// <summary>
		/// Get or set the internal name of this request. This identifier is used by the one ending the request
		/// so it can identify which request it is.
		/// </summary>
		public string InternalName
		{
			get { return _internalName; }
			set { _internalName = value; }
		}

		/// <summary>
		/// Get or set the NetworkId to transmit with the Request.
		/// </summary>
		public string NetworkId
		{
			get { return _name; }
			set { _name = value; }
		}

		/// <summary>
		/// Get or set the target for this network request. This property will not be serialised.
		/// </summary>
		[System.Xml.Serialization.XmlIgnore]
		public object Target
		{
			get { return _target; }
			set { _target = value; }
		}

		/// <summary>
		/// Get or set the type name of the class. This is usually used in conjuction when the Request type is
		/// RequestNetworkDataType.RequestType and is usd when requesting a specific object of the type specified
		/// from the host.
		/// </summary>
		public string ClassTypeName
		{
			get { return _typeName; }
			set { _typeName = value; }
		}

		/// <summary>
		/// Get or set the data to transmit with the Request. Usually used when sending a response from a request received.
		/// </summary>
		public INetworkData Data
		{
			get { return _data; }
			set { _data = value; }
		}
	}
}
