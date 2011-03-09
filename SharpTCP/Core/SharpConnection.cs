using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
//using NetworkPluginManager.Core;
using NetworkLibrary.Connection;
using NetworkLibrary.Exceptions;
using NetworkLibrary.Core;
using NetworkLibrary.Utilities;

namespace SharpTCP.Core
{
	public class SharpConnection : INetworkConnection
	{
		protected bool _disposed;
		protected Socket _connection;
		protected byte[] _buffer;
		protected int _maxPacketLength;
		protected List<NetworkPacket> _longPackets;
		public event delegateDisconnected OnDisconnected;
		public event delegatePacketRecieved OnPacketRecieved;
		public event delegateException OnExceptionOccured;
		public event delegateWarning OnWarningOccured;
		public event delegateNotification OnNotificationOccured;

		public SharpConnection()
		{
			_disposed = false;
			_longPackets = new List<NetworkPacket>();
			_connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_buffer = new byte[8192];
			_maxPacketLength = (_buffer.Length - sizeof(Int32) * 2 - HeaderCollection.HeaderLength);
		}

		/// <summary>
		/// The desctructor for Connection
		/// </summary>
		~SharpConnection()
		{
			//Check whether this object hasn't already been disposed
			if (_disposed)
				return;

			//Call Dispose but let it know it doesn't need to dispose managed resources
			Dispose(false);
		}

		/// <summary>
		/// Dispose this object and all data within.
		/// </summary>
		public void Dispose()
		{
			//Check whether this object hasn't already been disposed
			if (!_disposed)
			{
				//Call dispose and dispose all managed resources
				Dispose(true);

				//Suppress the GarbageCollector from calling the destroctur
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Dispose this object and if specified, all managed resources.
		/// </summary>
		/// <param name="disposeManagedResources">Specify whether managed resources need to be disposed or not</param>
		protected virtual void Dispose(bool disposeManagedResources)
		{
			//Check to see if this object hasn't been disposed already.
			if (_disposed)
				return;

			//Make sure dispose is not called again.
			_disposed = true;

			if (disposeManagedResources)
			{
				//Clear and null out all collections and variables
				//This is done to invalidate all methods in this class and break it.
				_longPackets.Clear();
				_longPackets = null;
				_buffer = null;
			}

			//Close the connection.
			_connection.Close();
		}

		/// <summary>
		/// Get whether the connection is up and active.
		/// </summary>
		public virtual bool Connected
		{
			get { return _connection.Connected; }
		}

		public void SendPacket(NetworkPacket packet, object target)
		{
			byte[] packetRaw = new byte[_buffer.Length];
			byte[] messageRaw = Encoding.UTF8.GetBytes(packet.Message);

			try
			{
				lock (_connection)
				{
					using (Stream m = new MemoryStream(packetRaw))
					{
						for (int i = 0; i < (messageRaw.Length / _maxPacketLength); i++)
						{
							m.Position = 0;
							m.Write(BitConverter.GetBytes(_maxPacketLength + sizeof(Int32) + HeaderCollection.HeaderLength).Reverse().ToArray(), 0, sizeof(Int32));
							m.Write(BitConverter.GetBytes((int)CorePacketCode.LongData).Reverse().ToArray(), 0, sizeof(Int32));
							m.Write(packet.Header.ConvertToRawHeader(), 0, HeaderCollection.HeaderLength);
							m.Write(messageRaw, _maxPacketLength * i, _maxPacketLength);
							m.Flush();
							SendRawPacket(target, packetRaw);
							System.Threading.Thread.Sleep(10);
						}
						m.Position = 0;
						m.Write(BitConverter.GetBytes(messageRaw.Length % _maxPacketLength + sizeof(Int32) + HeaderCollection.HeaderLength).Reverse().ToArray(), 0, sizeof(Int32));
						m.Write(BitConverter.GetBytes(packet.Id).Reverse().ToArray(), 0, sizeof(Int32));
						m.Write(packet.Header.ConvertToRawHeader(), 0, HeaderCollection.HeaderLength);
						m.Write(messageRaw, _maxPacketLength * (messageRaw.Length / _maxPacketLength), (messageRaw.Length % _maxPacketLength));
						m.Flush();
						SendRawPacket(target, packetRaw);
					}
				}
			}
			catch (Exception error)
			{
				ThrowException(error);
			}
		}

		protected void SendRawPacket(object target, byte[] contents)
		{
			try
			{
				if (target is Socket)
					(target as Socket).Send(contents);
				else
					_connection.Send(contents);
			}
			catch (Exception e)
			{
				if (target is Socket)
				{
					if (!(target as Socket).Connected)
					{
						if (OnDisconnected != null)
							OnDisconnected(target, e);
						(target as Socket).Close();
					}
					else
						ThrowException(e);
				}
				else if (!_connection.Connected)
				{
					if (OnDisconnected != null)
						OnDisconnected(target, e);
					_connection.Close();
				}
				else
					ThrowException(e);
			}
		}

		protected void OnDataReceive(IAsyncResult data)
		{
			Socket s = data.AsyncState as Socket;

			try
			{
				s.EndReceive(data);
			}
			catch (ObjectDisposedException)
			{
				//After a close has been called, beginreceive will receive it's last call
				//This is done to do all necessary cleanup. When this happens, a ObjectDisposedException
				//will be thrown by the EndReceive and we will ignore this since it's none of our business :)
				return;
			}
			catch (Exception error)
			{
				if (OnNotificationOccured != null)
					OnNotificationOccured(s, "Error while receaving data, message received: " + error.Message);
				if (s.Connected)
					s.Close();
				if (OnDisconnected != null)
					OnDisconnected(s, error);
				return;
			}

			int length = 0, codePacket = 0;
			string message = "";
			HeaderCollection h;

			try
			{
				byte[] reverse = _buffer.Reverse().ToArray();
				length = BitConverter.ToInt32(reverse, _buffer.Length - sizeof(Int32) - 0);
				if (length == 0)
				{
					if (OnNotificationOccured != null)
						OnNotificationOccured(_buffer, "An empty network packet was received and truncated.");
				}
				codePacket = BitConverter.ToInt32(reverse, _buffer.Length - sizeof(Int32) - 4);
				h = HeaderCollection.ConvertByteToCollection(_buffer, sizeof(int) * 2);
				char[] buffer = new char[_buffer.Length - 8 - HeaderCollection.HeaderLength + 1];
				int temp = Encoding.UTF8.GetDecoder().GetChars(_buffer, 8 + HeaderCollection.HeaderLength, length - sizeof(Int32) - HeaderCollection.HeaderLength, buffer, 0);
				message = new string(buffer, 0, temp);
			}
			catch (Exception e)
			{
				if (OnNotificationOccured != null)
					OnNotificationOccured(this, "A network packet of an unknown format was recieved and truncated. While formatting the following error occured: " + e.Message);
				OpenConnection(s);
				return;
			}

			if (!OpenConnection(s))
				return;

			NetworkPacket packet = new NetworkPacket(codePacket, message, s);
			packet.Header = h;
			if (packet.Id == (int)CorePacketCode.LongData)
				_longPackets.Add(packet);
			else if (OnPacketRecieved != null)
			{
				for (int i = _longPackets.Count - 1; i >= 0 ; i--)
					if (_longPackets[i].Source == packet.Source)
					{
						packet.Message = _longPackets[i].Message + packet.Message;
						_longPackets.Remove(_longPackets[i]);
					}
				OnPacketRecieved(this, packet);
			}
		}

		private bool OpenConnection(Socket s)
		{
			if (s.Connected)
			{
				try
				{
					s.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnDataReceive, s);
				}
				catch (Exception)
				{
					if (OnPacketRecieved != null)
						OnPacketRecieved(this, new NetworkPacket((int)CorePacketCode.Disconnected, null, s));
					return false;
				}
			}
			else
			{
				if (OnPacketRecieved != null)
					OnPacketRecieved(this, new NetworkPacket((int)CorePacketCode.Disconnected, null, s));
				return false;
			}
			return true;
		}
		protected void ThrowException(Exception e)
		{
			if (OnExceptionOccured != null)
				OnExceptionOccured(this, e);
			else
				throw e;
		}
		protected void ThrowWarning(Warning e)
		{
			if (OnWarningOccured != null)
				OnWarningOccured(this, e);
		}
	}
}
