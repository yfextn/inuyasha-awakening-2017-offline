using System;
using System.Reflection;
using Inuyasha;
using UnityEngine;
using XEngine;

namespace InuyashaLocalServer;

public sealed class SdkBypass : MonoBehaviour
{
	private const string ChannelUserId = "16013096";

	private const string ChannelId = "heitao_heitao";

	private const string Token = "local_single_player_token";

	private static SdkBypass _instance;

	private bool _done;

	private FieldInfo _callBackField;

	public static void Install()
	{
		if (_instance != null)
		{
			return;
		}
		try
		{
			GameObject obj = new GameObject("InuyashaLocalSdkBypass");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			_instance = obj.AddComponent<SdkBypass>();
			L.Log("[SDK] 已挂上登录绕过组件，等客户端请求登录后自动送入本地账号 16013096");
		}
		catch (Exception e)
		{
			L.Err("SdkBypass.Install", e);
		}
	}

	private void Update()
	{
		if (_done)
		{
			return;
		}
		try
		{
			SDKInterfaceManger instance = EngineSingleton<SDKInterfaceManger>.Instance;
			if (instance == null)
			{
				return;
			}
			if (_callBackField == null)
			{
				_callBackField = typeof(SDKInterfaceManger).GetField("callBack", BindingFlags.Instance | BindingFlags.NonPublic);
				if (_callBackField == null)
				{
					_done = true;
					L.Log("[SDK] 找不到 SDKInterfaceManger.callBack，放弃绕过（保持原流程）");
					return;
				}
			}
			if (_callBackField.GetValue(instance) != null)
			{
				_done = true;
				string text = "{\"authUrl\":\"\",\"channelId\":\"heitao_heitao\",\"channelUserId\":\"16013096\",\"token\":\"local_single_player_token\",\"platform\":\"2\"}";
				L.Log("[SDK] 客户端正在等登录回调，直接喂本地账号: " + text);
				instance.loginCallback(text);
			}
		}
		catch (Exception e)
		{
			_done = true;
			L.Err("SdkBypass.Update", e);
		}
	}
}
