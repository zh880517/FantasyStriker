using System;
using System.IO;

namespace Net
{
	public enum ParserState
	{
		PacketSize,
		PacketBody
	}
	
	public static class Packet
	{
		public const int PacketSizeLength2 = 2;
		public const int PacketSizeLength4 = 4;
		public const int MinPacketSize = 2;
		public const int OpcodeIndex = 0;
		public const int MessageIndex = 2;
	}

	public class PacketParser
	{
		private readonly CircularBuffer buffer;
		private int packetSize;
		private ParserState state;
		public MemoryStream memoryStream;
		private bool isOK;
		private readonly int packetSizeLength;

		public PacketParser(int packetSizeLength, CircularBuffer buffer, MemoryStream memoryStream)
		{
            this.packetSizeLength = packetSizeLength;
            this.buffer = buffer;
            this.memoryStream = memoryStream;
		}

		public bool Parse()
		{
			if (isOK)
			{
				return true;
			}

			bool finish = false;
			while (!finish)
			{
				switch (state)
				{
					case ParserState.PacketSize:
						if (buffer.Length < packetSizeLength)
						{
							finish = true;
						}
						else
						{
							buffer.Read(memoryStream.GetBuffer(), 0, packetSizeLength);
							
							switch (packetSizeLength)
							{
								case Packet.PacketSizeLength4:
									packetSize = BitConverter.ToInt32(memoryStream.GetBuffer(), 0);
									if (packetSize > ushort.MaxValue * 16 || packetSize < Packet.MinPacketSize)
									{
										throw new Exception($"recv packet size error, 可能是外网探测端口: {packetSize}");
									}
									break;
								case Packet.PacketSizeLength2:
									packetSize = BitConverter.ToUInt16(memoryStream.GetBuffer(), 0);
									if (packetSize > ushort.MaxValue || packetSize < Packet.MinPacketSize)
									{
										throw new Exception($"recv packet size error:, 可能是外网探测端口: {packetSize}");
									}
									break;
								default:
									throw new Exception("packet size byte count must be 2 or 4!");
							}
							state = ParserState.PacketBody;
						}
						break;
					case ParserState.PacketBody:
						if (buffer.Length < packetSize)
						{
							finish = true;
						}
						else
						{
							memoryStream.Seek(0, SeekOrigin.Begin);
							memoryStream.SetLength(packetSize);
							byte[] bytes = memoryStream.GetBuffer();
							buffer.Read(bytes, 0, packetSize);
							isOK = true;
							state = ParserState.PacketSize;
							finish = true;
						}
						break;
				}
			}
			return isOK;
		}

		public MemoryStream GetPacket()
		{
			isOK = false;
			return memoryStream;
		}
	}
}