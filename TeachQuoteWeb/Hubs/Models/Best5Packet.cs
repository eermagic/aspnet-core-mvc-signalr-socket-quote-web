namespace TeachQuoteWeb.Hubs.Models
{
	[Serializable]
	public class Best5Packet : IPacket
	{
		public string ID { get; set; }
		public string Symbol { get; set; }
		public double Bid1Price { get; set; }
		public double Bid1Qty { get; set; }
		public double Bid2Price { get; set; }
		public double Bid2Qty { get; set; }
		public double Bid3Price { get; set; }
		public double Bid3Qty { get; set; }
		public double Bid4Price { get; set; }
		public double Bid4Qty { get; set; }
		public double Bid5Price { get; set; }
		public double Bid5Qty { get; set; }
		public double Ask1Price { get; set; }
		public double Ask1Qty { get; set; }
		public double Ask2Price { get; set; }
		public double Ask2Qty { get; set; }
		public double Ask3Price { get; set; }
		public double Ask3Qty { get; set; }
		public double Ask4Price { get; set; }
		public double Ask4Qty { get; set; }
		public double Ask5Price { get; set; }
		public double Ask5Qty { get; set; }
	}
}
