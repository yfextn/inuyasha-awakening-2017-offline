using System.Collections.Generic;

namespace InuyashaLocalServer;

internal sealed class Account
{
	public long Id;

	public string Name = "";

	public string Password = "";

	public List<Role> Roles = new List<Role>();
}
