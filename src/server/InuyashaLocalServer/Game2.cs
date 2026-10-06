using System;
using System.Collections.Generic;
using Inuyasha.Generated.Protocol;
using XEngine;

namespace InuyashaLocalServer;

internal static class Game2
{
	internal static int Int(object v, int def)
	{
		return Game.Int(v, def);
	}

	internal static long Long(object v, long def)
	{
		return Game.Long(v, def);
	}

	internal static void Set(object o, string name, object v)
	{
		Game.Set(o, name, v);
	}

	public static void HReconnect(Conn c, XEngine.Packet pkt, object req)
	{
		ReconnectRequest reconnectRequest = req as ReconnectRequest;
		long uid = c.Sess.Uid;
		ReconnectResponse reconnectResponse = new ReconnectResponse();
		Set(reconnectResponse, "token", (reconnectRequest != null && !string.IsNullOrEmpty(reconnectRequest.Token)) ? reconnectRequest.Token : "local");
		Set(reconnectResponse, "server_time", Clock.NowMs());
		TcpServer.Send(c, 1005, reconnectResponse, pkt);
		L.Log("[TCP] >> 1005 ReconnectResponse uid=" + uid);
	}

	public static void HVipGetAward(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is VipGetAwardRequest vipGetAwardRequest) ? ((object)vipGetAwardRequest.VipLevel) : null, 1);
		VipGetAwardResponse vipGetAwardResponse = new VipGetAwardResponse();
		Set(vipGetAwardResponse, "vipLevel", num);
		TcpServer.Send(c, 1101, vipGetAwardResponse, pkt);
		L.Log("[TCP] >> 1101 VipGetAwardResponse vipLevel=" + num);
	}

	public static void HFirstChargeAward(Conn c, XEngine.Packet pkt, object req)
	{
		FirstChargeAwardResponse firstChargeAwardResponse = new FirstChargeAwardResponse();
		Set(firstChargeAwardResponse, "status", true);
		TcpServer.Send(c, 1104, firstChargeAwardResponse, pkt);
		L.Log("[TCP] >> 1104 FirstChargeAwardResponse status=true");
	}

	public static void HRechargeCommodityList(Conn c, XEngine.Packet pkt, object req)
	{
		RechargeCommodityListResponse body = new RechargeCommodityListResponse();
		TcpServer.Send(c, 1106, body, pkt);
		L.Log("[TCP] >> 1106 RechargeCommodityListResponse (空列表)");
	}

	public static void HInvestInfo(Conn c, XEngine.Packet pkt, object req)
	{
		InvestInfoResponse investInfoResponse = new InvestInfoResponse();
		InvestInfo investInfo = new InvestInfo();
		Set(investInfo, "type", 1);
		Set(investInfo, "stauts", 1);
		Set(investInfo, "value", 1);
		Set(investInfoResponse, "invest", new object[1] { investInfo });
		TcpServer.Send(c, 1108, investInfoResponse, pkt);
		L.Log("[TCP] >> 1108 InvestInfoResponse");
	}

	public static void HVipDayAward(Conn c, XEngine.Packet pkt, object req)
	{
		VipDayAwardResponse vipDayAwardResponse = new VipDayAwardResponse();
		Set(vipDayAwardResponse, "vipLevel", 1);
		TcpServer.Send(c, 1110, vipDayAwardResponse, pkt);
		L.Log("[TCP] >> 1110 VipDayAwardResponse vipLevel=1");
	}

	public static void HInvestAward(Conn c, XEngine.Packet pkt, object req)
	{
		InvestAwardRequest investAwardRequest = req as InvestAwardRequest;
		InvestAwardResponse investAwardResponse = new InvestAwardResponse();
		int num = Int((investAwardRequest != null) ? ((object)investAwardRequest.Type) : null, 1);
		Set(investAwardResponse, "type", num);
		TcpServer.Send(c, 1112, investAwardResponse, pkt);
		L.Log("[TCP] >> 1112 InvestAwardResponse type=" + num);
	}

	public static void HFirstChargeInfo(Conn c, XEngine.Packet pkt, object req)
	{
		FirstChargeInfoResponse firstChargeInfoResponse = new FirstChargeInfoResponse();
		Set(firstChargeInfoResponse, "status", 1);
		TcpServer.Send(c, 1114, firstChargeInfoResponse, pkt);
		L.Log("[TCP] >> 1114 FirstChargeInfoResponse status=1");
	}

	public static void HBuyEnergy(Conn c, XEngine.Packet pkt, object req)
	{
		BuyEnergyResponse buyEnergyResponse = new BuyEnergyResponse();
		Set(buyEnergyResponse, "times", 1);
		TcpServer.Send(c, 2010, buyEnergyResponse, pkt);
		L.Log("[TCP] >> 2010 BuyEnergyResponse times=1");
	}

	public static void HBuyCopper(Conn c, XEngine.Packet pkt, object req)
	{
		BuyCopperResponse buyCopperResponse = new BuyCopperResponse();
		Set(buyCopperResponse, "times", 1);
		Set(buyCopperResponse, "errorCode", 0);
		TcpServer.Send(c, 2012, buyCopperResponse, pkt);
		L.Log("[TCP] >> 2012 BuyCopperResponse times=1");
	}

	public static void HChangePic(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is ChangePicRequest changePicRequest) ? ((object)changePicRequest.PicId) : null, 8100010);
		// ★ 客户端 HeadOrBgListView.SureUsing 发来的是 HeadOrBg.level1Icon（8100010 / 8200020 这种图标编号），
		//   并且回调里立刻把 PlayerData.pic / picBg 也设成同一个 level1Icon。
		//   之前这里按 101 / 201 这种行编号存，跟客户端当前头像编号对不上，
		//   重进游戏后 HeadCellData.IsUsing（num == level1Icon）判不出“正在使用”，
		//   表现就是左上角头像换了没反应。统一按 level1Icon 存：810 开头=头像，820 开头=背景。
		UserState userState = Store.Progress(c.Sess.Uid);
		if (num >= 8200000 && num < 8300000)
		{
			userState.BlessSet("picbg", num);
		}
		else
		{
			userState.BlessSet("pic", num);
		}
		Store.SaveProgress();
		ChangePicResponse changePicResponse = new ChangePicResponse();
		Set(changePicResponse, "status", true);
		TcpServer.Send(c, 2020, changePicResponse, pkt);
		L.Log("[TCP] >> 2020 ChangePicResponse icon=" + num + " status=true");
	}

	public static void HChangeRoleInfo(Conn c, XEngine.Packet pkt, object req)
	{
		ChangeRoleInfoRequest changeRoleInfoRequest = req as ChangeRoleInfoRequest;
		long uid = c.Sess.Uid;
		string text = ((changeRoleInfoRequest != null && !string.IsNullOrEmpty(changeRoleInfoRequest.Name)) ? changeRoleInfoRequest.Name : ("user" + uid));
		int num = Int((changeRoleInfoRequest != null) ? ((object)changeRoleInfoRequest.Gender) : null, 1);
		bool isNew;
		Role role = Store.EnsureRole(uid, text, 1, out isNew);
		role.Nick = text;
		role.Gender = num;
		Store.Save();
		ChangeRoleInfoResponse changeRoleInfoResponse = new ChangeRoleInfoResponse();
		Set(changeRoleInfoResponse, "name", text);
		Set(changeRoleInfoResponse, "gender", num);
		Set(changeRoleInfoResponse, "cost", 0);
		TcpServer.Send(c, 2101, changeRoleInfoResponse, pkt);
		L.Log("[TCP] >> 2101 ChangeRoleInfoResponse name=" + text + " gender=" + num);
	}

	public static void HSignature(Conn c, XEngine.Packet pkt, object req)
	{
		SignatureResponse signatureResponse = new SignatureResponse();
		Set(signatureResponse, "status", true);
		TcpServer.Send(c, 2103, signatureResponse, pkt);
		L.Log("[TCP] >> 2103 SignatureResponse status=true");
	}

	public static void HItemSell(Conn c, XEngine.Packet pkt, object req)
	{
		ItemSellRequest itemSellRequest = req as ItemSellRequest;
		long uid = c.Sess.Uid;
		int itemId = Int((itemSellRequest != null) ? ((object)itemSellRequest.ItemId) : null, 0);
		int num = Int((itemSellRequest != null) ? ((object)itemSellRequest.UseNum) : null, 1);
		Store.Progress(uid).BagAdd(itemId, -num);
		Store.SaveProgress();
		ItemSellResponse itemSellResponse = new ItemSellResponse();
		Set(itemSellResponse, "success", true);
		TcpServer.Send(c, 3004, itemSellResponse, pkt);
		Game.PushBag(c, uid, "出售物品");
		L.Log("[TCP] >> 3004 ItemSellResponse item=" + itemId + " num=" + num);
	}

	public static void HItemDetail(Conn c, XEngine.Packet pkt, object req)
	{
		int itemId = Int((req is ItemDetailRequest itemDetailRequest) ? ((object)itemDetailRequest.ItemId) : null, 1);
		ItemDetailResponse itemDetailResponse = new ItemDetailResponse();
		Set(itemDetailResponse, "itemMsg", Msg.MakeItem(itemId, c.Sess.Uid, 1));
		TcpServer.Send(c, 3006, itemDetailResponse, pkt);
		L.Log("[TCP] >> 3006 ItemDetailResponse item=" + itemId);
	}

	public static void HItemSellBatch(Conn c, XEngine.Packet pkt, object req)
	{
		ItemSellBatchResponse itemSellBatchResponse = new ItemSellBatchResponse();
		Set(itemSellBatchResponse, "success", true);
		TcpServer.Send(c, 3008, itemSellBatchResponse, pkt);
		L.Log("[TCP] >> 3008 ItemSellBatchResponse success=true");
	}

	public static void HItemUseBatch(Conn c, XEngine.Packet pkt, object req)
	{
		ItemUseBatchResponse itemUseBatchResponse = new ItemUseBatchResponse();
		Set(itemUseBatchResponse, "success", true);
		TcpServer.Send(c, 3011, itemUseBatchResponse, pkt);
		L.Log("[TCP] >> 3011 ItemUseBatchResponse success=true");
	}

	public static void HCdKeyExchange(Conn c, XEngine.Packet pkt, object req)
	{
		CdKeyExchangeResponse cdKeyExchangeResponse = new CdKeyExchangeResponse();
		Set(cdKeyExchangeResponse, "status", true);
		TcpServer.Send(c, 3021, cdKeyExchangeResponse, pkt);
		L.Log("[TCP] >> 3021 CdKeyExchangeResponse status=true");
	}

	public static void HShopItemBuy(Conn c, XEngine.Packet pkt, object req)
	{
		ShopItemBuyRequest shopItemBuyRequest = req as ShopItemBuyRequest;
		int num = Int((shopItemBuyRequest != null) ? ((object)shopItemBuyRequest.ShopId) : null, 1);
		int num2 = Int((shopItemBuyRequest != null) ? ((object)shopItemBuyRequest.Slot) : null, 1);
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 2;
		int num7 = 1;
		for (int i = 0; i < C.ShopGoods.Length; i++)
		{
			if (C.ShopGoods[i][0] == num)
			{
				if (num7 == num2)
				{
					num3 = C.ShopGoods[i][1];
					num4 = C.ShopGoods[i][2];
					num5 = C.ShopGoods[i][3];
					num6 = C.ShopGoods[i][4];
					break;
				}
				num7++;
			}
		}
		if (num3 == 0)
		{
			ShopItemBuyResponse shopItemBuyResponse = new ShopItemBuyResponse();
			Set(shopItemBuyResponse, "status", 0);
			Set(shopItemBuyResponse, "shopId", num);
			Set(shopItemBuyResponse, "slot", num2);
			TcpServer.Send(c, 3113, shopItemBuyResponse, pkt);
			L.Log("[TCP] >> 3113 ShopItemBuyResponse 找不到商品 shop=" + num + " slot=" + num2);
			return;
		}
		if (num6 == 1)
		{
			if (userState.Gold < num5)
			{
				ShopItemBuyResponse shopItemBuyResponse2 = new ShopItemBuyResponse();
				Set(shopItemBuyResponse2, "status", 0);
				Set(shopItemBuyResponse2, "shopId", num);
				Set(shopItemBuyResponse2, "slot", num2);
				TcpServer.Send(c, 3113, shopItemBuyResponse2, pkt);
				L.Log("[TCP] >> 3113 ShopItemBuyResponse 金币不足 shop=" + num + " slot=" + num2);
				return;
			}
			userState.Gold -= num5;
		}
		else
		{
			if (userState.Copper < num5)
			{
				ShopItemBuyResponse shopItemBuyResponse3 = new ShopItemBuyResponse();
				Set(shopItemBuyResponse3, "status", 0);
				Set(shopItemBuyResponse3, "shopId", num);
				Set(shopItemBuyResponse3, "slot", num2);
				TcpServer.Send(c, 3113, shopItemBuyResponse3, pkt);
				L.Log("[TCP] >> 3113 ShopItemBuyResponse 银币不足 shop=" + num + " slot=" + num2);
				return;
			}
			userState.Copper -= num5;
		}
		userState.BagAdd(num3, num4);
		Store.SaveProgress();
		ShopItemBuyResponse shopItemBuyResponse4 = new ShopItemBuyResponse();
		Set(shopItemBuyResponse4, "status", 1);
		Set(shopItemBuyResponse4, "shopId", num);
		Set(shopItemBuyResponse4, "slot", num2);
		TcpServer.Send(c, 3113, shopItemBuyResponse4, pkt);
		Game.PushBag(c, uid, "商店购买");
		L.Log("[TCP] >> 3113 ShopItemBuyResponse shop=" + num + " slot=" + num2 + " item=" + num3 + "x" + num4);
	}

	public static void HShopItemRefresh(Conn c, XEngine.Packet pkt, object req)
	{
		ShopItemRefreshResponse shopItemRefreshResponse = new ShopItemRefreshResponse();
		Set(shopItemRefreshResponse, "blackMarketIsShow", 0);
		TcpServer.Send(c, 3115, shopItemRefreshResponse, pkt);
		L.Log("[TCP] >> 3115 ShopItemRefreshResponse");
	}

	public static void HTotalSignInfo(Conn c, XEngine.Packet pkt, object req)
	{
		TotalSignInfoResponse totalSignInfoResponse = new TotalSignInfoResponse();
		Set(totalSignInfoResponse, "totalTimes", 1);
		TcpServer.Send(c, 3205, totalSignInfoResponse, pkt);
		L.Log("[TCP] >> 3205 TotalSignInfoResponse totalTimes=1");
	}

	public static void HSignTotalAward(Conn c, XEngine.Packet pkt, object req)
	{
		SignTotalAwardResponse signTotalAwardResponse = new SignTotalAwardResponse();
		RowTotalSign rowTotalSign = new RowTotalSign();
		Set(rowTotalSign, "status", 1);
		Set(rowTotalSign, "times", 1);
		Set(signTotalAwardResponse, "rowTotalSign", rowTotalSign);
		TcpServer.Send(c, 3207, signTotalAwardResponse, pkt);
		L.Log("[TCP] >> 3207 SignTotalAwardResponse");
	}

	/// <summary>
	/// 4007 铸灵 / 觉醒。客户端三个按钮共用这一个请求（HeroQlyRequest 只有 heroUid + isVip）：
	///   ButtonAwake  → 成长率已经顶到本品质上限时才能点 → 觉醒（品质 +1）
	///   ButtonNormal → 成长率没满 → 普通铸灵（+growthRateFix）
	///   ButtonBest   → 成长率没满 → 特殊铸灵（isVip=true，+especialGrowth，必定成功）
	/// 所以服务端靠「当前成长率是否已到顶」就能分辨，不需要额外字段。
	/// </summary>
	public static void HHeroQly(Conn c, XEngine.Packet pkt, object req)
	{
		HeroQlyRequest heroQlyRequest = req as HeroQlyRequest;
		long num = Long((heroQlyRequest != null) ? ((object)heroQlyRequest.HeroUid) : null, 0L);
		bool isVip = heroQlyRequest != null && heroQlyRequest.IsVip;
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		if (num == 0L)
		{
			List<int> list = Game.Formation(uid, 1);
			int num2 = ((list.Count > 0) ? list[0] : 101);
			num = uid * 100 + num2;
		}
		int heroId = Game.HeroIdOf(num, uid);
		if (heroId == 0)
		{
			heroId = (int)(num % 100);
		}
		int qly = Game.HeroQlyOf(userState, heroId);
		int grow = Game.HeroGrowOf(userState, heroId);
		HeroAwakeTable.Row row = HeroAwakeTable.Get(heroId, qly);
		int max = (row != null) ? row.GrowthMax : 0;
		string act;
		if (max > 0 && grow >= max)
		{
			// 成长率已顶 → 觉醒
			HeroAwakeTable.Row next = (row != null && row.NextQly >= 0) ? HeroAwakeTable.Get(heroId, row.NextQly) : null;
			if (next != null)
			{
				qly = row.NextQly;
				grow = next.GrowthMin;
				act = "觉醒";
			}
			else
			{
				act = "已满级";
			}
		}
		else
		{
			int step = (row != null) ? (isVip ? row.EspecialGrowth : row.GrowthFix) : 0;
			if (step <= 0)
			{
				step = 10;
			}
			grow += step;
			if (max > 0 && grow > max)
			{
				grow = max;
			}
			act = isVip ? "特殊铸灵" : "普通铸灵";
		}
		Game.SetHeroQlyGrow(userState, heroId, qly, grow);
		Store.SaveProgress();
		HeroQlyResponse heroQlyResponse = new HeroQlyResponse();
		Set(heroQlyResponse, "heroUid", num);
		Set(heroQlyResponse, "heroQly", qly);
		Set(heroQlyResponse, "growthRate", grow);
		TcpServer.Send(c, 4008, heroQlyResponse, pkt);
		L.Log("[TCP] >> 4008 HeroQlyResponse hero=" + heroId + " " + act + " qly=" + qly + " grow=" + grow + "/" + max);
	}

	public static void HEquipList(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is EquipListRequest equipListRequest) ? ((object)equipListRequest.HeroUid) : null, 0L);
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		// ★ 客户端 EquipData.Request() 恒用 HeroUid=0 请求，并且一次只请求一回，
		//   期望一次性把「所有英雄」的装备都同步下来（RefreshEquipList 是按
		//   equipItem.HeroUid 找英雄再 SetData 的）。
		//   旧实现只回一个英雄 → 其余角色的 EquipBase.uid 一直是 0，
		//   点强化时 equipUid=0 发上去（logcat 里 "EquipEnhanceRequest (2B)"），
		//   回包也是 0，客户端 GetEquipByUid(0) 落到别的英雄的空槽上，
		//   表现就是「第二个之后（第 3 个起）点强化只闪一下连接中、什么也不发生」。
		List<long> heroUids = new List<long>();
		if (num != 0L)
		{
			heroUids.Add(num);
		}
		else
		{
			List<int> owned = Game.OwnedHeroes(uid);
			for (int h = 0; h < owned.Count; h++)
			{
				heroUids.Add(uid * 100 + owned[h]);
			}
			if (heroUids.Count == 0)
			{
				heroUids.Add(uid * 100 + 101);
			}
		}
		List<object> list2 = new List<object>();
		for (int h = 0; h < heroUids.Count; h++)
		{
			long heroUid = heroUids[h];
			for (int i = 1; i <= 6; i++)
			{
				long num3 = heroUid * 10 + i;
				EquipItem equipItem = new EquipItem();
				Set(equipItem, "posType", 200 + i);
				Set(equipItem, "qlyLv", (int)userState.BlessGet("eq_qly_" + num3, 0L));
				Set(equipItem, "enLv", (int)userState.BlessGet("eq_en_" + num3, 1L));
				Set(equipItem, "equipUid", num3);
				Set(equipItem, "heroUid", heroUid);
				// ★ 宝石槽必须恰好 3 个：客户端 EquipBase.SetData 是 gemList.Clear() 后按
				//   item.GemId 逐个 Add 的，OneEquip.SetGem 又直接按下标取 gemList[0..2]，
				//   少发一个就是 ArgumentOutOfRangeException（宝石界面一进去就报错/显示异常）。
				//   空槽用 0（ItemBase(0,1)，客户端用 tempid != 0 判断是否镶嵌）。
				List<int> list3 = new List<int>();
				for (int j = 1; j <= 3; j++)
				{
					list3.Add((int)userState.BlessGet("gem_" + num3 + "_" + j, 0L));
				}
				Set(equipItem, "gemId", list3);
				list2.Add(equipItem);
			}
		}
		EquipListResponse equipListResponse = new EquipListResponse();
		Set(equipListResponse, "eqiup", list2.ToArray());
		TcpServer.Send(c, 4010, equipListResponse, pkt);
		L.Log("[TCP] >> 4010 EquipListResponse heroUid=" + num + " 英雄数=" + heroUids.Count + " 装备数=" + list2.Count + " 每件宝石槽=3");
	}

	public static void HEquipEnhance(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is EquipEnhanceRequest equipEnhanceRequest) ? ((object)equipEnhanceRequest.EquipUid) : null, 0L);
		UserState userState = Store.Progress(c.Sess.Uid);
		int num2 = Math.Min((int)userState.BlessGet("eq_en_" + num, 1L) + 1, 150);
		userState.BlessSet("eq_en_" + num, num2);
		Store.SaveProgress();
		EquipEnhanceResponse equipEnhanceResponse = new EquipEnhanceResponse();
		Set(equipEnhanceResponse, "equipUid", num);
		Set(equipEnhanceResponse, "curEnLv", num2);
		TcpServer.Send(c, 4013, equipEnhanceResponse, pkt);
		L.Log("[TCP] >> 4013 EquipEnhanceResponse equip=" + num + " enLv=" + num2);
	}

	public static void HEquipUpgrade(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is EquipUpgradeRequest equipUpgradeRequest) ? ((object)equipUpgradeRequest.EquipUid) : null, 0L);
		UserState userState = Store.Progress(c.Sess.Uid);
		int num2 = Math.Min((int)userState.BlessGet("eq_qly_" + num, 0L) + 1, 9);
		userState.BlessSet("eq_qly_" + num, num2);
		Store.SaveProgress();
		EquipUpgradeResponse equipUpgradeResponse = new EquipUpgradeResponse();
		Set(equipUpgradeResponse, "equipUid", num);
		Set(equipUpgradeResponse, "curQly", num2);
		TcpServer.Send(c, 4015, equipUpgradeResponse, pkt);
		L.Log("[TCP] >> 4015 EquipUpgradeResponse equip=" + num + " qly=" + num2);
	}

	public static void HAttr(Conn c, XEngine.Packet pkt, object req)
	{
		AttrResponse attrResponse = new AttrResponse();
		Set(attrResponse, "rolePower", 10000);
		TcpServer.Send(c, 4024, attrResponse, pkt);
		L.Log("[TCP] >> 4024 AttrResponse rolePower=10000");
	}

	public static void HItemCombine(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is ItemCombineRequest itemCombineRequest) ? ((object)itemCombineRequest.ItemId) : null, 1);
		ItemCombineResponse itemCombineResponse = new ItemCombineResponse();
		Set(itemCombineResponse, "itemId", num);
		Set(itemCombineResponse, "itemUid", c.Sess.Uid * 100000000 + num);
		TcpServer.Send(c, 4026, itemCombineResponse, pkt);
		L.Log("[TCP] >> 4026 ItemCombineResponse item=" + num);
	}

	public static void HFateAbilityUp(Conn c, XEngine.Packet pkt, object req)
	{
		FateAbilityUpRequest fateAbilityUpRequest = req as FateAbilityUpRequest;
		long num = Long((fateAbilityUpRequest != null) ? ((object)fateAbilityUpRequest.HeroUid) : null, 0L);
		int num2 = Int((fateAbilityUpRequest != null) ? ((object)fateAbilityUpRequest.AbilityId) : null, 1);
		long uid = c.Sess.Uid;
		if (num <= 0 && num2 > 100)
		{
			num = uid * 100 + num2 / 100;
		}
		UserState userState = Store.Progress(uid);
		int num3 = (int)userState.BlessGet("fate_ab_" + num + "_" + num2, 0L) + 1;
		userState.BlessSet("fate_ab_" + num + "_" + num2, num3);
		userState.FateAbilityLv[num2] = num3;
		Store.SaveProgress();
		FateAbilityUpResponse fateAbilityUpResponse = new FateAbilityUpResponse();
		Set(fateAbilityUpResponse, "heroUid", num);
		AbilityMsg abilityMsg = new AbilityMsg();
		Set(abilityMsg, "abilityId", num2);
		Set(abilityMsg, "lv", num3);
		Set(fateAbilityUpResponse, "ability", abilityMsg);
		TcpServer.Send(c, 4054, fateAbilityUpResponse, pkt);
		L.Log("[TCP] >> 4054 FateAbilityUpResponse heroUid=" + num + " ability=" + num2 + " lv=" + num3);
	}

	public static void HFateAbilityActive(Conn c, XEngine.Packet pkt, object req)
	{
		FateAbilityActiveRequest fateAbilityActiveRequest = req as FateAbilityActiveRequest;
		long num = Long((fateAbilityActiveRequest != null) ? ((object)fateAbilityActiveRequest.HeroUid) : null, 0L);
		int num2 = Int((fateAbilityActiveRequest != null) ? ((object)fateAbilityActiveRequest.AbilityId) : null, 1);
		long uid = c.Sess.Uid;
		if (num <= 0 && num2 > 100)
		{
			num = uid * 100 + num2 / 100;
		}
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
		userState.BlessSet("fate_active_" + num, num2);
		userState.FateActive[key] = num2;
		Store.SaveProgress();
		FateAbilityActiveResponse fateAbilityActiveResponse = new FateAbilityActiveResponse();
		Set(fateAbilityActiveResponse, "heroUid", num);
		Set(fateAbilityActiveResponse, "abilityId", num2);
		TcpServer.Send(c, 4056, fateAbilityActiveResponse, pkt);
		L.Log("[TCP] >> 4056 FateAbilityActiveResponse heroUid=" + num + " ability=" + num2);
	}

	public static void HFateUp(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is FateUpRequest fateUpRequest) ? ((object)fateUpRequest.HeroUid) : null, 0L);
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		if (num <= 0)
		{
			num = userState.BlessGet("last_fate_hero", uid * 100 + 101);
		}
		int num2 = (int)userState.BlessGet("fate_lv_" + num, 0L) + 1;
		userState.BlessSet("fate_lv_" + num, num2);
		Store.SaveProgress();
		FateUpResponse fateUpResponse = new FateUpResponse();
		Set(fateUpResponse, "heroUid", num);
		Set(fateUpResponse, "fateLv", num2);
		Set(fateUpResponse, "isSuccess", true);
		TcpServer.Send(c, 4058, fateUpResponse, pkt);
		L.Log("[TCP] >> 4058 FateUpResponse heroUid=" + num + " lv=" + num2);
	}

	public static void HRankList(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is RankListRequest rankListRequest) ? ((object)rankListRequest.RankType) : null, 1);
		long uid = c.Sess.Uid;
		RankListResponse rankListResponse = new RankListResponse();
		Set(rankListResponse, "rankType", num);
		Set(rankListResponse, "myRank", 1);
		Set(rankListResponse, "preTime", 0L);
		Set(rankListResponse, "nextTime", Clock.NowMs() + 3600000);
		Set(rankListResponse, "myNextRank", 1);
		if (num == 1)
		{
			PowerRankMsg powerRankMsg = new PowerRankMsg();
			Set(powerRankMsg, "roleId", uid);
			Set(powerRankMsg, "rankNum", 1);
			Set(powerRankMsg, "power", 10000);
			Set(powerRankMsg, "icon", 1);
			Set(powerRankMsg, "roleLv", 150);
			Set(powerRankMsg, "nick", c.Sess.Nick);
			Set(powerRankMsg, "gender", 1);
			Set(powerRankMsg, "iconBackgrund", 1);
			Set(rankListResponse, "powerRank", new object[1] { powerRankMsg });
		}
		TcpServer.Send(c, 4065, rankListResponse, pkt);
		L.Log("[TCP] >> 4065 RankListResponse type=" + num + " (自占榜首)");
	}

	public static void HRedPoint(Conn c, XEngine.Packet pkt, object req)
	{
		RedPointResponse body = new RedPointResponse();
		TcpServer.Send(c, 4069, body, pkt);
		L.Log("[TCP] >> 4069 RedPointResponse (空)");
	}

	public static void HSkillLvUp(Conn c, XEngine.Packet pkt, object req)
	{
		SkillLvUpRequest skillLvUpRequest = req as SkillLvUpRequest;
		long num = Long((skillLvUpRequest != null) ? ((object)skillLvUpRequest.HeroUid) : null, 0L);
		int num2 = Int((skillLvUpRequest != null) ? ((object)skillLvUpRequest.SkillId) : null, 1);
		UserState userState = Store.Progress(c.Sess.Uid);
		int num3 = (int)userState.BlessGet("sk_" + num + "_" + num2, 1L) + 1;
		userState.BlessSet("sk_" + num + "_" + num2, num3);
		Store.SaveProgress();
		SkillLvUpResponse skillLvUpResponse = new SkillLvUpResponse();
		Set(skillLvUpResponse, "heroUid", num);
		Set(skillLvUpResponse, "skillId", num2);
		Set(skillLvUpResponse, "curLv", num3);
		TcpServer.Send(c, 4083, skillLvUpResponse, pkt);
		L.Log("[TCP] >> 4083 SkillLvUpResponse heroUid=" + num + " skill=" + num2 + " lv=" + num3);
	}

	public static void HShadowList(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is ShadowListRequest shadowListRequest) ? ((object)shadowListRequest.HeroUid) : null, 0L);
		if (num == 0L)
		{
			num = c.Sess.Uid * 100 + 101;
		}
		int heroId = Game.HeroIdOf(num, c.Sess.Uid);
		if (heroId == 0)
		{
			heroId = (int)(num % 100);
		}
		UserState userState = Store.Progress(c.Sess.Uid);
		// ★ 残影 id 就是 shadowConfig_D 的**组号 1..4**（客户端 ShadowBase.id = 组号），
		//   不是 heroId*100+i。旧代码发 10101 这种 id，跟客户端 shadowList[].id 一个都对不上：
		//   列表里全是未拥有、点切换没反应、shadowId 也永远匹配不到任何一档。
		List<int> list2 = ShadowTable.IdsOf(heroId);
		int num3 = (int)userState.BlessGet("shadow_on_" + num, ShadowTable.DefaultIdOf(heroId));
		List<object> list = new List<object>();
		for (int i = 0; i < list2.Count; i++)
		{
			int num4 = list2[i];
			// 第一档 needItems 为空 = 默认拥有；其余要么买过要么发过碎片解锁
			bool flag = ShadowTable.Get(heroId, num4) != null && string.IsNullOrEmpty(ShadowTable.Get(heroId, num4).NeedItems);
			if (flag || userState.BlessGet("shadow_" + num + "_" + num4, 0L) > 0)
			{
				list.Add(num4);
			}
		}
		ShadowListResponse shadowListResponse = new ShadowListResponse();
		Set(shadowListResponse, "heroUid", num);
		Set(shadowListResponse, "shadowId", num3);
		Set(shadowListResponse, "shadowList", list.ToArray());
		TcpServer.Send(c, 4085, shadowListResponse, pkt);
		L.Log("[TCP] >> 4085 ShadowListResponse hero=" + heroId + " 拥有=" + list.Count + "/" + list2.Count + " 当前=" + num3);
	}

	public static void HShadowOn(Conn c, XEngine.Packet pkt, object req)
	{
		ShadowOnRequest shadowOnRequest = req as ShadowOnRequest;
		long num = Long((shadowOnRequest != null) ? ((object)shadowOnRequest.HeroUid) : null, 0L);
		int num2 = Int((shadowOnRequest != null) ? ((object)shadowOnRequest.ShadowId) : null, 0);
		UserState userState = Store.Progress(c.Sess.Uid);
		if (userState.BlessGet("shadow_" + num + "_" + num2, 0L) <= 0)
		{
			userState.BlessSet("shadow_" + num + "_" + num2, 1L);
		}
		userState.BlessSet("shadow_on_" + num, num2);
		Store.SaveProgress();
		ShadowOnResponse shadowOnResponse = new ShadowOnResponse();
		Set(shadowOnResponse, "heroUid", num);
		Set(shadowOnResponse, "shadowId", num2);
		TcpServer.Send(c, 4087, shadowOnResponse, pkt);
		L.Log("[TCP] >> 4087 ShadowOnResponse heroUid=" + num + " shadow=" + num2);
	}

	public static void HBuyShadow(Conn c, XEngine.Packet pkt, object req)
	{
		BuyShadowRequest buyShadowRequest = req as BuyShadowRequest;
		long num = Long((buyShadowRequest != null) ? ((object)buyShadowRequest.HeroUid) : null, 0L);
		int num2 = Int((buyShadowRequest != null) ? ((object)buyShadowRequest.ShadowId) : null, 0);
		UserState userState = Store.Progress(c.Sess.Uid);
		if (userState.BlessGet("shadow_" + num + "_" + num2, 0L) <= 0)
		{
			userState.BlessSet("shadow_" + num + "_" + num2, 1L);
			userState.BlessSet("shadow_on_" + num, num2);
		}
		Store.SaveProgress();
		BuyShadowResponse buyShadowResponse = new BuyShadowResponse();
		Set(buyShadowResponse, "heroUid", num);
		Set(buyShadowResponse, "shadowId", num2);
		TcpServer.Send(c, 4093, buyShadowResponse, pkt);
		L.Log("[TCP] >> 4093 BuyShadowResponse heroUid=" + num + " shadow=" + num2);
	}

	public static void HGemInlay(Conn c, XEngine.Packet pkt, object req)
	{
		GemInlayRequest gemInlayRequest = req as GemInlayRequest;
		long num = Long((gemInlayRequest != null) ? ((object)gemInlayRequest.EquipUid) : null, 0L);
		int num2 = Int((gemInlayRequest != null) ? ((object)gemInlayRequest.SlotIndex) : null, 0);
		// ★ GemUid 是 long（= 玩家uid*1亿 + itemId，见 Msg.MakeItem）。
		//   旧代码写的是 Int(...GemUid...) → Convert.ToInt32(1.0e13) 抛 OverflowException，
		//   Int() 的 catch 直接返回默认值 0，于是回包 gemId=0、槽位被写成空，
		//   表现就是"选中宝石点镶嵌没有任何反应"。
		long gemUid = Long((gemInlayRequest != null) ? ((object)gemInlayRequest.GemUid) : null, 0L);
		int num3 = GemItemIdOf(gemUid);
		// 镶嵌要落盘，否则 4010 每次回的都是空槽，宝石界面永远显示未镶嵌
		UserState userState = Store.Progress(c.Sess.Uid);
		if (num != 0L && num2 >= 1 && num2 <= 3 && num3 != 0)
		{
			userState.BlessSet("gem_" + num + "_" + num2, (long)num3);
			// 宝石从背包"挪"进槽位：扣一颗，并推送 3002 让界面数量刷新
			if (userState.BagGet(num3) > 0)
			{
				userState.BagAdd(num3, -1L);
			}
			Store.SaveProgress();
			Game.PushBag(c, c.Sess.Uid, "gemInlay");
		}
		GemInlayResponse gemInlayResponse = new GemInlayResponse();
		Set(gemInlayResponse, "equipUid", num);
		Set(gemInlayResponse, "slotIndex", num2);
		Set(gemInlayResponse, "gemId", num3);
		TcpServer.Send(c, 4102, gemInlayResponse, pkt);
		L.Log("[TCP] >> 4102 GemInlayResponse equip=" + num + " slot=" + num2 + " gem=" + num3 + " (bagUid=" + gemUid + ")");
	}

	/// <summary>背包条目 uid（玩家uid*1亿 + itemId）→ itemId；已经是 itemId 就原样返回。</summary>
	internal static int GemItemIdOf(long bagUid)
	{
		if (bagUid <= 0L)
		{
			return 0;
		}
		int num = (int)(bagUid % 100000000L);
		if (num <= 0 || num > 9999999)
		{
			num = (int)bagUid;
		}
		return num;
	}

	public static void HGemTakeOff(Conn c, XEngine.Packet pkt, object req)
	{
		GemTakeOffRequest gemTakeOffRequest = req as GemTakeOffRequest;
		long num = Long((gemTakeOffRequest != null) ? ((object)gemTakeOffRequest.EquipUid) : null, 0L);
		int num2 = Int((gemTakeOffRequest != null) ? ((object)gemTakeOffRequest.SlotIndex) : null, 0);
		UserState userState = Store.Progress(c.Sess.Uid);
		if (num != 0L && num2 >= 1 && num2 <= 3)
		{
			userState.BlessSet("gem_" + num + "_" + num2, 0L);
			Store.SaveProgress();
		}
		GemTakeOffResponse gemTakeOffResponse = new GemTakeOffResponse();
		Set(gemTakeOffResponse, "equipUid", num);
		Set(gemTakeOffResponse, "slotIndex", num2);
		TcpServer.Send(c, 4104, gemTakeOffResponse, pkt);
		L.Log("[TCP] >> 4104 GemTakeOffResponse equip=" + num + " slot=" + num2);
	}

	// 宝石升级（4105）。之前这里只回一个空包，既没扣低级宝石也没产出高级宝石，
	// 客户端收到 4106 后 Refresh() 重新读背包，看到的还是原来的石头——
	// 表现就是「镶嵌宝石怎么点都升不了级」。
	//
	// 编号规则来自 gemUpdateConfig.bin：一条记录是 (源id, 目标id=源id+1, 需求数量)。
	// 生命/攻击等各系列都是 302 0 类别 等级，末位就是等级，升一级就是 +1，
	// 末位 9 已是顶级（配置里顶级宝石说明写「无法继续升级」），没有下一级。
	// 客户端把背包条目 uid 直接发过来，条目 uid = 玩家uid*1亿 + itemId（见 Msg.MakeItem）。
	internal static readonly int[] GemUpNeedCount = new int[2] { 4, 4 };

	public static void HGemLvUp(Conn c, XEngine.Packet pkt, object req)
	{
		GemLvUpRequest gemLvUpRequest = req as GemLvUpRequest;
		long gemUid = Long((gemLvUpRequest != null) ? ((object)gemLvUpRequest.GemUid) : null, 0L);
		bool isAll = gemLvUpRequest != null && gemLvUpRequest.IsAll;
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		// uid 反解 itemId：低 8 位就是 itemId（Msg.MakeItem 用 ownerUid*100000000 + itemId）
		int itemId = (int)(gemUid % 100000000L);
		if (itemId <= 0)
		{
			itemId = (int)gemUid;
		}
		int upTimes = 0;
		int need = GemUpNeedCount[0];
		if (itemId >= 3020101 && itemId <= 3020908)
		{
			// 末位是等级：3020x09 是顶级，不能再往上升
			while (itemId % 10 != 9 && upTimes < (isAll ? 20 : 1))
			{
				long have = userState.BagGet(itemId);
				if (have < need)
				{
					break;
				}
				// 扣 4 颗低级，产 1 颗高级；每合一次都要重查数量，避免一次把整包掏空
				userState.BagAdd(itemId, -need);
				userState.BagAdd(itemId + 1, 1L);
				upTimes++;
				itemId++;
			}
		}
		if (upTimes > 0)
		{
			Store.SaveProgress();
			// 客户端 Response_GemLvUpResponse 只负责判断 focusGem 还在不在 + Refresh()，
			// 真正的新数量必须靠 3002 背包推送进来，否则界面上的数字不会变。
			Game.PushBag(c, uid, "gemLvUp x" + upTimes);
		}
		GemLvUpResponse gemLvUpResponse = new GemLvUpResponse();
		TcpServer.Send(c, 4106, gemLvUpResponse, pkt);
		L.Log("[TCP] >> 4106 GemLvUpResponse 升级次数=" + upTimes + " isAll=" + isAll);
	}

	// 宝石槽升级（4107）：把槽位上已镶嵌的宝石升一级，成功后要把新 id 写回 gem_ 键并回包。
	// 原来这里固定回 gemId=0，客户端按 gemList[slot].tempid = 0 处理，等于镶嵌好的宝石「升级后消失」。
	public static void HGemSlotLvUp(Conn c, XEngine.Packet pkt, object req)
	{
		GemSlotLvUpRequest gemSlotLvUpRequest = req as GemSlotLvUpRequest;
		long num = Long((gemSlotLvUpRequest != null) ? ((object)gemSlotLvUpRequest.EquipUid) : null, 0L);
		int num2 = Int((gemSlotLvUpRequest != null) ? ((object)gemSlotLvUpRequest.SlotIndex) : null, 0);
		UserState userState = Store.Progress(c.Sess.Uid);
		int gemId = 0;
		if (num != 0L && num2 >= 1 && num2 <= 3)
		{
			gemId = (int)userState.BlessGet("gem_" + num + "_" + num2, 0L);
			if (gemId >= 3020101 && gemId <= 3020908 && gemId % 10 != 9)
			{
				int next = gemId + 1;
				// 槽位升级同样消耗 4 颗「同级同种」：镶嵌中的那颗算 1 颗，另需背包里 3 颗
				long have = userState.BagGet(gemId);
				if (have >= GemUpNeedCount[0] - 1)
				{
					userState.BagAdd(gemId, -(GemUpNeedCount[0] - 1));
					userState.BlessSet("gem_" + num + "_" + num2, next);
					Store.SaveProgress();
					gemId = next;
					Game.PushBag(c, c.Sess.Uid, "gemSlotLvUp");
				}
				else
				{
					L.Log("[TCP] 4107 槽位升级材料不足 equip=" + num + " slot=" + num2 + " gem=" + gemId);
				}
			}
		}
		GemSlotLvUpResponse gemSlotLvUpResponse = new GemSlotLvUpResponse();
		Set(gemSlotLvUpResponse, "equipUid", num);
		Set(gemSlotLvUpResponse, "slotIndex", num2);
		Set(gemSlotLvUpResponse, "gemId", gemId);
		TcpServer.Send(c, 4108, gemSlotLvUpResponse, pkt);
		L.Log("[TCP] >> 4108 GemSlotLvUpResponse equip=" + num + " slot=" + num2 + " gem=" + gemId);
	}

	public static void HCharmLvUp(Conn c, XEngine.Packet pkt, object req)
	{
		// heroUid 约定 = uid*100 + heroId，魅力/时装的持久化都用 heroId 作键，
		// 否则 Game.HHeroList 读不到（之前写 "charm_lv_<heroUid>"、读 "charm_lv_<heroId>"，永远对不上）。
		long heroUid = Long((req is CharmLvUpRequest charmLvUpRequest) ? ((object)charmLvUpRequest.HeroUid) : null, 0L);
		int heroId = Game.HeroIdOf(heroUid, c.Sess.Uid);
		UserState userState = Store.Progress(c.Sess.Uid);
		if (heroId == 0)
		{
			// 认不出来就原样回包，至少不让客户端拿到 0 级（0 级会让 CharmLevel 裸索引器抛异常）
			int fallbackLv = 1;
			CharmLvUpResponse fallback = new CharmLvUpResponse();
			Set(fallback, "heroUid", heroUid);
			Set(fallback, "charmLv", fallbackLv);
			Set(fallback, "charmVal", 0);
			TcpServer.Send(c, 4121, fallback, pkt);
			L.Log("[TCP] >> 4121 CharmLvUpResponse heroUid=" + heroUid + " (heroId 识别失败, lv=1)");
			return;
		}
		int oldLv = (int)userState.BlessGet("charm_lv_" + heroId, 1L);
		if (oldLv < 1)
		{
			oldLv = 1;
		}
		int maxLv = CharmTable.MaxLevelOf(heroId, 20);
		int newLv = oldLv + 1;
		int newVal;
		if (newLv > maxLv)
		{
			// 满级：等级不再增长，进度夹到该级 charmDemand 以内（超过会让进度条 fillAmount > 1）
			newLv = maxLv;
			int demand = CharmTable.DemandOf(heroId * 1000 + maxLv, 0);
			newVal = (demand > 0) ? demand - 1 : 0;
		}
		else
		{
			newVal = 0;
		}
		userState.BlessSet("charm_lv_" + heroId, newLv);
		userState.BlessSet("charm_val_" + heroId, newVal);
		Store.SaveProgress();
		CharmLvUpResponse charmLvUpResponse = new CharmLvUpResponse();
		Set(charmLvUpResponse, "heroUid", heroUid);
		Set(charmLvUpResponse, "charmLv", newLv);
		Set(charmLvUpResponse, "charmVal", newVal);
		TcpServer.Send(c, 4121, charmLvUpResponse, pkt);
		L.Log("[TCP] >> 4121 CharmLvUpResponse hero=" + heroId + " lv=" + oldLv + "->" + newLv);
	}

	public static void HUnlockClothes(Conn c, XEngine.Packet pkt, object req)
	{
		UnlockClothesRequest unlockClothesRequest = req as UnlockClothesRequest;
		long heroUid = Long((unlockClothesRequest != null) ? ((object)unlockClothesRequest.HeroUid) : null, 0L);
		int clothesId = Int((unlockClothesRequest != null) ? ((object)unlockClothesRequest.ClothesId) : null, 0);
		int heroId = Game.HeroIdOf(heroUid, c.Sess.Uid);
		UserState userState = Store.Progress(c.Sess.Uid);
		// 只解锁「属于该英雄」的时装：客户端 HeroData.Response_UnlockClothesResponse 会
		// GetClothesBase(clothesId) 取不到就 NRE（裸链式访问）。
		bool ok = heroId != 0 && clothesId != 0 && clothesId / 1000 == heroId;
		if (ok)
		{
			Game.SetClothesOwned(userState, clothesId);
			Store.SaveProgress();
		}
		else
		{
			L.Log("[TCP] !! 4122 解锁被拒 heroUid=" + heroUid + " clothes=" + clothesId + " hero=" + heroId);
		}
		UnlockClothesResponse unlockClothesResponse = new UnlockClothesResponse();
		Set(unlockClothesResponse, "heroUid", heroUid);
		Set(unlockClothesResponse, "clothesId", clothesId);
		Set(unlockClothesResponse, "isSuccess", ok);
		TcpServer.Send(c, 4123, unlockClothesResponse, pkt);
		L.Log("[TCP] >> 4123 UnlockClothesResponse hero=" + heroId + " clothes=" + clothesId + " ok=" + ok);
	}

	public static void HPutOnClothes(Conn c, XEngine.Packet pkt, object req)
	{
		PutOnClothesRequest putOnClothesRequest = req as PutOnClothesRequest;
		long heroUid = Long((putOnClothesRequest != null) ? ((object)putOnClothesRequest.HeroUid) : null, 0L);
		int clothesId = Int((putOnClothesRequest != null) ? ((object)putOnClothesRequest.ClothesId) : null, 0);
		int heroId = Game.HeroIdOf(heroUid, c.Sess.Uid);
		UserState userState = Store.Progress(c.Sess.Uid);
		if (heroId != 0 && clothesId != 0)
		{
			// 键用 heroId 与 Game.HHeroList 对齐；顺手记进已解锁集合，重启后仍然是这套
			Game.SetWornClothes(userState, heroId, clothesId);
			Store.SaveProgress();
		}
		PutOnClothesResponse putOnClothesResponse = new PutOnClothesResponse();
		Set(putOnClothesResponse, "heroUid", heroUid);
		Set(putOnClothesResponse, "curClothesId", (long)clothesId);
		TcpServer.Send(c, 4125, putOnClothesResponse, pkt);
		L.Log("[TCP] >> 4125 PutOnClothesResponse hero=" + heroId + " clothes=" + clothesId);
	}

	public static void HRewardReceive(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is RewardReceiveRequest rewardReceiveRequest) ? ((object)rewardReceiveRequest.RewardId) : null, 0);
		RewardReceiveResponse body = new RewardReceiveResponse();
		TcpServer.Send(c, 5023, body, pkt);
		L.Log("[TCP] >> 5023 RewardReceiveResponse reward=" + num);
	}

	public static void HStageSweep(Conn c, XEngine.Packet pkt, object req)
	{
		StageSweepRequest stageSweepRequest = req as StageSweepRequest;
		int num = Int((stageSweepRequest != null) ? ((object)stageSweepRequest.StageId) : null, 100101);
		int num2 = Int((stageSweepRequest != null) ? ((object)stageSweepRequest.Count) : null, 1);
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		long uid = c.Sess.Uid;
		UserState userState = Store.Progress(uid);
		userState.BlessSet("stage_best_" + num, 1L);
		userState.BlessSet("stage_clear_" + num, 1L);
		// 扫荡也算通关，HStageList 是按 Cleared 里的关卡号标"已通关"的
		if (!userState.Cleared.Contains(num))
		{
			userState.Cleared.Add(num);
		}
		// ★ 单次扫荡的经验/银币按 StageConfig.rewards 走（精英关 560 经验，不是写死的 150）。
		//   客户端每波的 "EXP 560 / 铜钱 1000" 就是读这张表显示出来的，
		//   旧代码固定 150 就出现「显示 5600 实际只给 1500」。
		int num6 = StageTable.CopperOf(num, 1000);
		int num7 = StageTable.ExpOf(num, 150);
		userState.Copper += (long)num6 * (long)num2;
		userState.BagAdd(4000001, 2L * (long)num2);
		userState.BagAdd(2100001, 2L * (long)num2);
		userState.BagAdd(4200001, num2);
		long num3 = (long)num7 * (long)num2;
		long num4 = (userState.Exp += num3);
		int num5 = (userState.Level = Game.LevelFromExp(num4));
		Store.SaveProgress();
		RoleUpLevel roleUpLevel = new RoleUpLevel();
		Set(roleUpLevel, "level", num5);
		Set(roleUpLevel, "exp", (int)num4);
		TcpServer.SendRaw(c, 2022, Msg.SerializeDyn(roleUpLevel), 0L, 2022);
		L.Log("[TCP] >> 2022 RoleUpLevel level=" + num5 + " exp=" + num4 + " (扫荡)");
		StageSweepResponse stageSweepResponse = new StageSweepResponse();
		List<object> list = new List<object>();
		int[][] array = new int[4][]
		{
			new int[2] { 2, 1000 },
			new int[2] { 4000001, 2 },
			new int[2] { 2100001, 2 },
			new int[2] { 4200001, 1 }
		};
		for (int i = 0; i < num2; i++)
		{
			StageSweepResponse.SweepReward sweepReward = new StageSweepResponse.SweepReward();
			List<object> list2 = new List<object>();
			for (int j = 0; j < array.Length; j++)
			{
				list2.Add(Msg.MakeItem(array[j][0], uid, array[j][1]));
			}
			Set(sweepReward, "item", list2.ToArray());
			Set(sweepReward, "vip_item", Msg.EmptyListBox);
			list.Add(sweepReward);
		}
		Set(stageSweepResponse, "reward", list.ToArray());
		TcpServer.Send(c, 5029, stageSweepResponse, pkt);
		Game.PushBag(c, uid, "扫荡");
		L.Log("[TCP] >> 5029 StageSweepResponse stage=" + num + " count=" + num2 + " exp=" + num3);
	}

	public static void HStageFightTimesBuy(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is StageFightTimesBuyRequest stageFightTimesBuyRequest) ? ((object)stageFightTimesBuyRequest.StageId) : null, 100101);
		StageFightTimesBuyResponse stageFightTimesBuyResponse = new StageFightTimesBuyResponse();
		Set(stageFightTimesBuyResponse, "stageId", num);
		Set(stageFightTimesBuyResponse, "addedFightTimes", 1);
		Set(stageFightTimesBuyResponse, "buyTimes", 1);
		TcpServer.Send(c, 5080, stageFightTimesBuyResponse, pkt);
		L.Log("[TCP] >> 5080 StageFightTimesBuyResponse stage=" + num);
	}

	public static void HMailList(Conn c, XEngine.Packet pkt, object req)
	{
		MailListResponse mailListResponse = new MailListResponse();
		MailInfo mailInfo = new MailInfo();
		Set(mailInfo, "mailId", 1L);
		Set(mailInfo, "title", "欢迎来到犬夜叉");
		Set(mailInfo, "from", "系统");
		Set(mailInfo, "attachment", false);
		Set(mailInfo, "getAttachment", false);
		Set(mailInfo, "readed", false);
		Set(mailListResponse, "mailInfos", new object[1] { mailInfo });
		TcpServer.Send(c, 60036, mailListResponse, pkt);
		L.Log("[TCP] >> 60036 MailListResponse 欢迎邮件x1");
	}

	public static void HMailDetail(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is MailDetailRequest mailDetailRequest) ? ((object)mailDetailRequest.MailId) : null, 1L);
		MailDetailResponse mailDetailResponse = new MailDetailResponse();
		MailInfo mailInfo = new MailInfo();
		Set(mailInfo, "mailId", num);
		Set(mailInfo, "title", "欢迎来到犬夜叉");
		Set(mailInfo, "from", "系统");
		Set(mailInfo, "attachment", false);
		Set(mailInfo, "getAttachment", false);
		Set(mailInfo, "readed", true);
		Set(mailDetailResponse, "mailInfo", mailInfo);
		TcpServer.Send(c, 60038, mailDetailResponse, pkt);
		L.Log("[TCP] >> 60038 MailDetailResponse mail=" + num);
	}

	public static void HMailGetAttachment(Conn c, XEngine.Packet pkt, object req)
	{
		MailGetAttachmentRequest mailGetAttachmentRequest = req as MailGetAttachmentRequest;
		long num = Long((mailGetAttachmentRequest != null) ? ((object)mailGetAttachmentRequest.MailId) : null, 0L);
		bool flag = mailGetAttachmentRequest?.BatchGet ?? false;
		MailGetAttachmentResponse mailGetAttachmentResponse = new MailGetAttachmentResponse();
		Set(mailGetAttachmentResponse, "batchGet", flag);
		Set(mailGetAttachmentResponse, "success", true);
		Set(mailGetAttachmentResponse, "mailId", num);
		TcpServer.Send(c, 60040, mailGetAttachmentResponse, pkt);
		Game.PushBag(c, c.Sess.Uid, "邮件附件");
		L.Log("[TCP] >> 60040 MailGetAttachmentResponse mail=" + num);
	}

	public static void HMailDelete(Conn c, XEngine.Packet pkt, object req)
	{
		MailDeleteRequest mailDeleteRequest = req as MailDeleteRequest;
		bool flag = mailDeleteRequest?.BatchDelete ?? false;
		long num = Long((mailDeleteRequest != null) ? ((object)mailDeleteRequest.MailId) : null, 0L);
		MailDeleteResponse mailDeleteResponse = new MailDeleteResponse();
		Set(mailDeleteResponse, "batchDelete", flag);
		Set(mailDeleteResponse, "mailId", num);
		TcpServer.Send(c, 60046, mailDeleteResponse, pkt);
		L.Log("[TCP] >> 60046 MailDeleteResponse mail=" + num);
	}

	public static void HLoginNotice(Conn c, XEngine.Packet pkt, object req)
	{
		LoginNoticeResponse loginNoticeResponse = new LoginNoticeResponse();
		Set(loginNoticeResponse, "version", "");
		Set(loginNoticeResponse, "content", "");
		TcpServer.Send(c, 60116, loginNoticeResponse, pkt);
		L.Log("[TCP] >> 60116 LoginNoticeResponse (空)");
	}

	public static void HPopupFunction(Conn c, XEngine.Packet pkt, object req)
	{
		PopupFunctionResponse popupFunctionResponse = new PopupFunctionResponse();
		LoginNoticeInfo loginNoticeInfo = new LoginNoticeInfo();
		Set(loginNoticeInfo, "version", "");
		Set(loginNoticeInfo, "content", "");
		Set(popupFunctionResponse, "notice", loginNoticeInfo);
		PopupFunctionInfo v = new PopupFunctionInfo();
		Set(popupFunctionResponse, "fun", v);
		PopupAdInfo v2 = new PopupAdInfo();
		Set(popupFunctionResponse, "ad", v2);
		TcpServer.Send(c, 60118, popupFunctionResponse, pkt);
		L.Log("[TCP] >> 60118 PopupFunctionResponse (无弹窗)");
	}

	public static void HActivityBaseList(Conn c, XEngine.Packet pkt, object req)
	{
		ActivityBaseListResponse body = new ActivityBaseListResponse();
		TcpServer.Send(c, 60147, body, pkt);
		L.Log("[TCP] >> 60147 ActivityBaseListResponse (空)");
	}

	public static void HActivityDetail(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is ActivityDetailRequest activityDetailRequest) ? ((object)activityDetailRequest.SubId) : null, 1);
		ActivityDetailResponse activityDetailResponse = new ActivityDetailResponse();
		ActivityDetailCommon activityDetailCommon = new ActivityDetailCommon();
		Set(activityDetailCommon, "subId", num);
		Set(activityDetailCommon, "version", 0);
		Set(activityDetailCommon, "subType", 0);
		Set(activityDetailCommon, "startTime", Clock.NowMs());
		Set(activityDetailCommon, "endTime", Clock.NowMs() + 31536000000L);
		Set(activityDetailResponse, "common", activityDetailCommon);
		TcpServer.Send(c, 60150, activityDetailResponse, pkt);
		L.Log("[TCP] >> 60150 ActivityDetailResponse subId=" + num);
	}

	public static void HFriendList(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is FriendListRequest friendListRequest) ? ((object)friendListRequest.Type) : null, 1);
		FriendListResponse friendListResponse = new FriendListResponse();
		Set(friendListResponse, "type", num);
		TcpServer.Send(c, 60161, friendListResponse, pkt);
		L.Log("[TCP] >> 60161 FriendListResponse type=" + num + " (空)");
	}

	public static void HFriendSearchList(Conn c, XEngine.Packet pkt, object req)
	{
		FriendSearchListResponse body = new FriendSearchListResponse();
		TcpServer.Send(c, 60163, body, pkt);
		L.Log("[TCP] >> 60163 FriendSearchListResponse (空)");
	}

	public static void HFriendSearch(Conn c, XEngine.Packet pkt, object req)
	{
		FriendSearchResponse body = new FriendSearchResponse();
		TcpServer.Send(c, 60165, body, pkt);
		L.Log("[TCP] >> 60165 FriendSearchResponse (未找到)");
	}

	public static void HOperateFriend(Conn c, XEngine.Packet pkt, object req)
	{
		OperateFriendRequest operateFriendRequest = req as OperateFriendRequest;
		int num = Int((operateFriendRequest != null) ? ((object)operateFriendRequest.Type) : null, 1);
		long num2 = Long((operateFriendRequest != null) ? ((object)operateFriendRequest.RoleId) : null, 0L);
		int num3 = Int((operateFriendRequest != null) ? ((object)operateFriendRequest.InviteType) : null, 0);
		OperateFriendResponse operateFriendResponse = new OperateFriendResponse();
		Set(operateFriendResponse, "type", num);
		Set(operateFriendResponse, "roleId", num2);
		Set(operateFriendResponse, "inviteType", num3);
		TcpServer.Send(c, 60170, operateFriendResponse, pkt);
		L.Log("[TCP] >> 60170 OperateFriendResponse type=" + num + " role=" + num2);
	}

	public static void HFriendRedPoint(Conn c, XEngine.Packet pkt, object req)
	{
		FriendRedPointResponse friendRedPointResponse = new FriendRedPointResponse();
		Set(friendRedPointResponse, "applyListShow", false);
		Set(friendRedPointResponse, "inviteListShow", false);
		TcpServer.Send(c, 60173, friendRedPointResponse, pkt);
		L.Log("[TCP] >> 60173 FriendRedPointResponse (无红点)");
	}

	public static void HGiveGift(Conn c, XEngine.Packet pkt, object req)
	{
		GiveGiftRequest giveGiftRequest = req as GiveGiftRequest;
		int num = Int((giveGiftRequest != null) ? ((object)giveGiftRequest.GiftType) : null, 1);
		long num2 = Long((giveGiftRequest != null) ? ((object)giveGiftRequest.OtherRoleId) : null, 0L);
		GiveGiftResponse giveGiftResponse = new GiveGiftResponse();
		Set(giveGiftResponse, "giftType", num);
		Set(giveGiftResponse, "type", num);
		Set(giveGiftResponse, "otherRoleId", num2);
		Set(giveGiftResponse, "otherName", "玩家" + num2);
		Set(giveGiftResponse, "goodFeeling", 0);
		TcpServer.Send(c, 60175, giveGiftResponse, pkt);
		L.Log("[TCP] >> 60175 GiveGiftResponse gift=" + num + " to=" + num2);
	}

	public static void HRoleBaseInfo(Conn c, XEngine.Packet pkt, object req)
	{
		long num = Long((req is RoleBaseInfoRequest roleBaseInfoRequest) ? ((object)roleBaseInfoRequest.RoleId) : null, c.Sess.Uid);
		long uid = c.Sess.Uid;
		bool isNew;
		Role role = Store.EnsureRole(num, "user" + num, 1, out isNew);
		UserState userState = Store.Progress(uid);
		RoleBaseInfoResponse roleBaseInfoResponse = new RoleBaseInfoResponse();
		RoleBaseInfoResponse.BaseInfo baseInfo = new RoleBaseInfoResponse.BaseInfo();
		Set(baseInfo, "roleId", num);
		Set(baseInfo, "name", role.Nick);
		Set(baseInfo, "level", userState.Level);
		Set(baseInfo, "vip", 0);
		Set(baseInfo, "gender", role.Gender);
		// ★ 这里以前写死 pic=1001 / picBg=1。
		//   RoleBaseInfo 的 pic / picBg 会被 NickSystem / PlayerInfoSystem 直接喂给
		//   PlayerFace.ChangeFace / ChangeFaceBg → AssetBundleType.PlayerHead + id，
		//   而 picBg=1 对应的 image/head/player/1.tex 在包里根本不存在，
		//   logcat 里就是 "Unable to open archive file: .../image/head/player/1.tex"，
		//   面板左侧头像框一片空白。改成玩家真实选中的头像/背景（level1Icon）。
		Set(baseInfo, "pic", (int)userState.BlessGet("pic", 8100010L));
		Set(baseInfo, "picBg", (int)userState.BlessGet("picbg", 8200010L));
		Set(baseInfo, "power", Game.CalcPower(uid));
		Set(baseInfo, "isFriend", false);
		Set(baseInfo, "signature", "");
		Set(baseInfo, "guidName", "");
		Set(baseInfo, "guidPosition", 0);
		Set(baseInfo, "goodFeeling", 0);
		Set(roleBaseInfoResponse, "info", baseInfo);
		RoleBaseInfoResponse.HeroInfo heroInfo = new RoleBaseInfoResponse.HeroInfo();
		Set(heroInfo, "teamRank", 1);
		List<object> list = new List<object>();
		List<int> list2 = Game.Formation(uid, 1);
		if (list2.Count == 0)
		{
			list2.Add(101);
		}
		for (int i = 0; i < list2.Count; i++)
		{
			int num2 = list2[i];
			Tables.HeroRow heroRow = Game.HeroRow(num2);
			HeroMsg heroMsg = new HeroMsg();
			Set(heroMsg, "heroId", num2);
			Set(heroMsg, "heroUid", uid * 100 + num2);
			Set(heroMsg, "heroQly", Game.HeroQlyOf(userState, num2));
			Set(heroMsg, "power", Game.CalcPower(uid));
			Set(heroMsg, "spMax", heroRow?.GetAttr(46, 1000) ?? 1000);
			List<object> list3 = new List<object>();
			if (heroRow != null)
			{
				Dictionary<int, int> dictionary = heroRow.CopyConfigAttrs();
				List<int> list4 = new List<int>(dictionary.Keys);
				list4.Sort();
				for (int j = 0; j < list4.Count; j++)
				{
					list3.Add(Msg.MakeAttr(list4[j], dictionary[list4[j]]));
				}
			}
			Set(heroMsg, "attr", list3.ToArray());
			Set(heroMsg, "growthRate", Game.HeroGrowOf(userState, num2));
			Set(heroMsg, "charmVal", 0);
			Set(heroMsg, "charmLv", 1);
			Set(heroMsg, "clothes", heroRow?.ClothesId ?? 0);
			Set(heroMsg, "unlockClothes", Msg.EmptyListBox);
			list.Add(heroMsg);
		}
		Set(heroInfo, "heroMsg", list.ToArray());
		Set(roleBaseInfoResponse, "hero", heroInfo);
		SocialInfo v = new SocialInfo();
		Set(roleBaseInfoResponse, "social", v);
		RoleBaseInfoResponse.StairInfo stairInfo = new RoleBaseInfoResponse.StairInfo();
		Set(stairInfo, "score", 0);
		Set(roleBaseInfoResponse, "stair", stairInfo);
		MySocialInfo mySocialInfo = new MySocialInfo();
		SocialInfo socialInfo = new SocialInfo();
		Set(socialInfo, "type", 1);
		Set(socialInfo, "num", 0);
		Set(socialInfo, "rank", 0);
		Set(mySocialInfo, "info", socialInfo);
		Set(mySocialInfo, "flowerHistoryCount", 0);
		Set(mySocialInfo, "eggHistoryCount", 0);
		Set(roleBaseInfoResponse, "myInfo", mySocialInfo);
		TcpServer.Send(c, 60184, roleBaseInfoResponse, pkt);
		L.Log("[TCP] >> 60184 RoleBaseInfoResponse role=" + num + " heroes=" + list.Count);
	}

	public static void HGuideSave(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is GuideSaveRequest guideSaveRequest) ? ((object)guideSaveRequest.GuideId) : null, 0);
		UserState userState = Store.Progress(c.Sess.Uid);
		if (num > (int)userState.BlessGet("guide_max", 0L))
		{
			userState.BlessSet("guide_max", num);
		}
		Store.SaveProgress();
		GuideSaveResponse guideSaveResponse = new GuideSaveResponse();
		Set(guideSaveResponse, "nextGuideId", 999999);
		TcpServer.Send(c, 70007, guideSaveResponse, pkt);
		L.Log("[TCP] >> 70007 GuideSaveResponse guide=" + num + " 结束引导");
	}

	public static void HDeliverTaskItemToNpc(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is DeliverTaskItemToNpcRequest deliverTaskItemToNpcRequest) ? ((object)deliverTaskItemToNpcRequest.NpcId) : null, 1001);
		DeliverTaskItemToNpcResponse deliverTaskItemToNpcResponse = new DeliverTaskItemToNpcResponse();
		Set(deliverTaskItemToNpcResponse, "npcId", num);
		TcpServer.Send(c, 70021, deliverTaskItemToNpcResponse, pkt);
		L.Log("[TCP] >> 70021 DeliverTaskItemToNpcResponse npc=" + num);
	}

	public static void HPray(Conn c, XEngine.Packet pkt, object req)
	{
		PrayResponse prayResponse = new PrayResponse();
		Set(prayResponse, "paryAddition", 0);
		Set(prayResponse, "costType", 0);
		Set(prayResponse, "price", 0);
		TcpServer.Send(c, 80010, prayResponse, pkt);
		L.Log("[TCP] >> 80010 PrayResponse");
	}

	public static void HSweep(Conn c, XEngine.Packet pkt, object req)
	{
		int num = Int((req is SweepRequest sweepRequest) ? ((object)sweepRequest.Type) : null, 1);
		SweepResponse sweepResponse = new SweepResponse();
		Set(sweepResponse, "type", num);
		TcpServer.Send(c, 80012, sweepResponse, pkt);
		L.Log("[TCP] >> 80012 SweepResponse type=" + num);
	}

	public static void HSpaceTimeReset(Conn c, XEngine.Packet pkt, object req)
	{
		// 重置：回到第 1 层、满血，最高层数保留
		UserState userState = Store.Progress(c.Sess.Uid);
		userState.BlessSet("st_wave", 1L);
		List<int> list = Game.SpaceTimeFormation(c.Sess.Uid);
		for (int i = 0; i < list.Count; i++)
		{
			userState.BlessSet("st_hp_" + list[i], 100000L);
			userState.BlessSet("st_sp_" + list[i], 1000L);
		}
		Store.SaveProgress();
		SpaceTimeResetResponse spaceTimeResetResponse = new SpaceTimeResetResponse();
		Set(spaceTimeResetResponse, "status", true);
		Set(spaceTimeResetResponse, "resetTimes", 0);
		TcpServer.Send(c, 90004, spaceTimeResetResponse, pkt);
		L.Log("[TCP] >> 90004 SpaceTimeResetResponse 回到第 1 层");
	}
}
