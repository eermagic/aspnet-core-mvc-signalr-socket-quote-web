using System.Net.Sockets;

namespace TeachQuoteWeb.Hubs.Models
{
	[Serializable]
	public class ClientInfo : IPacket
	{
		public string ID { get; set; }

		[NonSerialized]
		public TcpClient Client;
		public ClientInfo(string id)
		{
			this.ID = id;
		}
	}
}
