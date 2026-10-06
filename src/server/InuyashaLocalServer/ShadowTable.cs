using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace InuyashaLocalServer
{
	/// <summary>
	/// 残影表 shadowConfig_D.bin（嵌入资源）。
	///
	/// 结构是**两层**的：外层 key = 残影组（1..4，也就是客户端 ShadowBase.id），
	/// 内层 key = 英雄号，每行给一个残影的 名字/属性/解锁材料/特效。
	///   Data2["1"]["101"] = 绯墨(hero 101 的第一档残影, needItems 空 = 默认拥有)
	///   Data2["2"]["101"] = 琥珀(需要 17000001*20 = 残影碎片x20)
	///
	/// 旧服务端 HShadowList 把残影 id 当成 heroId*100+i（10101/10102/10103），
	/// 跟客户端 shadowList[].id（1/2/3/4）完全对不上：
	///   * 残影列表里没有一个"已拥有"，点切换没反应；
	///   * shadowId 设成 10101 之后，没有任何一个 shadow.id == 10101，界面永远不显示已装备。
	/// 服务端属性侧也一样：残影加的那点属性（如 3*110 = 生命+110）以前完全没算进去。
	/// </summary>
	public static class ShadowTable
	{
		public sealed class Row
		{
			public int Group;      // = 客户端的 ShadowBase.id
			public int HeroId;
			public string Name;
			public int Type;
			public int MinLv;
			public string NeedItems;
			public readonly Dictionary<int, int> Attrs = new Dictionary<int, int>();
		}

		private static Dictionary<int, Dictionary<int, Row>> _byGroup;   // group -> heroId -> row
		private static Dictionary<int, List<Row>> _byHero;               // heroId -> rows

		private static void Ensure()
		{
			if (_byGroup != null) return;
			_byGroup = new Dictionary<int, Dictionary<int, Row>>();
			_byHero = new Dictionary<int, List<Row>>();
			Assembly asm = Assembly.GetExecutingAssembly();
			Stream rs = asm.GetManifestResourceStream("shadowConfig_D.bin");
			if (rs == null) return;
			using (var br = new BinaryReader(rs))
			{
				int groups = br.ReadInt32();
				for (int g = 0; g < groups; g++)
				{
					int group = ParseInt(ReadLenStr(br));
					int inner = br.ReadInt32();
					for (int i = 0; i < inner; i++)
					{
						ReadLenStr(br);                       // 内层 key = 英雄号
						int id = br.ReadInt32();              // id（== 组号）
						int heroId = br.ReadInt32();
						string name = ReadLenStr(br);
						int type = br.ReadInt32();
						int minLv = br.ReadInt32();
						string need = ReadLenStr(br);
						int[] attr = ParsePairs(ReadLenStr(br));
						ReadLenStr(br);                       // descr
						ReadLenStr(br);                       // icon
						ReadLenStr(br);                       // shadow（特效名）

						var row = new Row();
						row.Group = (id != 0) ? id : group;
						row.HeroId = heroId;
						row.Name = name;
						row.Type = type;
						row.MinLv = minLv;
						row.NeedItems = need;
						for (int k = 0; k + 1 < attr.Length; k += 2)
						{
							row.Attrs[attr[k]] = attr[k + 1];
						}
						if (!_byGroup.ContainsKey(row.Group))
						{
							_byGroup[row.Group] = new Dictionary<int, Row>();
						}
						_byGroup[row.Group][heroId] = row;
						if (!_byHero.ContainsKey(heroId))
						{
							_byHero[heroId] = new List<Row>();
						}
						_byHero[heroId].Add(row);
					}
				}
			}
		}

		private static string ReadLenStr(BinaryReader br)
		{
			int shift = 0, len = 0;
			while (true)
			{
				byte by = br.ReadByte();
				len |= (by & 0x7F) << shift;
				if ((by & 0x80) == 0) break;
				shift += 7;
			}
			byte[] buf = br.ReadBytes(len);
			return Encoding.UTF8.GetString(buf);
		}

		private static int ParseInt(string s)
		{
			int v;
			return int.TryParse(s, out v) ? v : 0;
		}

		private static int[] ParsePairs(string text)
		{
			var outp = new List<int>();
			if (string.IsNullOrEmpty(text)) return outp.ToArray();
			string[] parts = text.Split(',');
			for (int i = 0; i < parts.Length; i++)
			{
				int star = parts[i].IndexOf('*');
				if (star <= 0) continue;
				int a, b;
				if (int.TryParse(parts[i].Substring(0, star), out a) &&
				    int.TryParse(parts[i].Substring(star + 1), out b))
				{
					outp.Add(a);
					outp.Add(b);
				}
			}
			return outp.ToArray();
		}

		/// <summary>某个英雄所有残影的 id（升序），表里没有就返回 {1}。</summary>
		public static List<int> IdsOf(int heroId)
		{
			Ensure();
			var list = new List<int>();
			List<Row> rows;
			if (_byHero != null && _byHero.TryGetValue(heroId, out rows))
			{
				for (int i = 0; i < rows.Count; i++)
				{
					list.Add(rows[i].Group);
				}
				list.Sort();
			}
			if (list.Count == 0)
			{
				list.Add(1);
			}
			return list;
		}

		/// <summary>默认拥有的那个（needItems 为空的那一档，通常是 1）。</summary>
		public static int DefaultIdOf(int heroId)
		{
			Ensure();
			List<Row> rows;
			if (_byHero != null && _byHero.TryGetValue(heroId, out rows))
			{
				for (int i = 0; i < rows.Count; i++)
				{
					if (string.IsNullOrEmpty(rows[i].NeedItems))
					{
						return rows[i].Group;
					}
				}
			}
			return 1;
		}

		public static Row Get(int heroId, int shadowId)
		{
			Ensure();
			Dictionary<int, Row> inner;
			if (_byGroup != null && _byGroup.TryGetValue(shadowId, out inner))
			{
				Row r;
				if (inner.TryGetValue(heroId, out r)) return r;
			}
			return null;
		}

		/// <summary>把该英雄已装备残影的属性并进战斗属性表。</summary>
		public static void ApplyAttrs(int heroId, int shadowId, Dictionary<int, int> attrs)
		{
			Row r = Get(heroId, shadowId);
			if (r == null || attrs == null) return;
			foreach (KeyValuePair<int, int> kv in r.Attrs)
			{
				int cur;
				attrs.TryGetValue(kv.Key, out cur);
				attrs[kv.Key] = cur + kv.Value;
			}
		}
	}
}
