using System.Collections.Generic;

namespace InuyashaLocalServer;

public static class Tables
{
	public sealed class SkillRow
	{
		public readonly int SkillId;

		public readonly int SkillLevel;

		public readonly int LimitLv;

		public readonly int SkillType;

		public SkillRow(int skillId, int skillLevel, int limitLv, int skillType)
		{
			SkillId = skillId;
			SkillLevel = skillLevel;
			LimitLv = limitLv;
			SkillType = skillType;
		}
	}

	public sealed class HeroRow
	{
		public readonly int HeroId;

		public readonly string Actor;

		public readonly int ClothesId;

		public readonly int SuperSkillId;

		public readonly int SuperNeedSp;

		public readonly int[] SuperSkillIds;

		public readonly SkillRow[] Skills;

		private readonly Dictionary<int, int> _attrs;

		public HeroRow(int heroId, string actor, int clothesId, int superSkillId, int superNeedSp, int[] superSkillIds, SkillRow[] skills, int[] attrsFlat)
		{
			HeroId = heroId;
			Actor = actor;
			ClothesId = clothesId;
			SuperSkillId = superSkillId;
			SuperNeedSp = superNeedSp;
			SuperSkillIds = superSkillIds;
			Skills = skills;
			_attrs = new Dictionary<int, int>();
			for (int i = 0; i + 1 < attrsFlat.Length; i += 2)
			{
				_attrs[attrsFlat[i]] = attrsFlat[i + 1];
			}
		}

		public Dictionary<int, int> CopyConfigAttrs()
		{
			return new Dictionary<int, int>(_attrs);
		}

		public int GetAttr(int type, int fallback)
		{
			if (!_attrs.TryGetValue(type, out var value))
			{
				return fallback;
			}
			return value;
		}
	}

	public sealed class RecruitRow
	{
		public readonly int HeroId;

		public readonly int Type;

		public readonly int Required;

		public readonly int ItemId;

		public readonly bool IsProgress;

		public readonly int StageId;

		public readonly string Name;

		public readonly string ItemName;

		public RecruitRow(int heroId, int type, int required, int itemId, bool isProgress, int stageId, string name, string itemName)
		{
			HeroId = heroId;
			Type = type;
			Required = required;
			ItemId = itemId;
			IsProgress = isProgress;
			StageId = stageId;
			Name = name;
			ItemName = itemName;
		}
	}

	public static readonly Dictionary<int, HeroRow> Heroes = BuildHeroes();

	public static readonly Dictionary<int, RecruitRow> Recruits = BuildRecruits();

	private static Dictionary<int, HeroRow> BuildHeroes()
	{
		Dictionary<int, HeroRow> dictionary = new Dictionary<int, HeroRow>();
		dictionary[101] = new HeroRow(101, "Inuyasha", 101001, 101400100, 900, new int[2] { 101400100, 101400500 }, new SkillRow[20]
		{
			new SkillRow(101101100, 1, 1, 101),
			new SkillRow(101101200, 1, 1, 101),
			new SkillRow(101101300, 1, 1, 101),
			new SkillRow(101102100, 1, 1, 102),
			new SkillRow(101102200, 1, 1, 102),
			new SkillRow(101102300, 1, 1, 102),
			new SkillRow(101103100, 1, 1, 103),
			new SkillRow(101104100, 1, 1, 104),
			new SkillRow(101201100, 1, 1, 201),
			new SkillRow(101201200, 1, 1, 201),
			new SkillRow(101201300, 1, 1, 201),
			new SkillRow(101202100, 1, 1, 202),
			new SkillRow(101202200, 1, 1, 202),
			new SkillRow(101202300, 1, 1, 202),
			new SkillRow(101203100, 1, 1, 203),
			new SkillRow(101204100, 1, 1, 204),
			new SkillRow(101300100, 1, 1, 300),
			new SkillRow(101300200, 1, 1, 300),
			new SkillRow(101400100, 1, 1, 400),
			new SkillRow(101400500, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		dictionary[102] = new HeroRow(102, "Gewei", 102001, 102400100, 900, new int[2] { 102400100, 102400300 }, new SkillRow[26]
		{
			new SkillRow(102101100, 1, 1, 101),
			new SkillRow(102101200, 1, 1, 101),
			new SkillRow(102101300, 1, 1, 101),
			new SkillRow(102102100, 1, 1, 102),
			new SkillRow(102102200, 1, 1, 102),
			new SkillRow(102102300, 1, 1, 102),
			new SkillRow(102103100, 1, 1, 103),
			new SkillRow(102103200, 1, 1, 103),
			new SkillRow(102103300, 1, 1, 103),
			new SkillRow(102103400, 1, 1, 103),
			new SkillRow(102104100, 1, 1, 104),
			new SkillRow(102104200, 1, 1, 104),
			new SkillRow(102104300, 1, 1, 104),
			new SkillRow(102104400, 1, 1, 104),
			new SkillRow(102201100, 1, 1, 201),
			new SkillRow(102201200, 1, 1, 201),
			new SkillRow(102201300, 1, 1, 201),
			new SkillRow(102202100, 1, 1, 202),
			new SkillRow(102202200, 1, 1, 202),
			new SkillRow(102202300, 1, 1, 202),
			new SkillRow(102203100, 1, 1, 203),
			new SkillRow(102204100, 1, 1, 204),
			new SkillRow(102300100, 1, 1, 300),
			new SkillRow(102300200, 1, 1, 300),
			new SkillRow(102400100, 1, 1, 400),
			new SkillRow(102400300, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		dictionary[103] = new HeroRow(103, "Qibao", 103001, 103400100, 900, new int[2] { 103400100, 103400300 }, new SkillRow[18]
		{
			new SkillRow(103101100, 1, 1, 101),
			new SkillRow(103101200, 1, 1, 101),
			new SkillRow(103101300, 1, 1, 101),
			new SkillRow(103102100, 1, 1, 102),
			new SkillRow(103102200, 1, 1, 102),
			new SkillRow(103102300, 1, 1, 102),
			new SkillRow(103103100, 1, 1, 103),
			new SkillRow(103104100, 1, 1, 104),
			new SkillRow(103201100, 1, 1, 201),
			new SkillRow(103201200, 1, 1, 201),
			new SkillRow(103202100, 1, 1, 202),
			new SkillRow(103202200, 1, 1, 202),
			new SkillRow(103203100, 1, 1, 203),
			new SkillRow(103204100, 1, 1, 204),
			new SkillRow(103300100, 1, 1, 300),
			new SkillRow(103300200, 1, 1, 300),
			new SkillRow(103400100, 1, 1, 400),
			new SkillRow(103400300, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		dictionary[104] = new HeroRow(104, "Shashengwan", 104001, 104400100, 900, new int[2] { 104400100, 104400500 }, new SkillRow[24]
		{
			new SkillRow(104101100, 1, 1, 101),
			new SkillRow(104101200, 1, 1, 101),
			new SkillRow(104101300, 1, 1, 101),
			new SkillRow(104102100, 1, 1, 102),
			new SkillRow(104102200, 1, 1, 102),
			new SkillRow(104102300, 1, 1, 102),
			new SkillRow(104103100, 1, 1, 103),
			new SkillRow(104103200, 1, 1, 103),
			new SkillRow(104103300, 1, 1, 103),
			new SkillRow(104103400, 1, 1, 103),
			new SkillRow(104104100, 1, 1, 104),
			new SkillRow(104201100, 1, 1, 201),
			new SkillRow(104201200, 1, 1, 201),
			new SkillRow(104201300, 1, 1, 201),
			new SkillRow(104202100, 1, 1, 202),
			new SkillRow(104202200, 1, 1, 202),
			new SkillRow(104202300, 1, 1, 202),
			new SkillRow(104202400, 1, 1, 202),
			new SkillRow(104203100, 1, 1, 203),
			new SkillRow(104204100, 1, 1, 204),
			new SkillRow(104300100, 1, 1, 300),
			new SkillRow(104300200, 1, 1, 300),
			new SkillRow(104400100, 1, 1, 400),
			new SkillRow(104400500, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		dictionary[106] = new HeroRow(106, "Feitian", 106001, 106400200, 900, new int[2] { 106400200, 106400400 }, new SkillRow[22]
		{
			new SkillRow(106101100, 1, 1, 101),
			new SkillRow(106101200, 1, 1, 101),
			new SkillRow(106101300, 1, 1, 101),
			new SkillRow(106101400, 1, 1, 101),
			new SkillRow(106102100, 1, 1, 102),
			new SkillRow(106102200, 1, 1, 102),
			new SkillRow(106102300, 1, 1, 102),
			new SkillRow(106103100, 1, 1, 103),
			new SkillRow(106103200, 1, 1, 103),
			new SkillRow(106103300, 1, 1, 103),
			new SkillRow(106104100, 1, 1, 104),
			new SkillRow(106201100, 1, 1, 201),
			new SkillRow(106201200, 1, 1, 201),
			new SkillRow(106202100, 1, 1, 202),
			new SkillRow(106202200, 1, 1, 202),
			new SkillRow(106203100, 1, 1, 203),
			new SkillRow(106204100, 1, 1, 204),
			new SkillRow(106204200, 1, 1, 204),
			new SkillRow(106300100, 1, 1, 300),
			new SkillRow(106300200, 1, 1, 300),
			new SkillRow(106400200, 1, 1, 400),
			new SkillRow(106400400, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		dictionary[108] = new HeroRow(108, "gangya", 108001, 108400200, 900, new int[2] { 108400200, 108400400 }, new SkillRow[21]
		{
			new SkillRow(108101100, 1, 1, 101),
			new SkillRow(108101200, 1, 1, 101),
			new SkillRow(108101300, 1, 1, 101),
			new SkillRow(108101400, 1, 1, 101),
			new SkillRow(108102100, 1, 1, 102),
			new SkillRow(108102200, 1, 1, 102),
			new SkillRow(108102300, 1, 1, 102),
			new SkillRow(108102400, 1, 1, 102),
			new SkillRow(108103100, 1, 1, 103),
			new SkillRow(108103200, 1, 1, 103),
			new SkillRow(108104100, 1, 1, 104),
			new SkillRow(108201100, 1, 1, 201),
			new SkillRow(108201200, 1, 1, 201),
			new SkillRow(108202100, 1, 1, 202),
			new SkillRow(108202200, 1, 1, 202),
			new SkillRow(108203100, 1, 1, 203),
			new SkillRow(108204100, 1, 1, 204),
			new SkillRow(108300100, 1, 1, 300),
			new SkillRow(108300200, 1, 1, 300),
			new SkillRow(108400200, 1, 1, 400),
			new SkillRow(108400400, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		// 桔梗（英雄 105）：本次通过 patch_bins_kikyo.py 给客户端 heroAttr.bin 补了 105 行，
		// 服务端也要有对应数据，否则 _formation / 上阵会被过滤掉。
		// 技能表与奥义取自 SkillGet 里 heroId=105 的 25 条（105400100 = SuperSkill_1）。
		dictionary[105] = new HeroRow(105, "Jiegeng", 105001, 105400100, 900, new int[2] { 105400100, 105400200 }, new SkillRow[25]
		{
			new SkillRow(105101100, 1, 1, 101),
			new SkillRow(105101200, 1, 1, 101),
			new SkillRow(105101300, 1, 1, 101),
			new SkillRow(105102100, 1, 1, 102),
			new SkillRow(105102200, 1, 1, 102),
			new SkillRow(105102300, 1, 1, 102),
			new SkillRow(105103100, 1, 1, 103),
			new SkillRow(105103200, 1, 1, 103),
			new SkillRow(105103300, 1, 1, 103),
			new SkillRow(105104100, 1, 1, 104),
			new SkillRow(105104200, 1, 1, 104),
			new SkillRow(105201100, 1, 1, 201),
			new SkillRow(105201200, 1, 1, 201),
			new SkillRow(105202100, 1, 1, 202),
			new SkillRow(105202200, 1, 1, 202),
			new SkillRow(105202300, 1, 1, 202),
			new SkillRow(105202400, 1, 1, 202),
			new SkillRow(105203100, 1, 1, 203),
			new SkillRow(105203200, 1, 1, 203),
			new SkillRow(105204100, 1, 1, 204),
			new SkillRow(105204200, 1, 1, 204),
			new SkillRow(105300100, 1, 1, 300),
			new SkillRow(105300200, 1, 1, 300),
			new SkillRow(105400100, 1, 1, 400),
			new SkillRow(105400200, 1, 10, 400)
		}, new int[34]
		{
			1, 0, 2, 0, 3, 0, 4, 0, 5, 0,
			6, 0, 7, 0, 8, 0, 9, 0, 10, 1000,
			11, 6, 12, 300, 13, 10, 23, 0, 24, 0,
			30, 1, 46, 1000
		});
		return dictionary;
	}

	private static Dictionary<int, RecruitRow> BuildRecruits()
	{
		return new Dictionary<int, RecruitRow>
		{
			[100] = new RecruitRow(100, 0, 0, 0, isProgress: false, 0, "日暮篱", ""),
			[101] = new RecruitRow(101, 1, 1, 0, isProgress: false, 100101, "犬夜叉", ""),
			[102] = new RecruitRow(102, 4, 380, 5010201, isProgress: true, 0, "日暮篱", "阿篱碎片"),
			[103] = new RecruitRow(103, 1, 1, 0, isProgress: false, 100105, "七宝", ""),
			[104] = new RecruitRow(104, 4, 480, 5010401, isProgress: true, 0, "杀生丸", "杀生丸碎片"),
			// 桔梗：用 recruit=1（通关解锁）走 HeroSystem 的 Pass 分支，
			// 这样不需要在 Itemstype 里新增「桔梗碎片」，避免客户端裸索引器取不到道具名而崩。
			[105] = new RecruitRow(105, 1, 1, 0, isProgress: false, 100101, "桔梗", ""),
			[106] = new RecruitRow(106, 4, 260, 5010601, isProgress: true, 0, "飞天", "飞天碎片"),
			[108] = new RecruitRow(108, 4, 320, 5010801, isProgress: true, 0, "钢牙", "钢牙碎片")
		};
	}
}
