using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NetworkLibrary.Utilities
{
	public class HeaderCollection : List<Header>
	{
		public static int HeaderLength = 128;
		byte[] _converted;

		public HeaderCollection()
			: base()
		{
		}

		public HeaderCollection Clone()
		{
			HeaderCollection h = new HeaderCollection();
			h.AddRange(this);
			return h;
		}

		public bool TryGetValue(string name, out object value)
		{
			for (int i = 0; i < this.Count; i++)
			{
				if (this[i].Name == name)
				{
					value = this[i].Value;
					return true;
				}
			}
			value = null;
			return false;
		}

		public T GetValue<T>(string name)
		{
			for (int i = 0; i < this.Count; i++)
				if (this[i].Name == name)
					return (T)this[i].Value;
			return default(T);
		}

		public byte[] ConvertToRawHeader()
		{
			if (_converted != null)
				return _converted;

			_converted = new byte[HeaderLength];

			using (Stream m = new MemoryStream(_converted))
			{
				m.WriteByte(Convert.ToByte(this.Count));
				byte[] temp;
				for (int i = 0; i < this.Count; i++)
				{
					temp = this[i].ConvertToRaw();
					if (m.Position + temp.Length > _converted.Length)
						throw new ArgumentOutOfRangeException("HeaderCollection grew too long and exceeded the 128 byte boundary.");
					m.Write(temp, 0, temp.Length);
				}
				m.Flush();
			}
			
			return _converted;
		}

		public static HeaderCollection ConvertByteToCollection(byte[] rawHeader, int offset)
		{
			if (rawHeader.Length == 0)
				throw new ArgumentOutOfRangeException("Error while creating HeaderCollection from raw data. The raw data was empty");
			
			HeaderCollection h = new HeaderCollection();
			Decoder decode = Encoding.UTF8.GetDecoder();
			using (MemoryStream m = new MemoryStream(rawHeader, false))
			{
				m.Position = offset;
				int length = m.ReadByte();
				for (int i = 0; i < length; i++)
				{
					int nameLength = m.ReadByte();
					int charLength = m.ReadByte();
					byte[] name = new byte[nameLength];
					m.Read(name, 0, nameLength);
					char[] nameEnc = new char[charLength];
					int decodeGetChars = decode.GetChars(name, 0, nameLength, nameEnc, 0);
					Header head = new Header(new string(nameEnc, 0, decodeGetChars));
					head.AssignValueFromStream(m);
					h.Add(head);
				}
			}
			return h;
			
		}
	}
}
