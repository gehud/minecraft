namespace Minecraft {
	public interface ISavePayload {
		string Name { get; set; }

		ConnectionRoles Role { get; set; }
	}
}
