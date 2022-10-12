using Microsoft.AspNetCore.SignalR;

namespace TeachQuoteWeb.Hubs
{
    public class QuoteHub : Hub
    {
        #region 屬性
        public static List<string> ConnIDList = new List<string>(); //用戶連線 ID 列表
        #endregion

        #region 方法
        /// <summary>
        /// 連線事件
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
            if (ConnIDList.Count == 0)
            {
                QuoteUtil.Init();
                // 連線報價伺服器
                string msg = QuoteUtil.Connect();
                if (msg != "")
                {
                    Clients.Client(Context.ConnectionId).SendAsync("Alert", msg);
                }
            }

            // 加入用戶連線列表
            if (ConnIDList.Any(w => w == Context.ConnectionId) == false)
            {
                ConnIDList.Add(Context.ConnectionId);
            }

            // 更新連線 ID
            await Clients.Client(Context.ConnectionId).SendAsync("SetHubConnId", Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 離線事件
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            string id = ConnIDList.Where(p => p == Context.ConnectionId).FirstOrDefault();
            if (id != null)
            {
                ConnIDList.Remove(id);

                QuoteUtil.UserDisconnect(id);
            }
            await base.OnDisconnectedAsync(ex);
        }
        #endregion
    }
}
