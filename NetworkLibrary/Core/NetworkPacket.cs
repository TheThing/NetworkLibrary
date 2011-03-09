using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Utilities;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// Represent the packet that was recieved from or is about to be sent to the plugin.
	/// </summary>
	public class NetworkPacket
	{
		private int _id;
		private string _message;
		private object _source;
		private HeaderCollection _header;
		public static HeaderCollection DefaultHeader;

		/// <summary>
		/// Initialise a new instance of a network packet to send to the network connection plugin.
		/// </summary>
		public NetworkPacket()
		{
			if (DefaultHeader != null)
				_header = DefaultHeader;
			else
				_header = new HeaderCollection();
		}

		/// <summary>
		/// Initialise a new instance of a network packet to transmit over the network with a specified parameters.
		/// </summary>
		/// <param name="id">The id of the packet.</param>
		/// <param name="message">The message or the content of the packet.</param>
		/// <param name="source">The source of the packet if the packet was recieved.</param>
		public NetworkPacket(int id, string message, object source)
			: this()
		{
			_id = id;
			_message = message;
			_source = source;
		}

		/// <summary>
		/// Get or set the id of the packet.
		/// </summary>
		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		/// <summary>
		/// Get or set the message or the content of the packet.
		/// </summary>
		public string Message
		{
			get { return _message; }
			set { _message = value; }
		}

		[System.Xml.Serialization.XmlIgnoreAttribute]
		/// <summary>
		/// Get or set the source of the packet if the packet was recieved. Otherwise this is null.
		/// </summary>
		public object Source
		{
			get { return _source; }
			set { _source = value; }
		}

		/// <summary>
		/// Get or set the header of the packet.
		/// </summary>
		public HeaderCollection Header
		{
			get { return _header; }
			set { _header = value; }
		}
	}
}
