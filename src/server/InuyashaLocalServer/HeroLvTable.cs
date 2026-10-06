using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace InuyashaLocalServer;

public static class HeroLvTable
{
	private static Dictionary<int, Dictionary<int, int>> _map;

	private static int _maxLv = 150;

	public static int MaxLevel
	{
		get
		{
			Ensure();
			return _maxLv;
		}
	}

	private static void Ensure()
	{
		if (_map != null)
		{
			return;
		}
		Dictionary<int, Dictionary<int, int>> dictionary = new Dictionary<int, Dictionary<int, int>>();
		using (BinaryReader binaryReader = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("heroLvUpConfig.bin") ?? throw new Exception("heroLvUpConfig.bin embedded resource missing")))
		{
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ReadLenStr(binaryReader);
				int key = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				int num2 = binaryReader.ReadInt32();
				Dictionary<int, int> value = ParseAttrs(ReadLenStr(binaryReader));
				dictionary[key] = value;
				if (num2 > _maxLv)
				{
					_maxLv = num2;
				}
			}
		}
		_map = dictionary;
	}

	private static string ReadLenStr(BinaryReader br)
	{
		int num = 0;
		int num2 = 0;
		while (true)
		{
			byte b = br.ReadByte();
			num2 |= (b & 0x7F) << num;
			if ((b & 0x80) == 0)
			{
				break;
			}
			num += 7;
		}
		byte[] bytes = br.ReadBytes(num2);
		return Encoding.UTF8.GetString(bytes);
	}

	private static Dictionary<int, int> ParseAttrs(string att)
	{
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		if (string.IsNullOrEmpty(att))
		{
			return dictionary;
		}
		string[] array = att.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length != 0)
			{
				int num = text.IndexOf('*');
				if (num > 0 && int.TryParse(text.Substring(0, num), out var result) && int.TryParse(text.Substring(num + 1), out var result2))
				{
					dictionary[result] = result2;
				}
			}
		}
		return dictionary;
	}

	public static Dictionary<int, int> GetBase(int heroId, int playerLv)
	{
		Ensure();
		if (_map.TryGetValue(heroId * 1000 + playerLv, out var value))
		{
			return new Dictionary<int, int>(value);
		}
		int num = ((playerLv < 1) ? 1 : ((playerLv > _maxLv) ? _maxLv : playerLv));
		if (_map.TryGetValue(heroId * 1000 + num, out value))
		{
			return new Dictionary<int, int>(value);
		}
		return new Dictionary<int, int>();
	}

	public static int GetAttr(int heroId, int playerLv, int type, int fallback)
	{
		Ensure();
		if (_map.TryGetValue(heroId * 1000 + playerLv, out var value))
		{
			if (!value.TryGetValue(type, out var value2))
			{
				return fallback;
			}
			return value2;
		}
		return fallback;
	}
}
