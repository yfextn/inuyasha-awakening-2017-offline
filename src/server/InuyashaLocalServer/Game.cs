using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Inuyasha.Generated.Protocol;
using XEngine;

namespace InuyashaLocalServer;

internal static class Game
{
	internal static int Int(object v, int def)
	{
		if (v == null)
		{
			return def;
		}
		try
		{
			return Convert.ToInt32(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return def;
		}
	}

	internal static long Long(object v, long def)
	{
		if (v == null)
		{
			return def;
		}
		try
		{
			return Convert.ToInt64(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return def;
		}
	}

	internal static void Set(object o, string name, object v)
	{
		Msg.Set(o, name, v);
	}

	internal static Tables.HeroRow HeroRow(int heroId)
	{
		if (!Tables.Heroes.TryGetValue(heroId, out var value))
		{
			return null;
		}
		return value;
	}

	internal static int CalcPower(long uid)
	{
		int num = 800;
		UserState userState = Store.Progress(uid);
		if (userState == null)
		{
			return num;
		}
		List<int> list = OwnedHeroes(uid);
		if (list.Count == 0)
		{
			list.Add(101);
		}
		foreach (int item in list)
		{
			long num2 = uid * 100 + item;
			num += HeroQlyOf(userState, item) * 500;
			for (int i = 1; i <= 6; i++)
			{
				long num3 = num2 * 10 + i;
				int num4 = (int)userState.BlessGet("eq_en_" + num3, 1L);
				num += (num4 - 1) * 30;
				num += (int)userState.BlessGet("eq_qly_" + num3, 0L) * 300;
			}
			Tables.HeroRow heroRow = HeroRow(item);
			if (heroRow != null && heroRow.Skills != null)
			{
				Tables.SkillRow[] skills = heroRow.Skills;
				for (int j = 0; j < skills.Length; j++)
				{
					long num5 = userState.BlessGet("sk_" + num2 + "_" + skills[j].SkillId, 0L);
					if (num5 > 0)
					{
						num += (int)num5 * 20;
					}
				}
			}
			num += (int)userState.BlessGet("fate_lv_" + num2, 0L) * 100;
			num += (int)userState.BlessGet("charm_lv_" + num2, 0L) * 50;
			Dictionary<int, int> d = HeroLvTable.GetBase(item, userState.Level);
			int dict = GetDict(d, 3);
			int dict2 = GetDict(d, 1);
			int dict3 = GetDict(d, 2);
			num += dict2 * 6 + dict / 2 + dict3 * 3;
		}
		return num;
	}

	private static int GetDict(Dictionary<int, int> d, int t)
	{
		if (!d.TryGetValue(t, out var value))
		{
			return 0;
		}
		return value;
	}

	internal static List<int> OwnedHeroes(long uid)
	{
		List<int> list = new List<int>(Store.Progress(uid).Heroes);
		if (!list.Contains(101))
		{
			list.Insert(0, 101);
		}
		list.Sort();
		List<int> list2 = new List<int>();
		for (int i = 0; i < list.Count; i++)
		{
			if (!list2.Contains(list[i]))
			{
				list2.Add(list[i]);
			}
		}
		return list2;
	}

	internal static List<int> Formation(long uid, int ftype)
	{
		UserState userState = Store.Progress(uid);
		List<int> list = OwnedHeroes(uid);
		List<int> list2 = new List<int>();
		if (userState.Formation.TryGetValue(ftype, out var value))
		{
			for (int i = 0; i < value.Count; i++)
			{
				int num = value[i];
				if (list.Contains(num) && Tables.Heroes.ContainsKey(num) && !list2.Contains(num))
				{
					list2.Add(num);
				}
			}
		}
		if (list2.Count == 0)
		{
			for (int j = 0; j < list.Count; j++)
			{
				if (list2.Count >= 3)
				{
					break;
				}
				if (Tables.Heroes.ContainsKey(list[j]))
				{
					list2.Add(list[j]);
				}
			}
		}
		if (list2.Count == 0)
		{
			list2.Add(101);
		}
		// ★ 阵容补满 3 人：清档重开后如果存档里只存过 [101,105]（桔梗），
		//   直接沿用就会变成"桔梗首发、七宝没上场"。这里用已拥有的英雄补齐到 3 个，
		//   顺序按英雄号，所以默认就回到 101/102/103。
		for (int k = 0; k < list.Count && list2.Count < 3; k++)
		{
			if (Tables.Heroes.ContainsKey(list[k]) && !list2.Contains(list[k]))
			{
				list2.Add(list[k]);
			}
		}
		return list2;
	}

	internal static List<int> SaveFormation(long uid, int ftype, List<int> heroIds)
	{
		UserState userState = Store.Progress(uid);
		List<int> list = OwnedHeroes(uid);
		List<int> list2 = new List<int>();
		for (int i = 0; i < heroIds.Count; i++)
		{
			int num = heroIds[i];
			if (list.Contains(num) && Tables.Heroes.ContainsKey(num) && !list2.Contains(num))
			{
				list2.Add(num);
			}
		}
		if (list2.Count > 0)
		{
			userState.Formation[ftype] = list2;
		}
		if (!userState.Formation.ContainsKey(1))
		{
			List<int> list3 = new List<int>();
			for (int j = 0; j < list.Count; j++)
			{
				if (list3.Count >= 3)
				{
					break;
				}
				if (Tables.Heroes.ContainsKey(list[j]))
				{
					list3.Add(list[j]);
				}
			}
			if (list3.Count == 0)
			{
				list3.Add(101);
			}
			userState.Formation[1] = list3;
		}
		Store.SaveProgress();
		return list2;
	}

	internal static int ChosenSuperSkill(long uid, int heroId)
	{
		Tables.HeroRow heroRow = HeroRow(heroId);
		if (heroRow == null)
		{
			return 0;
		}
		// ★ 玩家选中的奥义直接用，不再拿 SuperSkillIds 白名单去卡。
		//   原因：客户端 SkillGet.bin 里每个英雄其实有 5 个奥义（SkillGet 只列了 2 个，
		//   已由 patch_bins_ult.py 补齐），而这里硬编码的 SuperSkillIds 只有老的两个，
		//   于是选「爆流破(101400200)」会被判成不合法 → 回落到 SuperSkillId=风之伤，
		//   表现就是"界面上选了爆流破，进战斗还是风之伤"。
		//   奥义 id 的前三位就是英雄号（101400200/1000000 = 101），用它做校验足够。
		int chosen;
		if (Store.Progress(uid).BigSkillByHero.TryGetValue(heroId, out chosen) && chosen != 0
		    && (chosen / 1000000) == heroId)
		{
			return KikyoSuperSkill(heroId, chosen);
		}
		return KikyoSuperSkill(heroId, (heroRow.SuperSkillId != 0) ? heroRow.SuperSkillId : ((heroRow.SuperSkillIds.Length == 0) ? 0 : heroRow.SuperSkillIds[0]));
	}

	/// <summary>
	/// 桔梗（105）的奥义动作借日暮篱（102）的同档奥义。
	///
	/// 她的英雄预制体 prefabs/character/jiegeng.prefab 是个没做完的测试资源：
	/// 奥义动作能播（"只有动画"）但没有特效和命中数据，打不出伤害；
	/// 关卡里那个 NPC 桔梗是走 MonsterBase + monsterAi + character/ai/jiegeng.prefab
	/// 那一套的，"奥义"并不是英雄技能表里的一行，没法直接拷过来（SkillBaseConfig_T
	/// 里连一个 jg_ 开头的动作名都没有）。
	/// 所以这里退一步：让她用同为弓箭手的日暮篱的同档奥义（id 差 3000000），
	/// 至少特效和伤害是完整的，表现会变成日暮篱的奥义演出。
	/// 不想要这个借用，把这里 return 的 KikyoSuperSkill 换成原值即可。
	/// </summary>
	internal static int KikyoSuperSkill(int heroId, int skillId)
	{
		if (heroId == 105 && skillId >= 105400100 && skillId <= 105400500)
		{
			return skillId - 3000000;
		}
		return skillId;
	}

	internal static object[] BattleAttrs(long uid, int heroId)
	{
		UserState userState = Store.Progress(uid);
		int level = userState.Level;
		long num = uid * 100 + heroId;
		Dictionary<int, int> dictionary = HeroLvTable.GetBase(heroId, level);
		int num2 = 0;
		int num3 = 0;
		for (int i = 1; i <= 6; i++)
		{
			long num4 = num * 10 + i;
			int num5 = (int)userState.BlessGet("eq_en_" + num4, 1L);
			num2 += num5 - 1;
			int num6 = (int)userState.BlessGet("eq_qly_" + num4, 0L);
			if (num6 > num3)
			{
				num3 = num6;
			}
		}
		int num7 = HeroQlyOf(userState, heroId);
		int num8 = (int)userState.BlessGet("fate_lv_" + num, 0L);
		int num9 = (int)userState.BlessGet("charm_lv_" + num, 0L);
		int num10 = 0;
		Tables.HeroRow heroRow = HeroRow(heroId);
		if (heroRow != null && heroRow.Skills != null)
		{
			Tables.SkillRow[] skills = heroRow.Skills;
			for (int j = 0; j < skills.Length; j++)
			{
				if (userState.BlessGet("sk_" + num + "_" + skills[j].SkillId, 0L) > 0)
				{
					num10++;
				}
			}
		}
		int add = (level - 1) * 10 + num2 * 8 + num3 * 40 + num7 * 60 + num10 * 10 + num8 * 20 + num9 * 10;
		int add2 = (level - 1) * 60 + num2 * 30 + num3 * 150 + num7 * 200 + num8 * 100;
		int add3 = (level - 1) * 6 + num2 * 4 + num3 * 20 + num7 * 30 + num8 * 15;
		AddTo(dictionary, 1, add);
		AddTo(dictionary, 3, add2);
		AddTo(dictionary, 2, add3);
		// 残影：把当前装备那一档的残影属性并进战斗属性。
		// 客户端只有残影界面（记 shadowId），没有任何一处把残影属性算进战斗，
		// 所以「装了残影没效果」只能由服务端在这里补上。
		ShadowTable.ApplyAttrs(heroId, (int)userState.BlessGet("shadow_on_" + num, ShadowTable.DefaultIdOf(heroId)), dictionary);
		int num11 = heroRow?.SuperNeedSp ?? 900;
		if (!dictionary.TryGetValue(46, out var value) || value < num11 + 100)
		{
			value = num11 + 100;
		}
		dictionary[46] = value;
		dictionary[12] = value;
		List<int> list = new List<int>(dictionary.Keys);
		list.Sort();
		object[] array = new object[list.Count];
		for (int k = 0; k < list.Count; k++)
		{
			array[k] = Msg.MakeAttr(list[k], dictionary[list[k]]);
		}
		return array;
	}

	private static void AddTo(Dictionary<int, int> d, int type, int add)
	{
		d.TryGetValue(type, out var value);
		d[type] = value + add;
	}

	internal static StageHeroInfo BattleHero(int heroId, long uid)
	{
		Tables.HeroRow heroRow = HeroRow(heroId);
		int num = ((heroRow != null) ? ChosenSuperSkill(uid, heroId) : 0);
		StageHeroInfo stageHeroInfo = new StageHeroInfo();
		Set(stageHeroInfo, "hero_id", heroId);
		Set(stageHeroInfo, "power", CalcPower(uid));
		int num2 = heroRow?.ClothesId ?? 0;
		if (num2 == 0)
		{
			num2 = heroId * 1000 + 1;
		}
		Set(stageHeroInfo, "clothesId", num2);
		List<object> list = new List<object>();
		if (heroRow != null && heroRow.Skills.Length != 0)
		{
			List<Tables.SkillRow> list2 = new List<Tables.SkillRow>();
			List<Tables.SkillRow> list3 = new List<Tables.SkillRow>();
			for (int i = 0; i < heroRow.Skills.Length; i++)
			{
				if (heroRow.Skills[i].SkillType == 400)
				{
					list2.Add(heroRow.Skills[i]);
				}
				else
				{
					list3.Add(heroRow.Skills[i]);
				}
			}
			int pick = num;
			list2.Sort(delegate(Tables.SkillRow a, Tables.SkillRow b)
			{
				int num6 = ((a.SkillId != pick) ? 1 : 0);
				int num7 = ((b.SkillId != pick) ? 1 : 0);
				return (num6 == num7) ? (a.LimitLv - b.LimitLv) : (num6 - num7);
			});
			for (int num3 = 0; num3 < list3.Count; num3++)
			{
				list.Add(Msg.MakeSkillLevel(list3[num3].SkillId, list3[num3].SkillLevel));
			}
			for (int num4 = 0; num4 < list2.Count; num4++)
			{
				list.Add(Msg.MakeSkillLevel(list2[num4].SkillId, list2[num4].SkillLevel));
			}
		}
		if (list.Count == 0)
		{
			for (int num5 = 0; num5 < C.StarterSkillIds.Length; num5++)
			{
				list.Add(Msg.MakeSkillLevel(C.StarterSkillIds[num5], 1));
			}
		}
		// ★ 每个英雄在 SkillGet 里其实有 5 个奥义（客户端奥义列表就是按这张表建的，
		//   由 patch_bins_ult.py 补齐），但服务端 Tables 里每个英雄只写了 2 个 400 型技能。
		//   结果：玩家选了第 3/4/5 个奥义后，fate_ability_id 指向它、skill_level 里却没有它，
		//   战斗里那个奥义用不出来（"奥义还没法使用"）。
		//   奥义 id 规律：英雄号*1000000 + 400100 + 档位*100。
		for (int num8 = 0; num8 < 5; num8++)
		{
			int superId = heroId * 1000000 + 400100 + num8 * 100;
			// 桔梗的奥义实际用的是日暮篱那一档（见 KikyoSuperSkill），两套 id 都要进表
			int borrowId = KikyoSuperSkill(heroId, superId);
			for (int num10 = 0; num10 < 2; num10++)
			{
				int num11 = (num10 == 0) ? superId : borrowId;
				if (num11 <= 0)
				{
					continue;
				}
				bool exists = false;
				for (int num9 = 0; num9 < list.Count; num9++)
				{
					StageHeroInfo.SkillLevelInfo info = list[num9] as StageHeroInfo.SkillLevelInfo;
					if (info != null && info.SkillId == num11)
					{
						exists = true;
						break;
					}
				}
				if (!exists)
				{
					list.Add(Msg.MakeSkillLevel(num11, 1));
				}
			}
		}
		Set(stageHeroInfo, "skill_level", list.ToArray());
		Set(stageHeroInfo, "attr", BattleAttrs(uid, heroId));
		if (heroRow != null)
		{
			Set(stageHeroInfo, "fate_ability_id", (num != 0) ? num : heroRow.SuperSkillId);
			Set(stageHeroInfo, "fate_ability_level", 1);
		}
		return stageHeroInfo;
	}

	/// <summary>玩家当前阵容在某个属性上的平均值（attrType 见 AttributesEnum：1=攻击 2=防御 3=生命）。</summary>
	private static int TeamAvgAttr(long uid, int attrType)
	{
		List<int> list = Formation(uid, 1);
		if (list.Count == 0)
		{
			list.Add(101);
		}
		long sum = 0;
		int n = 0;
		for (int i = 0; i < list.Count; i++)
		{
			object[] array = BattleAttrs(uid, list[i]);
			for (int j = 0; j < array.Length; j++)
			{
				Attr attr = array[j] as Attr;
				if (attr != null && (int)attr.Type == attrType)
				{
					sum += attr.Value;
					n++;
					break;
				}
			}
		}
		return (n > 0) ? ((int)(sum / n)) : 0;
	}

	/// <summary>
	/// 竞技场 AI 专用属性：在 BattleHero 的基础上把对手调成「血厚、防御高、输出低」，
	/// 让一场能打久一点、玩家可以完整打完连招，而不是几秒秒杀。
	///
	/// ★ 血量不是各按各的基数放大，而是统一成「玩家阵容平均血量的 2 倍」。
	///   3v3 里各英雄基础血量差很多（犬夜叉远高于七宝），按各自基数放大的结果是
	///   对方犬夜叉打不死、其余两个一碰就没，最后变成犬夜叉和犬夜叉单挑。
	///   统一到均值后总血量不变（3×均值 = 三人的和），但三人一样耐打，才像一场 3v3。
	///
	/// 属性编号见 AttributesEnum.bin：1=攻击 2=防御 3=生命 4=暴击 6=暴击伤害 8=伤害减免 9=穿透。
	/// </summary>
	internal static StageHeroInfo ArenaBattleHero(int heroId, long uid)
	{
		StageHeroInfo stageHeroInfo = BattleHero(heroId, uid);
		int num = TeamAvgAttr(uid, 3);
		int numDef = TeamAvgAttr(uid, 2);
		List<Attr> list = stageHeroInfo.Attr;
		for (int i = 0; i < list.Count; i++)
		{
			Attr attr = list[i];
			int num2 = (int)attr.Type;
			int num3 = attr.Value;
			switch (num2)
			{
			case 1:
				// 实测标定：×0.55 时一场打满 5 分 27 秒且判负（双方都打不死，超时算输）。
				// 压低到 30% 后玩家能赢，且自己不会先被打死。
				num3 = num3 * 30 / 100;
				break;
			case 2:
				// ★ 防御必须跟着阵容均值走，不能各自放大：
				//   对手的属性是拿玩家自己的英雄算的，所以对面刷了装备的犬夜叉
				//   防御能有 900+（654*1.4），而刚招进来、没装备没品质的杀生丸
				//   攻击只有 381（日志实测：101:atk1425 102:atk894 104:atk381），
				//   攻-防直接变成负数 → 伤害被压到 1 点。
				//   现在统一压到阵容均防的一半，保证最弱的那个角色也能打出伤害。
				num3 = ((numDef > 0) ? (numDef * 50 / 100) : (num3 * 70 / 100));
				break;
			case 3:
				num3 = ((num > 0) ? (num * 200 / 100) : (num3 * 200 / 100));
				break;
			case 4:
				num3 = num3 * 25 / 100;
				break;
			case 6:
				num3 = num3 * 35 / 100;
				break;
			case 8:
				num3 += 10;
				break;
			case 9:
				num3 = num3 * 40 / 100;
				break;
			}
			if (num3 < 1)
			{
				num3 = 1;
			}
			Set(attr, "value", num3);
		}
		return stageHeroInfo;
	}

	internal static List<object> BagItems(long uid)
	{
		UserState userState = Store.Progress(uid);
		List<object> list = new List<object>();
		foreach (KeyValuePair<int, long> item in userState.Bag)
		{
			if (item.Value > 0)
			{
				list.Add(Msg.MakeItem(item.Key, uid, (int)item.Value));
			}
		}
		return list;
	}

	internal static BagItemListResponse BuildBagResponse(long uid)
	{
		BagItemListResponse bagItemListResponse = new BagItemListResponse();
		Set(bagItemListResponse, "item", BagItems(uid).ToArray());
		return bagItemListResponse;
	}

	internal static void PushBag(Conn c, long uid, string note)
	{
		BagItemListResponse bagItemListResponse = BuildBagResponse(uid);
		TcpServer.SendRaw(c, 3002, Msg.SerializeDyn(bagItemListResponse), 0L, 3002);
		L.Log("[TCP] >> 3002 BagItemListResponse [推送:" + note + "] items=" + bagItemListResponse.Item.Count);
	}

	internal static List<ItemMsg> GrantItems(long uid, int[][] rewards, string note)
	{
		UserState userState = Store.Progress(uid);
		List<ItemMsg> list = new List<ItemMsg>();
		for (int i = 0; i < rewards.Length; i++)
		{
			int num = rewards[i][0];
			int num2 = rewards[i][1];
			if (num > 0 && num2 > 0)
			{
				userState.BagAdd(num, num2);
				list.Add(Msg.MakeItem(num, uid, num2));
			}
		}
		if (list.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int j = 0; j < list.Count; j++)
			{
				if (j > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(list[j].ItemId).Append('x').Append(list[j].Num);
			}
			L.Log("[TCP] 发放[" + note + "] " + stringBuilder.ToString());
		}
		return list;
	}

	public static void HGameEnter(Conn c, XEngine.Packet pkt, object req)
	{
		GameEnterRequest gameEnterRequest = (GameEnterRequest)((req is GameEnterRequest) ? req : null);
		long num = gameEnterRequest?.UserId ?? 0;
		int serverId = ((gameEnterRequest == null || gameEnterRequest.ServerId == 0) ? 1 : gameEnterRequest.ServerId);
		string accountName = ((gameEnterRequest != null && !string.IsNullOrEmpty(gameEnterRequest.AccountName)) ? gameEnterRequest.AccountName : ("user" + num));
		bool isNew;
		Role role = Store.EnsureRole(num, accountName, serverId, out isNew);
		Store.Save();
		long num2 = Clock.NowMs();
		UserState userState = Store.Progress(num);
		if (userState.BagGet(4000001) < 2000)
		{
			userState.BagAdd(4000001, 2000L);
		}
		if (userState.BagGet(4200001) < 2000)
		{
			userState.BagAdd(4200001, 2000L);
		}
		if (userState.BagGet(2100001) < 2000)
		{
			userState.BagAdd(2100001, 2000L);
		}
		if (userState.BagGet(1200001) < 2000)
		{
			userState.BagAdd(1200001, 2000L);
		}
		if (userState.BagGet(1100001) < 2000)
		{
			userState.BagAdd(1100001, 2000L);
		}
		if (userState.BagGet(1100002) < 2000)
		{
			userState.BagAdd(1100002, 2000L);
		}
		if (userState.BagGet(1100003) < 2000)
		{
			userState.BagAdd(1100003, 2000L);
		}
		if (userState.BagGet(5100001) < 2000)
		{
			userState.BagAdd(5100001, 2000L);
		}
		if (userState.BagGet(5200001) < 2000)
		{
			userState.BagAdd(5200001, 2000L);
		}
		if (userState.BagGet(5300001) < 2000)
		{
			userState.BagAdd(5300001, 2000L);
		}
		if (userState.BagGet(3020101) < 200)
		{
			userState.BagAdd(3020101, 200L);
		}
		if (userState.BagGet(3020201) < 200)
		{
			userState.BagAdd(3020201, 200L);
		}
		if (userState.BagGet(3020301) < 200)
		{
			userState.BagAdd(3020301, 200L);
		}
		// 残影碎片（17000001）：shadowConfig_D 里后三档残影的解锁材料就是它
		// （17000001*20 / *33 / *56）。以前一件都不发，所以「残影碎片不知道怎么获得」。
		if (userState.BagGet(17000001) < 500)
		{
			userState.BagAdd(17000001, 500L);
		}
		if (userState.BagGet(5010101) < 200)
		{
			userState.BagAdd(5010101, 200L);
		}
		if (userState.BagGet(5010201) < 200)
		{
			userState.BagAdd(5010201, 200L);
		}
		if (userState.BagGet(5010401) < 200)
		{
			userState.BagAdd(5010401, 200L);
		}
		if (userState.BagGet(5010601) < 200)
		{
			userState.BagAdd(5010601, 200L);
		}
		if (userState.BagGet(5010801) < 200)
		{
			userState.BagAdd(5010801, 200L);
		}
		Store.Save();
		c.Sess.Uid = num;
		c.Sess.Nick = role.Nick;
		c.Sess.MainTaskId = userState.MainTaskId;
		c.Sess.MainTaskReady = userState.MainTaskReady;
		c.Sess.SyncedLv = userState.Level;
		c.Sess.SyncedExp = userState.Exp;
		GameEnterResponse gameEnterResponse = new GameEnterResponse();
		Set(gameEnterResponse, "name", role.Nick);
		Set(gameEnterResponse, "level", userState.Level);
		Set(gameEnterResponse, "exp", userState.Exp);
		Set(gameEnterResponse, "copper", userState.Copper);
		Set(gameEnterResponse, "gold", (int)userState.Gold);
		Set(gameEnterResponse, "energy", 120);
		Set(gameEnterResponse, "energy_max", 120);
		Set(gameEnterResponse, "vip", 10);
		Set(gameEnterResponse, "vipExp", 999999);
		Set(gameEnterResponse, "vipAward", 0);
		Set(gameEnterResponse, "energy_buy_times_limit", 5);
		Set(gameEnterResponse, "energy_buy_times_remain", 5);
		Set(gameEnterResponse, "copper_buy_times_limit", 5);
		Set(gameEnterResponse, "copper_buy_times_remain", 5);
		Set(gameEnterResponse, "lucky_bag", (int)userState.LuckyBag);
		Set(gameEnterResponse, "guild_money", 0);
		Set(gameEnterResponse, "areane_money", 0);
		Set(gameEnterResponse, "bully_medal", 0);
		Set(gameEnterResponse, "lichbane_money", 0);
		Set(gameEnterResponse, "gender", role.Gender);
		Set(gameEnterResponse, "roleId", num);
		Set(gameEnterResponse, "level_limit", 60);
		// ★ 头像解锁只看 GameEnterResponse.pics（PicInfo 列表 → 客户端 NickData.PicList）。
		//   HeadCellData.Unlock = PicList.Find(d => d.PicId == headOrBg.level1Icon) 且未过期；
		//   activePics 是另一个字段，之前只发 activePics（还用了包里没贴图的 1003/1004），
		//   结果头像列表整排带锁、点了也换不上。
		//   HeadOrBg.bin 的 level1Icon：头像 8100010/20/30/40/60/80（对应英雄 101..108），
		//   背景 8200010/20/30/40/50（对应 201..205），这些 <id>.tex 在 APK 里都真实存在。
		Set(gameEnterResponse, "pic", userState.BlessGet("pic", 8100010L));
		// 8100050 = 桔梗（英雄 105）的头像图标。HeadOrBg.bin 由 patch_bins_headorbg.py 补了她的行，
		// 这张 pics 列表要跟着一起补，否则头像列表里桔梗那一格永远带锁、点不了「使用」
		// （也就顺带选不了她当主城形象 —— 换头像的「使用」同时会发 TownRoleChangeRequest）。
		int[] iconIds = new int[12] { 8100010, 8100020, 8100030, 8100040, 8100050, 8100060, 8100080, 8200010, 8200020, 8200030, 8200040, 8200050 };
		// pics 是 List<PicInfo>，必须整体下发。之前是在循环里反复 Set 同一个字段，
		// 等于只保留最后一项，NickData.PicList 里找不到对应 level1Icon → HeadCellData.Unlock=false，
		// 表现就是头像整排带锁、左上角头像点了换不上。
		// ★ expireTime 必须是 -1（永久）。客户端 HeadCellData.Unlock → TimeValidity(deadline)：
		//     deadline == -1            -> 解锁（显示"永久"）
		//     deadline  >  当前服务器时间 -> 解锁（限时）
		//   以前发 0，0 > now 恒为 false，于是 PicList 里每一项都判定为未解锁：
		//   头像列表整排带锁（截图里犬夜叉显示"使用中"但右下角还有锁），
		//   HeadOrBgCell 的 bt_Use 按钮也不激活，点不到 → 头像和主城形象都换不了。
		List<object> picList = new List<object>();
		for (int pi = 0; pi < iconIds.Length; pi++)
		{
			PicInfo picInfo = new PicInfo();
			Set(picInfo, "picId", iconIds[pi]);
			Set(picInfo, "expireTime", -1L);
			picList.Add(picInfo);
		}
		Set(gameEnterResponse, "pics", picList.ToArray());
		Set(gameEnterResponse, "activePics", iconIds);
		Set(gameEnterResponse, "picBg", userState.BlessGet("picbg", 8200010L));
		Set(gameEnterResponse, "power", CalcPower(num));
		Set(gameEnterResponse, "renameCost", 100);
		Set(gameEnterResponse, "changeGenderCost", 100);
		Set(gameEnterResponse, "server_time", num2);
		Set(gameEnterResponse, "link_id", 1);
		Set(gameEnterResponse, "token", "local");
		Set(gameEnterResponse, "arenaNineWin", 0);
		Set(gameEnterResponse, "firstChargeAward", false);
		Set(gameEnterResponse, "is_new", false);
		Set(gameEnterResponse, "auto_fight_enabled", true);
		Set(gameEnterResponse, "sendFlowerFree", 1);
		Set(gameEnterResponse, "throwEggFree", 1);
		Set(gameEnterResponse, "signature", "");
		Set(gameEnterResponse, "flowerNum", 0);
		Set(gameEnterResponse, "eggNum", 0);
		Set(gameEnterResponse, "last_give_energy_tick", num2);
		Set(gameEnterResponse, "guild", Msg.Empty);
		GameEnterResponse.GuideInfo guideInfo = new GameEnterResponse.GuideInfo();
		Set(guideInfo, "group_id", 1);
		Set(guideInfo, "guide_id", 999999);
		Set(gameEnterResponse, "guide_progress", new object[1] { guideInfo });
		int msgType = MT.EnumId("_GameEnterResponse");
		TcpServer.Send(c, msgType, gameEnterResponse, pkt);
		L.Log("[TCP] >> " + msgType + " GameEnterResponse nick=" + role.Nick + " uid=" + num + " sid=" + serverId + " 新角色=" + isNew + " 铜钱=" + userState.Copper + " 元宝=" + userState.Gold);
	}

	public static void HClientGetData(Conn c, XEngine.Packet pkt, object req)
	{
		ClientGetDataRequest clientGetDataRequest = (ClientGetDataRequest)((req is ClientGetDataRequest) ? req : null);
		ClientGetDataResponse clientGetDataResponse = new ClientGetDataResponse();
		Set(clientGetDataResponse, "type", (int)(clientGetDataRequest?.Type ?? ((CurrencyType)0)));
		Set(clientGetDataResponse, "value", 999999);
		Set(clientGetDataResponse, "time", Clock.NowMs());
		TcpServer.Send(c, 1008, clientGetDataResponse, pkt);
	}

	// 主城形象编号规则：客户端 TownRoleInfo 的 hero_avatar_id / clothesId 形如 101001 = 英雄id*1000+序号。
	internal static int TownAvatarOf(int heroId)
	{
		return heroId * 1000 + 1;
	}

	// 玩家当前在主城展示的形象（英雄 + 已穿戴时装），没有有效存档时回落到 101。
	internal static void TownLookOf(long uid, out int heroId, out int clothesId)
	{
		UserState st = Store.Progress(uid);
		int hero = (int)st.BlessGet("town_hero", 101L);
		if (HeroRow(hero) == null || !OwnedHeroes(uid).Contains(hero))
		{
			hero = 101;
		}
		int clothes = WornClothesOf(st, hero, HeroRow(hero));
		if (clothes <= 0)
		{
			clothes = TownAvatarOf(hero);
		}
		heroId = hero;
		clothesId = clothes;
	}

	public static void HTownEnter(Conn c, XEngine.Packet pkt, object req)
	{
		TownEnterRequest townEnterRequest = (TownEnterRequest)((req is TownEnterRequest) ? req : null);
		int num = ((townEnterRequest == null || townEnterRequest.TownId == 0) ? 1 : townEnterRequest.TownId);
		long uid = c.Sess.Uid;
		string v = (string.IsNullOrEmpty(c.Sess.Nick) ? ("user" + uid) : c.Sess.Nick);
		int townHero, townClothes;
		TownLookOf(uid, out townHero, out townClothes);
		TownRoleInfo townRoleInfo = new TownRoleInfo();
		Set(townRoleInfo, "role_id", uid);
		Set(townRoleInfo, "hero_id", townHero);
		Set(townRoleInfo, "hero_avatar_id", TownAvatarOf(townHero));
		Set(townRoleInfo, "x", 0);
		Set(townRoleInfo, "y", 0);
		Set(townRoleInfo, "name", v);
		Set(townRoleInfo, "gender", 1);
		Set(townRoleInfo, "clothesId", townClothes);
		TownEnterResponse townEnterResponse = new TownEnterResponse();
		Set(townEnterResponse, "town_id", num);
		Set(townEnterResponse, "role", new object[1] { townRoleInfo });
		TcpServer.Send(c, 140002, townEnterResponse, pkt);
		L.Log("[TCP] >> 140002 TownEnterResponse town=" + num + " role_id=" + uid + " hero=" + townHero + " clothes=" + townClothes);
		UserState userState = Store.Progress(uid);
		if (userState.Level != c.Sess.SyncedLv || userState.Exp != c.Sess.SyncedExp)
		{
			RoleUpLevel roleUpLevel = new RoleUpLevel();
			Set(roleUpLevel, "level", userState.Level);
			Set(roleUpLevel, "exp", (int)userState.Exp);
			TcpServer.Send(c, 2022, roleUpLevel, pkt);
			c.Sess.SyncedLv = userState.Level;
			c.Sess.SyncedExp = userState.Exp;
			L.Log("[TCP] >> 2022 RoleUpLevel lv=" + userState.Level + " exp=" + userState.Exp + " (on town enter)");
		}
	}

	// ★ 140022 TownRoleChangeRequest：之前分发表里根本没有这条，客户端发出去石沉大海，
	//   所以「主城形象」点了完全没反应。客户端两个入口都走这里：
	//     HeadOrBgListView.SureUsing → Request_TownRoleChange(HeadOrBg.id, 0)（换头像时顺带换主城形象）
	//     HeroSystem               → Request_TownRoleChange(focusHero.tempid, 0)（英雄界面直接切形象）
	//   这两个 id 对头像而言就是英雄编号 101..108，所以直接存成 town_hero。
	//   回包 PlayerController.OnChangeCharacter 会比较 hero_id / hero_avatar_id，只要和当前不同
	//   就会卸载旧模型并按新形象重建，主城立刻变化。
	public static void HTownRoleChange(Conn c, XEngine.Packet pkt, object req)
	{
		TownRoleChangeRequest townRoleChangeRequest = req as TownRoleChangeRequest;
		int num = Int((townRoleChangeRequest != null) ? ((object)townRoleChangeRequest.HeroId) : null, 0);
		int num2 = Int((townRoleChangeRequest != null) ? ((object)townRoleChangeRequest.HeroAvatarId) : null, 0);
		long uid = c.Sess.Uid;
		if (num <= 0 || HeroRow(num) == null)
		{
			num = 101;
		}
		UserState userState = Store.Progress(uid);
		userState.BlessSet("town_hero", num);
		Store.SaveProgress();
		int townHero, townClothes;
		TownLookOf(uid, out townHero, out townClothes);
		if (num2 > 0)
		{
			townClothes = num2;
		}
		TownRoleInfo townRoleInfo = new TownRoleInfo();
		Set(townRoleInfo, "role_id", uid);
		Set(townRoleInfo, "hero_id", townHero);
		Set(townRoleInfo, "hero_avatar_id", TownAvatarOf(townHero));
		Set(townRoleInfo, "x", 0);
		Set(townRoleInfo, "y", 0);
		Set(townRoleInfo, "name", (string.IsNullOrEmpty(c.Sess.Nick) ? ("user" + uid) : c.Sess.Nick));
		Set(townRoleInfo, "gender", 1);
		Set(townRoleInfo, "clothesId", townClothes);
		TownRoleChangeResponse townRoleChangeResponse = new TownRoleChangeResponse();
		Set(townRoleChangeResponse, "role", townRoleInfo);
		TcpServer.Send(c, 140023, townRoleChangeResponse, pkt);
		L.Log("[TCP] >> 140023 TownRoleChangeResponse hero=" + townHero + " clothes=" + townClothes);
	}

	// 140029 TownHeroIdChange：与 140022 同源，只带 hero_id，同样要落盘，
	// 否则从别的入口换形象后，下次进主城又被 140002 的存档覆盖回去。
	public static void HTownHeroIdChange(Conn c, XEngine.Packet pkt, object req)
	{
		TownHeroIdChangeRequest townHeroIdChangeRequest = req as TownHeroIdChangeRequest;
		int num = Int((townHeroIdChangeRequest != null) ? ((object)townHeroIdChangeRequest.HeroId) : null, 0);
		long uid = c.Sess.Uid;
		if (num <= 0 || HeroRow(num) == null)
		{
			num = 101;
		}
		UserState userState = Store.Progress(uid);
		userState.BlessSet("town_hero", num);
		Store.SaveProgress();
		TownHeroIdChangeResponse townHeroIdChangeResponse = new TownHeroIdChangeResponse();
		Set(townHeroIdChangeResponse, "hero_id", num);
		TcpServer.Send(c, 140030, townHeroIdChangeResponse, pkt);
		L.Log("[TCP] >> 140030 TownHeroIdChangeResponse hero=" + num);
	}

	public static void HTownNpcList(Conn c, XEngine.Packet pkt, object req)
	{
		List<object> list = new List<object>();
		for (int i = 1001; i <= 1010; i++)
		{
			TownNpcInfo townNpcInfo = new TownNpcInfo();
			Set(townNpcInfo, "id", i);
			Set(townNpcInfo, "intimacy", 0);
			Set(townNpcInfo, "surplusTimes", 0);
			list.Add(townNpcInfo);
		}
		TownNpcListResponse townNpcListResponse = new TownNpcListResponse();
		Set(townNpcListResponse, "npc", list.ToArray());
		TcpServer.Send(c, 140025, townNpcListResponse, pkt);
		L.Log("[TCP] >> 140025 TownNpcListResponse NPCs=" + list.Count);
	}

	public static void HChapterList(Conn c, XEngine.Packet pkt, object req)
	{
		List<object> list = new List<object>();
		for (int i = 1; i <= 12; i++)
		{
			ChapterListResponse.ChapterInfo chapterInfo = new ChapterListResponse.ChapterInfo();
			Set(chapterInfo, "id", i);
			Set(chapterInfo, "points", 3);
			Set(chapterInfo, "elite_points", 3);
			list.Add(chapterInfo);
		}
		ChapterListResponse chapterListResponse = new ChapterListResponse();
		Set(chapterListResponse, "chapter", list.ToArray());
		TcpServer.Send(c, 5039, chapterListResponse, pkt);
		L.Log("[TCP] >> 5039 ChapterListResponse opened=1..12");
	}

	public static void HStageList(Conn c, XEngine.Packet pkt, object req)
	{
		StageListRequest stageListRequest = (StageListRequest)((req is StageListRequest) ? req : null);
		int num = Int((stageListRequest != null) ? ((object)stageListRequest.ChapterId) : null, 1);
		if (num < 1)
		{
			num = 1;
		}
		if (num > 12)
		{
			num = 12;
		}
		UserState userState = Store.Progress(c.Sess.Uid);
		// ★ 客户端 StageListRequest 只有 chapter_id 一个字段：一次请求就要把整章的
		//   普通关(100xxx) **和** 精英关(110xxx) 一起下发。
		//   ChapterData.RefreshPassList 是拿着返回的 stage_id 去 config 找 PassBase 再赋值，
		//   只回普通关的话：精英页永远锁着、精英关通了也不显示通关，
		//   而且 HPveBegin 还会把精英关 id 改写成 100101（见下），等于白打。
		List<int> list = StageTable.StageIdsOf(num, 10);
		List<int> list2 = StageTable.StageIdsOf(num, 11);
		if (list.Count == 0)
		{
			int num2 = 100000 + num * 100 + 1;
			for (int i = num2; i < num2 + 8; i++)
			{
				list.Add(i);
			}
		}
		List<object> list3 = new List<object>();
		// 每一章最后一个普通关强制给满分：客户端 ChapterBase.GetHardUnlocked() 就是看
		// normalList[最后一个].iStar > 0，不给分的话「精英」页签点不开。
		int num3 = (list.Count > 0) ? list[list.Count - 1] : 0;
		for (int j = 0; j < list.Count; j++)
		{
			list3.Add(MakeStageInfo(list[j], userState, list[j] == num3));
		}
		for (int k = 0; k < list2.Count; k++)
		{
			// 精英关全部 opened=true（不设解锁条件），通关后 score=7 正常显示通关
			list3.Add(MakeStageInfo(list2[k], userState, false));
		}
		StageListResponse stageListResponse = new StageListResponse();
		Set(stageListResponse, "chapter_id", num);
		Set(stageListResponse, "stage", list3.ToArray());
		TcpServer.Send(c, 5019, stageListResponse, pkt);
		L.Log("[TCP] >> 5019 StageListResponse chapter=" + num + " 普通=" + list.Count + " 精英=" + list2.Count);
	}

	/// <summary>一条 StageListResponse.StageInfo。</summary>
	private static object MakeStageInfo(int stageId, UserState st, bool forceCleared)
	{
		StageListResponse.StageInfo stageInfo = new StageListResponse.StageInfo();
		Set(stageInfo, "stage_id", stageId);
		bool flag = forceCleared || st.Cleared.Contains(stageId);
		Set(stageInfo, "score", flag ? 7 : 0);
		Set(stageInfo, "opened", true);
		Set(stageInfo, "fight_times", flag ? 1 : 0);
		Set(stageInfo, "buy_times", 0);
		Set(stageInfo, "added_fight_times", 0);
		return stageInfo;
	}

	public static void HStageState(Conn c, XEngine.Packet pkt, object req)
	{
		StageStateRequest stageStateRequest = (StageStateRequest)((req is StageStateRequest) ? req : null);
		List<int> list = new List<int>();
		if (stageStateRequest != null && stageStateRequest.StageId != null)
		{
			list.AddRange(stageStateRequest.StageId);
		}
		object[] array = new object[list.Count];
		for (int i = 0; i < list.Count; i++)
		{
			array[i] = true;
		}
		StageStateResponse stageStateResponse = new StageStateResponse();
		Set(stageStateResponse, "stage_id", list.ToArray());
		Set(stageStateResponse, "open", array);
		TcpServer.Send(c, 50089, stageStateResponse, pkt);
	}

	public static void HPveBegin(Conn c, XEngine.Packet pkt, object req)
	{
		PveRequest pveRequest = (PveRequest)((req is PveRequest) ? req : null);
		int num = Int((pveRequest != null) ? ((object)pveRequest.StageId) : null, 100101);
		// ★ 以前这里写死 `if (num < 100101 || num > 101208) num = 100101;`，
		//   精英关（110xxx）一律被改写成 100101：客户端进去打的是第一章第一关，
		//   通关记的也是 100101，于是「精英关打完不显示通关、后面关也解不开」。
		//   现在只要 StageConfig 里真有这一关就照原样下发。
		if (!StageTable.Exists(num))
		{
			num = 100101;
		}
		long uid = c.Sess.Uid;
		c.Sess.ActiveStage = num;
		List<int> list = Formation(uid, 1);
		List<object> list2 = new List<object>();
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(BattleHero(list[i], uid));
		}
		StageRoleInfo stageRoleInfo = new StageRoleInfo();
		Set(stageRoleInfo, "role_id", uid);
		Set(stageRoleInfo, "hero", list2.ToArray());
		PveResponse pveResponse = new PveResponse();
		Set(pveResponse, "stage_id", num);
		Set(pveResponse, "role", new object[1] { stageRoleInfo });
		TcpServer.Send(c, 50085, pveResponse, pkt);
		L.Log("[TCP] >> 50085 PveResponse stage=" + num + " uid=" + uid + " 队伍=[" + JoinInts(list) + "]");
	}

	internal static string JoinInts(List<int> xs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < xs.Count; i++)
		{
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(xs[i]);
		}
		return stringBuilder.ToString();
	}

	public static void HBattleReward(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int activeStage = c.Sess.ActiveStage;
		// 通关奖励改为读客户端自己的关卡表 StageConfig.rewards（itemId 2=银币 4=经验）。
		// 原来这里把经验写死成 150，导致不论打哪一关都只加 150，与关卡面板显示的奖励数量不符。
		int copperAdd = StageTable.CopperOf(activeStage, 1000);
		long num = StageTable.ExpOf(activeStage, 150);
		userState.Copper += copperAdd;
		userState.BagAdd(4000001, 2L);
		userState.BagAdd(2100001, 2L);
		userState.BagAdd(4200001, 1L);
		if (!userState.Cleared.Contains(activeStage))
		{
			userState.Cleared.Add(activeStage);
		}
		long num2 = (userState.Exp += num);
		int num3 = (userState.Level = PlayerLvTable.LevelFromExp(num2));
		int mainTaskId = userState.MainTaskId;
		if (!userState.MainTaskReady && userState.Cleared.Count > 0)
		{
			userState.MainTaskReady = true;
			c.Sess.MainTaskReady = true;
			TaskUpdateResponse taskUpdateResponse = new TaskUpdateResponse();
			TaskMsg taskMsg = new TaskMsg();
			Set(taskMsg, "id", mainTaskId);
			Set(taskMsg, "status", 2);
			Set(taskMsg, "curNum", 1);
			Set(taskMsg, "needNum", 1);
			Set(taskUpdateResponse, "task", new object[1] { taskMsg });
			TcpServer.Send(c, 70005, taskUpdateResponse, pkt);
		}
		c.Sess.MainTaskId = userState.MainTaskId;
		Store.SaveProgress();
		BattleRewardResponseV2 battleRewardResponseV = new BattleRewardResponseV2();
		Set(battleRewardResponseV, "success", true);
		Set(battleRewardResponseV, "score", 7);
		Set(battleRewardResponseV, "role_exp", (int)num2);
		Set(battleRewardResponseV, "role_level", num3);
		List<object> list = new List<object>();
		list.Add(Msg.MakeItem(2, uid, copperAdd));
		list.Add(Msg.MakeItem(4, uid, (int)num));
		Set(battleRewardResponseV, "fixed_rewards", list.ToArray());
		List<object> list2 = new List<object>();
		list2.Add(Msg.MakeItem(4000001, uid, 2));
		list2.Add(Msg.MakeItem(2100001, uid, 2));
		list2.Add(Msg.MakeItem(4200001, uid, 1));
		Set(battleRewardResponseV, "star_rewards", list2.ToArray());
		Set(battleRewardResponseV, "vip_rewards", Msg.EmptyListBox);
		TcpServer.Send(c, 5082, battleRewardResponseV, pkt);
		L.Log("[TCP] >> 5082 BattleRewardResponseV2 stage=" + activeStage + " score=7 exp=" + num + " copper=" + copperAdd + " totalExp=" + num2 + " lv=" + num3);
	}

	internal static int LevelFromExp(long exp)
	{
		return PlayerLvTable.LevelFromExp(exp);
	}

	// ---- 时装 / 魅力（FaceLiftSystem）辅助 ----
	//
	// 每个英雄的时装 id = heroId*1000 + n，定义在客户端 AvatarList.bin：
	//   101→101001/101002, 102→102001/102002/102003, 103→103001, 104→104001,
	//   106→106001/106002, 108→108001/108002
	// 这里只把「默认时装 + 已解锁时装」发出去，避免把别的英雄的时装 id 混进来
	// （客户端 HeroData.Response_UnlockClothesResponse 会 GetClothesBase(id) 取不到就 NRE）。
	private static readonly Dictionary<int, int[]> ClothesByHero = BuildClothesByHero();

	private static Dictionary<int, int[]> BuildClothesByHero()
	{
		Dictionary<int, int[]> dictionary = new Dictionary<int, int[]>();
		dictionary[101] = new int[2] { 101001, 101002 };
		dictionary[102] = new int[3] { 102001, 102002, 102003 };
		dictionary[103] = new int[1] { 103001 };
		dictionary[104] = new int[1] { 104001 };
		// 桔梗（105）：AvatarList.bin 里她的时装就是 105001 / 105002 两套，全部默认解锁
		dictionary[105] = new int[2] { 105001, 105002 };
		dictionary[106] = new int[2] { 106001, 106002 };
		dictionary[108] = new int[2] { 108001, 108002 };
		return dictionary;
	}

	/// <summary>该英雄的时装 id 列表（首个 = 默认时装）。带上玩家已解锁/正在穿的时装。</summary>
	private static List<int> HeroClothes(int heroId, UserState st)
	{
		List<int> list = new List<int>();
		int[] array;
		if (ClothesByHero.TryGetValue(heroId, out array))
		{
			list.AddRange(array);
		}
		else
		{
			Tables.HeroRow heroRow = HeroRow(heroId);
			list.Add((heroRow != null) ? heroRow.ClothesId : heroId * 1000 + 1);
		}
		if (st != null)
		{
			// 已解锁的时装：clothes_own_<clothesId>=1（复用 Store 已持久化的 bless 字典）
			for (int id = heroId * 1000 + 1; id <= heroId * 1000 + 99; id++)
			{
				if (st.BlessGet("clothes_own_" + id, 0L) > 0 && !list.Contains(id))
				{
					list.Add(id);
				}
			}
			int worn = WornClothesOf(st, heroId, HeroRow(heroId));
			if (worn != 0 && !list.Contains(worn))
			{
				list.Insert(0, worn);
			}
		}
		return list;
	}

	/// <summary>时装 id 是否属于该英雄（AvatarList 约定 id = heroId*1000 + n，n &lt; 1000）。</summary>
	private static bool IsClothesOfHero(int clothesId, int heroId)
	{
		return clothesId / 1000 == heroId;
	}

	/// <summary>heroUid（本项目约定 = uid*100 + heroId，全局统一）→ 英雄 tempid；认不出来返回 0。</summary>
	internal static int HeroIdOf(long heroUid, long uid)
	{
		long id = heroUid - uid * 100;
		if (id > 0 && id < 1000)
		{
			return (int)id;
		}
		return 0;
	}

	/// <summary>当前穿着的时装：玩家改过就用玩家选的，否则用表里默认那件（heroId*1000+1）。</summary>
	private static int WornClothesOf(UserState st, int heroId, Tables.HeroRow heroRow)
	{
		if (st != null)
		{
			int worn = (int)st.BlessGet("worn_clothes_" + heroId, 0L);
			if (worn != 0)
			{
				return worn;
			}
		}
		return (heroRow != null) ? heroRow.ClothesId : heroId * 1000 + 1;
	}

	/// <summary>设置/读取都走 bless 字典的同一套键，Game2 的 4120/4122/4124 也用这里。</summary>
	internal static void SetWornClothes(UserState st, int heroId, int clothesId)
	{
		st.BlessSet("worn_clothes_" + heroId, clothesId);
		if (IsClothesOfHero(clothesId, heroId))
		{
			st.BlessSet("clothes_own_" + clothesId, 1L);
		}
	}

	internal static void SetClothesOwned(UserState st, int clothesId)
	{
		st.BlessSet("clothes_own_" + clothesId, 1L);
	}

	/// <summary>
	/// 当前魅力等级，必须落在 1..20（CharmLevel.bin 的合法键范围）。
	/// 玩家没升过就是 1 级；clothesAddPoint/beautifulMax/beautifulIsMax 都用裸索引器取 "英雄id+3位等级"，
	/// 所以 0 会让客户端 KeyNotFoundException。
	/// </summary>
	private static int CharmLvOf(UserState st, int heroId)
	{
		int lv = (int)st.BlessGet("charm_lv_" + heroId, 1L);
		if (lv < 1)
		{
			return 1;
		}
		int maxLv = CharmTable.MaxLevelOf(heroId, 20);
		return (lv > maxLv) ? maxLv : lv;
	}

	/// <summary>魅力进度值，必须 0 &lt;= v &lt; 该级的 charmDemand，否则进度条会算出大于 1 的比例。</summary>
	private static int CharmValOf(UserState st, int heroId, int lv)
	{
		int demand = CharmTable.DemandOf(heroId * 1000 + lv, 0);
		int val = (int)st.BlessGet("charm_val_" + heroId, 0L);
		if (val < 0)
		{
			return 0;
		}
		if (demand > 0 && val >= demand)
		{
			return demand - 1;
		}
		return val;
	}

	// ---- 竞技场（异步竞技 / PvpAsync 60125..60138） ----
	//
	// 客户端 ArenaSystem.Open() → ArenaData.Request → 60125，等 60126。
	// 空响应会让 ArenaSystem.Refresh 在 Chest.Refresh() 里
	//   ArenaReward.Find(a => a.Num == Target).RewardState
	// 拿到 null 然后 NRE，异常发生在 Refresh() 的最后一行 bIsTransitionPreFinished = true 之前，
	// 于是 UIOpenTransition 永远停在等待态 = 点击竞技场一直加载。
	// 而且 resetExpireTime=0 会让 ArenaData.TimeLeft 变成 -1，OnTick 每秒重发一次 60125。
	// 所以 60126 必须给出：未来时间戳 + num 为 1/4/9 的 rewardState。

	private const int ArenaOpponentCount = 9;

	private static int[] ArenaOpponentIds()
	{
		int[] array = new int[ArenaOpponentCount];
		for (int i = 0; i < ArenaOpponentCount; i++)
		{
			array[i] = 900001 + i;
		}
		return array;
	}

	private static long ArenaResetExpire(UserState st)
	{
		long now = Clock.NowMs();
		long expire = st.BlessGet("arena_reset_ts", 0L);
		if (expire <= now)
		{
			expire = now + 86400000L;
			st.BlessSet("arena_reset_ts", expire);
		}
		return expire;
	}

	private static int ArenaWinCount(UserState st)
	{
		int[] ids = ArenaOpponentIds();
		int n = 0;
		for (int i = 0; i < ids.Length; i++)
		{
			if (st.BlessGet("arena_win_" + ids[i], 0L) > 0)
			{
				n++;
			}
		}
		return n;
	}

	/// <summary>已取胜场次 → 宝箱状态：1/4/9 胜三个宝箱，0=未达成 1=可领取 2=已领取。</summary>
	private static int ArenaChestState(int wins, int need, UserState st)
	{
		if (wins < need)
		{
			return 0;
		}
		return (st.BlessGet("arena_chest_" + need, 0L) > 0) ? 2 : 1;
	}

	public static void HPvpAsyncInfo(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		long expire = ArenaResetExpire(userState);
		int wins = ArenaWinCount(userState);
		Store.SaveProgress();

		PvpAsyncInfoResponse val = new PvpAsyncInfoResponse();

		// 1 hero_id：自己的竞技场阵容
		// ★ 竞技场用的是自己的阵容槽 FormationTypePvpAsync = 3（不是关卡的 1）。
		//   以前读 type 1，所以在竞技场里点"调整阵容"换了人（存到 3）也白换，
		//   进竞技场永远还是关卡那套人。
		List<int> list = ArenaFormation(uid);
		if (list.Count == 0)
		{
			list.Add(101);
		}
		Set(val, "hero_id", list);

		// 2 opponent：9 个对手；win 决定已完成行
		//
		// ★ 头像编号必须是 APK 里 image/head/player/<id>.tex 真实存在的贴图。
		//   曾经发过 1003 / 1004 / 背景 1，logcat 里全是
		//     Unable to open archive file: .../image/head/player/1003.tex
		//   表现就是「有些 AI 有头像、有些是空的」。
		//   可用头像：1001 1002 1006 1007 1008 1009 1010 1011 1012
		//   可用背景：3000 3001 3002 3003 3004
		// ★ 英雄编号同样只能取客户端 heroAttr.bin 里有的（101/102/103/104/106/108）。
		// ★ 头像编号必须和「玩家自己的 pic」同一套编号：客户端 RoleEntry.Refresh 用的是
		//     AssetBundleType.PlayerHead + PicId，RankSystem.ShowRoleIcon 同样把
		//     PlayerData.pic / picBg 直接当 PlayerHead 贴图名用，而这两个值恒等于
		//     HeadOrBg.level1Icon（810xxxx = 头像，820xxxx = 头像框底图）。
		//   以前发 1001/1002/1006..1012 和 3000..3004：贴图文件虽然存在，
		//   但 1006 之后的都是没画内容的占位图（3.6KB vs 1001 的 9.1KB），
		//   渲染出来就是空白 —— 表现就是"AI 有些有头像、有些是空的"。
		//   这里统一改成头像选择界面里验证过能正常显示的那 6 张。
		int[] ids = ArenaOpponentIds();
		int[] heroPool = new int[] { 101, 102, 103, 104, 106, 108 };
		int[] picPool = new int[] { 8100010, 8100020, 8100030, 8100040, 8100060, 8100080 };
		int[] picBgPool = new int[] { 8200010, 8200020, 8200030, 8200040, 8200050 };
		int playerPower = CalcPower(uid);
		Random rng = new Random((int)(uid ^ Clock.NowMs()));
		List<object> list2 = new List<object>();
		for (int i = 0; i < ids.Length; i++)
		{
			OpponentMsg opponentMsg = new OpponentMsg();
			Set(opponentMsg, "id", (long)ids[i]);
			Set(opponentMsg, "name", "挑战者" + (i + 1));
			// Bug2: 战力基于玩家战力，浮动 70%~130%
			int basePower = (int)(playerPower * (0.7 + rng.NextDouble() * 0.6));
			Set(opponentMsg, "power", basePower);
			// Bug1: 随机头像
			Set(opponentMsg, "picId", picPool[rng.Next(picPool.Length)]);
			Set(opponentMsg, "picBgId", picBgPool[rng.Next(picBgPool.Length)]);
			Set(opponentMsg, "level", userState.Level + rng.Next(-2, 3));
			Set(opponentMsg, "sex", 1 + rng.Next(2));
			Set(opponentMsg, "win", userState.BlessGet("arena_win_" + ids[i], 0L) > 0);
			Set(opponentMsg, "difficulty", i / 3 + 1);
			list2.Add(opponentMsg);
		}
		Set(val, "opponent", list2.ToArray());

		// 3 resetExpireTime：必须大于当前服务器时间，否则客户端每秒重发 60125
		Set(val, "resetExpireTime", expire);
		Set(val, "nineWin", wins);
		// 客户端 ArenaData.RestChallengeNum 就是这里的下发值，挑战按钮要求它小于 VIP 表的 challengeNum。
		// 想「无限挑战 / 无限重置」，最稳的做法是下发端不消耗：已用次数恒发 0，
		// CanResetTime 于是永远等于 VIP 表的 buyPKReset 满值，重置完再进面板仍可重置。
		Set(val, "useChallengeNum", 0);
		Set(val, "useReset", 0);
		Set(val, "rewardFlags", Msg.EmptyListBox);

		// 8 rewardState：num 必须是 1/4/9（ArenaSystem 里 Chests[0..2].Target）
		List<object> list3 = new List<object>();
		int[] needs = new int[3] { 1, 4, 9 };
		for (int j = 0; j < needs.Length; j++)
		{
			ArenaReward arenaReward = new ArenaReward();
			Set(arenaReward, "num", needs[j]);
			Set(arenaReward, "rewardState", ArenaChestState(wins, needs[j], userState));
			list3.Add(arenaReward);
		}
		Set(val, "rewardState", list3.ToArray());

		TcpServer.Send(c, 60126, val, pkt);
		L.Log("[TCP] >> 60126 PvpAsyncInfoResponse wins=" + wins + " resetExpire=" + expire + " team=[" + JoinInts(list) + "]");
		// 诊断用：把竞技场我方阵容每人的 攻击/生命/防御 打进日志，
		// 方便核对"某个角色伤害只有 1"到底是属性没算出来还是别的原因。
		L.Log("[TCP] 竞技场我方属性 " + AttrBrief(uid, list));
	}

	/// <summary>调试用：把一队人的 攻击/生命/防御 拼成 "101:atk250/hp3000/def120"。</summary>
	internal static string AttrBrief(long uid, List<int> team)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < team.Count; i++)
		{
			StageHeroInfo stageHeroInfo = BattleHero(team[i], uid);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			List<Attr> list = stageHeroInfo.Attr;
			for (int j = 0; j < list.Count; j++)
			{
				int num4 = (int)list[j].Type;
				if (num4 == 1) num = list[j].Value;
				else if (num4 == 3) num2 = list[j].Value;
				else if (num4 == 2) num3 = list[j].Value;
			}
			stringBuilder.Append(team[i]).Append(":atk").Append(num).Append("/hp").Append(num2).Append("/def").Append(num3).Append(' ');
		}
		return stringBuilder.ToString();
	}

	public static void HPvpAsyncBegin(Conn c, XEngine.Packet pkt, object req)
	{
		PvpAsyncRequest val = (PvpAsyncRequest)((req is PvpAsyncRequest) ? req : null);
		long uid = c.Sess.Uid;
		long opponentId = Long((val != null) ? ((object)val.OpponentRoleId) : null, 900001L);
		c.Sess.ArenaOpponentId = opponentId;

		// 50087：至少两个 role（客户端 ArenaSystem 会直接取 Role[1].Hero）。
		//
		// ★ stage_id 必须是真正的竞技场关卡 240101/240102/240103（StageConfig 里 stageType=24 的三关）。
		//   FightingContext 构造时会做 int.Parse(stageId.ToString().Substring(0, 2)) 取 LevelType，
		//   发 1 会直接 Substring 越界；随后 ToAsyncPvpLevel 里
		//     _sceneName = "Arena" + StageId.ToString().Substring(2, 4)
		//   得到 arena0101/0102/0103，正好对应 APK 里仅有的三个竞技场场景包。
		//   发别的值（例如 100302）会去找不存在的 arena0302，表现就是卡在「连接中」。
		PvpAsyncResponse val2 = new PvpAsyncResponse();
		int[] arenaStages = new int[3] { 240101, 240102, 240103 };
		int arenaStage = arenaStages[(int)(Math.Abs(opponentId) % 3)];
		Set(val2, "stage_id", arenaStage);
		List<object> list = new List<object>();
		object[] array = BattleRole(uid, 3);
		for (int i = 0; i < array.Length; i++)
		{
			list.Add(array[i]);
		}
		StageRoleInfo stageRoleInfo = new StageRoleInfo();
		Set(stageRoleInfo, "role_id", opponentId);
		// AI 随机阵容。编号必须同时满足：服务端 Tables.Heroes 有属性 + 客户端 heroAttr.bin 有该英雄。
		// 曾发过 109/110/111，客户端 CreateHero 取不到配置直接 NullReferenceException，
		// 表现就是点挑战后停在「连接中」，回主城后所有场景请求全部堵死。
		int[] allHeroes = new int[] { 101, 102, 103, 104, 106, 108 };
		Random enemyRng = new Random((int)(opponentId ^ Clock.NowMs()));
		List<int> enemyTeam = new List<int>();
		List<int> heroCopy = new List<int>(allHeroes);
		for (int k = heroCopy.Count - 1; k > 0; k--)
		{
			int swap = enemyRng.Next(k + 1);
			int tmp = heroCopy[k]; heroCopy[k] = heroCopy[swap]; heroCopy[swap] = tmp;
		}
		for (int k = 0; k < Math.Min(3, heroCopy.Count); k++)
		{
			enemyTeam.Add(heroCopy[k]);
		}
		List<object> list3 = new List<object>();
		for (int j = 0; j < enemyTeam.Count; j++)
		{
			// 必须用玩家uid计算属性，opponentId=900001是假ID没有存档数据会导致NullRef
			// 再用 ArenaBattleHero 把 AI 调成「血厚、伤害低」，让一场能打久一点。
			list3.Add(ArenaBattleHero(enemyTeam[j], uid));
		}
		Set(stageRoleInfo, "hero", list3.ToArray());
		list.Add(stageRoleInfo);
		Set(val2, "role", list.ToArray());
		Set(val2, "difficulty", 1);

		TcpServer.Send(c, 50087, val2, pkt);
		L.Log("[TCP] >> 50087 PvpAsyncResponse opponent=" + opponentId + " arenaStage=" + arenaStage + " (场景 arena" + arenaStage.ToString().Substring(2, 4) + ")");
	}

	public static void HPvpAsyncFinish(Conn c, XEngine.Packet pkt, object req)
	{
		PvpAsyncFinishRequest val = (PvpAsyncFinishRequest)((req is PvpAsyncFinishRequest) ? req : null);
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		bool win = val != null && val.Win;
		long opponentId = c.Sess.ArenaOpponentId;
		if (opponentId == 0)
		{
			opponentId = 900001L;
		}
		if (win)
		{
			userState.BlessSet("arena_win_" + opponentId, 1L);
		}
		userState.BlessSet("arena_used", userState.BlessGet("arena_used", 0L) + 1L);
		int wins = ArenaWinCount(userState);
		Store.SaveProgress();

		PvpAsyncFinishResponse val2 = new PvpAsyncFinishResponse();
		// opponetRoleId 必须等于之前 60126 里发过的 OpponentMsg.id，否则 ArenaData.SetWin 里 GetOpponent 返回 null
		Set(val2, "opponetRoleId", opponentId);
		Set(val2, "winNum", wins);
		List<object> list = new List<object>();
		if (win)
		{
			list.Add(Msg.MakeItem(2, uid, 500));
			list.Add(Msg.MakeItem(2100001, uid, 2));
		}
		Set(val2, "rewards", list.ToArray());
		// ★ 客户端 BattleResultPanel_ArenaWin.Open 里是 (rate != 1) ? rewards[j].Num / rate : ...
		//   发 0 会 DivideByZeroException，结算面板打不开 → 打完一局永久卡住只能强退。
		Set(val2, "rate", 1);
		Set(val2, "nextRewardRest", 0);

		TcpServer.Send(c, 60134, val2, pkt);
		L.Log("[TCP] >> 60134 PvpAsyncFinishResponse win=" + win + " opponent=" + opponentId + " winNum=" + wins);
	}

	public static void HPvpAsyncReset(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int used = (int)userState.BlessGet("arena_reset_used", 0L) + 1;
		userState.BlessSet("arena_reset_used", used);
		// 重置后重新开始：清掉对手胜场与宝箱领取状态，并把重置时间推到下一个周期
		int[] ids = ArenaOpponentIds();
		for (int i = 0; i < ids.Length; i++)
		{
			userState.BlessSet("arena_win_" + ids[i], 0L);
		}
		int[] needs = new int[3] { 1, 4, 9 };
		for (int j = 0; j < needs.Length; j++)
		{
			userState.BlessSet("arena_chest_" + needs[j], 0L);
		}
		userState.BlessSet("arena_reset_ts", Clock.NowMs() + 86400000L);
		Store.SaveProgress();

		PvpAsyncResetResponse val = new PvpAsyncResetResponse();
		// 客户端 CanResetTime = VIP表.buyPKReset - UseReset。
		// 「无限重置」在下发端实现：这里永远回 0，可重置次数就恒为满值；
		// HPvpAsyncInfo 里的 useReset 同样固定下发 0，面板刷新也不会变少。
		// used 仍然存进存档，方便以后想恢复真实限次时只要改回下发 used。
		Set(val, "useReset", 0);
		TcpServer.Send(c, 60130, val, pkt);
		L.Log("[TCP] >> 60130 PvpAsyncResetResponse useReset=0 (stored=" + used + ")");
	}

	public static void HPvpAsyncGetWinReward(Conn c, XEngine.Packet pkt, object req)
	{
		PvpAsyncGetWinRewardRequest val = (PvpAsyncGetWinRewardRequest)((req is PvpAsyncGetWinRewardRequest) ? req : null);
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int num = Int((val != null) ? ((object)val.Num) : null, 1);
		// 只允许领取 1/4/9 三个宝箱，且必须已达成
		if (num != 1 && num != 4 && num != 9)
		{
			num = 1;
		}
		int wins = ArenaWinCount(userState);
		if (wins >= num && userState.BlessGet("arena_chest_" + num, 0L) <= 0)
		{
			userState.BlessSet("arena_chest_" + num, 1L);
			GrantItems(uid, new int[2][] { new int[2] { 2, 1000 * num }, new int[2] { 2100001, num } }, "arena_chest_" + num);
			Store.SaveProgress();
			PushBag(c, uid, "arena_chest");
		}
		PvpAsyncGetWinRewardResponse val2 = new PvpAsyncGetWinRewardResponse();
		Set(val2, "num", num);
		TcpServer.Send(c, 60138, val2, pkt);
		L.Log("[TCP] >> 60138 PvpAsyncGetWinRewardResponse num=" + num + " wins=" + wins);
	}

	// ================= 觉醒 / 铸灵（heroAwakeConfig） =================
	//
	// 客户端 HeroBase 的这些属性都是**裸索引器**：
	//   quality       = m_heroAwakeConfig[tempid*100 + qualityLv].showQly
	//   growUpTween   = ...[qualityLv].growthRateInterval
	//   lvUpNeedItems = ...[qualityLv].awokeCost
	// 每个英雄在表里只有 quality 0..9 十行。老服务端 HHeroQly 是无条件 +1，
	// 所以点十下铸灵品质就变 10 → getValue 返回 null → 一路 NRE：
	// 面板卡死、切不了角色、别的界面一起废（logcat 10:16:11/17/23 三波 NullReference）。
	//
	// 现在：品质按表封顶；成长率落在本品质的 [min,max] 区间里；
	// 成长率到顶再点就是「觉醒」→ 品质 +1、成长率回到新品质的下限。

	/// <summary>存盘键沿用 heroUid 形式（CalcPower / BattleAttrs 一直这么读）。</summary>
	private static long HeroQlyKey(long uid, int heroId)
	{
		return uid * 100 + heroId;
	}

	internal static int HeroQlyOf(UserState st, int heroId)
	{
		int q = (int)st.BlessGet("hero_qly_" + HeroQlyKey(st.Uid, heroId), 0L);
		int max = HeroAwakeTable.MaxQuality(heroId);
		if (q < 0) q = 0;
		if (q > max) q = max;          // 老存档被点到 10+ 的，读的时候就夹回来
		return q;
	}

	internal static int HeroGrowOf(UserState st, int heroId)
	{
		int q = HeroQlyOf(st, heroId);
		int min = HeroAwakeTable.GrowthMin(heroId, q);
		int max = HeroAwakeTable.GrowthMax(heroId, q);
		int g = (int)st.BlessGet("hero_grow_" + HeroQlyKey(st.Uid, heroId), min);
		if (g < min) g = min;          // 老存档的 100 之类无效值
		if (max > 0 && g > max) g = max;
		return g;
	}

	internal static void SetHeroQlyGrow(UserState st, int heroId, int qly, int grow)
	{
		long key = HeroQlyKey(st.Uid, heroId);
		st.BlessSet("hero_qly_" + key, qly);
		st.BlessSet("hero_grow_" + key, grow);
	}

	public static void HHeroList(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		List<int> list = OwnedHeroes(uid);
		List<int> list2 = new List<int>();
		for (int i = 0; i < list.Count; i++)
		{
			if (Tables.Heroes.ContainsKey(list[i]))
			{
				list2.Add(list[i]);
			}
		}
		if (list2.Count == 0)
		{
			list2.Add(101);
		}
		List<object> list3 = new List<object>();
		for (int j = 0; j < list2.Count; j++)
		{
			int num = list2[j];
			Tables.HeroRow heroRow = HeroRow(num);
			int wornClothes = WornClothesOf(userState, num, heroRow);
			// 时装（FaceLiftSystem）面板会读 HeroBase.clothesAddPoint/beautifulMax，
			// 它们用裸索引器 CharmLevel.Data1[tempid + beautifulLv.ToString("000")]，
			// 而 CharmLevel.bin 只有 1..20 级。原先发 charmLv=0 → 查 "101000" → KeyNotFoundException，
			// 异常发生在 FaceLiftSystem.Open() 的最后一行之前，跳转遮罩永远停在等待态 = 无限加载。
			// 另外 unlockClothes 必须包含当前穿着的时装，否则界面认为它未解锁。
			int charmLv = CharmLvOf(userState, num);
			List<int> list4 = HeroClothes(num, userState);
			if (wornClothes != 0 && !list4.Contains(wornClothes))
			{
				list4.Insert(0, wornClothes);
			}
			HeroMsg heroMsg = new HeroMsg();
			Set(heroMsg, "heroId", num);
			Set(heroMsg, "heroUid", uid * 100 + num);
			// 觉醒品质/成长率必须来自 heroAwakeConfig：客户端拿 growthRate/10*10 当 growUp，
			// 再和表里的 growthRateInterval 比较决定「铸灵 / 觉醒」两个按钮的状态。
			// 旧值写死 1 / 100，跟表里的 1000~4500 完全对不上，界面直接是坏的。
			Set(heroMsg, "heroQly", HeroQlyOf(userState, num));
			Set(heroMsg, "power", CalcPower(uid));
			Set(heroMsg, "spMax", heroRow?.GetAttr(46, 1000) ?? 1000);
			Set(heroMsg, "attr", BattleAttrs(uid, num));
			Set(heroMsg, "growthRate", HeroGrowOf(userState, num));
			Set(heroMsg, "charmVal", CharmValOf(userState, num, charmLv));
			Set(heroMsg, "charmLv", charmLv);
			Set(heroMsg, "clothes", wornClothes);
			Set(heroMsg, "unlockClothes", list4);
			list3.Add(heroMsg);
		}
		HeroListResponse heroListResponse = new HeroListResponse();
		Set(heroListResponse, "hero", list3.ToArray());
		TcpServer.Send(c, 4001, heroListResponse, pkt);
		L.Log("[TCP] >> 4001 HeroListResponse roster=[" + JoinInts(list2) + "]");
	}

	public static void HHeroRecruit(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		List<int> list = OwnedHeroes(uid);
		List<object> list2 = new List<object>();
		List<int> list3 = new List<int>(Tables.Recruits.Keys);
		list3.Sort();
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < list3.Count; i++)
		{
			int num = list3[i];
			Tables.RecruitRow recruitRow = Tables.Recruits[num];
			if (num != 100 && recruitRow.Required > 0 && HeroRow(num) != null)
			{
				RecruitInfo recruitInfo = new RecruitInfo();
				Set(recruitInfo, "heroId", num);
				int num2;
				bool flag;
				if (list.Contains(num))
				{
					num2 = recruitRow.Required;
					flag = true;
				}
				else if (recruitRow.Type == 1 && recruitRow.StageId != 0)
				{
					// 通关解锁：只看「目标关」会在跳过前面关卡时永远解不开
					// （服务端只把实际打过的那一关记进 cleared）。改成同章任意一关通关即可。
					bool num3 = StageTable.ChapterCleared(recruitRow.StageId, userState.Cleared);
					num2 = (num3 ? recruitRow.Required : 0);
					flag = num3;
				}
				else if (recruitRow.Type == 4 && recruitRow.ItemId != 0)
				{
					num2 = (int)userState.BagGet(recruitRow.ItemId);
					flag = num2 >= recruitRow.Required;
				}
				else
				{
					num2 = 0;
					flag = false;
				}
				Set(recruitInfo, "status", flag);
				Set(recruitInfo, "curNum", num2);
				list2.Add(recruitInfo);
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(num).Append('=').Append(num2)
					.Append('/')
					.Append(recruitRow.Required);
				if (flag)
				{
					stringBuilder.Append("可激活");
				}
			}
		}
		HeroRecruitResponse heroRecruitResponse = new HeroRecruitResponse();
		Set(heroRecruitResponse, "recruitInfo", list2.ToArray());
		TcpServer.Send(c, 4042, heroRecruitResponse, pkt);
		L.Log("[TCP] >> 4042 HeroRecruitResponse " + stringBuilder.ToString());
	}

	public static void HHeroSummon(Conn c, XEngine.Packet pkt, object req)
	{
		HeroSummonRequest heroSummonRequest = (HeroSummonRequest)((req is HeroSummonRequest) ? req : null);
		int num = Int((heroSummonRequest != null) ? ((object)heroSummonRequest.HeroId) : null, 0);
		if (HeroRow(num) == null)
		{
			num = 103;
		}
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		if (Tables.Recruits.TryGetValue(num, out var value))
		{
			num2 = value.Required;
			num3 = value.ItemId;
		}
		if (num2 > 0 && num3 != 0)
		{
			long num5 = userState.BagGet(num3);
			if (num5 < num2)
			{
				L.Log("[TCP] 注意：激活 " + num + " 时 " + num3 + " 只有 " + num5 + "/" + num2);
			}
			num4 = (int)Math.Min(num5, num2);
			userState.Bag[num3] = num5 - num4;
		}
		if (!userState.Heroes.Contains(num))
		{
			userState.Heroes.Add(num);
		}
		userState.Heroes.Sort();
		Store.SaveProgress();
		HeroSummonResponse heroSummonResponse = new HeroSummonResponse();
		Set(heroSummonResponse, "heroId", num);
		Set(heroSummonResponse, "heroUid", uid * 100 + num);
		TcpServer.Send(c, 4004, heroSummonResponse, pkt);
		L.Log("[TCP] >> 4004 HeroSummonResponse hero=" + num + " heroUid=" + (uid * 100 + num) + " 消耗" + num3 + "x" + num4);
		PushBag(c, uid, "激活角色" + num);
	}

	public static void HBagList(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		if (userState.Bag.Count == 0 && userState.Cleared.Count > 0)
		{
			userState.BagAdd(5010401, userState.Cleared.Count);
		}
		if (userState.BagGet(2100001) < 5)
		{
			userState.Bag[2100001] = 10L;
		}
		Store.SaveProgress();
		BagItemListResponse bagItemListResponse = BuildBagResponse(uid);
		TcpServer.Send(c, 3002, bagItemListResponse, pkt);
		L.Log("[TCP] >> 3002 BagItemListResponse items=" + bagItemListResponse.Item.Count);
	}

	public static void HBlessInfo(Conn c, XEngine.Packet pkt, object req)
	{
		UserState userState = Store.Progress(c.Sess.Uid);
		List<object> list = new List<object>();
		for (int i = 0; i < C.BlessCosts.Length; i++)
		{
			BlessBaseInfo blessBaseInfo = new BlessBaseInfo();
			Set(blessBaseInfo, "id", C.BlessCosts[i][0]);
			Set(blessBaseInfo, "itemTemplateId", C.BlessCosts[i][1]);
			Set(blessBaseInfo, "itemNum", C.BlessCosts[i][2]);
			Set(blessBaseInfo, "freeNum", (C.BlessCosts[i][0] == 1) ? 1 : 0);
			Set(blessBaseInfo, "CD", (C.BlessCosts[i][0] == 1) ? 86400 : 0);
			list.Add(blessBaseInfo);
		}
		List<object> list2 = new List<object>();
		for (int j = 1; j <= 6; j++)
		{
			BlessInfo blessInfo = new BlessInfo();
			Set(blessInfo, "id", j);
			Set(blessInfo, "freeNum", (int)userState.BlessGet(j.ToString(CultureInfo.InvariantCulture), 0L));
			Set(blessInfo, "preGetTime", 0L);
			list2.Add(blessInfo);
		}
		List<object> list3 = new List<object>();
		int[] array = new int[3] { 1, 2, 7 };
		for (int k = 0; k < array.Length; k++)
		{
			BlessCurrencyTypeData blessCurrencyTypeData = new BlessCurrencyTypeData();
			Set(blessCurrencyTypeData, "currencyType", array[k]);
			Set(blessCurrencyTypeData, "blessNum", (int)userState.BlessGet("draws_" + array[k], 0L));
			Set(blessCurrencyTypeData, "progress", (int)userState.BlessGet("progress_" + array[k], 0L));
			list3.Add(blessCurrencyTypeData);
		}
		BlessInfoResponse blessInfoResponse = new BlessInfoResponse();
		Set(blessInfoResponse, "blessInfo", list2.ToArray());
		Set(blessInfoResponse, "blessBaseInfo", list.ToArray());
		Set(blessInfoResponse, "data", list3.ToArray());
		TcpServer.Send(c, 60032, blessInfoResponse, pkt);
		L.Log("[TCP] >> 60032 BlessInfoResponse types=6");
	}

	public static void HBless(Conn c, XEngine.Packet pkt, object req)
	{
		BlessRequest blessRequest = (BlessRequest)((req is BlessRequest) ? req : null);
		int num = Int((blessRequest != null) ? ((object)blessRequest.BlessId) : null, 1);
		if (num < 1)
		{
			num = 1;
		}
		if (num > 6)
		{
			num = 6;
		}
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < C.BlessCosts.Length; i++)
		{
			if (C.BlessCosts[i][0] == num)
			{
				num2 = C.BlessCosts[i][1];
				num3 = C.BlessCosts[i][2];
			}
		}
		int num4 = ((num != 2 && num != 4 && num != 6) ? 1 : 10);
		bool flag = num == 1 && userState.BlessGet("1", 0L) == 0;
		string text = num2 switch
		{
			2 => "copper", 
			1 => "gold", 
			_ => "lucky_bag", 
		};
		long num5 = ((text == "gold") ? userState.Gold : ((text == "copper") ? userState.Copper : userState.LuckyBag));
		long num6 = (flag ? 0 : Math.Min(num3, num5));
		long num7 = num5 - num6;
		if (text == "gold")
		{
			userState.Gold = num7;
		}
		else if (text == "copper")
		{
			userState.Copper = num7;
		}
		else
		{
			userState.LuckyBag = num7;
		}
		userState.BlessSet(num.ToString(CultureInfo.InvariantCulture), userState.BlessGet(num.ToString(CultureInfo.InvariantCulture), 0L) + 1);
		userState.BlessSet("draws_" + num2, userState.BlessGet("draws_" + num2, 0L) + num4);
		userState.BlessSet("progress_" + num2, userState.BlessGet("progress_" + num2, 0L) + num4);
		List<int[]> list = new List<int[]>();
		for (int j = 0; j < C.BlessRewards.Length; j++)
		{
			if (C.BlessRewards[j][0] == num)
			{
				list.Add(new int[2]
				{
					C.BlessRewards[j][1],
					C.BlessRewards[j][2]
				});
			}
		}
		int num8 = Math.Min(num4, list.Count);
		if (num8 < list.Count)
		{
			Random random = new Random();
			for (int num9 = list.Count - 1; num9 > 0; num9--)
			{
				int index = random.Next(num9 + 1);
				int[] value = list[num9];
				list[num9] = list[index];
				list[index] = value;
			}
		}
		int[] array = new int[num8 * 2];
		for (int k = 0; k < num8; k++)
		{
			array[k * 2] = list[k][0];
			array[k * 2 + 1] = list[k][1];
		}
		int[][] array2 = new int[num8][];
		for (int l = 0; l < num8; l++)
		{
			array2[l] = new int[2]
			{
				array[l * 2],
				array[l * 2 + 1]
			};
		}
		List<ItemMsg> list2 = GrantItems(uid, array2, "祈福" + num);
		if (userState.Heroes.Count < 2 && !userState.Heroes.Contains(103))
		{
			userState.Heroes.Add(103);
		}
		userState.Heroes.Sort();
		Store.SaveProgress();
		BlessInfo blessInfo = new BlessInfo();
		Set(blessInfo, "id", num);
		Set(blessInfo, "freeNum", (int)userState.BlessGet(num.ToString(CultureInfo.InvariantCulture), 0L));
		Set(blessInfo, "preGetTime", Clock.NowMs());
		BlessCurrencyTypeData blessCurrencyTypeData = new BlessCurrencyTypeData();
		Set(blessCurrencyTypeData, "currencyType", num2);
		Set(blessCurrencyTypeData, "blessNum", (int)userState.BlessGet("draws_" + num2, 0L));
		Set(blessCurrencyTypeData, "progress", (int)userState.BlessGet("progress_" + num2, 0L));
		BlessResponse blessResponse = new BlessResponse();
		Set(blessResponse, "itemMsgs", list2.ToArray());
		Set(blessResponse, "blessInfo", blessInfo);
		Set(blessResponse, "data", blessCurrencyTypeData);
		TcpServer.Send(c, 60034, blessResponse, pkt);
		L.Log("[TCP] >> 60034 BlessResponse id=" + num + " free=" + flag + " 消耗" + text + num6 + " 奖励数=" + list2.Count);
		PushBag(c, uid, "祈福" + num);
	}

	public static void HBlessReward(Conn c, XEngine.Packet pkt, object req)
	{
		int num = (int)(((BlessRewardGetRequest)((req is BlessRewardGetRequest) ? req : null))?.CurrencyType ?? CurrencyType.Gold);
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int[][] rewards = ((num == 1) ? C.BlessProgressReward : new int[0][]);
		List<ItemMsg> list = GrantItems(uid, rewards, "祈福进度奖励(currency=" + num + ")");
		userState.BlessSet("claimed_" + num, userState.BlessGet("claimed_" + num, 0L) + 1);
		Store.SaveProgress();
		BlessRewardGetResponse blessRewardGetResponse = new BlessRewardGetResponse();
		Set(blessRewardGetResponse, "currencyType", num);
		Set(blessRewardGetResponse, "progressValue", (int)userState.BlessGet("progress_" + num, 0L));
		TcpServer.Send(c, 60029, blessRewardGetResponse, pkt);
		L.Log("[TCP] >> 60029 BlessRewardGetResponse currency=" + num + " 领取数=" + list.Count);
		if (list.Count > 0)
		{
			PushBag(c, uid, "祈福进度奖励");
		}
	}

	public static void HSkillActive(Conn c, XEngine.Packet pkt, object req)
	{
		SkillActiveRequest skillActiveRequest = (SkillActiveRequest)((req is SkillActiveRequest) ? req : null);
		long uid = c.Sess.Uid;
		long num = ((skillActiveRequest != null && skillActiveRequest.HeroUid != 0L) ? skillActiveRequest.HeroUid : (uid * 100 + 101));
		int num2 = skillActiveRequest?.SkillId ?? 0;
		UserState userState = Store.Progress(uid);
		int key = 101;
		foreach (int hero in userState.Heroes)
		{
			if (uid * 100 + hero == num)
			{
				key = hero;
				break;
			}
		}
		if (num2 != 0)
		{
			userState.BigSkillByHero[key] = num2;
			userState.BigSkill = num2;
			Store.SaveProgress();
		}
		SkillActiveResponse skillActiveResponse = new SkillActiveResponse();
		Set(skillActiveResponse, "heroUid", num);
		Set(skillActiveResponse, "skillId", num2);
		TcpServer.Send(c, 4097, skillActiveResponse, pkt);
		L.Log("[TCP] >> 4097 SkillActiveResponse heroUid=" + num + " 奥义=" + num2);
	}

	public static void HHeroFormation(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		List<int> list = new List<int>();
		foreach (int key in userState.Formation.Keys)
		{
			if (!list.Contains(key))
			{
				list.Add(key);
			}
		}
		if (!list.Contains(1))
		{
			list.Add(1);
		}
		list.Sort();
		List<object> list2 = new List<object>();
		for (int i = 0; i < list.Count; i++)
		{
			HeroFormationResponse.HeroListMsg heroListMsg = new HeroFormationResponse.HeroListMsg();
			Set(heroListMsg, "type", list[i]);
			Set(heroListMsg, "hero_id", Formation(uid, list[i]).ToArray());
			list2.Add(heroListMsg);
		}
		HeroFormationResponse heroFormationResponse = new HeroFormationResponse();
		Set(heroFormationResponse, "hero_list", list2.ToArray());
		TcpServer.Send(c, 5041, heroFormationResponse, pkt);
		L.Log("[TCP] >> 5041 HeroFormationResponse " + list.Count + " 个阵容");
	}

	public static void HHeroFormationSave(Conn c, XEngine.Packet pkt, object req)
	{
		HeroFormationSaveRequest heroFormationSaveRequest = (HeroFormationSaveRequest)((req is HeroFormationSaveRequest) ? req : null);
		long uid = c.Sess.Uid;
		List<int> list = new List<int>();
		if (heroFormationSaveRequest != null && heroFormationSaveRequest.HeroId != null)
		{
			list.AddRange(heroFormationSaveRequest.HeroId);
		}
		int num = (int)(heroFormationSaveRequest?.FormationType ?? FormationType.FormationTypeNormal);
		if (num < 1 || num > 8)
		{
			num = 1;
		}
		List<int> list2 = SaveFormation(uid, num, list);
		HeroFormationSaveResponse heroFormationSaveResponse = new HeroFormationSaveResponse();
		Set(heroFormationSaveResponse, "hero_id", list2.ToArray());
		TcpServer.Send(c, 5044, heroFormationSaveResponse, pkt);
		L.Log("[TCP] >> 5044 HeroFormationSaveResponse type=" + num + " 上阵=[" + JoinInts(list2) + "]");
	}

	public static void HTaskList(Conn c, XEngine.Packet pkt, object req)
	{
		TaskListRequest taskListRequest = (TaskListRequest)((req is TaskListRequest) ? req : null);
		List<int> list = new List<int>();
		if (taskListRequest != null && taskListRequest.Type != null)
		{
			for (int i = 0; i < taskListRequest.Type.Count; i++)
			{
				list.Add((int)taskListRequest.Type[i]);
			}
		}
		bool num = list.Count == 0 || list.Contains(1);
		UserState userState = Store.Progress(c.Sess.Uid);
		c.Sess.MainTaskId = userState.MainTaskId;
		c.Sess.MainTaskReady = userState.MainTaskReady;
		int mainTaskId = userState.MainTaskId;
		int num2 = ((!userState.MainTaskReady) ? 1 : 2);
		List<object> list2 = new List<object>();
		if (num)
		{
			TaskMsg taskMsg = new TaskMsg();
			Set(taskMsg, "id", mainTaskId);
			Set(taskMsg, "status", num2);
			Set(taskMsg, "curNum", (num2 == 2) ? 1 : 0);
			Set(taskMsg, "needNum", 1);
			list2.Add(taskMsg);
		}
		TaskListResponse taskListResponse = new TaskListResponse();
		Set(taskListResponse, "task", list2.ToArray());
		TcpServer.Send(c, 70002, taskListResponse, pkt);
		L.Log("[TCP] >> 70002 TaskListResponse main_task=" + mainTaskId + " status=" + num2);
	}

	public static void HTaskFinish(Conn c, XEngine.Packet pkt, object req)
	{
		TaskFinishRequest taskFinishRequest = (TaskFinishRequest)((req is TaskFinishRequest) ? req : null);
		UserState userState = Store.Progress(c.Sess.Uid);
		int num = ((taskFinishRequest != null && taskFinishRequest.TaskId != 0) ? taskFinishRequest.TaskId : userState.MainTaskId);
		int mainTaskId = userState.MainTaskId;
		TaskFinishResponse taskFinishResponse = new TaskFinishResponse();
		Set(taskFinishResponse, "task_id", num);
		TcpServer.Send(c, 70004, taskFinishResponse, pkt);
		if (num == mainTaskId)
		{
			userState.MainTaskId = mainTaskId + 1;
			userState.MainTaskReady = false;
			c.Sess.MainTaskId = userState.MainTaskId;
			c.Sess.MainTaskReady = false;
			Store.SaveProgress();
		}
		L.Log("[TCP] >> 70004 TaskFinishResponse task=" + num + " next=" + userState.MainTaskId);
	}

	public static void HTaskAccept(Conn c, XEngine.Packet pkt, object req)
	{
		TaskAcceptRequest taskAcceptRequest = (TaskAcceptRequest)((req is TaskAcceptRequest) ? req : null);
		TaskAcceptResponse taskAcceptResponse = new TaskAcceptResponse();
		Set(taskAcceptResponse, "task_id", taskAcceptRequest?.TaskId ?? 0);
		TcpServer.Send(c, 70015, taskAcceptResponse, pkt);
		L.Log("[TCP] >> 70015 TaskAcceptResponse");
	}

	public static void HTaskDialogue(Conn c, XEngine.Packet pkt, object req)
	{
		TaskNpcDialogueRequest taskNpcDialogueRequest = (TaskNpcDialogueRequest)((req is TaskNpcDialogueRequest) ? req : null);
		TaskNpcDialogueResponse taskNpcDialogueResponse = new TaskNpcDialogueResponse();
		Set(taskNpcDialogueResponse, "npcId", taskNpcDialogueRequest?.NpcId ?? 0);
		Set(taskNpcDialogueResponse, "dialogueId", taskNpcDialogueRequest?.DialogueId ?? 0);
		TcpServer.Send(c, 70017, taskNpcDialogueResponse, pkt);
		L.Log("[TCP] >> 70017 TaskNpcDialogueResponse");
	}

	public static void HTaskGoStage(Conn c, XEngine.Packet pkt, object req)
	{
		TaskGoStageRequest taskGoStageRequest = (TaskGoStageRequest)((req is TaskGoStageRequest) ? req : null);
		TaskGoStageResponse taskGoStageResponse = new TaskGoStageResponse();
		Set(taskGoStageResponse, "stageId", taskGoStageRequest?.StageId ?? 0);
		Set(taskGoStageResponse, "dramaId", taskGoStageRequest?.DramaId ?? 0);
		TcpServer.Send(c, 70019, taskGoStageResponse, pkt);
		L.Log("[TCP] >> 70019 TaskGoStageResponse stage=" + (taskGoStageRequest?.StageId ?? 0));
	}

	public static void HTaskActiveness(Conn c, XEngine.Packet pkt, object req)
	{
		UserState userState = Store.Progress(c.Sess.Uid);
		int num = Math.Max(userState.Activeness, 1);
		List<object> list = new List<object>();
		for (int i = 0; i < C.TaskActivenessPhases.Length; i++)
		{
			int num2 = C.TaskActivenessPhases[i];
			TaskActivenessRewardProgressResponse.TaskActivenessPhaseReward taskActivenessPhaseReward = new TaskActivenessRewardProgressResponse.TaskActivenessPhaseReward();
			Set(taskActivenessPhaseReward, "phase_id", num2);
			Set(taskActivenessPhaseReward, "recieved", userState.ActivenessClaimed.Contains(num2));
			list.Add(taskActivenessPhaseReward);
		}
		TaskActivenessRewardProgressResponse taskActivenessRewardProgressResponse = new TaskActivenessRewardProgressResponse();
		Set(taskActivenessRewardProgressResponse, "reward", list.ToArray());
		Set(taskActivenessRewardProgressResponse, "activeness", num);
		TcpServer.Send(c, 70010, taskActivenessRewardProgressResponse, pkt);
		L.Log("[TCP] >> 70010 TaskActivenessRewardProgressResponse 活跃度=" + num);
	}

	public static void HTaskActivenessReceive(Conn c, XEngine.Packet pkt, object req)
	{
		int num = ((TaskActivenessRewardReceiveRequest)((req is TaskActivenessRewardReceiveRequest) ? req : null))?.PhaseId ?? 0;
		UserState userState = Store.Progress(c.Sess.Uid);
		if (Array.IndexOf(C.TaskActivenessPhases, num) >= 0 && !userState.ActivenessClaimed.Contains(num))
		{
			userState.ActivenessClaimed.Add(num);
			userState.ActivenessClaimed.Sort();
		}
		Store.SaveProgress();
		TaskActivenessRewardReceiveResponse body = new TaskActivenessRewardReceiveResponse();
		TcpServer.Send(c, 70012, body, pkt);
		L.Log("[TCP] >> 70012 TaskActivenessRewardReceiveResponse phase=" + num);
		HTaskActiveness(c, pkt, null);
	}

	public static void HFateInfo(Conn c, XEngine.Packet pkt, object req)
	{
		FateInfoRequest fateInfoRequest = (FateInfoRequest)((req is FateInfoRequest) ? req : null);
		long uid = c.Sess.Uid;
		long num = ((fateInfoRequest != null && fateInfoRequest.HeroUid != 0L) ? fateInfoRequest.HeroUid : (uid * 100 + 101));
		UserState userState = Store.Progress(uid);
		int num2 = 101;
		foreach (int hero in userState.Heroes)
		{
			if (uid * 100 + hero == num)
			{
				num2 = hero;
				break;
			}
		}
		bool flag = HeroRow(num2) != null;
		List<object> list = new List<object>();
		for (int i = 1; i <= 5; i++)
		{
			int num3 = num2 * 100 + i;
			long num4 = userState.BlessGet("fate_ab_" + num + "_" + num3, 0L);
			if (num4 == 0L && userState.FateAbilityLv.TryGetValue(num3, out var value))
			{
				num4 = value;
			}
			if (num4 == 0L)
			{
				num4 = ((i == 1 && flag) ? 1 : 0);
			}
			AbilityMsg abilityMsg = new AbilityMsg();
			Set(abilityMsg, "abilityId", num3);
			Set(abilityMsg, "lv", num4);
			list.Add(abilityMsg);
		}
		long num5 = userState.BlessGet("fate_active_" + num, 0L);
		if (num5 == 0L)
		{
			num5 = ((!userState.FateActive.TryGetValue(num2, out var value2) || value2 == 0) ? (num2 * 100 + 1) : value2);
		}
		long num6 = userState.BlessGet("fate_lv_" + num, 1L);
		if (num6 <= 0)
		{
			num6 = 1L;
		}
		userState.BlessSet("last_fate_hero", num);
		FateInfoResponse fateInfoResponse = new FateInfoResponse();
		Set(fateInfoResponse, "heroUid", num);
		Set(fateInfoResponse, "ability", list.ToArray());
		Set(fateInfoResponse, "fateLv", num6);
		Set(fateInfoResponse, "activeAbilityId", num5);
		TcpServer.Send(c, 4050, fateInfoResponse, pkt);
		L.Log("[TCP] >> 4050 FateInfoResponse heroUid=" + num);
	}

	public static void HSignInfo(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		long num = Clock.NowMs();
		long num2 = Clock.LocalDayStartMs(num);
		List<object> list = new List<object>();
		for (int num3 = 6; num3 >= 0; num3--)
		{
			long num4 = num2 - (long)num3 * 86400000L;
			DaySignInfo daySignInfo = new DaySignInfo();
			Set(daySignInfo, "times", num4);
			List<object> list2 = new List<object>();
			for (int i = 0; i < C.SignRewards.Length; i++)
			{
				OneSign oneSign = new OneSign();
				Set(oneSign, "itemMsg", Msg.MakeItem(C.SignRewards[i][0], uid, C.SignRewards[i][1]));
				Set(oneSign, "isAward", userState.SignedDays.Contains(num4) ? 1 : 0);
				list2.Add(oneSign);
			}
			Set(daySignInfo, "oneSign", list2.ToArray());
			list.Add(daySignInfo);
		}
		List<long> list3 = new List<long>(userState.SignedDays);
		list3.Sort();
		SignInfoResponse signInfoResponse = new SignInfoResponse();
		Set(signInfoResponse, "daySignInfo", list.ToArray());
		Set(signInfoResponse, "signInterval", new string[1] { "00:00-23:59" });
		Set(signInfoResponse, "serverTime", num);
		Set(signInfoResponse, "signs", list3.ToArray());
		TcpServer.Send(c, 3201, signInfoResponse, pkt);
		L.Log("[TCP] >> 3201 SignInfoResponse days=" + list.Count + " 今日=" + num2);
	}

	public static void HSignAction(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		long num = Clock.LocalDayStartMs(Clock.NowMs());
		if (!userState.SignedDays.Contains(num))
		{
			userState.SignedDays.Add(num);
		}
		userState.SignedDays.Sort();
		Store.SaveProgress();
		DaySignInfo daySignInfo = new DaySignInfo();
		Set(daySignInfo, "times", num);
		List<object> list = new List<object>();
		for (int i = 0; i < C.SignRewards.Length; i++)
		{
			OneSign oneSign = new OneSign();
			Set(oneSign, "itemMsg", Msg.MakeItem(C.SignRewards[i][0], uid, C.SignRewards[i][1]));
			Set(oneSign, "isAward", 1);
			list.Add(oneSign);
		}
		Set(daySignInfo, "oneSign", list.ToArray());
		RowTotalSign rowTotalSign = new RowTotalSign();
		Set(rowTotalSign, "itemMsg", Msg.EmptyListBox);
		Set(rowTotalSign, "status", 0);
		Set(rowTotalSign, "times", userState.SignedDays.Count);
		SignActionResponse signActionResponse = new SignActionResponse();
		Set(signActionResponse, "daySignInfo", daySignInfo);
		Set(signActionResponse, "totalSignTimes", userState.SignedDays.Count);
		Set(signActionResponse, "rowTotalSign", rowTotalSign);
		Set(signActionResponse, "signSpace", 0);
		TcpServer.Send(c, 3203, signActionResponse, pkt);
		L.Log("[TCP] >> 3203 SignActionResponse 已签" + userState.SignedDays.Count + "天");
	}

	public static void HShopItemInit(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		long num = Clock.NowMs();
		List<object> list = new List<object>();
		for (int i = 0; i < C.ShopIds.Length; i++)
		{
			int num2 = C.ShopIds[i];
			List<object> list2 = new List<object>();
			int num3 = 1;
			for (int j = 0; j < C.ShopGoods.Length; j++)
			{
				if (C.ShopGoods[j][0] == num2)
				{
					OneShopItem oneShopItem = new OneShopItem();
					Set(oneShopItem, "slot", num3);
					Set(oneShopItem, "itemMsg", Msg.MakeItem(C.ShopGoods[j][1], uid, C.ShopGoods[j][2]));
					Set(oneShopItem, "configId", C.ShopGoods[j][1]);
					Set(oneShopItem, "type", C.ShopGoods[j][4]);
					Set(oneShopItem, "pirce", C.ShopGoods[j][3]);
					Set(oneShopItem, "buy", 0);
					Set(oneShopItem, "rate", 100);
					Set(oneShopItem, "status", 0);
					list2.Add(oneShopItem);
					num3++;
				}
			}
			OneShopInfo oneShopInfo = new OneShopInfo();
			Set(oneShopInfo, "shopId", num2);
			Set(oneShopInfo, "shopItem", list2.ToArray());
			Set(oneShopInfo, "refreshTimes", 0);
			Set(oneShopInfo, "nextRefreshTime", num + 86400000);
			list.Add(oneShopInfo);
		}
		ShopItemInitResponse shopItemInitResponse = new ShopItemInitResponse();
		Set(shopItemInitResponse, "shopInfo", list.ToArray());
		TcpServer.Send(c, 3111, shopItemInitResponse, pkt);
		L.Log("[TCP] >> 3111 ShopItemInitResponse 商店数=" + list.Count);
	}

	public static void HSkillList(Conn c, XEngine.Packet pkt, object req)
	{
		SkillListRequset skillListRequset = (SkillListRequset)((req is SkillListRequset) ? req : null);
		long uid = c.Sess.Uid;
		long num = ((skillListRequset != null && skillListRequset.HeroUid != 0L) ? skillListRequset.HeroUid : (uid * 100 + 101));
		int num2 = (int)(num - uid * 100);
		if (num2 <= 0 || num2 > 999)
		{
			num2 = 101;
		}
		Tables.HeroRow heroRow = HeroRow(num2);
		List<int> list = new List<int>();
		if (heroRow != null)
		{
			for (int i = 0; i < heroRow.Skills.Length; i++)
			{
				list.Add(heroRow.Skills[i].SkillId);
			}
		}
		if (list.Count == 0)
		{
			list.Add(101101100);
		}
		int num3 = ChosenSuperSkill(uid, num2);
		if (num3 == 0)
		{
			num3 = list[list.Count - 1];
		}
		List<int> list2 = new List<int>();
		for (int j = 0; j < list.Count; j++)
		{
			list2.Add(1);
		}
		HeroSkillItem heroSkillItem = new HeroSkillItem();
		Set(heroSkillItem, "heroUid", num);
		Set(heroSkillItem, "bigSkillId", num3);
		Set(heroSkillItem, "skillId", list.ToArray());
		Set(heroSkillItem, "lv", list2.ToArray());
		SkillListResponse skillListResponse = new SkillListResponse();
		Set(skillListResponse, "skillItems", new object[1] { heroSkillItem });
		TcpServer.Send(c, 4081, skillListResponse, pkt);
		L.Log("[TCP] >> 4081 SkillListResponse heroUid=" + num + " 奥义=" + num3 + " 技能数=" + list.Count);
	}

	internal static List<int> SpaceTimeFormation(long uid)
	{
		List<int> list = Formation(uid, 5);
		if (list.Count <= 0)
		{
			return Formation(uid, 1);
		}
		return list;
	}

	public static void HSpaceTimeInfo(Conn c, XEngine.Packet pkt, object req)
	{
		long uid = c.Sess.Uid;
		UserState st = Store.Progress(uid);
		List<int> list = SpaceTimeFormation(uid);
		int wave = CurrentSpaceTimeWave(st);
		List<object> list2 = new List<object>();
		for (int i = 0; i < list.Count; i++)
		{
			SpaceTimeHeroInfo spaceTimeHeroInfo = new SpaceTimeHeroInfo();
			Set(spaceTimeHeroInfo, "heroId", list[i]);
			// 上一场打完剩多少血就还多少血（HSpaceTimeFinish 会存），没记录过就按满血给。
			Set(spaceTimeHeroInfo, "hp", (int)st.BlessGet("st_hp_" + list[i], 100000L));
			Set(spaceTimeHeroInfo, "sp", (int)st.BlessGet("st_sp_" + list[i], 1000L));
			list2.Add(spaceTimeHeroInfo);
		}
		InitSpaceTimeInfoResponse initSpaceTimeInfoResponse = new InitSpaceTimeInfoResponse();
		// curFight / maxScore 必须是「层号」：客户端 PurgatoryData 把 StageConfig.mapId 当键，
		// 时空炼狱 1..99 层的 mapId 正好是 1..99。老代码恒发 1/0，所以每次进来都重打第 1 层、
		// 通关记录永远不前进，打完结算界面拿到的下一层也是错的。
		Set(initSpaceTimeInfoResponse, "maxScore", (int)st.BlessGet("st_max", 0L));
		Set(initSpaceTimeInfoResponse, "curFight", wave);
		Set(initSpaceTimeInfoResponse, "resetTimes", 0);
		Set(initSpaceTimeInfoResponse, "surplsBuyTimes", 0);
		Set(initSpaceTimeInfoResponse, "heroList", list2.ToArray());
		Set(initSpaceTimeInfoResponse, "buyResetTimeCost", 0);
		Set(initSpaceTimeInfoResponse, "bossHp", (int)st.BlessGet("st_boss", 100L));
		Set(initSpaceTimeInfoResponse, "bossTotalHp", (int)st.BlessGet("st_bossmax", 100L));
		TcpServer.Send(c, 90002, initSpaceTimeInfoResponse, pkt);
		L.Log("[TCP] >> 90002 InitSpaceTimeInfoResponse wave=" + wave + " max=" + st.BlessGet("st_max", 0L) + " 队伍=[" + JoinInts(list) + "]");
	}

	/// <summary>竞技场阵容（FormationTypePvpAsync=3），没配过就退回关卡阵容。</summary>
	internal static List<int> ArenaFormation(long uid)
	{
		List<int> list = Formation(uid, 3);
		if (list.Count == 0)
		{
			list = Formation(uid, 1);
		}
		return list;
	}

	/// <summary>当前该打第几层（1 起）。</summary>
	internal static int CurrentSpaceTimeWave(UserState st)
	{
		int wave = (int)st.BlessGet("st_wave", 1L);
		if (wave < 1)
		{
			wave = 1;
		}
		if (StageTable.SpaceTimeStageOf(wave) == 0)
		{
			wave = 1;
		}
		return wave;
	}

	public static void HSpaceTimeBegin(Conn c, XEngine.Packet pkt, object req)
	{
		SpaceTimeBeginRequest spaceTimeBeginRequest = (SpaceTimeBeginRequest)((req is SpaceTimeBeginRequest) ? req : null);
		long uid = c.Sess.Uid;
		UserState st = Store.Progress(uid);
		int num = Int((spaceTimeBeginRequest != null) ? ((object)spaceTimeBeginRequest.StageId) : null, 0);
		// 客户端一般发 0（由它自己按 curFight 取层），这里按存档里的当前层兜底
		if (num == 0)
		{
			num = StageTable.SpaceTimeStageOf(CurrentSpaceTimeWave(st));
		}
		if (num == 0)
		{
			num = 260001;
		}
		// 客户端之后就用这个 stageId 去打关，先记下来（PurgatoryLevel 结算也只带 stageId）
		c.Sess.ActiveStage = num;
		List<int> list = SpaceTimeFormation(uid);
		List<object> list2 = new List<object>();
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(BattleHero(list[i], uid));
		}
		StageRoleInfo stageRoleInfo = new StageRoleInfo();
		Set(stageRoleInfo, "role_id", uid);
		Set(stageRoleInfo, "hero", list2.ToArray());
		SpaceTimeBeginResponse spaceTimeBeginResponse = new SpaceTimeBeginResponse();
		Set(spaceTimeBeginResponse, "stage_id", num);
		Set(spaceTimeBeginResponse, "role", new object[1] { stageRoleInfo });
		Set(spaceTimeBeginResponse, "surplusHp", (int)st.BlessGet("st_hp_" + ((list.Count > 0) ? list[0] : 101), 100000L));
		TcpServer.Send(c, 90011, spaceTimeBeginResponse, pkt);
		L.Log("[TCP] >> 90011 SpaceTimeBeginResponse stage=" + num + " (第 " + StageTable.MapIdOf(num) + " 层) 队伍=[" + JoinInts(list) + "]");
	}

	public static void HSpaceTimeFinish(Conn c, XEngine.Packet pkt, object req)
	{
		SpaceTimeFinishRequest spaceTimeFinishRequest = (SpaceTimeFinishRequest)((req is SpaceTimeFinishRequest) ? req : null);
		long uid = c.Sess.Uid;
		UserState st = Store.Progress(uid);
		int num = ((spaceTimeFinishRequest != null && spaceTimeFinishRequest.StageId != 0) ? spaceTimeFinishRequest.StageId : c.Sess.ActiveStage);
		if (num == 0)
		{
			num = 260001;
		}
		bool flag = spaceTimeFinishRequest == null || spaceTimeFinishRequest.Success;
		// 把这一场打完的残血/残 SP 存下来：下一层开始就是接着这个血量打，跟客户端一致
		if (spaceTimeFinishRequest != null && spaceTimeFinishRequest.HeroList != null)
		{
			for (int i = 0; i < spaceTimeFinishRequest.HeroList.Count; i++)
			{
				SpaceTimeHeroInfo spaceTimeHeroInfo = spaceTimeFinishRequest.HeroList[i];
				if (spaceTimeHeroInfo != null && spaceTimeHeroInfo.HeroId != 0)
				{
					st.BlessSet("st_hp_" + spaceTimeHeroInfo.HeroId, spaceTimeHeroInfo.Hp);
					st.BlessSet("st_sp_" + spaceTimeHeroInfo.HeroId, spaceTimeHeroInfo.Sp);
				}
			}
		}
		if (spaceTimeFinishRequest != null && spaceTimeFinishRequest.BossTotalHp > 0)
		{
			st.BlessSet("st_boss", spaceTimeFinishRequest.BossHp);
			st.BlessSet("st_bossmax", spaceTimeFinishRequest.BossTotalHp);
		}
		int num2 = StageTable.MapIdOf(num);
		List<object> list = new List<object>();
		if (flag && num2 > 0)
		{
			st.BlessSet("stage_clear_" + num, 1L);
			st.BlessSet("st_max", Math.Max((int)st.BlessGet("st_max", 0L), num2));
			// 下一层：层号 +1（表里没有就停在最后一层）
			int num3 = StageTable.SpaceTimeStageOf(num2 + 1);
			st.BlessSet("st_wave", (num3 != 0) ? (num2 + 1) : num2);
			int num4 = StageTable.CopperOf(num, 1000);
			int num5 = StageTable.ExpOf(num, 200);
			st.Copper += num4;
			long num6 = (st.Exp += num5);
			int num7 = (st.Level = PlayerLvTable.LevelFromExp(num6));
			RoleUpLevel roleUpLevel = new RoleUpLevel();
			Set(roleUpLevel, "level", num7);
			Set(roleUpLevel, "exp", (int)num6);
			TcpServer.SendRaw(c, 2022, Msg.SerializeDyn(roleUpLevel), 0L, 2022);
			list.Add(Msg.MakeItem(2, uid, num4));
			list.Add(Msg.MakeItem(4, uid, num5));
			Game.PushBag(c, uid, "时空炼狱");
		}
		Store.SaveProgress();
		SpaceTimeFinishResponse spaceTimeFinishResponse = new SpaceTimeFinishResponse();
		Set(spaceTimeFinishResponse, "stageId", num);
		Set(spaceTimeFinishResponse, "success", flag);
		// ★ awards 不能发空列表：BattleResultPanel_Purgatory.SetRewards 会按
		//   grid_reward 的子节点数遍历，空列表时它靠 FindOrCreateChild 返回 null 兜住，
		//   但界面会是一片空白；这里至少把银币/经验发出去，面板才有内容。
		Set(spaceTimeFinishResponse, "awards", list.ToArray());
		TcpServer.Send(c, 90008, spaceTimeFinishResponse, pkt);
		L.Log("[TCP] >> 90008 SpaceTimeFinishResponse stage=" + num + " 第" + num2 + "层 success=" + flag + " 下一层=" + st.BlessGet("st_wave", 1L));
	}

	internal static object[] BattleRole(long uid, int ftype)
	{
		List<int> list = Formation(uid, ftype);
		if (list.Count == 0)
		{
			list = Formation(uid, 1);
		}
		List<object> list2 = new List<object>();
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(BattleHero(list[i], uid));
		}
		StageRoleInfo stageRoleInfo = new StageRoleInfo();
		Set(stageRoleInfo, "role_id", uid);
		Set(stageRoleInfo, "hero", list2.ToArray());
		return new object[1] { stageRoleInfo };
	}

	public static void HDogKingInfo(Conn c, XEngine.Packet pkt, object req)
	{
		List<object> list = new List<object>();
		int[] array = new int[3] { 1, 2, 3 };
		for (int i = 0; i < array.Length; i++)
		{
			DogKingTypeInfo dogKingTypeInfo = new DogKingTypeInfo();
			Set(dogKingTypeInfo, "type", array[i]);
			Set(dogKingTypeInfo, "surplusTimes", 99);
			Set(dogKingTypeInfo, "open", true);
			Set(dogKingTypeInfo, "dayMaxScore", 0);
			Set(dogKingTypeInfo, "historyMaxScore", 0);
			Set(dogKingTypeInfo, "dayBestTime", 0);
			Set(dogKingTypeInfo, "historyBestTime", 0);
			Set(dogKingTypeInfo, "surplusBuyTimes", 99);
			Set(dogKingTypeInfo, "costType", 2);
			Set(dogKingTypeInfo, "costValue", 20);
			list.Add(dogKingTypeInfo);
		}
		InitDogKingInfoResponse initDogKingInfoResponse = new InitDogKingInfoResponse();
		Set(initDogKingInfoResponse, "typeInfo", list.ToArray());
		TcpServer.Send(c, 100002, initDogKingInfoResponse, pkt);
		L.Log("[TCP] >> 100002 InitDogKingInfoResponse 模式=1,2,3 全部 open 次数=99");
	}

	public static void HDogKingBegin(Conn c, XEngine.Packet pkt, object req)
	{
		DogKingBeginRequest dogKingBeginRequest = (DogKingBeginRequest)((req is DogKingBeginRequest) ? req : null);
		int num = Int((dogKingBeginRequest != null) ? ((object)dogKingBeginRequest.StageId) : null, 220101);
		long uid = c.Sess.Uid;
		c.Sess.ActiveStage = num;
		DogKingBeginResponse dogKingBeginResponse = new DogKingBeginResponse();
		Set(dogKingBeginResponse, "stageId", num);
		Set(dogKingBeginResponse, "role", BattleRole(uid, 6));
		TcpServer.Send(c, 100012, dogKingBeginResponse, pkt);
		L.Log("[TCP] >> 100012 DogKingBeginResponse stage=" + num + " 队伍=[" + JoinInts(Formation(uid, 6)) + "]");
	}

	public static void HDogKingFinish(Conn c, XEngine.Packet pkt, object req)
	{
		DogKingFightFinishRequest dogKingFightFinishRequest = (DogKingFightFinishRequest)((req is DogKingFightFinishRequest) ? req : null);
		int num = Int((dogKingFightFinishRequest != null) ? ((object)dogKingFightFinishRequest.StageId) : null, 220101);
		int num2 = Int((dogKingFightFinishRequest != null) ? ((object)dogKingFightFinishRequest.Score) : null, 0);
		int num3 = Int((dogKingFightFinishRequest != null) ? ((object)dogKingFightFinishRequest.Time) : null, 0);
		UserState userState = Store.Progress(c.Sess.Uid);
		long num4 = userState.BlessGet("dk_best_" + num, 0L);
		if (num2 >= (int)num4)
		{
			userState.BlessSet("dk_best_" + num, num2);
		}
		Store.SaveProgress();
		DogKingFightFinishResponse dogKingFightFinishResponse = new DogKingFightFinishResponse();
		Set(dogKingFightFinishResponse, "stageId", num);
		Set(dogKingFightFinishResponse, "score", num2);
		Set(dogKingFightFinishResponse, "time", num3);
		Set(dogKingFightFinishResponse, "award", new DogKingAward());
		TcpServer.Send(c, 100004, dogKingFightFinishResponse, pkt);
		L.Log("[TCP] >> 100004 DogKingFightFinishResponse stage=" + num + " score=" + num2 + " time=" + num3);
	}

	public static void HDogKingSweep(Conn c, XEngine.Packet pkt, object req)
	{
		DogKingSweepRequest dogKingSweepRequest = (DogKingSweepRequest)((req is DogKingSweepRequest) ? req : null);
		int num = Int((dogKingSweepRequest != null) ? ((object)dogKingSweepRequest.StageId) : null, 220101);
		DogKingSweepResponse dogKingSweepResponse = new DogKingSweepResponse();
		Set(dogKingSweepResponse, "award", new DogKingAward());
		TcpServer.Send(c, 100006, dogKingSweepResponse, pkt);
		L.Log("[TCP] >> 100006 DogKingSweepResponse stage=" + num);
	}

	public static void HDogKingBuyTimes(Conn c, XEngine.Packet pkt, object req)
	{
		DogKingBuyTimesRequest dogKingBuyTimesRequest = (DogKingBuyTimesRequest)((req is DogKingBuyTimesRequest) ? req : null);
		int num = Int((dogKingBuyTimesRequest != null) ? ((object)dogKingBuyTimesRequest.Type) : null, 0);
		DogKingBuyTimesResponse dogKingBuyTimesResponse = new DogKingBuyTimesResponse();
		Set(dogKingBuyTimesResponse, "type", num);
		Set(dogKingBuyTimesResponse, "surlpusTimes", 99);
		Set(dogKingBuyTimesResponse, "costType", 2);
		Set(dogKingBuyTimesResponse, "costValue", 20);
		TcpServer.Send(c, 100008, dogKingBuyTimesResponse, pkt);
		L.Log("[TCP] >> 100008 DogKingBuyTimesResponse type=" + num);
	}

	public static void HTrainInfo(Conn c, XEngine.Packet pkt, object req)
	{
		TrainInfoResponse trainInfoResponse = new TrainInfoResponse();
		Set(trainInfoResponse, "finishId", Msg.EmptyListBox);
		TcpServer.Send(c, 130011, trainInfoResponse, pkt);
		L.Log("[TCP] >> 130011 TrainInfoResponse finish=none");
	}

	public static void HTrainFight(Conn c, XEngine.Packet pkt, object req)
	{
		TrainFightRequest trainFightRequest = (TrainFightRequest)((req is TrainFightRequest) ? req : null);
		int num = Int((trainFightRequest != null) ? ((object)trainFightRequest.TrainId) : null, 0);
		long uid = c.Sess.Uid;
		int heroId = TrainHero(num);
		List<object> list = new List<object>();
		list.Add(BattleHero(heroId, uid));
		StageRoleInfo stageRoleInfo = new StageRoleInfo();
		Set(stageRoleInfo, "role_id", uid);
		Set(stageRoleInfo, "hero", list.ToArray());
		TrainFightResponse trainFightResponse = new TrainFightResponse();
		Set(trainFightResponse, "trainId", num);
		Set(trainFightResponse, "role", new object[1] { stageRoleInfo });
		TcpServer.Send(c, 130015, trainFightResponse, pkt);
		L.Log("[TCP] >> 130015 TrainFightResponse train=" + num + " hero=" + heroId);
	}

	internal static int TrainHero(int trainId)
	{
		if (trainId <= 0)
		{
			return 101;
		}
		if (trainId <= 18)
		{
			return 101;
		}
		if (trainId <= 36)
		{
			return 102;
		}
		if (trainId <= 53)
		{
			return 103;
		}
		if (trainId <= 72)
		{
			return 106;
		}
		if (trainId <= 90)
		{
			return 104;
		}
		return 108;
	}

	public static void HTrainFinish(Conn c, XEngine.Packet pkt, object req)
	{
		TrainFinishRequest trainFinishRequest = (TrainFinishRequest)((req is TrainFinishRequest) ? req : null);
		int num = Int((trainFinishRequest != null) ? ((object)trainFinishRequest.TrainId) : null, 0);
		TrainFinishResponse trainFinishResponse = new TrainFinishResponse();
		Set(trainFinishResponse, "trainId", num);
		Set(trainFinishResponse, "rewards", Msg.EmptyListBox);
		TcpServer.Send(c, 130013, trainFinishResponse, pkt);
		L.Log("[TCP] >> 130013 TrainFinishResponse train=" + num);
	}

	internal static int HellStageId(long uid)
	{
		return 210101;
	}

	public static void HHellInfo(Conn c, XEngine.Packet pkt, object req)
	{
		int num = HellStageId(c.Sess.Uid);
		InitHellBreakInfoResponse initHellBreakInfoResponse = new InitHellBreakInfoResponse();
		Set(initHellBreakInfoResponse, "maxScore", 0);
		Set(initHellBreakInfoResponse, "resetTimes", 0);
		Set(initHellBreakInfoResponse, "currFightRoundId", num);
		Set(initHellBreakInfoResponse, "surplusBuyTimes", 99);
		Set(initHellBreakInfoResponse, "prayTimes", 0);
		TcpServer.Send(c, 80002, initHellBreakInfoResponse, pkt);
		L.Log("[TCP] >> 80002 InitHellBreakInfoResponse round=" + num);
	}

	public static void HHellLayerAward(Conn c, XEngine.Packet pkt, object req)
	{
		HellBreakLayerAwardInfoResponse hellBreakLayerAwardInfoResponse = new HellBreakLayerAwardInfoResponse();
		Set(hellBreakLayerAwardInfoResponse, "awardInfo", Msg.EmptyListBox);
		TcpServer.Send(c, 80015, hellBreakLayerAwardInfoResponse, pkt);
		L.Log("[TCP] >> 80015 HellBreakLayerAwardInfoResponse (空)");
	}

	public static void HHellBegin(Conn c, XEngine.Packet pkt, object req)
	{
		HellBreakBeginRequest hellBreakBeginRequest = (HellBreakBeginRequest)((req is HellBreakBeginRequest) ? req : null);
		long uid = c.Sess.Uid;
		int num = Int((hellBreakBeginRequest != null) ? ((object)hellBreakBeginRequest.StageId) : null, 0);
		if (num == 0)
		{
			num = HellStageId(uid);
		}
		c.Sess.ActiveStage = num;
		HellBreakBeginResponse hellBreakBeginResponse = new HellBreakBeginResponse();
		Set(hellBreakBeginResponse, "stage_id", num);
		Set(hellBreakBeginResponse, "role", BattleRole(uid, 4));
		TcpServer.Send(c, 80019, hellBreakBeginResponse, pkt);
		L.Log("[TCP] >> 80019 HellBreakBeginResponse stage=" + num + " 队伍=[" + JoinInts(Formation(uid, 4)) + "]");
	}

	public static void HHellFinish(Conn c, XEngine.Packet pkt, object req)
	{
		HellBreakFinishRequest hellBreakFinishRequest = (HellBreakFinishRequest)((req is HellBreakFinishRequest) ? req : null);
		long uid = c.Sess.Uid;
		int num = Int((hellBreakFinishRequest != null) ? ((object)hellBreakFinishRequest.StageId) : null, 0);
		if (num == 0)
		{
			num = HellStageId(uid);
		}
		HellBreakFinishResponse hellBreakFinishResponse = new HellBreakFinishResponse();
		Set(hellBreakFinishResponse, "success", true);
		Set(hellBreakFinishResponse, "currentStageId", num);
		Set(hellBreakFinishResponse, "nextStageId", num + 1);
		Set(hellBreakFinishResponse, "awards", Msg.EmptyListBox);
		Set(hellBreakFinishResponse, "currencys", Msg.EmptyListBox);
		TcpServer.Send(c, 80013, hellBreakFinishResponse, pkt);
		L.Log("[TCP] >> 80013 HellBreakFinishResponse stage=" + num + " success=true");
	}

	public static void HHellRoundReset(Conn c, XEngine.Packet pkt, object req)
	{
		HellBreakRoundResetResponse hellBreakRoundResetResponse = new HellBreakRoundResetResponse();
		Set(hellBreakRoundResetResponse, "status", true);
		Set(hellBreakRoundResetResponse, "resetTimes", 0);
		Set(hellBreakRoundResetResponse, "resetCostType", 2);
		Set(hellBreakRoundResetResponse, "resetCost", 0);
		TcpServer.Send(c, 80008, hellBreakRoundResetResponse, pkt);
		L.Log("[TCP] >> 80008 HellBreakRoundResetResponse");
	}

	public static void HHellBuyReset(Conn c, XEngine.Packet pkt, object req)
	{
		BuyHellBreakResetTimesResponse buyHellBreakResetTimesResponse = new BuyHellBreakResetTimesResponse();
		Set(buyHellBreakResetTimesResponse, "resetTimes", 0);
		Set(buyHellBreakResetTimesResponse, "surplsBuyTimes", 99);
		TcpServer.Send(c, 80017, buyHellBreakResetTimesResponse, pkt);
		L.Log("[TCP] >> 80017 BuyHellBreakResetTimesResponse");
	}

	public static void HHellFirstAward(Conn c, XEngine.Packet pkt, object req)
	{
		GetHellBreakFirstAwardRequest getHellBreakFirstAwardRequest = (GetHellBreakFirstAwardRequest)((req is GetHellBreakFirstAwardRequest) ? req : null);
		int num = Int((getHellBreakFirstAwardRequest != null) ? ((object)getHellBreakFirstAwardRequest.LayerId) : null, 0);
		GetHellBreakFirstAwardResponse getHellBreakFirstAwardResponse = new GetHellBreakFirstAwardResponse();
		Set(getHellBreakFirstAwardResponse, "status", true);
		Set(getHellBreakFirstAwardResponse, "layerId", num);
		TcpServer.Send(c, 80006, getHellBreakFirstAwardResponse, pkt);
		L.Log("[TCP] >> 80006 GetHellBreakFirstAwardResponse layer=" + num);
	}
}
