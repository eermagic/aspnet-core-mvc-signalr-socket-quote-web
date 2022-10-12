namespace TeachQuoteWeb.Hubs.Models
{
	[Serializable]
	public class RequestQuotePacket : IPacket
	{
		public string ID { get; set; }
		public List<string> Symbol { get; set; }

		public RequestQuotePacket(string id)
		{
			this.ID = id;
		}
	}
}
