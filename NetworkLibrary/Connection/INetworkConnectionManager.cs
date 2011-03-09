using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using NetworkLibrary.Core;

namespace NetworkLibrary.Connection
{
	/// <summary>
	/// Provides methods and properties to communicate to a network connection plugin. Used to initiate a network connection instance.
	/// </summary>
	public interface INetworkConnectionManager : IDisposable
	{
		/// <summary>
		/// Get the name of the network connection plugin.
		/// </summary>
		string Name { get; }
		/// <summary>
		/// Get the description of the network connection plugin.
		/// </summary>
		string Description { get; }
		/// <summary>
		/// Get or set the path of the location of the plugin.
		/// </summary>
		string PathLocation { get; set; }
		/// <summary>
		/// Check whether the plugin has a settings dialog implemented to configure internal connection settings.
		/// </summary>
		bool HasSettingsDialog { get; }
		/// <summary>
		/// Show the settings dialog to manage internal plugin setings if one exists.
		/// </summary>
		void ShowSettingsDialog();
		/// <summary>
		/// Initiate a new instance of a network connection from the plugin.
		/// </summary>
		/// <param name="connectionType">The type of the connection to create, whether if the connection is a client or a host.</param>
		/// <returns>A new instance of a network connection from the plugin.</returns>
		INetworkConnection CreateConnection(NetworkType connectionType);
	}
}
