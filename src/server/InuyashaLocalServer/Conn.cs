using System.Net.Sockets;

namespace InuyashaLocalServer;

internal sealed class Conn
{
	public Socket Sock;

	public readonly object SendLock = new object();

	public Session Sess = new Session();

	public byte[] Buf = new byte[16384];

	public int Len;

	public string Peer = "";
}
