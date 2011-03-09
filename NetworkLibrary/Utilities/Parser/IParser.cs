using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;
using NetworkLibrary.Exceptions;

namespace NetworkLibrary.Utilities.Parser
{
	/// <summary>
	/// A public interface to parse specifically some packet code packets.
	/// </summary>
	public interface IParser : IExceptionHandler
	{
		/// <summary>
		/// Parse an object to it's relevant string for an outgoing packet.
		/// </summary>
		/// <param name="item">The item to parse.</param>
		/// <param name="arguments">Arguments to pass to the parser.</param>
		/// <returns>The object parsed.</returns>
		string ParseObject(object item, object[] arguments);
		/// <summary>
		/// Parse an incoming string from an incoming packet and return a specialised NetworkEventArgs.
		/// </summary>
		/// <param name="item">The packet to parse.</param>
		/// <returns>Specialised NetworkEventArgs.</returns>
		NetworkEventArgs ParsePacket(NetworkPacket packet);
	}
}