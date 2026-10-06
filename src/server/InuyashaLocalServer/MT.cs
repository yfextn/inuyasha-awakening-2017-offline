using System;
using System.Collections.Generic;
using Inuyasha.Generated.Protocol;

namespace InuyashaLocalServer;

internal static class MT
{
	private static readonly Dictionary<int, string> Id2Enum;

	private static readonly Dictionary<string, int> Enum2Id;

	static MT()
	{
		Id2Enum = new Dictionary<int, string>();
		Enum2Id = new Dictionary<string, int>();
		foreach (MessageType value in Enum.GetValues(typeof(MessageType)))
		{
			int num = Convert.ToInt32(value);
			string text = value.ToString();
			Id2Enum[num] = text;
			if (!Enum2Id.ContainsKey(text))
			{
				Enum2Id[text] = num;
			}
		}
	}

	public static string ClassName(int id)
	{
		if (!Id2Enum.TryGetValue(id, out var value) || value == null)
		{
			return null;
		}
		if (id == 0)
		{
			return value;
		}
		if (!value.StartsWith("_"))
		{
			return value;
		}
		return value.Substring(1);
	}

	public static string Name(int id)
	{
		if (Id2Enum.TryGetValue(id, out var value) && value != null)
		{
			if (!value.StartsWith("_"))
			{
				return value;
			}
			return value.Substring(1);
		}
		return "<未知:" + id + ">";
	}

	public static int EnumId(string ident)
	{
		if (ident == null || !Enum2Id.TryGetValue(ident, out var value))
		{
			return 0;
		}
		return value;
	}

	public static int ResponseIdFor(int id)
	{
		if (!Id2Enum.TryGetValue(id, out var value) || string.IsNullOrEmpty(value))
		{
			return 0;
		}
		string text = (value.EndsWith("Requset") ? "Requset" : "Request");
		if (!value.EndsWith(text))
		{
			return 0;
		}
		return EnumId(value.Substring(0, value.Length - text.Length) + "Response");
	}
}
