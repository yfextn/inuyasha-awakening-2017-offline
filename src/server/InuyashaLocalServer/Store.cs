using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace InuyashaLocalServer;

internal static class Store
{
	public static readonly object Gate = new object();

	public static readonly Dictionary<string, Account> Accounts = new Dictionary<string, Account>();

	public static readonly Dictionary<long, UserState> States = new Dictionary<long, UserState>();

	private static string _path;

	public static string FilePath => _path;

	public static string ResolveDataDir()
	{
		try
		{
			string persistentDataPath = Application.persistentDataPath;
			if (!string.IsNullOrEmpty(persistentDataPath))
			{
				return persistentDataPath;
			}
		}
		catch (Exception ex)
		{
			L.Log("[存档] Application.persistentDataPath 不可用(" + ex.GetType().Name + ")，尝试其它目录");
		}
		try
		{
			string temporaryCachePath = Application.temporaryCachePath;
			if (!string.IsNullOrEmpty(temporaryCachePath))
			{
				return temporaryCachePath;
			}
		}
		catch
		{
		}
		try
		{
			string dataPath = Application.dataPath;
			if (!string.IsNullOrEmpty(dataPath))
			{
				return Path.Combine(dataPath, "..");
			}
		}
		catch
		{
		}
		try
		{
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			if (!string.IsNullOrEmpty(baseDirectory))
			{
				return baseDirectory;
			}
		}
		catch
		{
		}
		return "/data/local/tmp";
	}

	public static void Init(string dir)
	{
		_path = Path.Combine(dir, "inu_local_progress.txt");
		Load();
		L.Log("[存档] 文件 " + _path + "  账号=" + Accounts.Count + " 存档=" + States.Count);
	}

	public static void Load()
	{
		lock (Gate)
		{
			Accounts.Clear();
			States.Clear();
			if (string.IsNullOrEmpty(_path) || !File.Exists(_path))
			{
				return;
			}
			string[] array;
			try
			{
				array = File.ReadAllLines(_path, Encoding.UTF8);
			}
			catch (Exception e)
			{
				L.Err("存档读取", e);
				return;
			}
			Account account = null;
			UserState userState = null;
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0 || text[0] == '#')
				{
					continue;
				}
				if (text.StartsWith("[acc]"))
				{
					account = new Account();
					account.Name = Unescape(text.Substring(5));
					Accounts[account.Name] = account;
					userState = null;
					continue;
				}
				if (text.StartsWith("[prog]"))
				{
					long result = 0L;
					long.TryParse(text.Substring(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
					userState = new UserState();
					userState.Uid = result;
					userState.Heroes.Clear();
					States[result] = userState;
					account = null;
					continue;
				}
				int num = text.IndexOf('=');
				if (num <= 0)
				{
					continue;
				}
				string text2 = text.Substring(0, num);
				string v = text.Substring(num + 1);
				try
				{
					if (account != null)
					{
						ApplyAccount(account, text2, v);
					}
					else if (userState != null)
					{
						ApplyState(userState, text2, v);
					}
				}
				catch (Exception e2)
				{
					L.Err("存档解析 " + text2, e2);
				}
			}
		}
	}

	private static void ApplyAccount(Account a, string k, string v)
	{
		switch (k)
		{
		case "id":
			a.Id = ParseLong(v);
			break;
		case "pass":
			a.Password = Unescape(v);
			break;
		case "role":
		{
			string[] array = v.Split('|');
			Role role = new Role();
			if (array.Length != 0)
			{
				role.ServerId = (int)ParseLong(array[0]);
			}
			if (array.Length > 1)
			{
				role.Nick = Unescape(array[1]);
			}
			if (array.Length > 2)
			{
				role.Gender = (int)ParseLong(array[2]);
			}
			if (array.Length > 3)
			{
				role.Level = (int)ParseLong(array[3]);
			}
			if (array.Length > 4)
			{
				role.LogoutTime = ParseLong(array[4]);
			}
			a.Roles.Add(role);
			break;
		}
		}
	}

	private static void ApplyState(UserState s, string k, string v)
	{
		if (k == null)
		{
			return;
		}
		switch (k.Length)
		{
		case 7:
			switch (k[0])
			{
			case 'c':
				if (k == "cleared")
				{
					ParseIntList(v, s.Cleared);
				}
				break;
			case 'f':
				if (k == "fate_lv")
				{
					ParseIntPairs(v, s.FateAbilityLv);
				}
				break;
			}
			break;
		case 6:
			switch (k[0])
			{
			case 'h':
				if (k == "heroes")
				{
					ParseIntList(v, s.Heroes);
				}
				break;
			case 'c':
				if (k == "copper")
				{
					s.Copper = ParseLong(v);
				}
				break;
			case 's':
				if (k == "signed")
				{
					ParseLongList(v, s.SignedDays);
				}
				break;
			}
			break;
		case 9:
			switch (k[0])
			{
			case 'b':
				if (k == "big_skill")
				{
					s.BigSkill = (int)ParseLong(v);
				}
				break;
			case 'f':
				if (k == "formation")
				{
					ParseListMap(v, s.Formation);
				}
				break;
			case 'l':
				if (k == "lucky_bag")
				{
					s.LuckyBag = ParseLong(v);
				}
				break;
			}
			break;
		case 4:
			switch (k[0])
			{
			case 'b':
				if (k == "bsbh")
				{
					ParseIntPairs(v, s.BigSkillByHero);
				}
				break;
			case 'g':
				if (k == "gold")
				{
					s.Gold = ParseLong(v);
				}
				break;
			}
			break;
		case 11:
			switch (k[0])
			{
			case 'a':
				if (k == "act_claimed")
				{
					ParseIntList(v, s.ActivenessClaimed);
				}
				break;
			case 'f':
				if (k == "fate_active")
				{
					ParseIntPairs(v, s.FateActive);
				}
				break;
			}
			break;
		case 12:
			if (k == "main_task_id")
			{
				s.MainTaskId = (int)ParseLong(v);
			}
			break;
		case 15:
			if (k == "main_task_ready")
			{
				s.MainTaskReady = v == "1";
			}
			break;
		case 3:
			if (k == "bag")
			{
				ParsePairs(v, s.Bag);
				s.BagInitialized = true;
			}
			break;
		case 5:
			if (k == "bless")
			{
				ParseStrPairs(v, s.Bless);
			}
			break;
		case 10:
			if (k == "activeness")
			{
				s.Activeness = (int)ParseLong(v);
			}
			break;
		case 8:
		case 13:
		case 14:
			break;
		}
	}

	private static long ParseLong(string s)
	{
		long result = 0L;
		long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
		return result;
	}

	private static string[] Split(string s, char sep)
	{
		if (string.IsNullOrEmpty(s))
		{
			return new string[0];
		}
		return s.Split(sep);
	}

	private static void ParseIntList(string v, List<int> into)
	{
		into.Clear();
		string[] array = Split(v, ',');
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Length != 0)
			{
				into.Add((int)ParseLong(array[i]));
			}
		}
	}

	private static void ParseLongList(string v, List<long> into)
	{
		into.Clear();
		string[] array = Split(v, ',');
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Length != 0)
			{
				into.Add(ParseLong(array[i]));
			}
		}
	}

	private static void ParsePairs(string v, Dictionary<int, long> into)
	{
		into.Clear();
		string[] array = Split(v, ',');
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i].IndexOf(':');
			if (num > 0)
			{
				into[(int)ParseLong(array[i].Substring(0, num))] = ParseLong(array[i].Substring(num + 1));
			}
		}
	}

	private static void ParseIntPairs(string v, Dictionary<int, int> into)
	{
		into.Clear();
		string[] array = Split(v, ',');
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i].IndexOf(':');
			if (num > 0)
			{
				into[(int)ParseLong(array[i].Substring(0, num))] = (int)ParseLong(array[i].Substring(num + 1));
			}
		}
	}

	private static void ParseStrPairs(string v, Dictionary<string, long> into)
	{
		into.Clear();
		string[] array = Split(v, ',');
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i].IndexOf(':');
			if (num > 0)
			{
				into[array[i].Substring(0, num)] = ParseLong(array[i].Substring(num + 1));
			}
		}
	}

	private static void ParseListMap(string v, Dictionary<int, List<int>> into)
	{
		into.Clear();
		string[] array = Split(v, ';');
		for (int i = 0; i < array.Length; i++)
		{
			int num = array[i].IndexOf(':');
			if (num > 0)
			{
				List<int> list = new List<int>();
				ParseIntList(array[i].Substring(num + 1), list);
				into[(int)ParseLong(array[i].Substring(0, num))] = list;
			}
		}
	}

	private static string Escape(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		return s.Replace("%", "%25").Replace("|", "%7C").Replace("\r", "%0D")
			.Replace("\n", "%0A")
			.Replace("=", "%3D");
	}

	private static string Unescape(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		return s.Replace("%0A", "\n").Replace("%0D", "\r").Replace("%3D", "=")
			.Replace("%7C", "|")
			.Replace("%25", "%");
	}

	private static string JoinInts(IEnumerable<int> xs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (int x in xs)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(x.ToString(CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}

	private static string JoinLongs(IEnumerable<long> xs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (long x in xs)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(x.ToString(CultureInfo.InvariantCulture));
		}
		return stringBuilder.ToString();
	}

	private static string JoinPairs(Dictionary<int, long> d)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, long> item in d)
		{
			if (item.Value > 0)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(item.Key).Append(':').Append(item.Value);
			}
		}
		return stringBuilder.ToString();
	}

	private static string JoinIntPairs(Dictionary<int, int> d)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, int> item in d)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(item.Key).Append(':').Append(item.Value);
		}
		return stringBuilder.ToString();
	}

	private static string JoinStrPairs(Dictionary<string, long> d)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, long> item in d)
		{
			if (item.Value != 0L)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(item.Key).Append(':').Append(item.Value);
			}
		}
		return stringBuilder.ToString();
	}

	private static string JoinListMap(Dictionary<int, List<int>> d)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<int, List<int>> item in d)
		{
			if (item.Value != null && item.Value.Count != 0)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(';');
				}
				stringBuilder.Append(item.Key).Append(':').Append(JoinInts(item.Value));
			}
		}
		return stringBuilder.ToString();
	}

	public static void Save()
	{
		lock (Gate)
		{
			if (string.IsNullOrEmpty(_path))
			{
				return;
			}
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("#InuyashaLocalServer v1\r\n");
				foreach (KeyValuePair<string, Account> account in Accounts)
				{
					Account value = account.Value;
					stringBuilder.Append("[acc]").Append(Escape(value.Name)).Append("\r\n");
					stringBuilder.Append("id=").Append(value.Id).Append("\r\n");
					stringBuilder.Append("pass=").Append(Escape(value.Password)).Append("\r\n");
					for (int i = 0; i < value.Roles.Count; i++)
					{
						Role role = value.Roles[i];
						stringBuilder.Append("role=").Append(role.ServerId).Append('|')
							.Append(Escape(role.Nick))
							.Append('|')
							.Append(role.Gender)
							.Append('|')
							.Append(role.Level)
							.Append('|')
							.Append(role.LogoutTime)
							.Append("\r\n");
					}
				}
				foreach (KeyValuePair<long, UserState> state in States)
				{
					UserState value2 = state.Value;
					stringBuilder.Append("[prog]").Append(value2.Uid).Append("\r\n");
					stringBuilder.Append("cleared=").Append(JoinInts(value2.Cleared)).Append("\r\n");
					stringBuilder.Append("main_task_id=").Append(value2.MainTaskId).Append("\r\n");
					stringBuilder.Append("main_task_ready=").Append(value2.MainTaskReady ? "1" : "0").Append("\r\n");
					stringBuilder.Append("bag=").Append(JoinPairs(value2.Bag)).Append("\r\n");
					stringBuilder.Append("heroes=").Append(JoinInts(value2.Heroes)).Append("\r\n");
					stringBuilder.Append("bless=").Append(JoinStrPairs(value2.Bless)).Append("\r\n");
					stringBuilder.Append("big_skill=").Append(value2.BigSkill).Append("\r\n");
					stringBuilder.Append("formation=").Append(JoinListMap(value2.Formation)).Append("\r\n");
					stringBuilder.Append("bsbh=").Append(JoinIntPairs(value2.BigSkillByHero)).Append("\r\n");
					stringBuilder.Append("copper=").Append(value2.Copper).Append("\r\n");
					stringBuilder.Append("gold=").Append(value2.Gold).Append("\r\n");
					stringBuilder.Append("lucky_bag=").Append(value2.LuckyBag).Append("\r\n");
					stringBuilder.Append("signed=").Append(JoinLongs(value2.SignedDays)).Append("\r\n");
					stringBuilder.Append("activeness=").Append(value2.Activeness).Append("\r\n");
					stringBuilder.Append("act_claimed=").Append(JoinInts(value2.ActivenessClaimed)).Append("\r\n");
					stringBuilder.Append("fate_lv=").Append(JoinIntPairs(value2.FateAbilityLv)).Append("\r\n");
					stringBuilder.Append("fate_active=").Append(JoinIntPairs(value2.FateActive)).Append("\r\n");
				}
				string text = _path + ".tmp";
				File.WriteAllText(text, stringBuilder.ToString(), Encoding.UTF8);
				if (File.Exists(_path))
				{
					File.Delete(_path);
				}
				File.Move(text, _path);
			}
			catch (Exception e)
			{
				L.Err("存档写入（继续使用内存状态）", e);
			}
		}
	}

	private static long NextId()
	{
		long num = 0L;
		foreach (Account value in Accounts.Values)
		{
			if (value.Id > num)
			{
				num = value.Id;
			}
		}
		if (num <= 0)
		{
			return 100001L;
		}
		return num + 1;
	}

	public static Account Register(string name, string password, out string error)
	{
		lock (Gate)
		{
			if (Accounts.ContainsKey(name))
			{
				error = "账号已存在";
				return null;
			}
			Account account = new Account();
			account.Id = NextId();
			account.Name = name;
			account.Password = password;
			Accounts[name] = account;
			Save();
			error = null;
			return account;
		}
	}

	public static Account Login(string name, string password, out string error)
	{
		lock (Gate)
		{
			if (!Accounts.TryGetValue(name, out var value))
			{
				error = "账号不存在";
				return null;
			}
			if (value.Password != password)
			{
				error = "密码错误";
				return null;
			}
			error = null;
			return value;
		}
	}

	public static Account EnsureAccount(string name, string password)
	{
		lock (Gate)
		{
			if (Accounts.TryGetValue(name, out var value))
			{
				return value;
			}
			value = new Account();
			value.Id = NextId();
			value.Name = name;
			value.Password = password;
			Accounts[name] = value;
			Save();
			return value;
		}
	}

	public static Account FindById(long uid)
	{
		lock (Gate)
		{
			foreach (Account value in Accounts.Values)
			{
				if (value.Id == uid)
				{
					return value;
				}
			}
			return null;
		}
	}

	public static Role EnsureRole(long uid, string accountName, int serverId, out bool isNew)
	{
		lock (Gate)
		{
			Account account = null;
			foreach (Account value in Accounts.Values)
			{
				if (value.Id == uid)
				{
					account = value;
					break;
				}
			}
			if (account == null)
			{
				account = new Account();
				account.Id = uid;
				account.Name = accountName;
				account.Password = "";
				Accounts[accountName] = account;
			}
			Role role = null;
			for (int i = 0; i < account.Roles.Count; i++)
			{
				if (account.Roles[i].ServerId == serverId)
				{
					role = account.Roles[i];
					break;
				}
			}
			isNew = role == null;
			if (isNew)
			{
				role = new Role();
				role.ServerId = serverId;
				role.Nick = accountName;
				role.Gender = 1;
				role.Level = 1;
				role.LogoutTime = Clock.NowMs();
				account.Roles.Add(role);
			}
			Save();
			return role;
		}
	}

	public static UserState Progress(long uid)
	{
		lock (Gate)
		{
			if (!States.TryGetValue(uid, out var value))
			{
				value = new UserState();
				value.Uid = uid;
				States[uid] = value;
			}
			if (!value.BagInitialized)
			{
				value.Bag.Clear();
				value.Bag[5010401] = value.Cleared.Count;
				value.Bag[2100001] = 10L;
				// 宝石：客户端宝石界面只认 bagType==4 的背包物品（Itemstype 里 id 302TTLL，
				// TT=类型 01..10，LL=等级 01..09）。一件不给的话宝石列表是空的。
				// ★ 只发 APK 里真有图标的：image/icon/302TTLL.tex 只覆盖 TT=01..09，
				//   3021001（TT=10）没有贴图，一发出去客户端就每帧刷
				//     Unable to open archive file: .../image/icon/3021001.tex
				//   并抛异常，宝石界面会一直重绘失败。
				value.Bag[3020101] = 2L;
				value.Bag[3020102] = 1L;
				value.Bag[3020201] = 2L;
				value.Bag[3020301] = 1L;
				value.BagInitialized = true;
			}
			// 老存档里可能已经塞过没贴图的 3021001，直接清掉，否则客户端会一直刷
			// "Unable to open archive file: .../image/icon/3021001.tex" 并抛异常。
			if (value.Bag.Remove(3021001))
			{
				SaveProgress();
			}
			// 注意：曾经在这里把桔梗的时装强行夹回 105001（当时以为换第二套会 NRE）。
			// 结果这段代码每次取存档都跑一遍，玩家在「时装」界面换到第二套后立刻被打回去，
			// 表现就是"桔梗主城形象不能换、永远锁第一套"。
			// 后来确认那条 CreateSkin NRE 和穿哪套无关，所以这段强制改回已经删掉，
			// 时装完全交给玩家自己选。
			// 宝石槽默认镶嵌：给每个英雄的武器槽第 1 格预置一颗生命宝石，
			// 这样宝石界面一进去就能看到已镶嵌状态（键与 Game2.HGemInlay 落盘的键一致）。
			foreach (int heroId in value.Heroes)
			{
				long equipUid = (uid * 100 + heroId) * 10 + 1;
				if (!value.Bless.ContainsKey("gem_" + equipUid + "_1"))
				{
					value.Bless["gem_" + equipUid + "_1"] = 3020101L;
				}
			}
			if (value.Heroes.Count == 0)
			{
				value.Heroes.Add(101);
			}
			// ★ 剧情英雄默认全给（101 犬夜叉 / 102 日暮篱 / 103 七宝 / 104 杀生丸 /
			//   106 飞天 / 108 钢牙）。
			//   原版这几个是靠「招募」条件一点点解锁的（通关某关、消耗道具），
			//   清档重开后玩家会看到日暮篱、七宝、杀生丸全挂锁，而且默认阵容只剩
			//   [101,105] —— 桔梗反而成了首发。这里统一保证这几个都在阵容里，
			//   默认上阵就会回到 101/102/103。
			int[] array = new int[6] { 101, 102, 103, 104, 106, 108 };
			for (int i = 0; i < array.Length; i++)
			{
				if (Tables.Heroes.ContainsKey(array[i]) && !value.Heroes.Contains(array[i]))
				{
					value.Heroes.Add(array[i]);
				}
			}
			value.Heroes.Sort();
			// 桔梗（105）：默认直接送，方便一进游戏就能在「角色」里看到并上阵。
			// 前提是客户端的 heroAttr.bin 已经有 105 行（见 patch_bins_kikyo.py），
			// 否则客户端会因为找不到模板而 NRE 卡死。不想要就把它改成 false。
			if (C.GrantKikyoByDefault && !value.Heroes.Contains(105) && Tables.Heroes.ContainsKey(105))
			{
				value.Heroes.Add(105);
				value.Heroes.Sort();
			}
			if (value.BigSkill == 0)
			{
				value.BigSkill = 101400100;
			}
			int key = value.BigSkill / 1000000;
			if (value.BigSkill != 0 && Tables.Heroes.ContainsKey(key) && !value.BigSkillByHero.ContainsKey(key))
			{
				value.BigSkillByHero[key] = value.BigSkill;
			}
			return value;
		}
	}

	public static void SaveProgress()
	{
		Save();
	}
}
