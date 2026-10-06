using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace InuyashaLocalServer
{
	/// <summary>
	/// 关卡权威表（StageConfig.bin，嵌入资源）。
	///
	/// rewards 字段是 [[itemId, num], ...]，实测 itemId:
	///   2 = 银币（铜币）   4 = 经验（玩家/战队经验）
	/// 例如 100101 rewards=[[2,546],[4,155]] → 通关给 546 银币 + 155 经验。
	///
	/// 原先服务端把通关经验写死成 150（"long num = 150L;"），所以任何关卡都只加 150 经验，
	/// 和关卡面板上显示的奖励数量对不上。这里改成读客户端自己的表。
	///
	/// 字段顺序取自客户端 ConfigManager.cs 的 `public class StageConfig : CsvBaseData`，
	/// 共 35 个字段，按声明顺序写入（CsvBinReader.ReadConfigFromBinaryStream1 是位置读取）。
	/// </summary>
	public static class StageTable
	{
		private class Row
		{
			public int Copper;
			public int Exp;
			public int ChapterId;
			public int StageType;
			public int MapId;
			public int PreStageId;
			public int NextStageId;
		}

		private static Dictionary<int, Row> _rows;

		private static void Ensure()
		{
			if (_rows != null) return;
			var rows = new Dictionary<int, Row>();
			Assembly asm = Assembly.GetExecutingAssembly();
			Stream rs = asm.GetManifestResourceStream("StageConfig.bin");
			if (rs == null) throw new Exception("StageConfig.bin embedded resource missing");
			using (var br = new BinaryReader(rs))
			{
				int count = br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					ReadLenStr(br);                 // 1  key (字符串形式的 stageId)
					// 字段顺序严格照抄客户端 StageConfig 的 35 个字段（已用 738 条记录验证，0 字节剩余）
					int stageId = br.ReadInt32();   // 2  stageId
					ReadLenStr(br);                 // 3  name
					br.ReadInt32();                 // 4  picId
					int chapterId = br.ReadInt32(); // 5  chapterId
					br.ReadInt32();                 // 6  bellsType
					br.ReadInt32();                 // 7  bellsPic
					ReadLenStr(br);                 // 8  bellsComponentPic
					ReadLenStr(br);                 // 9  bellsComponentColor
					br.ReadInt32();                 // 10 bellsStreaksPic
					int mapId = br.ReadInt32();     // 11 mapId（时空炼狱的层号就是这个）
					int stageType = br.ReadInt32(); // 12 stageType（10=普通 11=精英 24=竞技场 26=时空炼狱）
					br.ReadInt32();                 // 13 needLevel
					br.ReadInt32();                 // 14 needSta
					br.ReadInt32();                 // 15 countdownEnd
					br.ReadInt32();                 // 16 stageTypeTips
					br.ReadInt32();                 // 17 timeScore
					br.ReadInt32();                 // 18 hpScore
					br.ReadInt32();                 // 19 dailyFreeSweepNum
					br.ReadInt32();                 // 20 everydayTimes
					br.ReadInt32();                 // 21 power
					br.ReadInt32();                 // 22 powerMin
					ReadLenStr(br);                 // 23 desc
					int preStageId = br.ReadInt32();// 24 preStageId
					ReadLenStr(br);                 // 25 enableStageIds
					int nextStageId = br.ReadInt32();// 26 nextStageId
					ReadLenStr(br);                 // 27 showItems（掉落预览，不是实际奖励）
					List<int[]> rewards = ParsePairs(ReadLenStr(br)); // 28 rewards ★ 实际发放
					br.ReadInt32();                 // 29 heroExpReward（英雄经验，不是玩家经验）
					br.ReadInt32();                 // 30 goldLottery
					ReadLenStr(br);                 // 31 heroIds
					ReadLenStr(br);                 // 32 bannedHeroIds
					br.ReadInt32();                 // 33 objectId
					ReadLenStr(br);                 // 34 movieName
					ReadLenStr(br);                 // 35 stageMonster
					ReadLenStr(br);                 // 36 sweepConsume

					var row = new Row();
					row.ChapterId = chapterId;
					row.StageType = stageType;
					row.MapId = mapId;
					row.PreStageId = preStageId;
					row.NextStageId = nextStageId;
					for (int k = 0; k < rewards.Count; k++)
					{
						int itemId = rewards[k][0];
						int num = rewards[k][1];
						if (itemId == 2) row.Copper = num;
						else if (itemId == 4) row.Exp = num;
					}
					rows[stageId] = row;
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

		/// <summary>解析 "k*v,k*v"（CsvBinReader 先试 ParseCsvPay，再退回 [[k,v],[k,v]]）。</summary>
		private static List<int[]> ParsePairs(string text)
		{
			var outp = new List<int[]>();
			if (string.IsNullOrEmpty(text)) return outp;
			string[] parts = text.Split(',');
			bool ok = parts.Length > 0;
			for (int i = 0; i < parts.Length; i++)
			{
				int star = parts[i].IndexOf('*');
				if (star <= 0) { ok = false; break; }
				int a, b;
				if (!int.TryParse(parts[i].Substring(0, star), out a) ||
				    !int.TryParse(parts[i].Substring(star + 1), out b)) { ok = false; break; }
				outp.Add(new int[2] { a, b });
			}
			if (ok && outp.Count > 0) return outp;

			// 退回 "[[k,v],[k,v]]" 形式
			outp.Clear();
			int p = 0;
			while (p < text.Length)
			{
				int lb = text.IndexOf('[', p);
				if (lb < 0) break;
				int rb = text.IndexOf(']', lb + 1);
				if (rb < 0) break;
				string inner = text.Substring(lb + 1, rb - lb - 1);
				string[] kv = inner.Split(',');
				if (kv.Length == 2)
				{
					int a, b;
					if (int.TryParse(kv[0].Trim(), out a) && int.TryParse(kv[1].Trim(), out b))
						outp.Add(new int[2] { a, b });
				}
				p = rb + 1;
			}
			return outp;
		}

		/// <summary>关卡的通关经验（itemId 4）。表里没有该关卡时返回 fallback。</summary>
		public static int ExpOf(int stageId, int fallback)
		{
			Ensure();
			Row r;
			if (_rows.TryGetValue(stageId, out r) && r.Exp > 0) return r.Exp;
			return fallback;
		}

		/// <summary>关卡的通关银币（itemId 2）。表里没有该关卡时返回 fallback。</summary>
		public static int CopperOf(int stageId, int fallback)
		{
			Ensure();
			Row r;
			if (_rows.TryGetValue(stageId, out r) && r.Copper > 0) return r.Copper;
			return fallback;
		}

		/// <summary>某关卡属于第几章（StageConfig.chapterId）。表里没有返回 0。</summary>
		public static int ChapterOf(int stageId)
		{
			Ensure();
			Row r;
			return _rows.TryGetValue(stageId, out r) ? r.ChapterId : 0;
		}

		/// <summary>stageType：10 普通 / 11 精英 / 24 竞技场 / 26 时空炼狱 …。表里没有返回 0。</summary>
		public static int StageTypeOf(int stageId)
		{
			Ensure();
			Row r;
			return _rows.TryGetValue(stageId, out r) ? r.StageType : 0;
		}

		/// <summary>mapId（时空炼狱的层号）。</summary>
		public static int MapIdOf(int stageId)
		{
			Ensure();
			Row r;
			return _rows.TryGetValue(stageId, out r) ? r.MapId : 0;
		}

		public static bool Exists(int stageId)
		{
			Ensure();
			return _rows.ContainsKey(stageId);
		}

		/// <summary>把 id 按 (chapterId, stageType) 归组，用于一次性下发整章的普通关 + 精英关。</summary>
		public static List<int> StageIdsOf(int chapterId, int stageType)
		{
			Ensure();
			var list = new List<int>();
			foreach (KeyValuePair<int, Row> kv in _rows)
			{
				if (kv.Value.StageType == stageType && kv.Value.ChapterId == chapterId)
				{
					list.Add(kv.Key);
				}
			}
			list.Sort();
			return list;
		}

		/// <summary>时空炼狱（stageType 26）：层号 mapId → stageId。</summary>
		public static int SpaceTimeStageOf(int wave)
		{
			Ensure();
			foreach (KeyValuePair<int, Row> kv in _rows)
			{
				if (kv.Value.StageType == 26 && kv.Value.MapId == wave)
				{
					return kv.Key;
				}
			}
			return 0;
		}

		/// <summary>
		/// 这一章是否算「已经打过」：只要玩家在**同一章**里通关过任意一关就算。
		///
		/// 为什么不能只判断「目标关本身在 cleared 里」：
		/// 服务端只在收到 5082（战斗结算）时把**那一关**记进 cleared，
		/// 而玩家经常是「主城任务直接拉去打后面的关」，于是第一关 100101 永远没被记，
		/// 桔梗/七宝这种 pass 解锁（getHero recruit=1）就一直卡在「完成第1章第1关」解不了。
		/// </summary>
		public static bool ChapterCleared(int requiredStageId, System.Collections.Generic.List<int> cleared)
		{
			Ensure();
			Row req;
			if (!_rows.TryGetValue(requiredStageId, out req)) return false;
			if (cleared == null) return false;
			for (int i = 0; i < cleared.Count; i++)
			{
				if (cleared[i] == requiredStageId) return true;
				Row r;
				if (_rows.TryGetValue(cleared[i], out r) && r.ChapterId != 0 && r.ChapterId == req.ChapterId)
				{
					return true;
				}
			}
			return false;
		}
	}
}
