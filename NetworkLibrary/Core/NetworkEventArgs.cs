using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using NetworkLibrary.Utilities;
using NetworkLibrary.Exceptions;
using StructureMap;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// A NetworkEvent class used when calling any event registered in the NetworkLibrary.
	/// </summary>
	public class NetworkEventArgs
	{
		private NetworkPacket _packet;
		private string _networkDataId;
		private string _propertyName;
		private object _data;
		private bool _handled;
		private NetworkEventType _eventType;

		/// <summary>
		/// Initialise a new instance of NetworkEvent with specified parameters.
		/// </summary>
		/// <param name="packet">The original packet raw.</param>
		public NetworkEventArgs(NetworkPacket packet)
		{
			_handled = false;
			_packet = packet;
		}

		/// <summary>
		/// Forwards the contents of this package to the rest of the clients if the connection is host. Otherwise
		/// this does nothing.
		/// </summary>
		public void Forward()
		{
			//Check to see if this really is a packet that needs forwarding or not.
			if (this.SourceConnection != null)
			{
				//This packet has a source so grab our network configuration and info
				INetwork network = ObjectFactory.GetInstance<INetwork>();

				//Check to see if our connection is host.
				if (network.NetworkType == NetworkType.Host)
				{
					//The connection is host which means we want to forward this package
					//to the rest of the clients that are connected.
					network.SendEvent(this._packet, false, this._packet.Source);
				}
			}
		}

		/// <summary>
		/// Send reply event to the sender of this packet. This is a shortcut to the
		/// INetwork.SendEvent method.
		/// </summary>
		public void SendReply(int code, object data)
		{
			//Grab our network configuration and info
			INetwork network = ObjectFactory.GetInstance<INetwork>();

			//Send our packet to a single source
			network.SendSingleEvent(code, data, _packet.Source); 
		}

		/// <summary>
		/// Returns the raw source of the packet receaved.
		/// </summary>
		public NetworkPacket BasePacket
		{
			get { return _packet; }
		}

		/// <summary>
		/// Get or set the type of this event.
		/// </summary>
		public NetworkEventType EventType
		{
			get { return _eventType; }
			set { _eventType = value; }
		}

		/// <summary>
		/// Get or set the object or value that was sent with the packet if one was send.
		/// If the packet was a property changed or collection changed, this will
		/// contain the relevant value.
		/// </summary>
		public object Data
		{
			get { return _data; }
			set { _data = value; }
		}

		/// <summary>
		/// Get or set the name id of the target registered network object. This contains the
		/// id of the object if the source was from a property and collection changed. otherwise
		/// this will return null.
		/// </summary>
		public string NetworkDataId
		{
			get { return _networkDataId; }
			set { _networkDataId = value; }
		}

		/// <summary>
		/// Get or set the name of the property that was changed if the packet type is from a property changing.
		/// Otherwise this will return null.
		/// </summary>
		public string PropertyName
		{
			get { return _propertyName; }
			set { _propertyName = value; }
		}

		/// <summary>
		/// Returns the id of the packet.
		/// </summary>
		public int PacketCode
		{
			get { return _packet.Id; }
		}

		/// <summary>
		/// Returns the connection source where the packet was sent from.
		/// </summary>
		public object SourceConnection
		{
			get { return _packet.Source; }
		}

		/// <summary>
		/// Get or set whether this event has been handled.
		/// </summary>
		public bool Handled
		{
			get { return _handled; }
			set { _handled = value; }
		}

		/// <summary>
		/// Returns the type of the data or value being sent.
		/// </summary>
		public Type DataType
		{
			get { return _data.GetType(); }
		}
	}
}
