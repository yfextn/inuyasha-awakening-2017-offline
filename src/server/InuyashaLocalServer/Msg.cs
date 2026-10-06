using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Inuyasha.Generated.Protocol;
using ProtoBuf;

namespace InuyashaLocalServer;

internal static class Msg
{
	public static readonly EmptyMsg Empty;

	public static readonly EmptyList EmptyListBox;

	private static readonly Dictionary<string, Type> ByName;

	private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropCache;

	private static readonly Dictionary<Type, MethodInfo> SerCache;

	private static readonly Dictionary<Type, MethodInfo> DeserCache;

	private static readonly object Gate;

	static Msg()
	{
		Empty = new EmptyMsg();
		EmptyListBox = new EmptyList();
		ByName = new Dictionary<string, Type>();
		PropCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
		SerCache = new Dictionary<Type, MethodInfo>();
		DeserCache = new Dictionary<Type, MethodInfo>();
		Gate = new object();
		try
		{
			Assembly assembly = typeof(MessageType).Assembly;
			Type[] array = null;
			try
			{
				array = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				array = ex.Types;
			}
			if (array == null)
			{
				return;
			}
			Type[] array2 = array;
			foreach (Type type in array2)
			{
				if (type != null && !type.IsAbstract && !type.IsInterface && !type.IsEnum)
				{
					if (!ByName.TryGetValue(type.Name, out var value))
					{
						ByName[type.Name] = type;
					}
					else if (value.IsNested && !type.IsNested)
					{
						ByName[type.Name] = type;
					}
				}
			}
		}
		catch (Exception e)
		{
			L.Err("Msg 静态初始化", e);
		}
	}

	public static Type FindType(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (!ByName.TryGetValue(name, out var value))
		{
			return null;
		}
		return value;
	}

	private static string Norm(string s)
	{
		if (s == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		foreach (char c in s)
		{
			if (c != '_')
			{
				stringBuilder.Append(char.ToLowerInvariant(c));
			}
		}
		return stringBuilder.ToString();
	}

	private static Dictionary<string, PropertyInfo> PropsOf(Type t)
	{
		lock (Gate)
		{
			if (PropCache.TryGetValue(t, out var value))
			{
				return value;
			}
			value = new Dictionary<string, PropertyInfo>();
			PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (propertyInfo.GetIndexParameters().Length == 0)
				{
					string key = Norm(propertyInfo.Name);
					if (!value.ContainsKey(key))
					{
						value[key] = propertyInfo;
					}
				}
			}
			PropCache[t] = value;
			return value;
		}
	}

	public static PropertyInfo FindProp(Type t, string protoName)
	{
		if (PropsOf(t).TryGetValue(Norm(protoName), out var value))
		{
			return value;
		}
		return null;
	}

	public static bool IsList(Type t)
	{
		if (t.IsGenericType)
		{
			return t.GetGenericTypeDefinition() == typeof(List<>);
		}
		return false;
	}

	private static IEnumerable AsItems(object value)
	{
		if (value is EmptyList)
		{
			return new object[0];
		}
		if (value is string text)
		{
			return new object[1] { text };
		}
		if (value is byte[] array)
		{
			return new object[1] { array };
		}
		if (!(value is IEnumerable result))
		{
			return new object[1] { value };
		}
		return result;
	}

	private static object ConvertTo(Type target, object v)
	{
		if (v == null)
		{
			return null;
		}
		if (target.IsInstanceOfType(v))
		{
			return v;
		}
		if (target.IsEnum)
		{
			return Enum.ToObject(target, Convert.ToInt64(v, CultureInfo.InvariantCulture));
		}
		if (target == typeof(string))
		{
			return v.ToString();
		}
		if (target == typeof(bool))
		{
			return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
		}
		return Convert.ChangeType(v, target, CultureInfo.InvariantCulture);
	}

	public static bool Set(object obj, string protoName, object value)
	{
		if (obj == null || value == null)
		{
			return false;
		}
		Type type = obj.GetType();
		PropertyInfo propertyInfo = FindProp(type, protoName);
		if (propertyInfo == null)
		{
			L.Log("[Set] 找不到字段 " + type.Name + "." + protoName + "（客户端字段名可能变了）");
			return false;
		}
		Type propertyType = propertyInfo.PropertyType;
		if (IsList(propertyType))
		{
			IList list = null;
			try
			{
				list = propertyInfo.GetValue(obj, null) as IList;
			}
			catch
			{
			}
			if (list == null)
			{
				L.Log("[Set] 列表字段拿不到实例 " + type.Name + "." + protoName);
				return false;
			}
			Type target = propertyType.GetGenericArguments()[0];
			foreach (object item in AsItems(value))
			{
				if (item != null)
				{
					list.Add(ConvertTo(target, item));
				}
			}
			return true;
		}
		if (value == Empty)
		{
			try
			{
				propertyInfo.SetValue(obj, Activator.CreateInstance(propertyType), null);
				return true;
			}
			catch (Exception e)
			{
				L.Err("Set(Empty) " + protoName, e);
				return false;
			}
		}
		if (!propertyInfo.CanWrite)
		{
			L.Log("[Set] 字段只读 " + type.Name + "." + protoName);
			return false;
		}
		try
		{
			propertyInfo.SetValue(obj, ConvertTo(propertyType, value), null);
			return true;
		}
		catch (Exception e2)
		{
			L.Err("Set(" + protoName + ")", e2);
			return false;
		}
	}

	public static byte[] SerializeMsg<T>(T msg)
	{
		MemoryStream memoryStream = new MemoryStream();
		Serializer.Serialize(memoryStream, msg);
		return memoryStream.ToArray();
	}

	public static T DeserializeMsg<T>(byte[] data)
	{
		if (data == null || data.Length == 0)
		{
			return default(T);
		}
		return Serializer.Deserialize<T>(new MemoryStream(data));
	}

	private static MethodInfo FindGeneric(string name, int paramCount)
	{
		MethodInfo[] methods = typeof(Msg).GetMethods(BindingFlags.Static | BindingFlags.Public);
		for (int i = 0; i < methods.Length; i++)
		{
			if (!(methods[i].Name != name) && methods[i].IsGenericMethodDefinition && methods[i].GetParameters().Length == paramCount)
			{
				return methods[i];
			}
		}
		return null;
	}

	public static byte[] SerializeDyn(object msg)
	{
		if (msg == null)
		{
			return new byte[0];
		}
		Type type = msg.GetType();
		MethodInfo value;
		lock (Gate)
		{
			if (!SerCache.TryGetValue(type, out value))
			{
				value = FindGeneric("SerializeMsg", 1)?.MakeGenericMethod(type);
				SerCache[type] = value;
			}
		}
		if (value == null)
		{
			L.Log("[Msg] 找不到序列化方法 " + type.Name);
			return new byte[0];
		}
		try
		{
			return (byte[])value.Invoke(null, new object[1] { msg });
		}
		catch (Exception e)
		{
			L.Err("SerializeDyn " + type.Name, e);
			return new byte[0];
		}
	}

	public static object DeserializeDyn(Type t, byte[] data)
	{
		if (t == null)
		{
			return null;
		}
		MethodInfo value;
		lock (Gate)
		{
			if (!DeserCache.TryGetValue(t, out value))
			{
				value = FindGeneric("DeserializeMsg", 1)?.MakeGenericMethod(t);
				DeserCache[t] = value;
			}
		}
		if (value == null)
		{
			return null;
		}
		try
		{
			return value.Invoke(null, new object[1] { data });
		}
		catch (Exception e)
		{
			L.Err("DeserializeDyn " + t.Name, e);
			return null;
		}
	}

	public static object BuildDefault(Type t, int depth)
	{
		object obj = Activator.CreateInstance(t);
		if (depth <= 0)
		{
			return obj;
		}
		PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetIndexParameters().Length != 0)
			{
				continue;
			}
			Type propertyType = propertyInfo.PropertyType;
			if (propertyType.IsClass && propertyType != typeof(string) && propertyType != typeof(byte[]) && !IsList(propertyType) && propertyInfo.CanWrite)
			{
				try
				{
					propertyInfo.SetValue(obj, BuildDefault(propertyType, depth - 1), null);
				}
				catch
				{
				}
			}
		}
		return obj;
	}

	public static Attr MakeAttr(int type, int value)
	{
		Attr attr = new Attr();
		Set(attr, "type", type);
		Set(attr, "value", value);
		return attr;
	}

	public static ItemMsg MakeItem(int itemId, long ownerUid, int num)
	{
		ItemMsg itemMsg = new ItemMsg();
		Set(itemMsg, "itemId", itemId);
		Set(itemMsg, "itemUid", ownerUid * 100000000 + itemId);
		Set(itemMsg, "num", num);
		return itemMsg;
	}

	public static StageHeroInfo.SkillLevelInfo MakeSkillLevel(int skillId, int lv)
	{
		StageHeroInfo.SkillLevelInfo skillLevelInfo = new StageHeroInfo.SkillLevelInfo();
		Set(skillLevelInfo, "skill_id", skillId);
		Set(skillLevelInfo, "skill_level", lv);
		return skillLevelInfo;
	}
}
