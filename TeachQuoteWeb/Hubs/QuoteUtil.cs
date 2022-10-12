using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using TeachQuoteWeb.Hubs.Models;

namespace TeachQuoteWeb.Hubs
{
    public class QuoteUtil
    {
        #region 屬性
        public static IHubContext<QuoteHub> hubContext;
        public static List<UserRquest> listUserRquest = new List<UserRquest>(); //使用者訂閱名單
        public static ClientInfo? clientInfo = null;
        static TcpClient TCPClient = new TcpClient();
        static bool isTCPListen = false;
        static Thread? ThreadTCPListen;
        #endregion

        #region 建構子
        /// <summary>
        /// 報價連線初始化
        /// </summary>
        public static void Init()
        {
            if (clientInfo == null)
            {
                string ID = DateTime.Now.ToString("ss") + DateTime.Now.Millisecond;
                clientInfo = new ClientInfo(ID);
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 連線伺服器
        /// </summary>
        /// <returns></returns>
        public static string Connect()
        {
            // 連線報價伺服器
            if (!TCPClient.Connected)
            {
                string serverIp = "127.0.0.1";
                int serverPort = 8888;

                try
                {
                    IPAddress address;
                    if (!IPAddress.TryParse(serverIp, out address))
                    {
                        address = Dns.Resolve(serverIp).AddressList[0];
                    }
                    IPEndPoint ServerEndpoint = new IPEndPoint(address, serverPort);

                    TCPClient.Client.Connect(ServerEndpoint);
                    TCPClient.Client.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);

                    isTCPListen = true;
                    ListenTCP();

                    SendTCP(clientInfo);
                }
                catch (Exception ex)
                {
                    return "連線錯誤: " + ex.Message;
                }
            }
            return "";
        }

        /// <summary>
        /// 使用者離線
        /// </summary>
        public static void UserDisconnect(string connID)
        {
            UserRquest item = listUserRquest.FirstOrDefault(w => w.ID == connID);
            if (item != null)
            {
                listUserRquest.Remove(item);
            }
        }

        /// <summary>
        /// 監聽 TCP
        /// </summary>
        private static void ListenTCP()
        {
            ThreadTCPListen = new Thread(new ThreadStart(delegate
            {
                byte[] receiveBuffer = new byte[0];
                byte[] processBuffer = new byte[0];
                byte[] packet = new byte[1024];
                byte[] lenPacket = new byte[8];
                int size = 0;
                int bytesRead = 0;
                while (isTCPListen)
                {
                    try
                    {
                        bytesRead = TCPClient.GetStream().Read(packet, 0, packet.Length);

                        if (bytesRead > 0)
                        {

                            receiveBuffer = MargeByte(receiveBuffer, packet, bytesRead);
                            if (receiveBuffer.Length < 8 && bytesRead < 8)
                            {
                                continue;
                            }

                            lenPacket = GetByteData(receiveBuffer, 0, 8);
                            size = int.Parse(Encoding.UTF8.GetString(lenPacket));
                            while (size > 0)
                            {
                                if (size <= receiveBuffer.Length - 8)
                                {
                                    processBuffer = GetByteData(receiveBuffer, 8, size);
                                    IPacket Item = ByteToPacket(processBuffer);
                                    ProcessReceive(Item);

                                    receiveBuffer = GetByteData(receiveBuffer, 8 + size, receiveBuffer.Length - size - 8);
                                    if (receiveBuffer.Length < 8)
                                    {
                                        break;
                                    }
                                    lenPacket = GetByteData(receiveBuffer, 0, 8);
                                    size = int.Parse(Encoding.UTF8.GetString(lenPacket));
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            isTCPListen = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowAlert("TCP 接收錯誤: " + ex.Message);
                        break;
                    }
                }

                // 停止連線
                isTCPListen = false;
                TCPClient.Client.Disconnect(true);
            }));

            ThreadTCPListen.IsBackground = true;

            if (isTCPListen)
            {
                ThreadTCPListen.Start();
            }
        }

        /// <summary>
        /// 處理接收項目
        /// </summary>
        /// <param name="Item"></param>
        private static void ProcessReceive(IPacket Item)
        {
            if (isTCPListen == false)
            {
                return;
            }
            if (Item.GetType() == typeof(TickPacket))
            {
                // Tick 回報
                TickPacket packet = (TickPacket)Item;
                List<UserRquest> items = listUserRquest.Where(w => w.Symbol == packet.Symbol).ToList();
                foreach (var item in items)
                {
                    // 傳送至前端
                    hubContext.Clients.Client(item.ID).SendAsync("UpdTick", packet.Symbol, packet.Close, packet.Qty);
                }
            }
            else if (Item.GetType() == typeof(Best5Packet))
            {
                //Best5 回報
                Best5Packet packet = (Best5Packet)Item;
                List<UserRquest> items = listUserRquest.Where(w => w.Symbol == packet.Symbol).ToList();
                foreach (var item in items)
                {
                    // 傳送至前端
                    hubContext.Clients.Client(item.ID).SendAsync("UpdBest5", packet.Symbol, JsonConvert.SerializeObject(packet));
                }
            }
        }

        /// <summary>
        /// 傳送 TCP
        /// </summary>
        /// <param name="Item"></param>
        public static void SendTCP(IPacket Item)
        {
            if (TCPClient.Connected)
            {
                byte[] Data = PacketToByteArray(Item);
                NetworkStream NetStream = TCPClient.GetStream();
                NetStream.Write(Data, 0, Data.Length);
            }
        }

        /// <summary>
        /// 傳送物件轉 Byte
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static byte[] PacketToByteArray(IPacket packet)
        {
            string type = packet.GetType().Name;
            string jsonString = JsonConvert.SerializeObject(packet);
            byte[] data = Encoding.UTF8.GetBytes(type + "|" + jsonString);

            int len = data.Length;
            if (len > 99999999)
            {
                throw new Exception("傳送字串超過長度限制");
            }
            byte[] lenData = Encoding.UTF8.GetBytes(len.ToString("00000000"));
            byte[] newData = MargeByte(lenData, data, 0);
            return newData;
        }

        /// <summary>
        /// 用戶訂閱報價
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reqSymbol"></param>
        /// <returns></returns>
        public static string GetRequestSymbol(string id, string symbol)
        {
            // 檢查是否已連接
            if (isTCPListen == false)
            {
                return "報價伺服器未連線";
            }


            // 檢查存在訂閱商品代碼列表
            listUserRquest.RemoveAll(w => w.ID == id);
            listUserRquest.Add(new UserRquest()
            {
                ID = id,
                Symbol = symbol,
            });

            RequestQuotePacket reqQuote = new RequestQuotePacket(clientInfo.ID);
            reqQuote.Symbol = new List<string>();
            reqQuote.Symbol.Add(symbol);
            SendTCP(reqQuote);

            return "";
        }

        /// <summary>
        /// 連線心跳檢測
        /// </summary>
        /// <returns></returns>
        public static byte[] GetKeepAliveData()
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//keep-alive間隔
            BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);// 嘗試間隔
            return inOptionValues;
        }

        /// <summary>
        /// 合併位元組
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="bsz"></param>
        /// <returns></returns>
        public static byte[] MargeByte(byte[] a, byte[] b, int bsz)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(a, 0, a.Length);
                ms.Write(b, 0, (bsz == 0) ? b.Length : bsz);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 取得位元組
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="pos"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetByteData(byte[] buf, int pos, int length)
        {
            byte[] b = new byte[length];
            Array.Copy(buf, pos, b, 0, length);
            return b;
        }

        /// <summary>
        /// Byte 轉傳送物件
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IPacket ByteToPacket(byte[] bytes)
        {
            string jsonString = Encoding.UTF8.GetString(bytes);
            string type = jsonString.Split('|')[0];
            try
            {
                switch (type)
                {
                    case "TickPacket":
                        return JsonConvert.DeserializeObject<TickPacket>(jsonString.Split('|')[1]);
                    case "Best5Packet":
                        return JsonConvert.DeserializeObject<Best5Packet>(jsonString.Split('|')[1]);
                    default:
                        throw new Exception("Not Support Type, \nSource:" + jsonString);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Convert Error: " + ex.Message + "\nSource:" + jsonString);
            }
        }

        /// <summary>
        /// 廣播顯示訊息
        /// </summary>
        /// <param name="msg"></param>
        public static void ShowAlert(string msg)
        {
            hubContext.Clients.All.SendAsync("Alert", msg);
        }
        #endregion
    }
}
