using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace InuyashaLocalServer;

public static class Boot
{
	private static int _started;

	public static void Start()
	{
		if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
		{
			return;
		}
		try
		{
			try
			{
				new AndroidJavaClass("android.util.Log").CallStatic<int>("i", new object[2] { "InuLocal", "Boot.Start 进入（内嵌本地服 v1.1）" });
			}
			catch
			{
			}
			L.Log("==================================================================");
			L.Log(" 犬夜叉觉醒 内嵌单机服务端 (C#) v1.0");
			L.Log("==================================================================");
			string dir = Store.ResolveDataDir();
			dir = PrepareWritableDir(dir);
			// LAN 版可以在这里读 inu_local_config.txt 覆盖监听/通告地址；单机版不会读。
			C.LoadRuntimeConfig(dir);
			L.InitFile(Path.Combine(dir, "inu_local_server.log"));
			Store.Init(dir);
			try
			{
				new AndroidJavaClass("android.util.Log").CallStatic<int>("i", new object[2]
				{
					"InuLocal",
					"数据目录=" + dir + " HTTP/TCP 已启动"
				});
			}
			catch
			{
			}
			L.Log("[配置] HTTP " + (C.ListenAll ? "0.0.0.0" : "127.0.0.1") + ":" + C.HttpPort
				+ " / TCP " + (C.ListenAll ? "0.0.0.0" : "127.0.0.1") + ":" + C.TcpPort
				+ " / 对外地址 " + C.AdvertiseHost + (C.ListenAll ? "（局域网模式）" : "（单机模式）")
				+ " / 英雄表 " + Tables.Heroes.Count + " 个 / 招募表 " + Tables.Recruits.Count + " 条");
			L.Log("==================================================================");
			Thread thread = new Thread(HttpServer.Loop);
			thread.IsBackground = true;
			thread.Start();
			Thread thread2 = new Thread(TcpServer.Loop);
			thread2.IsBackground = true;
			thread2.Start();
			SdkBypass.Install();
		}
		catch (Exception ex)
		{
			L.Err("Boot.Start", ex);
			try
			{
				new AndroidJavaClass("android.util.Log").CallStatic<int>("e", new object[2]
				{
					"InuLocal",
					"Boot.Start 失败: " + ex
				});
			}
			catch
			{
			}
		}
	}

	private static string PrepareWritableDir(string dir)
	{
		if (TryWrite(dir))
		{
			return dir;
		}
		string[] array = new string[3] { "getCacheDir", "getFilesDir", "getExternalFilesDir" };
		for (int i = 0; i < array.Length; i++)
		{
			try
			{
				AndroidJavaObject androidJavaObject = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
				if (androidJavaObject != null)
				{
					string text = ((i != 2) ? androidJavaObject.Call<AndroidJavaObject>(array[i], new object[0]) : androidJavaObject.Call<AndroidJavaObject>(array[i], new object[1])).Call<string>("getAbsolutePath", new object[0]);
					if (!string.IsNullOrEmpty(text) && TryWrite(text))
					{
						L.Log("[数据目录] 回退到 Android " + array[i] + "() = " + text);
						return text;
					}
				}
			}
			catch (Exception ex)
			{
				L.Log("[数据目录] " + array[i] + " 不可用: " + ex.GetType().Name);
			}
		}
		return dir;
	}

	private static bool TryWrite(string dir)
	{
		if (string.IsNullOrEmpty(dir))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(dir);
			string path = Path.Combine(dir, "inu_write_test.tmp");
			File.WriteAllText(path, "x");
			File.Delete(path);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
