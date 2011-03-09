using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;

namespace NetworkLibrary.Exceptions
{
	public class PacketParsingException : Exception
	{
		public PacketParsingException(string message, NetworkPacket packet)
			: this(message, packet, null)
		{
		}

		public PacketParsingException(string message, NetworkPacket packet, Exception e)
			: base(message, e)
		{
			_packet = packet;
		}

		private NetworkPacket _packet;

		public NetworkPacket Packet
		{
			get { return _packet; }
		}

		public override string ToString()
		{
			return string.Format("{0}. Packet: Id = '{1}' Message = '{2}'", base.Message, _packet.Id, _packet.Message);
		}
	}
}
