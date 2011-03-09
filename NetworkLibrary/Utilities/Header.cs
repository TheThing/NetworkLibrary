using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NetworkLibrary.Utilities
{
	public class Header
	{
		private static Dictionary<Type, short> _headerType;

		private string _name;
		private object _value;
		private byte[] _cache;
		private bool _isCached;

		public Header()
			: this(null)
		{
		}

		public Header(string name)
			: this(name, null)
		{
		}

		public Header(string name, object value)
		{
			_isCached = false;
			_name = name;
			if (_name != null)
				if (_name.Contains(':'))
					throw new FormatException("Header name cannot contain the letter ':'");

			_value = value;

			if (_value != null)
				ConvertToRaw();
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public object Value
		{
			get { return _value; }
			set { _value = value; }
		}

		public byte[] Cache
		{
			get { return _cache; }
		}

		public void AssignNewValue(object value)
		{
			_value = value;
			if (_value != null)
				ConvertToRaw();
		}

		private void ConvertToRaw(MemoryStream m)
		{
			Encoding utf8 = Encoding.UTF8;
			byte[] holder = utf8.GetBytes(_name);
			m.WriteByte(Convert.ToByte(holder.Length));
			m.WriteByte(Convert.ToByte(_name.Length));
			m.Write(holder, 0, holder.Length);
			m.WriteByte(Convert.ToByte(_headerType[_value.GetType()]));

			short typeId = 0;
			if (_headerType.TryGetValue(_value.GetType(), out typeId))
			{
				switch (typeId)
				{
					case 1: //Bool
						holder = BitConverter.GetBytes((bool)_value);
						break;
					case 2: //Byte
						holder = new byte[] { (byte)_value };
						break;
					case 3: //Short
						holder = BitConverter.GetBytes((short)_value); 
						break;
					case 4: //uShort
						holder = BitConverter.GetBytes((ushort)_value);
						break;
					case 5: //Int
						holder = BitConverter.GetBytes((int)_value);
						break;
					case 6: //uInt
						holder = BitConverter.GetBytes((uint)_value);
						break;
					case 7: //Long
						holder = BitConverter.GetBytes((long)_value);
						break;
					case 8: //uLong
						holder = BitConverter.GetBytes((ulong)_value);
						break;
					case 9: //Float
						holder = BitConverter.GetBytes((float)_value);
						break;
					case 10: //Double
						holder = BitConverter.GetBytes((double)_value);
						break;
					case 20: //string
						holder = utf8.GetBytes((string)_value);
						m.WriteByte(Convert.ToByte(holder.Length));
						m.WriteByte(Convert.ToByte((_value as string).Length));
						break;
				}
				if (typeId < 20)
					holder = holder.Reverse().ToArray();
				m.Write(holder, 0, holder.Length);
			}
			else
				throw new FormatException(string.Format("Value of a header is of a type {0} which is unsupported. Header can only contain basic values!", _value.GetType().FullName));

			m.Flush();
		}

		public byte[] ConvertToRaw()
		{
			if (_headerType == null)
				CreateTypeHeader();
			if (_isCached)
				return _cache;
			_cache = new byte[128];

			long length = 0;
			using (MemoryStream m = new MemoryStream(_cache))
            {
				ConvertToRaw(m);
				length = m.Position;
            }
			Array.Resize(ref _cache, Convert.ToInt32(length));
			_isCached = true;
			return Cache;
		}

		public void AssignValueFromStream(MemoryStream memStream)
		{
			int typeb = memStream.ReadByte();
			byte[] holder;
			switch (typeb)
			{
				case 1: //Bool
					_value = memStream.ReadByte() == 1;
					break;
				case 2: //Byte
					_value = memStream.ReadByte();
					break;
				case 3: //Short
					holder = new byte[sizeof(short)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToInt16(holder.Reverse().ToArray(), 0);
					break;
				case 4: //uShort
					holder = new byte[sizeof(ushort)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToUInt16(holder.Reverse().ToArray(), 0);
					break;
				case 5: //Int
					holder = new byte[sizeof(int)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToInt32(holder.Reverse().ToArray(), 0);
					break;
				case 6: //uInt
					holder = new byte[sizeof(uint)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToUInt32(holder.Reverse().ToArray(), 0);
					break;
				case 7: //Long
					holder = new byte[sizeof(long)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToInt64(holder.Reverse().ToArray(), 0);
					break;
				case 8: //uLong
					holder = new byte[sizeof(ulong)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToUInt64(holder.Reverse().ToArray(), 0);
					break;
				case 9: //Float
					holder = new byte[sizeof(float)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToSingle(holder.Reverse().ToArray(), 0);
					break;
				case 10: //Double
					holder = new byte[sizeof(double)];
					memStream.Read(holder, 0, holder.Length);
					_value = BitConverter.ToDouble(holder.Reverse().ToArray(), 0);
					break;
				case 20: //string
					holder = new byte[memStream.ReadByte()];
					int length = memStream.ReadByte();
					char[] rawText = new char[length];
					memStream.Read(holder, 0, holder.Length);
					int outLen = Encoding.UTF8.GetDecoder().GetChars(holder, 0, length, rawText, 0);
					_value = new string(rawText, 0, outLen);
					break;
			}
		}

		private static void CreateTypeHeader()
		{
			_headerType = new Dictionary<Type, short>(12);
			_headerType.Add(typeof(bool), 1);
			_headerType.Add(typeof(byte), 2);
			_headerType.Add(typeof(short), 3);
			_headerType.Add(typeof(ushort), 4);
			_headerType.Add(typeof(int), 5);
			_headerType.Add(typeof(uint), 6);
			_headerType.Add(typeof(long), 7);
			_headerType.Add(typeof(ulong), 8);
			_headerType.Add(typeof(float), 9);
			_headerType.Add(typeof(double), 10);
			_headerType.Add(typeof(string), 20);
		}

	}
}
