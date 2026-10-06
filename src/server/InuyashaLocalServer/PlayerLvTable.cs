using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace InuyashaLocalServer;

public static class PlayerLvTable
{
	private class Row
	{
		public int level;

		public int needExp;

		public int expTotal;

		public int heroCap;
	}

	private static Dictionary<int, Row> _byLv;

	private static List<Row> _rows;

	public static int MaxLevel
	{
		get
		{
			Ensure();
			return _rows[_rows.Count - 1].level;
		}
	}

	private static void Ensure()
	{
		if (_rows != null)
		{
			return;
		}
		Dictionary<int, Row> dictionary = new Dictionary<int, Row>();
		List<Row> list = new List<Row>();
		using (BinaryReader binaryReader = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("PlayerLevel.bin") ?? throw new Exception("PlayerLevel.bin embedded resource missing")))
		{
			int num = binaryReader.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				ReadLenStr(binaryReader);
				int num2 = binaryReader.ReadInt32();
				int needExp = binaryReader.ReadInt32();
				int expTotal = binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				binaryReader.ReadInt32();
				int heroCap = binaryReader.ReadInt32();
				ReadLenStr(binaryReader);
				ReadLenStr(binaryReader);
				Row item = (dictionary[num2] = new Row
				{
					level = num2,
					needExp = needExp,
					expTotal = expTotal,
					heroCap = heroCap
				});
				list.Add(item);
			}
		}
		list.Sort((Row a, Row b) => a.level.CompareTo(b.level));
		_byLv = dictionary;
		_rows = list;
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

	public static int LevelFromExp(long exp)
	{
		Ensure();
		int result = 1;
		for (int i = 0; i < _rows.Count; i++)
		{
			Row row = _rows[i];
			if (row.expTotal > exp)
			{
				break;
			}
			result = row.level;
		}
		return result;
	}

	public static int GetExpTotal(int lv)
	{
		Ensure();
		if (_byLv.TryGetValue(lv, out var value))
		{
			return value.expTotal;
		}
		return 0;
	}

	public static int GetNeedExp(int lv)
	{
		Ensure();
		if (_byLv.TryGetValue(lv, out var value))
		{
			return value.needExp;
		}
		return 0;
	}

	public static int GetHeroCap(int lv)
	{
		Ensure();
		if (_byLv.TryGetValue(lv, out var value))
		{
			return value.heroCap;
		}
		return lv;
	}
}
