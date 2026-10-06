namespace InuyashaLocalServer;

internal sealed class Session
{
	public long Uid;

	public string Nick = "";

	public int MainTaskId = 101002;

	public bool MainTaskReady;

	public int ActiveStage = 100101;

	public int SyncedLv;

	public long SyncedExp;

	/// <summary>竞技场：当前正在交战的对手 id（60126 里发过的 OpponentMsg.id）。
	/// 60134 必须原样带回，否则客户端 ArenaData.SetWin 里 GetOpponent 返回 null 会 NRE。</summary>
	public long ArenaOpponentId;
}
