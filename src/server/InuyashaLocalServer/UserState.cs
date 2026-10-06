using System.Collections.Generic;

namespace InuyashaLocalServer;

internal sealed class UserState
{
	public long Uid;

	public List<int> Cleared = new List<int>();

	public int MainTaskId = 101002;

	public bool MainTaskReady;

	public Dictionary<int, long> Bag = new Dictionary<int, long>();

	public bool BagInitialized;

	public List<int> Heroes = new List<int>();

	public Dictionary<string, long> Bless = new Dictionary<string, long>();

	public int BigSkill = 101400100;

	public Dictionary<int, List<int>> Formation = new Dictionary<int, List<int>>();

	public Dictionary<int, int> BigSkillByHero = new Dictionary<int, int>();

	public long Copper = 1000000L;

	public long Gold = 100000L;

	public long LuckyBag = 100L;

	public List<long> SignedDays = new List<long>();

	public int Activeness = 40;

	public List<int> ActivenessClaimed = new List<int>();

	public Dictionary<int, int> FateAbilityLv = new Dictionary<int, int>();

	public Dictionary<int, int> FateActive = new Dictionary<int, int>();

	// 时装/魅力状态直接存进已持久化的 Bless 字典，键统一为：
	//   worn_clothes_<heroId>   = 当前穿着的时装 id
	//   clothes_own_<clothesId> = 1 表示已解锁
	//   charm_lv_<heroId>       = 魅力等级（必须 1..20）
	//   charm_val_<heroId>      = 魅力进度（0..charmDemand-1）
	// 这样不用改 Store 的长度分支（ApplyState 是按 key 长度 switch 的）。

	public int Level
	{
		get
		{
			return (int)BlessGet("plv", 1L);
		}
		set
		{
			BlessSet("plv", value);
		}
	}

	public long Exp
	{
		get
		{
			return BlessGet("pexp", 0L);
		}
		set
		{
			BlessSet("pexp", value);
		}
	}

	public UserState()
	{
		Heroes.Add(101);
	}

	public long BagGet(int itemId)
	{
		if (!Bag.TryGetValue(itemId, out var value))
		{
			return 0L;
		}
		return value;
	}

	public void BagAdd(int itemId, long num)
	{
		Bag[itemId] = BagGet(itemId) + num;
	}

	public long BlessGet(string key, long def)
	{
		if (!Bless.TryGetValue(key, out var value))
		{
			return def;
		}
		return value;
	}

	public void BlessSet(string key, long value)
	{
		Bless[key] = value;
	}
}
