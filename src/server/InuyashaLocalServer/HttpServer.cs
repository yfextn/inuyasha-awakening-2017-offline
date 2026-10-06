using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace InuyashaLocalServer;

internal static class HttpServer
{
	internal static class J
	{
		public static string Esc(string s)
		{
			if (s == null)
			{
				return "\"\"";
			}
			StringBuilder stringBuilder = new StringBuilder(s.Length + 2);
			stringBuilder.Append('"');
			foreach (char c in s)
			{
				switch (c)
				{
				case '"':
					stringBuilder.Append("\\\"");
					continue;
				case '\\':
					stringBuilder.Append("\\\\");
					continue;
				case '\n':
					stringBuilder.Append("\\n");
					continue;
				case '\r':
					stringBuilder.Append("\\r");
					continue;
				case '\t':
					stringBuilder.Append("\\t");
					continue;
				case '\b':
					stringBuilder.Append("\\b");
					continue;
				case '\f':
					stringBuilder.Append("\\f");
					continue;
				}
				if (c < ' ')
				{
					StringBuilder stringBuilder2 = stringBuilder.Append("\\u");
					int num = c;
					stringBuilder2.Append(num.ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			stringBuilder.Append('"');
			return stringBuilder.ToString();
		}

		public static string Num(long v)
		{
			return v.ToString(CultureInfo.InvariantCulture);
		}

		public static string Num(int v)
		{
			return v.ToString(CultureInfo.InvariantCulture);
		}

		public static string Bool(bool v)
		{
			if (!v)
			{
				return "false";
			}
			return "true";
		}

		/// <summary>
		/// host 必须是「玩家自己能连到的地址」。这里按连接来源自动决定：
		///   客户端从 127.0.0.1 过来（模拟器与游戏同机）      -> 127.0.0.1
		///   客户端从局域网 IP 过来（手机 / 另一台机器）      -> 电脑的局域网 IP（C.LanIp）
		/// 这样同一个 APK 既能本机模拟器跑，也能丢给局域网里的设备跑，
		/// 不需要为两种玩法打两个包（早期版本只发固定 IP，模拟器就会「连接超时」）。
		/// </summary>
		public static string ServerInfo(int sid, string sourceIp)
		{
			string host = PickHost(sourceIp);
			return "{\"id\":" + Num(sid) + ",\"name\":" + Esc("本地服务器" + sid) + ",\"host\":" + Esc(host) + ",\"port\":" + Num(C.TcpPort) + ",\"status\":2,\"power\":0,\"area\":" + Esc("本地") + ",\"recommended\":" + Bool(sid == 1) + "}";
		}

		public static string ServerInfo(int sid)
		{
			return ServerInfo(sid, null);
		}

		/// <summary>把客户端来源地址映射成它对服务端可达的地址。</summary>
		public static string PickHost(string sourceIp)
		{
			if (string.IsNullOrEmpty(sourceIp))
			{
				return C.ListenAll ? C.LanIp : "127.0.0.1";
			}
			// 回环（含 127.x）说明客户端与游戏在同一台机器上（雷电模拟器走共享回环）
			if (sourceIp.StartsWith("127.") || sourceIp == "::1")
			{
				return "127.0.0.1";
			}
			// 局域网/其它机器：用我方局域网地址（客户端能连到它）
			return C.ListenAll ? C.LanIp : sourceIp;
		}

		public static string RoleJson(Role r)
		{
			return "{\"serverId\":" + Num(r.ServerId) + ",\"nick\":" + Esc(r.Nick) + ",\"gender\":" + Num(r.Gender) + ",\"level\":" + Num(r.Level) + ",\"avatar\":0,\"avatarFrame\":0,\"vip\":0,\"power\":0,\"logoutTime\":" + Num(r.LogoutTime) + "}";
		}
	}

	private const string QycLogin = "{\"errno\":0,\"errmsg\":\"\",\"title\":\"提示\",\"isforce\":0,\"updateurl\":\"\",\"is_update_by_self\":0,\"open_type\":0,\"vname\":\"1.9.00\",\"vcode\":1900,\"data\":{\"uid\":\"16013096\",\"puid\":\"16013096\",\"pfid\":\"heitao\",\"token\":\"46d9cc8e8a8ebdc3a5c7e4c10a907edc\",\"custom\":\"\",\"anti_addiction\":0,\"sdk\":{}}}";

	public static void Loop()
	{
		TcpListener tcpListener = null;
		string bindDesc = C.ListenAll ? ("0.0.0.0:" + C.HttpPort + "（局域网，对外 " + C.AdvertiseHost + "）")
		                            : ("127.0.0.1:" + C.HttpPort);
		try
		{
			tcpListener = new TcpListener(C.ListenAddress, C.HttpPort);
			tcpListener.Start();
			L.Log("[HTTP] 监听 " + bindDesc + " （/health /register /login /servers /version/check2 /qyc/*）");
		}
		catch (Exception e)
		{
			L.Err("HTTP 监听 " + bindDesc + " 失败", e);
			return;
		}
		while (true)
		{
			Socket socket = null;
			try
			{
				socket = tcpListener.AcceptSocket();
			}
			catch (Exception e2)
			{
				L.Err("HTTP accept", e2);
				Thread.Sleep(200);
				continue;
			}
			Socket s = socket;
			Thread thread = new Thread((ThreadStart)delegate
			{
				Handle(s);
			});
			thread.IsBackground = true;
			thread.Start();
		}
	}

	private static int FindHeaderEnd(byte[] b, int len)
	{
		for (int i = 0; i + 3 < len; i++)
		{
			if (b[i] == 13 && b[i + 1] == 10 && b[i + 2] == 13 && b[i + 3] == 10)
			{
				return i;
			}
		}
		return -1;
	}

	private static byte[] Sub(byte[] b, int start, int len)
	{
		if (len <= 0)
		{
			return new byte[0];
		}
		byte[] array = new byte[len];
		Array.Copy(b, start, array, 0, len);
		return array;
	}

	private static int HexVal(byte c)
	{
		if (c >= 48 && c <= 57)
		{
			return c - 48;
		}
		if (c >= 97 && c <= 102)
		{
			return c - 97 + 10;
		}
		if (c >= 65 && c <= 70)
		{
			return c - 65 + 10;
		}
		return -1;
	}

	private static byte[] PercentDecode(byte[] b)
	{
		List<byte> list = new List<byte>(b.Length);
		for (int i = 0; i < b.Length; i++)
		{
			if (b[i] == 37 && i + 2 < b.Length)
			{
				int num = HexVal(b[i + 1]);
				int num2 = HexVal(b[i + 2]);
				if (num >= 0 && num2 >= 0)
				{
					list.Add((byte)((num << 4) | num2));
					i += 2;
					continue;
				}
			}
			list.Add(b[i]);
		}
		return list.ToArray();
	}

	private static string Utf8(byte[] b)
	{
		return Encoding.UTF8.GetString(b);
	}

	private static void DecodeQuery(byte[] raw, out Dictionary<string, string> pars, out string basePath)
	{
		pars = new Dictionary<string, string>();
		int num = -1;
		for (int i = 0; i < raw.Length; i++)
		{
			if (raw[i] == 63)
			{
				num = i;
				break;
			}
		}
		byte[] b = ((num < 0) ? raw : Sub(raw, 0, num));
		byte[] array = ((num < 0) ? new byte[0] : Sub(raw, num + 1, raw.Length - num - 1));
		basePath = Utf8(PercentDecode(b));
		int num2 = 0;
		while (num2 <= array.Length)
		{
			int num3 = -1;
			for (int j = num2; j < array.Length; j++)
			{
				if (array[j] == 38)
				{
					num3 = j;
					break;
				}
			}
			int num4 = ((num3 < 0) ? array.Length : num3);
			if (num4 > num2)
			{
				int num5 = -1;
				for (int k = num2; k < num4; k++)
				{
					if (array[k] == 61)
					{
						num5 = k;
						break;
					}
				}
				byte[] b2 = ((num5 < 0) ? Sub(array, num2, num4 - num2) : Sub(array, num2, num5 - num2));
				byte[] b3 = ((num5 < 0) ? new byte[0] : Sub(array, num5 + 1, num4 - num5 - 1));
				pars[Utf8(PercentDecode(b2))] = Utf8(PercentDecode(b3));
			}
			if (num3 >= 0)
			{
				num2 = num3 + 1;
				continue;
			}
			break;
		}
	}

	private static string Md5Hex(string s)
	{
		using MD5 mD = MD5.Create();
		byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes(s));
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2", CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}

	private static string CalcSign(Dictionary<string, string> pars)
	{
		List<string> list = new List<string>(pars.Keys);
		list.Sort(StringComparer.Ordinal);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			if (!(text == "sign") && !(text == "sign_return"))
			{
				string text2 = pars[text];
				switch (text2)
				{
				case null:
				case "0":
				case "":
					continue;
				}
				stringBuilder.Append(text2).Append('#');
			}
		}
		stringBuilder.Append("Shanghai XiaYu Game Co.Ltd.");
		return Md5Hex(stringBuilder.ToString());
	}

	private static void Json(Socket sock, string body)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(body);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("HTTP/1.1 200 OK\r\n");
		stringBuilder.Append("Content-Type: application/json; charset=utf-8\r\n");
		stringBuilder.Append("Content-Length: ").Append(bytes.Length.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
		stringBuilder.Append("Connection: close\r\n\r\n");
		byte[] bytes2 = Encoding.ASCII.GetBytes(stringBuilder.ToString());
		try
		{
			sock.Send(bytes2);
			if (bytes.Length != 0)
			{
				sock.Send(bytes);
			}
		}
		catch (Exception e)
		{
			L.Err("HTTP 回写", e);
		}
	}

	private static void Handle(Socket sock)
	{
		try
		{
			try
			{
				sock.ReceiveTimeout = 5000;
			}
			catch
			{
			}
			byte[] array = new byte[16384];
			int num = 0;
			int num2 = -1;
			while (num < array.Length)
			{
				int num3;
				try
				{
					num3 = sock.Receive(array, num, array.Length - num, SocketFlags.None);
				}
				catch (Exception)
				{
					return;
				}
				if (num3 <= 0)
				{
					return;
				}
				num += num3;
				num2 = FindHeaderEnd(array, num);
				if (num2 >= 0)
				{
					break;
				}
			}
			if (num2 < 0)
			{
				return;
			}
			int num4 = -1;
			for (int i = 0; i + 1 < num; i++)
			{
				if (array[i] == 13 && array[i + 1] == 10)
				{
					num4 = i;
					break;
				}
			}
			if (num4 <= 0)
			{
				return;
			}
			int num5 = -1;
			int num6 = -1;
			for (int j = 0; j < num4; j++)
			{
				if (array[j] == 32)
				{
					if (num5 >= 0)
					{
						num6 = j;
						break;
					}
					num5 = j;
				}
			}
			if (num5 < 0)
			{
				return;
			}
			if (num6 < 0)
			{
				num6 = num4;
			}
			string text = Encoding.ASCII.GetString(array, 0, num5);
			byte[] raw = Sub(array, num5 + 1, num6 - num5 - 1);
			if (text == "POST")
			{
				int result = 0;
				string text2 = Encoding.ASCII.GetString(array, 0, num2);
				int num7 = text2.ToLowerInvariant().IndexOf("content-length:");
				if (num7 >= 0)
				{
					int num8 = text2.IndexOf('\r', num7);
					int.TryParse(((num8 < 0) ? text2.Substring(num7 + 15) : text2.Substring(num7 + 15, num8 - num7 - 15)).Trim(), out result);
				}
				int num9 = num - (num2 + 4);
				int num10 = result - num9;
				while (num10 > 0)
				{
					int num11;
					try
					{
						num11 = sock.Receive(array, 0, Math.Min(array.Length, num10), SocketFlags.None);
					}
					catch
					{
						break;
					}
					if (num11 <= 0)
					{
						break;
					}
					num10 -= num11;
				}
			}
			DecodeQuery(raw, out var pars, out var basePath);
			string text3 = basePath;
			if (text3.StartsWith("/auth/"))
			{
				text3 = text3.Substring(5);
			}
			else if (text3 == "/auth")
			{
				text3 = "/";
			}
			string text4 = "null";
			if (pars.ContainsKey("sign"))
			{
				text4 = ((CalcSign(pars) == pars["sign"]) ? "true" : "false");
			}
			L.Log("[HTTP] " + text3 + " " + ParamPreview(pars) + " sign_ok=" + text4);
			switch (text3)
			{
			case "/health":
			case "/":
				Json(sock, "{\"status\":\"ok\",\"server\":\"inuyasha-local\",\"version\":\"1.0\",\"advertise_host\":" + J.Esc(C.AdvertiseHost) + ",\"tcp_port\":" + J.Num(C.TcpPort) + "}");
				return;
			case "/qyc/login/hta":
				Json(sock, "{\"errno\":0,\"errmsg\":\"\",\"title\":\"提示\",\"isforce\":0,\"updateurl\":\"\",\"is_update_by_self\":0,\"open_type\":0,\"vname\":\"1.9.00\",\"vcode\":1900,\"data\":{\"uid\":\"16013096\",\"puid\":\"16013096\",\"pfid\":\"heitao\",\"token\":\"46d9cc8e8a8ebdc3a5c7e4c10a907edc\",\"custom\":\"\",\"anti_addiction\":0,\"sdk\":{}}}");
				return;
			case "/qyc/init/hta":
				Json(sock, "{\"errno\":0,\"errmsg\":\"\",\"title\":\"提示\",\"data\":{}}");
				return;
			}
			if (text3.StartsWith("/qyc/"))
			{
				Json(sock, "{\"errno\":0,\"errmsg\":\"\",\"title\":\"提示\",\"isforce\":0,\"updateurl\":\"\",\"data\":{}}");
				return;
			}
			switch (text3)
			{
			case "/version/check2":
				RouteVersion(sock, pars);
				break;
			case "/register":
				RouteRegister(sock, pars);
				break;
			case "/login":
				RouteLogin(sock, pars);
				break;
			case "/servers":
				RouteServers(sock, pars);
				break;
			default:
				L.Log("[HTTP] 未实现的路由 " + text3);
				Json(sock, "{\"ret\":1,\"msg\":" + J.Esc("未实现: " + text3) + "}");
				break;
			}
		}
		catch (Exception e)
		{
			L.Err("HTTP 处理", e);
		}
		finally
		{
			try
			{
				sock.Close();
			}
			catch
			{
			}
		}
	}

	private static string ParamPreview(Dictionary<string, string> pars)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> par in pars)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(par.Key).Append('=').Append((par.Value.Length > 40) ? par.Value.Substring(0, 40) : par.Value);
		}
		return stringBuilder.ToString();
	}

	private static string Get(Dictionary<string, string> p, string k)
	{
		if (!p.TryGetValue(k, out var value))
		{
			return null;
		}
		return value;
	}

	private static void RouteVersion(Socket sock, Dictionary<string, string> p)
	{
		string text = Get(p, "version");
		if (string.IsNullOrEmpty(text))
		{
			text = "2017.1.90";
		}
		Json(sock, "{\"ret\":0,\"data\":{\"effect\":\"\",\"diffs\":[],\"version\":" + J.Esc(text) + "}}");
	}

	private static void RouteRegister(Socket sock, Dictionary<string, string> p)
	{
		string text = Get(p, "name") ?? "";
		string password = Get(p, "password") ?? "";
		if (text.Length == 0)
		{
			Json(sock, "{\"ret\":1,\"msg\":" + J.Esc("账号为空") + "}");
			return;
		}
		string error;
		Account account = Store.Register(text, password, out error);
		if (error != null)
		{
			Json(sock, "{\"ret\":1,\"msg\":" + J.Esc(error) + "}");
			return;
		}
		Json(sock, "{\"ret\":0,\"msg\":\"\",\"data\":{\"id\":" + J.Num(account.Id) + ",\"name\":" + J.Esc(account.Name) + ",\"password\":" + J.Esc(account.Password) + "}}");
	}

	private static void RouteLogin(Socket sock, Dictionary<string, string> p)
	{
		string text = Get(p, "token");
		Account account = null;
		if (!string.IsNullOrEmpty(text))
		{
			string text2 = Get(p, "channelUserId") ?? "";
			string text3 = text2;
			if (text3.Length == 0)
			{
				text3 = "3rd_" + ((text.Length > 12) ? text.Substring(0, 12) : text);
			}
			account = Store.EnsureAccount(text3, "");
			L.Log("[HTTP] 第三方登录 channel=" + Get(p, "channel") + " channelUserId=" + text2 + " -> 账号 " + text3 + "(id=" + account.Id + ")");
		}
		else
		{
			account = Store.Login(Get(p, "name") ?? "", Get(p, "password") ?? "", out var error);
			if (error != null)
			{
				Json(sock, "{\"ret\":1,\"msg\":" + J.Esc(error) + ",\"data\":null}");
				return;
			}
		}
		int v = Math.Max(1, 10);
		Json(sock, "{\"ret\":0,\"msg\":\"\",\"data\":{\"recommended_server\":" + J.ServerInfo(1) + ",\"serverCount\":" + J.Num(v) + ",\"user_id\":" + J.Num(account.Id) + ",\"notice\":\"\",\"super\":false,\"maintaining_notice\":\"\"}}");
	}

	/// <summary>取对端 IP（失败返回空串，调用方会退回默认地址）。</summary>
	private static string SourceIp(Socket sock)
	{
		try
		{
			if (sock != null && sock.RemoteEndPoint is IPEndPoint)
			{
				return ((IPEndPoint)sock.RemoteEndPoint).Address.ToString();
			}
		}
		catch
		{
		}
		return null;
	}

	private static void RouteServers(Socket sock, Dictionary<string, string> p)
	{
		string srcIp = SourceIp(sock);
		string s = Get(p, "userId") ?? "0";
		int result = 0;
		int.TryParse(Get(p, "offset") ?? "0", out result);
		StringBuilder stringBuilder;
		if (result == 0)
		{
			long result2 = 0L;
			long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result2);
			Account account = Store.FindById(result2);
			stringBuilder = new StringBuilder();
			stringBuilder.Append("{\"ret\":0,\"msg\":\"\",\"data\":[");
			bool flag = true;
			if (account != null)
			{
				for (int i = 0; i < account.Roles.Count; i++)
				{
					if (!flag)
					{
						stringBuilder.Append(',');
					}
					flag = false;
					stringBuilder.Append("{\"role\":").Append(J.RoleJson(account.Roles[i])).Append(",\"server\":")
						.Append(J.ServerInfo(account.Roles[i].ServerId, srcIp))
						.Append('}');
				}
			}
			stringBuilder.Append("]}");
			Json(sock, stringBuilder.ToString());
			return;
		}
		int num = Math.Min(10, Math.Max(1, 10));
		stringBuilder = new StringBuilder();
		stringBuilder.Append("{\"ret\":0,\"msg\":\"\",\"data\":[");
		for (int j = 0; j < num; j++)
		{
			if (j > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(J.ServerInfo(result + j, srcIp));
		}
		stringBuilder.Append("]}");
		Json(sock, stringBuilder.ToString());
	}
}
