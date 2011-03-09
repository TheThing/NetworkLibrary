using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using NetworkPluginManager.Exceptions;

namespace NetworkLibrary.Exceptions
{
	public delegate void delegateException(object source, Exception exception);
	public delegate void delegateWarning(object source, Warning warning);
	public delegate void delegateNotification(object source, string message);

	/// <summary>
	/// Public interface that specifies event callbacks for exeptions and such. Allows an exception to bubble up
	/// the application.
	/// </summary>
	public interface IExceptionHandler
	{
		/// <summary>
		/// Occurs whenever an exception occurs in the network library or the network plugin.
		/// </summary>
		event delegateException OnExceptionOccured;
		/// <summary>
		/// Occurs whenever a warning is unhandled in the network library or the network plugin. This
		/// generally is a message to the developer about some of the internal happenings in the plugin.
		/// </summary>
		event delegateWarning OnWarningOccured;
		/// <summary>
		/// Occurs whenever the network library or network plugin have a message to appear, like when
		/// the client is disconnected or something.
		/// </summary>
		event delegateNotification OnNotificationOccured;
	}
}
