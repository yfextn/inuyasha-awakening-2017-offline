using System;
using System.IO;
using System.Text;

namespace InuyashaLocalServer;

internal static class L
{
	private static readonly object _lock = new object();

	private static string _file;

	private static bool _fileBroken;

	private static int _fileLines;

	public static void InitFile(string path)
	{
		_file = path;
		try
		{
			if (!string.IsNullOrEmpty(_file))
			{
				File.AppendAllText(_file, "\r\n===== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " InuyashaLocalServer 启动 =====\r\n", Encoding.UTF8);
			}
		}
		catch
		{
			_fileBroken = true;
		}
	}

	public static void Log(string msg)
	{
		string text = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg;
		lock (_lock)
		{
			try
			{
				Console.WriteLine(text);
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(_file) && !_fileBroken && _fileLines < 200000)
			{
				try
				{
					File.AppendAllText(_file, text + "\r\n", Encoding.UTF8);
					_fileLines++;
					return;
				}
				catch
				{
					_fileBroken = true;
					return;
				}
			}
		}
	}

	public static void Err(string where, Exception e)
	{
		Log("[错误] " + where + ": " + e.GetType().Name + ": " + e.Message);
	}
}
