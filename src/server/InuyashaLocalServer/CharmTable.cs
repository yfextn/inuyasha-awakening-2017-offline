using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace InuyashaLocalServer
{
	/// <summary>
	/// 魅力（时装）等级表（CharmLevel.bin，嵌入资源）。
	///
	/// 客户端 HeroBase.clothesAddPoint / beautifulMax / beautifulIsMax 用裸索引器
	/// `ConfigManager.m_CharmLevel.Data1[tempid + beautifulLv.ToString("000")]`，
	/// 键 = 英雄id*1000 + 等级(1..20)。服务端只需要 charmDemand 来把 charmVal
	/// 夹在合法区间里（大于等于 charmDemand 会让进度条 fillAmount &gt; 1）。
	///
	/// 字段顺序取自客户端 ConfigManager.cs 的 `public class CharmLevel : CsvBaseData`（7 个字段）。
	/// </summary>
	public static class CharmTable
	{
		private static Dictionary<int, int> _demand;

		private static void Ensure()
		{
			if (_demand != null) return;
			var demand = new Dictionary<int, int>();
			Assembly asm = Assembly.GetExecutingAssembly();
			Stream rs = asm.GetManifestResourceStream("CharmLevel.bin");
			if (rs == null) throw new Exception("CharmLevel.bin embedded resource missing");
			using (var br = new BinaryReader(rs))
			{
				int count = br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					ReadLenStr(br);              // 1 key
					int id = br.ReadInt32();     // 2 id   (= heroId*1000 + levelId)
					br.ReadInt32();              // 3 heroId
					br.ReadInt32();              // 4 levelId
					br.ReadInt32();              // 5 nextLevel (-1 = 满级)
					int charmDemand = br.ReadInt32(); // 6 charmDemand
					br.ReadInt32();              // 7 promote
					ReadLenStr(br);              // 8 itemConsume (List<string>)
					demand[id] = charmDemand;
				}
			}
			_demand = demand;
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

		/// <summary>某英雄某一级所需的魅力值；表里没有该键时返回 fallback。</summary>
		public static int DemandOf(int charmKey, int fallback)
		{
			Ensure();
			int v;
			if (_demand.TryGetValue(charmKey, out v) && v > 0) return v;
			return fallback;
		}

		/// <summary>该英雄魅力的最高等级（表里通常 1..20）。</summary>
		public static int MaxLevelOf(int heroId, int fallback)
		{
			Ensure();
			int max = 0;
			for (int lv = 1; lv <= 99; lv++)
			{
				if (_demand.ContainsKey(heroId * 1000 + lv)) max = lv;
			}
			return (max > 0) ? max : fallback;
		}
	}
}
