using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ProtoBuf;
using XEngine;

namespace InuyashaLocalServer;

internal static class TcpServer
{
	public static readonly Dictionary<int, Handler> Handlers = BuildHandlers();

	private static Dictionary<int, Handler> BuildHandlers()
	{
		return new Dictionary<int, Handler>
		{
			[2005] = Game.HGameEnter,
			[1007] = Game.HClientGetData,
			[140001] = Game.HTownEnter,
			[140022] = Game.HTownRoleChange,
			[140029] = Game.HTownHeroIdChange,
			[140024] = Game.HTownNpcList,
			[5038] = Game.HChapterList,
			[5018] = Game.HStageList,
			[50088] = Game.HStageState,
			[50084] = Game.HPveBegin,
			[5081] = Game.HBattleReward,
			[4000] = Game.HHeroList,
			[4003] = Game.HHeroSummon,
			[4041] = Game.HHeroRecruit,
			[3001] = Game.HBagList,
			[60031] = Game.HBlessInfo,
			[60033] = Game.HBless,
			[60028] = Game.HBlessReward,
			[4096] = Game.HSkillActive,
			[5040] = Game.HHeroFormation,
			[5032] = Game.HHeroFormationSave,
			[70001] = Game.HTaskList,
			[70003] = Game.HTaskFinish,
			[70014] = Game.HTaskAccept,
			[70016] = Game.HTaskDialogue,
			[70018] = Game.HTaskGoStage,
			[70009] = Game.HTaskActiveness,
			[70011] = Game.HTaskActivenessReceive,
			[4049] = Game.HFateInfo,
			[3200] = Game.HSignInfo,
			[3202] = Game.HSignAction,
			[3110] = Game.HShopItemInit,
			[4080] = Game.HSkillList,
			[90001] = Game.HSpaceTimeInfo,
			[90010] = Game.HSpaceTimeBegin,
			[90007] = Game.HSpaceTimeFinish,
			[100001] = Game.HDogKingInfo,
			[100011] = Game.HDogKingBegin,
			[100003] = Game.HDogKingFinish,
			[100005] = Game.HDogKingSweep,
			[100007] = Game.HDogKingBuyTimes,
			[130010] = Game.HTrainInfo,
			[130014] = Game.HTrainFight,
			[130012] = Game.HTrainFinish,
			[80001] = Game.HHellInfo,
			[80014] = Game.HHellLayerAward,
			[80018] = Game.HHellBegin,
			[80020] = Game.HHellFinish,
			[80007] = Game.HHellRoundReset,
			[80016] = Game.HHellBuyReset,
			[80005] = Game.HHellFirstAward,
			[1004] = Game2.HReconnect,
			[1100] = Game2.HVipGetAward,
			[1103] = Game2.HFirstChargeAward,
			[1105] = Game2.HRechargeCommodityList,
			[1107] = Game2.HInvestInfo,
			[1109] = Game2.HVipDayAward,
			[1111] = Game2.HInvestAward,
			[1113] = Game2.HFirstChargeInfo,
			[2009] = Game2.HBuyEnergy,
			[2011] = Game2.HBuyCopper,
			[2019] = Game2.HChangePic,
			[2100] = Game2.HChangeRoleInfo,
			[2102] = Game2.HSignature,
			[3003] = Game2.HItemSell,
			[3005] = Game2.HItemDetail,
			[3007] = Game2.HItemSellBatch,
			[3010] = Game2.HItemUseBatch,
			[3020] = Game2.HCdKeyExchange,
			[3112] = Game2.HShopItemBuy,
			[3114] = Game2.HShopItemRefresh,
			[3204] = Game2.HTotalSignInfo,
			[3206] = Game2.HSignTotalAward,
			[4007] = Game2.HHeroQly,
			[4009] = Game2.HEquipList,
			[4012] = Game2.HEquipEnhance,
			[4014] = Game2.HEquipUpgrade,
			[4023] = Game2.HAttr,
			[4025] = Game2.HItemCombine,
			[4053] = Game2.HFateAbilityUp,
			[4055] = Game2.HFateAbilityActive,
			[4057] = Game2.HFateUp,
			[4064] = Game2.HRankList,
			[4068] = Game2.HRedPoint,
			[4082] = Game2.HSkillLvUp,
			[4084] = Game2.HShadowList,
			[4086] = Game2.HShadowOn,
			[4092] = Game2.HBuyShadow,
			[4101] = Game2.HGemInlay,
			[4103] = Game2.HGemTakeOff,
			[4105] = Game2.HGemLvUp,
			[4107] = Game2.HGemSlotLvUp,
			[4120] = Game2.HCharmLvUp,
			[4122] = Game2.HUnlockClothes,
			[4124] = Game2.HPutOnClothes,
			[5022] = Game2.HRewardReceive,
			[5028] = Game2.HStageSweep,
			[5079] = Game2.HStageFightTimesBuy,
			[60035] = Game2.HMailList,
			[60037] = Game2.HMailDetail,
			[60039] = Game2.HMailGetAttachment,
			[60045] = Game2.HMailDelete,
			[60115] = Game2.HLoginNotice,
			[60117] = Game2.HPopupFunction,
			[60146] = Game2.HActivityBaseList,
			[60149] = Game2.HActivityDetail,
			[60160] = Game2.HFriendList,
			[60162] = Game2.HFriendSearchList,
			[60164] = Game2.HFriendSearch,
			[60169] = Game2.HOperateFriend,
			[60172] = Game2.HFriendRedPoint,
			[60174] = Game2.HGiveGift,
			[60183] = Game2.HRoleBaseInfo,
			[70006] = Game2.HGuideSave,
			[70020] = Game2.HDeliverTaskItemToNpc,
			[80009] = Game2.HPray,
			[80011] = Game2.HSweep,
			[90003] = Game2.HSpaceTimeReset,
			// ---- 竞技场（异步竞技）：空响应会让面板无限加载 + 每秒重发 60125 ----
			// 4120/4122/4124（时装/魅力）已注册到 Game2，见上方。
			[60125] = Game.HPvpAsyncInfo,
			[50086] = Game.HPvpAsyncBegin,
			[60133] = Game.HPvpAsyncFinish,
			[60129] = Game.HPvpAsyncReset,
			[60137] = Game.HPvpAsyncGetWinReward
		};
	}

	public static void Loop()
	{
		TcpListener tcpListener = null;
		string bindDesc = C.ListenAll ? ("0.0.0.0:" + C.TcpPort + "（局域网，对外 " + C.AdvertiseHost + "）")
		                            : ("127.0.0.1:" + C.TcpPort);
		try
		{
			tcpListener = new TcpListener(C.ListenAddress, C.TcpPort);
			tcpListener.Start();
			L.Log("[TCP] 游戏网关监听 " + bindDesc);
		}
		catch (Exception e)
		{
			L.Err("TCP 监听 " + bindDesc + " 失败", e);
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
				L.Err("TCP accept", e2);
				Thread.Sleep(200);
				continue;
			}
			Socket s = socket;
			Thread thread = new Thread((ThreadStart)delegate
			{
				ClientLoop(s);
			});
			thread.IsBackground = true;
			thread.Start();
		}
	}

	private static void ClientLoop(Socket sock)
	{
		Conn conn = new Conn();
		conn.Sock = sock;
		try
		{
			conn.Peer = sock.RemoteEndPoint.ToString();
		}
		catch
		{
			conn.Peer = "?";
		}
		L.Log("[TCP] 客户端接入 " + conn.Peer);
		try
		{
			try
			{
				sock.NoDelay = true;
			}
			catch
			{
			}
			while (true)
			{
				int num;
				try
				{
					num = sock.Receive(conn.Buf, conn.Len, conn.Buf.Length - conn.Len, SocketFlags.None);
				}
				catch (Exception ex)
				{
					L.Log("[TCP] " + conn.Peer + " 读取异常: " + ex.GetType().Name);
					return;
				}
				if (num <= 0)
				{
					break;
				}
				conn.Len += num;
				while (conn.Len >= 4)
				{
					int num2 = (conn.Buf[0] << 24) | (conn.Buf[1] << 16) | (conn.Buf[2] << 8) | conn.Buf[3];
					if (num2 < 0 || num2 > 8388608)
					{
						L.Log("[TCP] 长度异常 " + num2 + "，断开");
						return;
					}
					if (conn.Len < 4 + num2)
					{
						break;
					}
					byte[] array = new byte[num2];
					Array.Copy(conn.Buf, 4, array, 0, num2);
					int num3 = 4 + num2;
					Array.Copy(conn.Buf, num3, conn.Buf, 0, conn.Len - num3);
					conn.Len -= num3;
					try
					{
						OnPacket(conn, array);
					}
					catch (Exception e)
					{
						L.Err("处理数据包", e);
					}
				}
				if (conn.Len == conn.Buf.Length)
				{
					byte[] array2 = new byte[conn.Buf.Length * 2];
					Array.Copy(conn.Buf, 0, array2, 0, conn.Len);
					conn.Buf = array2;
				}
			}
			L.Log("[TCP] " + conn.Peer + " 断开");
		}
		catch (Exception e2)
		{
			L.Err("TCP 连接", e2);
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

	private static void OnPacket(Conn c, byte[] payload)
	{
		Packet packet;
		try
		{
			packet = Msg.DeserializeMsg<Packet>(payload);
		}
		catch (Exception e)
		{
			L.Err("解析 Packet", e);
			return;
		}
		if (packet == null)
		{
			return;
		}
		if (packet.Data == null)
		{
			packet.Data = new byte[0];
		}
		string text = MT.Name(packet.Type);
		string text2 = MT.ClassName(packet.Type);
		L.Log("[TCP] << " + packet.Type + " " + text + " (" + packet.Data.Length + "B)");
		object req = null;
		if (!string.IsNullOrEmpty(text2))
		{
			Type type = Msg.FindType(text2);
			if (type != null)
			{
				req = Msg.DeserializeDyn(type, packet.Data);
			}
		}
		if (Handlers.TryGetValue(packet.Type, out var value))
		{
			try
			{
				value(c, packet, req);
				return;
			}
			catch (Exception e2)
			{
				L.Err("处理 " + text, e2);
				return;
			}
		}
		int num = MT.ResponseIdFor(packet.Type);
		if (num == 0)
		{
			L.Log("[TCP] (无对应 Response，忽略 " + text + ")");
			return;
		}
		Type type2 = Msg.FindType(MT.ClassName(num));
		byte[] array = new byte[0];
		if (type2 != null)
		{
			array = Msg.SerializeDyn(Msg.BuildDefault(type2, 3));
		}
		Send(c, num, array, packet);
		L.Log("[TCP] >> " + num + " " + MT.Name(num) + " (防NRE默认 " + array.Length + "B, 自动)");
	}

	public static void Send(Conn c, int msgType, object body, Packet req)
	{
		byte[] data = ((body == null) ? new byte[0] : ((body is byte[] array) ? array : Msg.SerializeDyn(body)));
		int identify = 0;
		if (req != null)
		{
			identify = ((req.Identify != 0) ? req.Identify : req.Type);
		}
		SendRaw(c, msgType, data, 0L, identify);
	}

	public static void SendRaw(Conn c, int msgType, byte[] data, long sequence, int identify)
	{
		if (data == null)
		{
			data = new byte[0];
		}
		Packet packet = new Packet();
		packet.Type = msgType;
		packet.Sequence = sequence;
		packet.Data = data;
		packet.Identify = identify;
		byte[] buf;
		try
		{
			MemoryStream memoryStream = new MemoryStream();
			Serializer.SerializeWithLengthPrefix(memoryStream, packet, PrefixStyle.Fixed32BigEndian);
			buf = memoryStream.ToArray();
		}
		catch (Exception e)
		{
			L.Err("打包 " + MT.Name(msgType), e);
			return;
		}
		if (c == null || c.Sock == null)
		{
			L.Log("[TCP] >> " + msgType + " " + MT.Name(msgType) + " (" + data.Length + "B) [无 socket，仅日志]");
			return;
		}
		lock (c.SendLock)
		{
			try
			{
				c.Sock.Send(buf);
			}
			catch (Exception e2)
			{
				L.Err("发送 " + MT.Name(msgType), e2);
			}
		}
	}
}
