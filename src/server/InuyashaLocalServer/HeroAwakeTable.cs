using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace InuyashaLocalServer
{
	/// <summary>
	/// 觉醒表 heroAwakeConfig.bin（嵌入资源）。
	///
	/// 客户端 HeroBase 的属性全是**裸索引器**访问这张表：
	///   quality      = m_heroAwakeConfig.getValue(tempid * 100 + qualityLv).showQly
	///   growUpTween  = ...growthRateInterval
	///   lvUpNeedItems= ...awokeCost / commenCost / especialCost
	/// 一旦 qualityLv 超出表范围（每个英雄只有 0..9 十行），getValue 返回 null，
	/// 紧接着 .showQly / .growthRateInterval 就是 NullReferenceException。
	///
	/// 旧服务端 HHeroQly 是无条件 `hero_qly + 1`，点 10 次普通铸灵就把品质顶到 10，
	/// 客户端立刻开始刷 NRE（logcat 10:16:11 / 10:16:17 / 10:16:23 三波），
	/// 面板卡死、切不了角色、其他界面也一起废掉。
	///
	/// 字段顺序取自客户端 ConfigManager.cs 的 `class heroAwakeConfig`（14 个字段，位置读取）。
	/// </summary>
	public static class HeroAwakeTable
	{
		public sealed class Row
		{
			public int RoleId;
			public int Quality;
			public int NextQly;
			public int ShowQly;
			public int GrowthMin;
			public int GrowthMax;
			public int GrowthFix;        // 普通铸灵每次加的成长率
			public int EspecialGrowth;   // 特殊铸灵每次加的成长率
			public int AwokeRate;
		}

		private static Dictionary<int, Row> _rows;

		private static void Ensure()
		{
			if (_rows != null) return;
			var rows = new Dictionary<int, Row>();
			Assembly asm = Assembly.GetExecutingAssembly();
			Stream rs = asm.GetManifestResourceStream("heroAwakeConfig.bin");
			if (rs == null) { _rows = rows; return; }
			using (var br = new BinaryReader(rs))
			{
				int count = br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					ReadLenStr(br);              // 1  key
					br.ReadInt32();              // 2  indexID
					int roleId = br.ReadInt32(); // 3  roleID
					int quality = br.ReadInt32();// 4  quality
					int nextQly = br.ReadInt32();// 5  nextQly
					int showQly = br.ReadInt32();// 6  showQly
					int[] interval = ParsePairs(ReadLenStr(br)); // 7 growthRateInterval
					int growthFix = br.ReadInt32();     // 8  growthRateFix
					br.ReadInt32();                     // 9  growthValFix
					ReadLenStr(br);                     // 10 commenCost
					int especialGrowth = br.ReadInt32();// 11 especialGrowth
					ReadLenStr(br);                     // 12 especialCost
					int awokeRate = br.ReadInt32();     // 13 awokeRate
					ReadLenStr(br);                     // 14 awokeCost
					ReadLenStr(br);                     // 15 addAttr

					var row = new Row();
					row.RoleId = roleId;
					row.Quality = quality;
					row.NextQly = nextQly;
					row.ShowQly = showQly;
					row.GrowthFix = growthFix;
					row.EspecialGrowth = especialGrowth;
					row.AwokeRate = awokeRate;
					row.GrowthMin = (interval.Length >= 1) ? interval[0] : 0;
					row.GrowthMax = (interval.Length >= 2) ? interval[1] : row.GrowthMin;
					rows[roleId * 100 + quality] = row;
				}
			}
			_rows = rows;
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

		/// <summary>"k*v,k*v" → [k,v,...]（与 StageTable 同样的解析）。</summary>
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

		public static Row Get(int heroId, int quality)
		{
			Ensure();
			Row r;
			if (_rows.TryGetValue(heroId * 100 + quality, out r)) return r;
			return null;
		}

		/// <summary>该英雄表里最大的 quality（表缺失时返回 9）。</summary>
		public static int MaxQuality(int heroId)
		{
			Ensure();
			int max = -1;
			for (int q = 0; q <= 20; q++)
			{
				if (_rows.ContainsKey(heroId * 100 + q)) max = q;
			}
			return (max < 0) ? 9 : max;
		}

		public static int GrowthMin(int heroId, int quality)
		{
			Row r = Get(heroId, quality);
			return (r != null) ? r.GrowthMin : 0;
		}

		public static int GrowthMax(int heroId, int quality)
		{
			Row r = Get(heroId, quality);
			return (r != null) ? r.GrowthMax : 0;
		}
	}
}
