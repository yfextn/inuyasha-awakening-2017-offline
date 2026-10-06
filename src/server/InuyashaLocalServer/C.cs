using System.Collections.Generic;
using System.Net;

namespace InuyashaLocalServer;

internal static class C
{
	public const int HttpPort = 8089;

	public const int TcpPort = 9000;

	// ===================== LAN CONFIG (compile-time default) =====================
	// LanIp   = the LAN IP of the PC acting as host (advertised to clients)
	// LanMode = true  -> listen on 0.0.0.0 and advertise LanIp (LAN play)
	//           false -> listen on 127.0.0.1 and advertise that (SINGLE-PLAYER)
	// Runtime override: put a file named inu_local_config.txt next to the
	// inu_local_server.log (i.e. <external files dir>) with one key per line:
	//     mode = standalone | lan
	//     lan_ip = 192.168.0.104
	// Only the LAN build reads it (so the single-player build can never go LAN by accident).
	// ==========================================================================
	public const string LanIp = "192.168.0.104";

	public const bool LanMode = false;

	private static volatile string _advertiseHost = LanMode ? LanIp : "127.0.0.1";

	private static volatile bool _listenAll = LanMode;

	private static bool _cfgLoaded;

	/// <summary>
	/// 读运行期配置文件（只有 LAN 版会读）。放成公开静态方法，Boot.Start() 在起监听线程之前调一次。
	/// </summary>
	public static void LoadRuntimeConfig(string dir)
	{
		_cfgLoaded = true;
#if LANSERVER
		try
		{
			if (string.IsNullOrEmpty(dir)) return;
			string path = System.IO.Path.Combine(dir, "inu_local_config.txt");
			if (!System.IO.File.Exists(path)) return;
			string[] lines = System.IO.File.ReadAllLines(path);
			foreach (string raw in lines)
			{
				string line = raw.Trim();
				if (line.Length == 0 || line.StartsWith("#")) continue;
				int eq = line.IndexOf('=');
				if (eq <= 0) continue;
				string k = line.Substring(0, eq).Trim().ToLowerInvariant();
				string v = line.Substring(eq + 1).Trim();
				if (k == "mode")
				{
					if (v.Equals("lan", System.StringComparison.OrdinalIgnoreCase))
					{
						_advertiseHost = LanIp;
						_listenAll = true;
					}
					else if (v.Equals("standalone", System.StringComparison.OrdinalIgnoreCase)
					        || v.Equals("single", System.StringComparison.OrdinalIgnoreCase))
					{
						_advertiseHost = "127.0.0.1";
						_listenAll = false;
					}
				}
				else if (k == "lan_ip" && v.Length > 0)
				{
					if (_listenAll) _advertiseHost = v;
				}
				else if (k == "bind_all")
				{
					_listenAll = v == "1" || v.Equals("true", System.StringComparison.OrdinalIgnoreCase);
				}
			}
		}
		catch
		{
		}
#endif
	}

	public static string AdvertiseHost
	{
		get { return _advertiseHost; }
	}

	public static IPAddress ListenAddress
	{
		get { return _listenAll ? IPAddress.Any : IPAddress.Loopback; }
	}

	/// <summary>当前是否监听所有网卡（用于日志显示）。</summary>
	public static bool ListenAll
	{
		get { return _listenAll; }
	}

	// 桔梗（105）：客户端 heroAttr.bin 已经通过 patch_bins_kikyo.py 补了 105 行，
	// 服务端 Tables 里也有 105（actor Jiegeng / clothes 105001），AvatarList 里同样有
	// 105001、105002 两套时装。默认直接送，玩家才能在「角色」里选她，
	// 并用 HeroSystem 的换形象按钮把她放到主城（HTownRoleChange → town_hero=105）。
	public const bool GrantKikyoByDefault = true;


	public const string AppSecret = "Shanghai XiaYu Game Co.Ltd.";

	public const string ServerName = "\u672c\u5730\u670d\u52a1\u5668";

	public const string ServerArea = "\u672c\u5730";

	public const int MaxServers = 10;

	public const string ClientVersion = "2017.1.90";

	public const int TownHeroId = 100;

	public const int TownClothesId = 100001;

	public const int MaxPlayerLevel = 150;

	public const int TaskActivenessDefault = 40;

	public static readonly Dictionary<int, int> BattleAttrOverride = MakeOverride();

	public static readonly int[] TaskActivenessPhases = new int[3] { 40, 80, 120 };

	public static readonly int[][] SignRewards = new int[3][]
	{
		new int[2] { 5010401, 1 },
		new int[2] { 2100001, 2 },
		new int[2] { 4000001, 5 }
	};

	public static readonly int[] ShopIds = new int[6] { 1, 2, 3, 4, 5, 6 };

	public static readonly int[][] ShopGoods = new int[40][]
	{
		new int[5] { 1, 2100001, 5, 2000, 2 },
		new int[5] { 1, 4000001, 5, 3000, 2 },
		new int[5] { 1, 1200001, 2, 8000, 2 },
		new int[5] { 1, 4200001, 5, 4000, 2 },
		new int[5] { 1, 5100001, 5, 5000, 2 },
		new int[5] { 1, 5200001, 5, 5000, 2 },
		new int[5] { 1, 1100001, 10, 3000, 2 },
		new int[5] { 1, 1100002, 10, 3000, 2 },
		new int[5] { 1, 3020101, 1, 6000, 2 },
		new int[5] { 1, 3020201, 1, 6000, 2 },
		new int[5] { 1, 3020301, 1, 6000, 2 },
		new int[5] { 1, 5010201, 1, 2000, 2 },
		new int[5] { 1, 5010401, 1, 2000, 2 },
		new int[5] { 1, 5010601, 1, 2000, 2 },
		new int[5] { 1, 5010801, 1, 2000, 2 },
		new int[5] { 1, 5010101, 1, 2000, 2 },
		new int[5] { 2, 5010401, 1, 20, 1 },
		new int[5] { 2, 5010301, 1, 20, 1 },
		new int[5] { 2, 5010201, 1, 20, 1 },
		new int[5] { 2, 5010601, 1, 20, 1 },
		new int[5] { 2, 5010801, 1, 20, 1 },
		new int[5] { 2, 5010101, 1, 20, 1 },
		new int[5] { 3, 4000001, 10, 500, 2 },
		new int[5] { 3, 1200001, 3, 1500, 2 },
		new int[5] { 3, 4200001, 10, 2000, 2 },
		new int[5] { 3, 5100001, 5, 2500, 2 },
		new int[5] { 3, 5200001, 5, 2500, 2 },
		new int[5] { 3, 1100001, 10, 1500, 2 },
		new int[5] { 3, 3020101, 1, 3000, 2 },
		new int[5] { 3, 3020201, 1, 3000, 2 },
		new int[5] { 3, 3020301, 1, 3000, 2 },
		new int[5] { 4, 5010401, 2, 30, 1 },
		new int[5] { 4, 2100001, 10, 500, 2 },
		new int[5] { 4, 5010201, 2, 30, 1 },
		new int[5] { 4, 5010601, 2, 30, 1 },
		new int[5] { 4, 5010801, 2, 30, 1 },
		new int[5] { 5, 1200001, 1, 200, 2 },
		new int[5] { 5, 4000001, 20, 800, 2 },
		new int[5] { 6, 5010401, 3, 50, 1 },
		new int[5] { 6, 2100001, 20, 500, 2 }
	};

	public static readonly int[][] BlessRewards = new int[70][]
	{
		new int[3] { 1, 2100001, 1 },
		new int[3] { 1, 4000001, 1 },
		new int[3] { 2, 2100001, 10 },
		new int[3] { 2, 4000001, 5 },
		new int[3] { 2, 5010401, 2 },
		new int[3] { 3, 5010401, 4 },
		new int[3] { 3, 5010201, 2 },
		new int[3] { 3, 5010601, 2 },
		new int[3] { 3, 5010801, 2 },
		new int[3] { 3, 2100001, 2 },
		new int[3] { 4, 5010401, 40 },
		new int[3] { 4, 5010201, 20 },
		new int[3] { 4, 5010601, 20 },
		new int[3] { 4, 5010801, 20 },
		new int[3] { 4, 2100001, 20 },
		new int[3] { 5, 5010201, 8 },
		new int[3] { 5, 5010601, 8 },
		new int[3] { 5, 5010801, 8 },
		new int[3] { 5, 4000001, 10 },
		new int[3] { 6, 5010201, 80 },
		new int[3] { 6, 5010601, 80 },
		new int[3] { 6, 5010801, 80 },
		new int[3] { 6, 4000001, 50 },
		new int[3] { 2, 3020101, 1 },
		new int[3] { 2, 3020201, 1 },
		new int[3] { 2, 3020301, 1 },
		new int[3] { 2, 5100001, 2 },
		new int[3] { 2, 5200001, 2 },
		new int[3] { 2, 1100001, 2 },
		new int[3] { 2, 1100002, 2 },
		new int[3] { 2, 1100003, 2 },
		new int[3] { 2, 1200001, 1 },
		new int[3] { 2, 4200001, 1 },
		new int[3] { 2, 5010101, 2 },
		new int[3] { 3, 3020101, 2 },
		new int[3] { 3, 3020201, 2 },
		new int[3] { 3, 3020301, 2 },
		new int[3] { 3, 5100001, 4 },
		new int[3] { 3, 5200001, 4 },
		new int[3] { 3, 1100001, 4 },
		new int[3] { 3, 1200001, 2 },
		new int[3] { 3, 4200001, 2 },
		new int[3] { 4, 3020101, 5 },
		new int[3] { 4, 3020201, 5 },
		new int[3] { 4, 3020301, 5 },
		new int[3] { 4, 5100001, 10 },
		new int[3] { 4, 5200001, 10 },
		new int[3] { 4, 1100001, 10 },
		new int[3] { 4, 1100002, 10 },
		new int[3] { 4, 1100003, 10 },
		new int[3] { 4, 1200001, 5 },
		new int[3] { 4, 4200001, 5 },
		new int[3] { 5, 3020101, 3 },
		new int[3] { 5, 3020201, 3 },
		new int[3] { 5, 3020301, 3 },
		new int[3] { 5, 5100001, 6 },
		new int[3] { 5, 5200001, 6 },
		new int[3] { 5, 1100001, 6 },
		new int[3] { 5, 1200001, 3 },
		new int[3] { 5, 4200001, 3 },
		new int[3] { 5, 5010101, 5 },
		new int[3] { 6, 3020101, 10 },
		new int[3] { 6, 3020201, 10 },
		new int[3] { 6, 3020301, 10 },
		new int[3] { 6, 5100001, 20 },
		new int[3] { 6, 5200001, 20 },
		new int[3] { 6, 1100001, 20 },
		new int[3] { 6, 1200001, 10 },
		new int[3] { 6, 4200001, 10 },
		new int[3] { 6, 5010101, 10 }
	};

	public static readonly int[][] BlessCosts = new int[6][]
	{
		new int[3] { 1, 2, 10000 },
		new int[3] { 2, 2, 90000 },
		new int[3] { 3, 1, 280 },
		new int[3] { 4, 1, 2580 },
		new int[3] { 5, 7, 10 },
		new int[3] { 6, 7, 100 }
	};

	public static readonly int[][] BlessProgressReward = new int[1][] { new int[2] { 5010401, 1 } };

	public static readonly int[] StarterSkillIds = new int[20]
	{
		101101100, 101101200, 101101300, 101102100, 101102200, 101102300, 101103100, 101104100, 101201100, 101201200,
		101201300, 101202100, 101202200, 101202300, 101203100, 101204100, 101300100, 101300200, 101400100, 101400500
	};

	private static Dictionary<int, int> MakeOverride()
	{
		return new Dictionary<int, int>
		{
			[1] = 120,
			[2] = 80,
			[3] = 2000,
			[45] = 2000,
			[4] = 20,
			[5] = 20,
			[6] = 1500
		};
	}
}
