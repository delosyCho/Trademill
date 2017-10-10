using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using TradeMill;

namespace TradeMill
{
    
    public partial class Form1 : Form
    {
        bool Restricted_Mode = false;
        bool Dynamic_Mode = false;

        float min_Loss_Percent = 0;
        int min_Loss = 0;

        float meet_Profit_Percent = 0;
        int meet_Profit = 0;

        bool is_buffer_Done = false;
        bool is_Load = false;
        bool Connected = false;
        bool isBuffer = true;
        
        int g_scr_no = 1000;
        int Sequence_Length = 20;
        int Expectation_Length = 0;

        int Exp_Loop_Index = 0;
        int Exp_Type_Index = 0;

        int Loop_Index = 0;
        int Time_Index = 0;
        int Reg_Index = 0;
        int Type_Index = -1;
        
        string Time_Stamp = "";
        Thread time_Thread = null;

        System.Windows.Forms.Timer[] Price_Refresh_Timer = new System.Windows.Forms.Timer[200];

        Bitmap bitmap = null;
        string Buffer = "";
        long[] BufferData = null;
        string[] TK = null;

        int Count = 0;
        int Prev_Cnt = 0;

        String g_user_id = "";
        String g_accnt_no = "";
        String Cursor_ID = "";

        int Cursor_Index = 0;

        bool IsListCall = false;
        bool 전체종목리스트_초기체크 = false;

        int whole_Number_Of_Available_Stock = 0;

        String[] Stock_Codes = null;
        String[] Stock_Names = null;
        String[] Stock_Prices = null;

        String[] Favorite_Stock_List_Names = null;
        String[] Favorite_Stock_List_Codes = null;

        Trade_Info[] Tr_In = new Trade_Info[1000];
        Order_Info[] Or_In = new Order_Info[1000];

        public string log(String text)
        {
            label1.Text = text;

            return label1.Text;
        }

        /*
        Network Methods
        */

        Thread Clients_Data_Recieve_Thread = null;

        private Boolean g_Connected;
        private Socket m_ClientSocket = null;
        private AsyncCallback m_fnReceiveHandler;
        private AsyncCallback m_fnSendHandler;
        
        void Recieve_Clients_Data()
        {
            while (true)
            {
                //SendMessage("#Client_Data@");
                Thread.Sleep(500);
            }
        }

        public class AsyncObject
        {
            public Byte[] Buffer;
            public Socket WorkingSocket;
            public AsyncObject(Int32 bufferSize)
            {
                this.Buffer = new Byte[bufferSize];
            }
        }

        public bool ConnectToServer(String hostName, UInt16 hostPort)
        {
            // TCP 통신을 위한 소켓을 생성합니다.
            m_ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            Boolean isConnected = false;
            try
            {
                // 연결 시도
                m_ClientSocket.Connect(hostName, hostPort);

                // 연결 성공
                isConnected = true;
            }
            catch
            {
                // 연결 실패 (연결 도중 오류가 발생함)
                isConnected = false;
            }
            g_Connected = isConnected;

            if (isConnected)
            {

                // 4096 바이트의 크기를 갖는 바이트 배열을 가진 AsyncObject 클래스 생성
                AsyncObject ao = new AsyncObject(4096);

                // 작업 중인 소켓을 저장하기 위해 sockClient 할당
                ao.WorkingSocket = m_ClientSocket;

                // 비동기적으로 들어오는 자료를 수신하기 위해 BeginReceive 메서드 사용!
                m_ClientSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);

                Console.WriteLine("연결 성공!");

            }
            else {

                Console.WriteLine("연결 실패!");

            }

            return isConnected;
        }

        public void StopClient()
        {
            // 가차없이 클라이언트 소켓을 닫습니다.
            m_ClientSocket.Close();
        }

        public void SendMessage(String message)
        {
            // 추가 정보를 넘기기 위한 변수 선언
            // 크기를 설정하는게 의미가 없습니다.
            // 왜냐하면 바로 밑의 코드에서 문자열을 유니코드 형으로 변환한 바이트 배열을 반환하기 때문에
            // 최소한의 크기르 배열을 초기화합니다.
            AsyncObject ao = new AsyncObject(1);
            Byte[] buffer = Encoding.UTF8.GetBytes(message);
            // 문자열을 바이트 배열으로 변환
            ao.Buffer = Encoding.UTF8.GetBytes(message);

            ao.WorkingSocket = m_ClientSocket;

            // 전송 시작!
            try
            {
                //m_ClientSocket.BeginSend(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnSendHandler, ao);
                m_ClientSocket.Send(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("전송 중 오류 발생!\n메세지: {0}", ex.Message);
            }
        }

        private void handleDataReceive(IAsyncResult ar)
        {

            // 넘겨진 추가 정보를 가져옵니다.
            // AsyncState 속성의 자료형은 Object 형식이기 때문에 형 변환이 필요합니다~!
            AsyncObject ao = (AsyncObject)ar.AsyncState;

            // 받은 바이트 수 저장할 변수 선언
            Int32 recvBytes;

            try
            {
                // 자료를 수신하고, 수신받은 바이트를 가져옵니다.
                recvBytes = ao.WorkingSocket.EndReceive(ar);
            }
            catch
            {
                // 예외가 발생하면 함수 종료!
                return;
            }

            // 수신받은 자료의 크기가 1 이상일 때에만 자료 처리
            if (recvBytes > 0)
            {
                // 공백 문자들이 많이 발생할 수 있으므로, 받은 바이트 수 만큼 배열을 선언하고 복사한다.
                Byte[] msgByte = new Byte[recvBytes];
                Array.Copy(ao.Buffer, msgByte, recvBytes);

                String Message = Encoding.UTF8.GetString(msgByte);
                Console.WriteLine(Message);

                String[] TK = Message.Split('#');

                if (TK[0].CompareTo("@rqdata") == 0)
                {
                    int index = 0;

                    String Msg = "";

                    if (int.Parse(TK[1]) == 0)
                    {
                        Msg = Tr_In[Cursor_Index].get_Data_Prices();
                    }
                    else if (int.Parse(TK[1]) == 1)
                    {
                        Msg = Tr_In[Cursor_Index].get_Data_Prices(true);
                    }
                    else if (int.Parse(TK[1]) == 2)
                    {
                        Msg = Tr_In[Cursor_Index].get_Data_Prices(false);
                    }

                    SendMessage(Msg);
                }
                else if (TK[0].CompareTo("clrbuffer") == 0)
                {
                    Buffer = "";
                }
                else if (TK[0].CompareTo("setdata") == 0)
                {
                    TK = Buffer.Split('@');
                    BufferData = new long[TK.Length - 1];

                    for(int i = 0; i < TK.Length - 1; i++)
                    {
                        BufferData[i] = long.Parse(TK[i]);
                    }
                }else if (TK[0].CompareTo("@notTrain") == 0)
                {
                    int index = -1;

                    for (int i = 0; i < listView2.Items.Count; i++)
                    {
                        if (Favorite_Stock_List_Codes[i].CompareTo(TK[1]) == 0)
                        {
                            index = i;
                        }
                    }

                    if(index != -1)
                    {
                        this.BeginInvoke(new Action(() => listView2.Items[index].SubItems[4].Text = "Trained:N"));
                        
                    }
                }
                else if (TK[0].CompareTo("@Trained") == 0)
                {
                    int index = -1;

                    for (int i = 0; i < listView2.Items.Count; i++)
                    {
                        if (Favorite_Stock_List_Codes[i].CompareTo(TK[1]) == 0)
                        {
                            index = i;
                        }
                    }

                    if (index != -1)
                    {
                        this.BeginInvoke(new Action(() => listView2.Items[index].SubItems[4].Text = "Trained:Y"));
                        if(TK[2].CompareTo("minute") == 0)
                        {
                            Tr_In[index].isTrained = true;
                        }
                        else if (TK[2].CompareTo("day") == 0)
                        {
                            Tr_In[index].isTrained_day = true;
                        }
                        else if (TK[2].CompareTo("week") == 0)
                        {
                            Tr_In[index].isTrained_week = true;
                        }
                    }
                }
                else if (TK[0].CompareTo("@Exp_data") == 0)
                {
                    int index = -1;
                    int type_index = -1;

                    if(TK[2].CompareTo("minute") == 0)
                    {
                        type_index = 0;
                    }else if (TK[2].CompareTo("day") == 0)
                    {
                        type_index = 1;
                    }
                    else if (TK[2].CompareTo("week") == 0)
                    {
                        type_index = 2;
                    }

                    for (int i = 0; i < listView2.Items.Count; i++)
                    {
                        if (Favorite_Stock_List_Codes[i].CompareTo(TK[1]) == 0)
                        {
                            index = i;
                        }
                    }

                    if (index != -1)
                    {
                        if(type_index == 1)
                        {
                            Tr_In[index].Exp_Data(TK[3].Split('!'), true);
                        }else if(type_index == 2)
                        {
                            Tr_In[index].Exp_Data(TK[3].Split('!'), false);
                        }else if(type_index == 0)
                        {
                            Tr_In[index].Exp_Data(TK[3].Split('!'), Time_Index);
                        }

                        this.BeginInvoke(new Action(() => listView2.Items[index].SubItems[4].Text = "Trained:Y"));
                    }
                }

                // 받은 메세지를 출력
                Console.WriteLine("메세지 받음: {0}", Encoding.UTF8.GetString(msgByte));

            }

            try
            {
                // 자료 처리가 끝났으면~
                // 이제 다시 데이터를 수신받기 위해서 수신 대기를 해야 합니다.
                // Begin~~ 메서드를 이용해 비동기적으로 작업을 대기했다면
                // 반드시 대리자 함수에서 End~~ 메서드를 이용해 비동기 작업이 끝났다고 알려줘야 합니다!
                ao.WorkingSocket.BeginReceive(ao.Buffer, 0, ao.Buffer.Length, SocketFlags.None, m_fnReceiveHandler, ao);
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 예외 정보 출력 후 함수를 종료한다.
                Console.WriteLine("자료 수신 대기 도중 오류 발생! 메세지: {0}", ex.Message);
                return;
            }
        }
        private void handleDataSend(IAsyncResult ar)
        {

            // 넘겨진 추가 정보를 가져옵니다.
            AsyncObject ao = (AsyncObject)ar.AsyncState;

            // 보낸 바이트 수를 저장할 변수 선언
            Int32 sentBytes;

            try
            {
                // 자료를 전송하고, 전송한 바이트를 가져옵니다.
                sentBytes = ao.WorkingSocket.EndSend(ar);
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 예외 정보 출력 후 함수를 종료한다.
                Console.WriteLine("자료 송신 도중 오류 발생! 메세지: {0}", ex.Message);
                return;
            }

            if (sentBytes > 0)
            {
                // 여기도 마찬가지로 보낸 바이트 수 만큼 배열 선언 후 복사한다.
                Byte[] msgByte = new Byte[sentBytes];
                Array.Copy(ao.Buffer, msgByte, sentBytes);

                Console.WriteLine("메세지 보냄: {0}", Encoding.UTF8.GetString(msgByte));
            }
        }

        /// /////////////////////////////////////////////

        /*
        기본 메소트
        */

        public bool Decision_Sell(int i)
        {

            bool Sell_Decision = false;

            long Profit_Loss = 0;
            float Profit_Loss_Percent = 0;

            Profit_Loss = Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices[Time_Index] - Or_In[i].ordered_Price;
            Profit_Loss_Percent = 1 + (float)(Profit_Loss / Or_In[i].ordered_Price);
            Profit_Loss = Profit_Loss * Or_In[i].ordered_Amount;

            int MinutePrice_Factor = 0;
            int DayPrice_Factor = 0;
            int WeekPrice_Factor = 0;

            for (int a = 0; a < 20 - 1; a++)
            {
                if (Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices[100 + a] < Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices[100 + a + 1])
                {
                    MinutePrice_Factor++;
                }
                else
                {
                    MinutePrice_Factor--;
                }

                if (Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_day[100 + a] < Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_day[100 + a + 1])
                {
                    DayPrice_Factor++;
                }
                else
                {
                    DayPrice_Factor--;
                }

                if (Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_week[100 + a] < Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_week[100 + a + 1])
                {
                    WeekPrice_Factor++;
                }
                else
                {
                    WeekPrice_Factor--;
                }
            }

            if (meet_Profit > Profit_Loss || meet_Profit_Percent > Profit_Loss_Percent)
            {
                Sell_Decision = true;
            }

            if (DayPrice_Factor < -10 && WeekPrice_Factor < -5)
            {
                if (MinutePrice_Factor < -5)
                {
                    if (min_Loss < Profit_Loss || min_Loss_Percent < Profit_Loss_Percent)
                    {
                        Sell_Decision = true;
                    }
                }
            }

            if (Dynamic_Mode)
            {
                if (DayPrice_Factor < -15 && WeekPrice_Factor < -10)
                {
                    if (MinutePrice_Factor < -10)
                    {
                        Sell_Decision = true;
                    }
                }
            }

            return Sell_Decision;
        }

        public bool Decision_Buy(int i)
        {

            bool Buy_Decision = false;

            long Profit_Loss = 0;
            float Profit_Loss_Percent = 0;

            Profit_Loss = Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices[Time_Index] - Or_In[i].ordered_Price;
            Profit_Loss_Percent = 1 + (float)(Profit_Loss / Or_In[i].ordered_Price);
            Profit_Loss = Profit_Loss * Or_In[i].ordered_Amount;

            int MinutePrice_Factor = 0;
            int DayPrice_Factor = 0;
            int WeekPrice_Factor = 0;

            for (int a = 0; a < 20 - 1; a++)
            {
                if (Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices[100 + a] < Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices[100 + a + 1])
                {
                    MinutePrice_Factor++;
                }
                else
                {
                    MinutePrice_Factor--;
                }

                if (Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_day[100 + a] < Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_day[100 + a + 1])
                {
                    DayPrice_Factor++;
                }
                else
                {
                    DayPrice_Factor--;
                }

                if (Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_week[100 + a] < Tr_In[Or_In[i].Cursor_Index].Real_Time_Prices_week[100 + a + 1])
                {
                    WeekPrice_Factor++;
                }
                else
                {
                    WeekPrice_Factor--;
                }
            }

            if (DayPrice_Factor > 10 && WeekPrice_Factor > 5)
            {
                if (MinutePrice_Factor < -5)
                {
                    if (min_Loss < Profit_Loss || min_Loss_Percent < Profit_Loss_Percent)
                    {
                        Buy_Decision = true;
                    }
                }
            }

            if (Dynamic_Mode)
            {
                if (MinutePrice_Factor > 15)
                {
                    Buy_Decision = true;
                }
            }

            return Buy_Decision;
        }


        public void predict_day_price()
        {

        }

        public void predict_week_price()
        {

        }

        public void create_order_chcek_box(bool isSell)
        {
            DialogResult dr;

            if (isSell)
            {
                dr = MessageBox.Show("매도하시겠습니까??", "알림", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            }
            else
            {
                dr = MessageBox.Show("매수하시겠습니까??", "알림", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            }
            
            if (dr == DialogResult.OK)
            {
                order_Stock(isSell);
            }
            else
            {
                MessageBox.Show("취소했습니다.");
            }
        }

        public void order_Stock(bool isSell)
        {
            string l_scr_no = get_scr_no();
            int l_buy_ord_stock_cnt = int.Parse(textBox4.Text);

            int ret = 0;

            if(isSell == true)
            {
                axKHOpenAPI1.SendOrder("매도주문", l_scr_no, g_accnt_no,
                1, Cursor_ID, l_buy_ord_stock_cnt,
                0, "03", "");
            }
            else
            {
                axKHOpenAPI1.SendOrder("매수주문", l_scr_no, g_accnt_no,
                1, Cursor_ID, l_buy_ord_stock_cnt,
                0, "03", "");
            }

            if (ret == 0)
            {

            }
            else
            {

            }
            delay(200); // 0.2초 지연
        }

        private string get_scr_no() //Open API 화면번호 가져오기 메서드
        {
            if (g_scr_no < 9999)
                g_scr_no++;
            else
                g_scr_no = 1000;
            return g_scr_no.ToString();
        }

        public void Register_Expectation_Value()
        {
            string Msg ="";

            int index = Time_Index;
            if(index < 360)
            {
                if (Time_Index > 20 && Connected)
                {
                    for (int i = Time_Index - Sequence_Length; i < Time_Index; i++)
                    {
                        Msg += Tr_In[Exp_Loop_Index].Real_Time_Prices[index] + "@";

                        index--;
                    }

                    string[] dic = { "min", "day", "week" };

                    SendMessage(Msg);
                    SendMessage("@exp_data&" + Favorite_Stock_List_Codes[Exp_Loop_Index] + "&" + dic[0]);
                }

            }

        }

        public void Register_Expectation_Value(bool isDay)
        {
            string Msg = "";

            if (Time_Index > 20 && Connected)
            {
                if (isDay)
                {
                    for (int i = 0; i < Sequence_Length; i++)
                    {
                        Msg += Tr_In[Exp_Loop_Index].Real_Time_Prices_day[i] + "@";
                    }

                    string[] dic = { "min", "day", "week" };

                    SendMessage(Msg);
                    SendMessage("@exp_data&" + Favorite_Stock_List_Codes[Cursor_Index] + "&" + dic[1]);
                }
                else
                {
                    for (int i = 0; i < Sequence_Length; i++)
                    {
                        Msg += Tr_In[Exp_Loop_Index].Real_Time_Prices_day[i] + "@";
                    }

                    string[] dic = { "min", "day", "week" };

                    SendMessage(Msg);
                    SendMessage("@exp_data&" + Favorite_Stock_List_Codes[Cursor_Index] + "&" + dic[2]);
                }
            }

        }

        public void Register_Tr_In_()
        {
            axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
            axKHOpenAPI1.CommRqData("분봉버퍼_", "OPT10080", 0, "1002");

            resetGraph_();
        }

        public void Register_Tr_In_(bool isday)
        {
            resetGraph_();

            if (isday)
            {
                axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                axKHOpenAPI1.CommRqData("일봉버퍼_", "OPT10081", 0, "1002");
            }
            else
            {
                axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                axKHOpenAPI1.CommRqData("주봉버퍼_", "OPT10082", 0, "1002");
            }
            
        }

        public void get_Current_Price(int index)
        {
            ////////////////////////////////////
            if(Time_Index < 361)
            {
                axKHOpenAPI1.SetInputValue("종목코드", Favorite_Stock_List_Codes[Loop_Index]);
                axKHOpenAPI1.CommRqData("현재가받기", "OPT10080", 0, "1002");
            }
            
        }

        public void Time_Refresh()
        {
            while (true)
            {
                string cur_time = DateTime.Now.ToString();

                label3.BeginInvoke(new Action(() => label3.Text = cur_time ));
            }
        }

        public int get_Current_Time_Index(string time_str)
        {
            return -1;
        }

        public string get_Current_Time()
        {
            DateTime l_cur_time;
            string l_cur_tm;
            l_cur_time = DateTime.Now; // 현재시각을 l_cur_time에 저장
            l_cur_tm = l_cur_time.ToString("HHmmss"); // 시분초를 l_cur_tm에 저장

            return l_cur_tm; // 현재시각 리턴
        }
        
        //실시간으로 시세정보 받아오기
        public void realtime_price_request()
        {

        }

        //listview에 item추가
        private void listView1_add(string number, string code, string codeString)
        {
            ListViewItem item2 = new ListViewItem(number, 0);
            item2.SubItems.Add(code);
            item2.SubItems.Add(codeString);

            listView1.Items.Add(item2);
        }

        //날짜 가져오기
        public DateTime delay(int MS)
        {
            DateTime ThisMoment = DateTime.Now;
            TimeSpan duration = new TimeSpan(0, 0, 0, 0, MS);
            DateTime AfterWards = ThisMoment.Add(duration);
            while (AfterWards >= ThisMoment)
            {
                try
                {
                    unsafe
                    {

                        System.Windows.Forms.Application.DoEvents();
                    }
                }
                catch (AccessViolationException ex)
                {
                    //write_err_log("delay() ex.Message : [" + ex.Message + "]\n", 0);
                }
                ThisMoment = DateTime.Now;
            }
            return DateTime.Now;
        }

        public void resetGraph()
        {
            bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bitmap;

            Brush b = new SolidBrush(Color.White);
            Graphics g = Graphics.FromImage(pictureBox1.Image);
            g.FillRectangle(b, 0, 0, pictureBox1.Image.Width, pictureBox1.Image.Height);

        }

        public void drawGraph(long[] Data, Color color)
        {
            int maxHeight = (pictureBox1.Height / 3) * 2;
            int maxWidth = (pictureBox1.Width) / 3;
            
            int ep = maxWidth;
            if (Data.Length < maxWidth)
            {
                ep = Data.Length;
            }

            Graphics g = Graphics.FromImage(pictureBox1.Image);
            Pen pen = new Pen(color);

            float Max = -99;
            float Min = 99999;

            int MaxIndex = -1;
            int MinIndex = -1;

            for(int i = 0; i < ep; i++)
            {
                if(Max < (float)Data[i])
                {
                    Max = (float)Data[i];
                    MaxIndex = i;
                }

                if (Min > (float)Data[i])
                {
                    Min = (float)Data[i];
                    MinIndex = i;
                }
            }

            int Image_Depth = pictureBox1.Height / 2;
            int Image_Offset = pictureBox1.Height / 4;

            float Depth = Max - Min;
            
            for(int i = 0; i < ep - 1; i++)
            {
                float f_Height = (float)pictureBox1.Height;
                int Y = (int)(f_Height - ((float)Image_Offset + (Image_Depth / Depth) * ((float)Data[i] - Min)));
                int Y2 = (int)(f_Height - ((float)Image_Offset + (Image_Depth / Depth) * ((float)Data[i + 1] - Min)));

                Point point1 = new Point(i * 3, Y);
                Point point2 = new Point(i * 3 + 3, Y2);

                g.DrawLine(pen, point1, point2);
            }

            pictureBox1.Refresh();
        }

        public void resetGraph_()
        {
            bitmap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            pictureBox2.Image = bitmap;

            Brush b = new SolidBrush(Color.White);
            Graphics g = Graphics.FromImage(pictureBox2.Image);
            g.FillRectangle(b, 0, 0, pictureBox2.Image.Width, pictureBox2.Image.Height);

        }

        public void drawGraph_(long[] Data, Color color)
        {
            int maxHeight = (pictureBox2.Height / 3) * 2;
            
            int ep = Time_Index;
            Graphics g = Graphics.FromImage(pictureBox2.Image);
            Pen pen = new Pen(color);

            float Max = -99;
            float Min = 99999;

            int MaxIndex = -1;
            int MinIndex = -1;

            for (int i = 0; i < ep; i++)
            {
                if (Max < (float)Data[i])
                {
                    Max = (float)Data[i];
                    MaxIndex = i;
                }

                if (Min > (float)Data[i])
                {
                    Min = (float)Data[i];
                    MinIndex = i;
                }
            }

            int Image_Depth = pictureBox2.Height / 2;
            int Image_Offset = pictureBox2.Height / 4;

            float Depth = Max - Min;

            for (int i = 0; i < ep - 1; i++)
            {
                float f_Height = (float)pictureBox2.Height;
                int Y = (int)(f_Height - ((float)Image_Offset + (Image_Depth / Depth) * ((float)Data[i] - Min)));
                int Y2 = (int)(f_Height - ((float)Image_Offset + (Image_Depth / Depth) * ((float)Data[i + 1] - Min)));

                Point point1 = new Point(i, Y);
                Point point2 = new Point(i + 1, Y2);

                g.DrawLine(pen, point1, point2);
            }

            pictureBox2.Refresh();
        }

        /// <summary>
        /// ///////////////////////////////////////////////////////////////////////////////////////////////
        /// </summary>

        string filePath = "";
        string folerPath = "";

        public Form1()
        {
            InitializeComponent();

            resetGraph();
            resetGraph_();

            listView1.View = View.Details;
            listView2.View = View.Details;

            ColumnHeader cHeader = new ColumnHeader();   // 헤더 생성
            cHeader.Text = "Index";     // 헤더에 들어갈 텍스트
            cHeader.Width = 30;
            listView2.Columns.Add(cHeader);

            ColumnHeader cHeader2 = new ColumnHeader();   // 헤더 생성
            cHeader2.Text = "Stock Codes";     // 헤더에 들어갈 텍스트
            cHeader2.Width = 50;
            listView2.Columns.Add(cHeader2);

            ColumnHeader cHeader3 = new ColumnHeader();   // 헤더 생성
            cHeader3.Text = "Stock Names";     // 헤더에 들어갈 텍스트
            cHeader3.Width = 150;
            listView2.Columns.Add(cHeader3);

            ColumnHeader cHeader4 = new ColumnHeader();   // 헤더 생성
            cHeader4.Text = "Current Stock Price";     // 헤더에 들어갈 텍스트
            cHeader4.Width = 100;
            listView2.Columns.Add(cHeader4);

            ColumnHeader cHeader5 = new ColumnHeader();   // 헤더 생성
            cHeader5.Text = "Status";     // 헤더에 들어갈 텍스트
            cHeader5.Width = 100;
            listView2.Columns.Add(cHeader5);

            m_fnReceiveHandler = new AsyncCallback(handleDataReceive);
            m_fnSendHandler = new AsyncCallback(handleDataSend);

            timer1.Interval = 1000;
            timer1.Start();

            timer2.Interval = 10000;
            timer3.Interval = 1000;
            
            string sDirPath;
            sDirPath = "C:\\Users\\Administrator\\Documents\\Trademill_backup";
            filePath = sDirPath;
            DirectoryInfo di = new DirectoryInfo(sDirPath);
            if (di.Exists == false)
            {
                di.Create();
            }

            sDirPath = "C:\\Users\\Administrator\\Documents\\Trademill_backup\\PriceData";
            folerPath = sDirPath;
            DirectoryInfo di2 = new DirectoryInfo(sDirPath);
            if (di2.Exists == false)
            {
                di2.Create();
            }

            sDirPath = "C:\\Users\\Administrator\\Documents\\Trademill_backup\\backupdata.txt";
            System.IO.FileInfo fi = new System.IO.FileInfo(sDirPath);
            if (fi.Exists == false)
            {
                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Connected)
            {
                int ret = 0;
                int ret2 = 0;
                String l_accno = null; // 증권계좌번호
                String l_accno_cnt = null; // 소유한 증권계좌번호의 수
                String[] l_accno_arr = null; // N개의 증권계좌번호를 저장할 배열
                ret = axKHOpenAPI1.CommConnect();// 로그인 창 호출

                if (ret == 0)
                {
                    //toolStripStatusLabel1.Text = "로그인 중...";
                    for (;;)
                    {
                        ret2 = axKHOpenAPI1.GetConnectState(); // 로그인 완료 여부를 가져옴
                        if (ret2 == 1) // 로그인이 완료되면
                        {
                            break; // 반복문을 벗어남
                        }
                        else // 그렇지 않으면
                        {
                            //Console.Write("TQTQTQ");
                            delay(1000); // 1초 지연
                                         //System.Threading.Thread.Sleep(500);
                                         //break;
                        }
                    }

                    //toolStripStatusLabel1.Text = "로그인 완료"; // 화면 하단 상태란에 메시지 출력
                    g_user_id = "";
                    g_user_id = axKHOpenAPI1.GetLoginInfo("USER_ID").Trim(); // 사용자 아이디를 가져와서 클래스 변수에 저장

                    l_accno_cnt = "";
                    l_accno_cnt = axKHOpenAPI1.GetLoginInfo("ACCOUNT_CNT").Trim(); // 사용자의 증권계좌번호 수를 가져옴
                    l_accno_arr = new String[int.Parse(l_accno_cnt)];
                    l_accno = "";
                    l_accno = axKHOpenAPI1.GetLoginInfo("ACCNO").Trim(); // 증권계좌번호 가져옴
                    l_accno_arr = l_accno.Split(';');

                    comboBox1.Items.Clear();
                    comboBox1.Items.AddRange(l_accno_arr); // N개의 증권계좌번호를 콤보박스에 저장
                    comboBox1.SelectedIndex = 0; // 첫 번째 계좌번호를 콤포박스 초기 선택으로 설정
                    g_accnt_no = comboBox1.SelectedItem.ToString().Trim();

                }
            }
            else
            {
                MessageBox.Show("네트워크 프로그램 연결을 해야 합니다!", "Connection Error");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            axKHOpenAPI1.CommTerminate();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (g_user_id.CompareTo("") == 0)
            {
                MessageBox.Show("로그인이 필요합니다.");
            }
            else
            {
                if (IsListCall == true)
                {
                    //MessageBox.Show("이미 호출된 상태입니다.");
                    //return;
                }

                //  Dictionary<string, string> favorite_list = listVIew1_select();
                //Console.WriteLine(favorite_list);

                IsListCall = true;

                전체종목리스트_초기체크 = true;
                
                listView2.CheckBoxes = true; // 항목옆에 확인란을 표시할지 여부를 선택합니다.
                                                 //listView2.Click += new EventHandler(ListViewCheckClick);
                                                 //listView2.OwnerDraw = true;

                //GetCodeListByMarket 메소드는 메소드의 인자로 시장 구분 코드를 문자열로 넘겨주면 메소드의 리턴 값으로 해당 시장에 속해 있는 종목들의 종목 코드 목록을 리턴
                //sMarket – 0:장내, 3:ELW, 4:뮤추얼펀드, 5:신주인수권, 6:리츠, 8:ETF, 9:하이일드펀드, 10:코스닥, 30:제3시장
                Console.WriteLine("종목 불러오기 시작");
                string[] marketList = { "0", "10" };
                int InCnt = 0;
                int OutCnt = 0;
                int i = 0;

                int offset_index = 0;

                whole_Number_Of_Available_Stock = 0;

                foreach (string MNumber in marketList)
                {
                    string result = axKHOpenAPI1.GetCodeListByMarket(MNumber);
                    string[] stockList = result.Split(';');

                    whole_Number_Of_Available_Stock += stockList.Length;
                }

                Stock_Codes = new String[whole_Number_Of_Available_Stock];
                Stock_Names = new String[whole_Number_Of_Available_Stock];
                Stock_Prices = new String[whole_Number_Of_Available_Stock];

                foreach (string MNumber in marketList)
                {
                    string result = axKHOpenAPI1.GetCodeListByMarket(MNumber);
                    string[] stockList = result.Split(';');

                    if (MNumber == "0")
                    {
                        InCnt = stockList.Count();
                    }
                    else
                    {
                        OutCnt = stockList.Count();
                    }

                    int listView1_cnt = 0;
                    foreach (string code in stockList)
                    {
                        if (code != "")
                        {
                            string StockName = axKHOpenAPI1.GetMasterCodeName(code);

                            Stock_Codes[listView1_cnt] = code;
                            Stock_Names[listView1_cnt] = StockName;
                            Stock_Prices[listView1_cnt] = axKHOpenAPI1.GetMasterLastPrice(code).ToString();

                            listView1_cnt++;
                        }

                        offset_index++;
                    }
                }

                listView1.Items.Clear();
                listView2.Items.Clear();

                listView1.Refresh();

                listView1.BeginUpdate();

                ColumnHeader cHeader = new ColumnHeader();   // 헤더 생성
                cHeader.Text = "Index";     // 헤더에 들어갈 텍스트
                cHeader.Width = 30;
                listView1.Columns.Add(cHeader);
                
                ColumnHeader cHeader2 = new ColumnHeader();   // 헤더 생성
                cHeader2.Text = "Stock Codes";     // 헤더에 들어갈 텍스트
                cHeader2.Width = 50;
                listView1.Columns.Add(cHeader2);

                ColumnHeader cHeader3 = new ColumnHeader();   // 헤더 생성
                cHeader3.Text = "Stock Names";     // 헤더에 들어갈 텍스트
                cHeader3.Width = 150;
                listView1.Columns.Add(cHeader3);

                ColumnHeader cHeader4 = new ColumnHeader();   // 헤더 생성
                cHeader4.Text = "Stock Master Price";     // 헤더에 들어갈 텍스트
                cHeader4.Width = 100;
                listView1.Columns.Add(cHeader4);

                for (int a = 0; a < whole_Number_Of_Available_Stock; a++)
                {
                    //전체종목리스트 ListView row 만들자
                    if(Stock_Names[a] != null)
                    {
                        if (Stock_Names[a].CompareTo("") != 0)
                        {
                            string[] row = { a.ToString(), Stock_Codes[a], Stock_Names[a], Stock_Prices[a] };
                            var listViewItem = new ListViewItem(row);

                            listView1.Items.Add(listViewItem);
                        }
                    }
                }

                listView1.EndUpdate();

                전체종목리스트_초기체크 = false;
                
            }

            //이전 설정 및 종목 데이터 불러오기
            System.IO.FileInfo fi = new System.IO.FileInfo(filePath + "\\list");
            if (fi.Exists)
            {
                System.IO.StreamReader fs = new StreamReader(filePath + "\\list");

                String str = fs.ReadLine();
                int Count = int.Parse(str);

                string stockName = "";

                for (int i = 0; i < Count; i++)
                {
                    string Code = fs.ReadLine();

                    for(int k = 0; k < listView1.Items.Count; k++)
                    {
                        if(listView1.Items[k].SubItems[1].Text.CompareTo(Code) == 0)
                        {
                            stockName = listView1.Items[k].SubItems[2].Text;
                        }
                    }

                    int numberOf_Fav_List = listView2.Items.Count;

                    String[] Favorite_Stock_List_Codes_ = new String[numberOf_Fav_List + 1];
                    String[] Favorite_Stock_List_Names_ = new String[numberOf_Fav_List + 1];

                    bool is_Exist = false;
                    
                    if (!is_Exist)
                    {
                        for (int a = 0; a < numberOf_Fav_List; a++)
                        {
                            Favorite_Stock_List_Names_[a] = Favorite_Stock_List_Names[a];
                            Favorite_Stock_List_Codes_[a] = Favorite_Stock_List_Codes[a];
                        }

                        Favorite_Stock_List_Codes_[numberOf_Fav_List] = Code;
                        Favorite_Stock_List_Names_[numberOf_Fav_List] = stockName;

                        string[] row = { numberOf_Fav_List.ToString(), Code, stockName, "", "" };
                        var listViewItem = new ListViewItem(row);

                        listView2.Items.Add(listViewItem);

                        Favorite_Stock_List_Codes = Favorite_Stock_List_Codes_;
                        Favorite_Stock_List_Names = Favorite_Stock_List_Names_;
                        Tr_In[numberOf_Fav_List] = new Trade_Info(Code);
                        Cursor_Index = numberOf_Fav_List;
                        textBox1.Text = Code;
                    }
                }

                for (int i = 0; i < Count; i++)
                {
                    string mypath_minute = folerPath + "\\" + Favorite_Stock_List_Codes[i] + "_" + "minute";
                    System.IO.StreamReader fr = new StreamReader(mypath_minute);

                    string mypath_minute_am = folerPath + "\\" + Favorite_Stock_List_Codes[i] + "_" + "minute_am";
                    System.IO.StreamReader fr_am = new StreamReader(mypath_minute_am);

                    string data = fr.ReadToEnd().Split('\r')[0];
                    string[] TK = data.Split('@');

                    string data_am = fr.ReadToEnd().Split('\r')[0];
                    string[] TK_am = data.Split('@');

                    if (data.CompareTo("null") == 0)
                    {

                    }
                    else
                    {
                        long[] pricedata = new long[TK.Length - 1];
                        long[] pricedata_am = new long[TK.Length - 1];

                        for (int a = 0; a < pricedata.Length; a++)
                        {
                            pricedata[a] = long.Parse(TK[a]);
                            pricedata_am[a] = long.Parse(TK[a]);
                        }

                        Tr_In[i].UpdateData(pricedata, pricedata_am);
                        Tr_In[i].MinuteData = true;
                    }
                    fr.Close();
                    fr_am.Close();
                    
                    string mypath_day = folerPath + "\\" + Favorite_Stock_List_Codes[i] + "_" + "day";
                    fr = new StreamReader(mypath_day);

                    string mypath_day_am = folerPath + "\\" + Favorite_Stock_List_Codes[i] + "_" + "day_am";
                    fr_am = new StreamReader(mypath_day_am);

                    data = fr.ReadToEnd().Split('\r')[0];
                    TK = data.Split('@');

                    data_am = fr.ReadToEnd().Split('\r')[0];
                    TK_am = data.Split('@');

                    if (data.CompareTo("null") == 0)
                    {

                    }
                    else
                    {
                        long[] pricedata = new long[TK.Length - 1];
                        long[] pricedata_am = new long[TK.Length - 1];

                        for (int a = 0; a < pricedata.Length; a++)
                        {
                            pricedata[a] = long.Parse(TK[a]);
                            pricedata_am[a] = long.Parse(TK[a]);
                        }

                        Tr_In[i].UpdateData(pricedata, pricedata_am, true);
                        Tr_In[i].DayData = true;
                    }
                    fr.Close();
                    fr_am.Close();

                    string mypath_week = folerPath + "\\" + Favorite_Stock_List_Codes[i] + "_" + "week";
                    fr = new StreamReader(mypath_week);

                    string mypath_week_am = folerPath + "\\" + Favorite_Stock_List_Codes[i] + "_" + "week_am";
                    fr_am = new StreamReader(mypath_week_am);

                    data = fr.ReadToEnd().Split('\r')[0];
                    TK = data.Split('@');

                    data_am = fr.ReadToEnd().Split('\r')[0];
                    TK_am = data.Split('@');

                    if (data.CompareTo("null") == 0)
                    {

                    }
                    else
                    {
                        long[] pricedata = new long[TK.Length - 1];
                        long[] pricedata_am = new long[TK.Length - 1];

                        for (int a = 0; a < pricedata.Length; a++)
                        {
                            pricedata[a] = long.Parse(TK[a]);
                            pricedata_am[a] = long.Parse(TK[a]);
                        }

                        Tr_In[i].UpdateData(pricedata, pricedata_am, false);
                        Tr_In[i].WeekData = true;
                    }

                }
                
                timer5.Interval = 500;
                if (Count > 0)
                {
                    timer5.Start();
                }
                else
                {
                    is_buffer_Done = true;
                }
            }

            if(listView2.Items.Count > 0)
            {
                textBox1.Text = listView2.Items[0].SubItems[1].Text;
            }
            is_Load = true;

        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                string code = textBox1.Text;

                string StockName = axKHOpenAPI1.GetMasterCodeName(code);
                string prices = axKHOpenAPI1.GetMasterLastPrice(code).ToString();

                int numberOf_Fav_List = listView2.Items.Count;

                String[] Favorite_Stock_List_Codes_ = new String[numberOf_Fav_List + 1];
                String[] Favorite_Stock_List_Names_ = new String[numberOf_Fav_List + 1];

                bool is_Exist = false;

                for (int i = 0; i < numberOf_Fav_List; i++)
                {
                    if (Favorite_Stock_List_Codes[i].CompareTo(textBox1.Text) == 0)
                    {
                        is_Exist = true;
                    }
                }

                if (!is_Exist)
                {
                    for (int i = 0; i < numberOf_Fav_List; i++)
                    {
                        Favorite_Stock_List_Names_[i] = Favorite_Stock_List_Names[i];
                        Favorite_Stock_List_Codes_[i] = Favorite_Stock_List_Codes[i];
                    }

                    Favorite_Stock_List_Codes_[numberOf_Fav_List] = code;
                    Favorite_Stock_List_Names_[numberOf_Fav_List] = StockName;

                    string[] row = { numberOf_Fav_List.ToString(), code, StockName, "", "" };
                    var listViewItem = new ListViewItem(row);

                    listView2.Items.Add(listViewItem);

                    Favorite_Stock_List_Codes = Favorite_Stock_List_Codes_;
                    Favorite_Stock_List_Names = Favorite_Stock_List_Names_;
                    Tr_In[numberOf_Fav_List] = new Trade_Info(code);
                    Cursor_Index = numberOf_Fav_List;
                    Reg_Index = Cursor_Index;
                    Register_Tr_In_();

                    timer3.Stop();
                    System.Threading.Thread.Sleep(250);

                    Register_Expectation_Value(true);
                    System.Threading.Thread.Sleep(250);
                    Register_Expectation_Value(false);
                    System.Threading.Thread.Sleep(250);
                    timer3.Start();
                }
            }
            catch
            {

            }
            
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = listView1.SelectedItems[0].SubItems[1].Text;
            }
            catch
            {

            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Count = 0;

            log(comboBox2.Text + "를 받아오겠습니다. 잠시만 기다려주세요.");

            int numberOf_Fav_List = listView2.Items.Count;
            bool is_Exist = false;
            Cursor_Index = -1;

            for (int i = 0; i < numberOf_Fav_List; i++)
            {
                if (Favorite_Stock_List_Codes[i].CompareTo(textBox1.Text) == 0)
                {
                    is_Exist = true;
                    Cursor_Index = i;
                }
            }
            
            if (is_Exist)
            {
                if (comboBox2.Text.CompareTo("분봉") == 0)
                {
                    axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                    axKHOpenAPI1.CommRqData("분봉", "OPT10080", 0, "1002");
                    
                    Tr_In[Cursor_Index].MinuteData = true;
                    Tr_In[Cursor_Index].reset_Data();
                }
                else if (comboBox2.Text.CompareTo("일봉") == 0)
                {
                    axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                    axKHOpenAPI1.CommRqData("일봉", "OPT10081", 0, "1002");

                    Tr_In[Cursor_Index].DayData = true;
                    Tr_In[Cursor_Index].reset_Data(true);
                }
                else if (comboBox2.Text.CompareTo("주봉") == 0)
                {
                    axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                    axKHOpenAPI1.CommRqData("주봉", "OPT10082", 0, "1002");

                    Tr_In[Cursor_Index].WeekData = true;
                    Tr_In[Cursor_Index].reset_Data(false);
                }
            }
            
        }

        void Recieve()
        {
            if (comboBox2.Text.CompareTo("분봉") == 0)
            {
                axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                axKHOpenAPI1.CommRqData("분봉", "OPT10080", 2, "1002");

                if (Prev_Cnt == 0)
                {
                    Prev_Cnt += 2;
                }
            }
            else if (comboBox2.Text.CompareTo("일봉") == 0)
            {
                axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                axKHOpenAPI1.CommRqData("일봉", "OPT10081", 2, "1002");

                if (Prev_Cnt == 0)
                {
                    //Prev_Cnt += 2;
                }
            }
            else if (comboBox2.Text.CompareTo("주봉") == 0)
            {
                axKHOpenAPI1.SetInputValue("종목코드", textBox1.Text);
                axKHOpenAPI1.CommRqData("주봉", "OPT10082", 2, "1002");

                if (Prev_Cnt == 0)
                {
                    Prev_Cnt += 2;
                }
            }

            Count++;
        }

        private void axKHOpenAPI1_OnReceiveTrData(object sender, AxKHOpenAPILib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            int a = 0;
            int Length = 0;

            try
            {
                a = int.Parse(textBox2.Text);
            }
            catch
            {

            }
            
            bool isDone = false;
            long[] Prices_tmp = new long[900];
            long[] Amouns_temp = new long[900];

            if(e.sRQName.CompareTo("현재가받기") == 0)
            {
                Tr_In[Loop_Index].Real_Time_Prices[Time_Index - 1] = Math.Abs(long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "시가")));
                Tr_In[Loop_Index].Real_Time_Amounts[Time_Index - 1] = Math.Abs(long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, 0, "거래량")));
            }
            else if (e.sRQName.CompareTo("분봉버퍼") == 0)
            {
                if(is_Load == true)
                {
                    for (int i = 0 + a; i < Time_Index; i++)
                    {

                        if (axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가").CompareTo("") == 0 ||
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").CompareTo("") == 0)
                        {
                            //log("Done!!");
                            Console.WriteLine("Done!!");
                            isDone = true;

                            Length = i - 1;
                            i = 900;
                        }
                        else
                        {
                            Console.WriteLine(i + ":" + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가") + " " +
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Prices_tmp[i] = Math.Abs(long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가")));
                            Amouns_temp[i] = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Length = i;
                        }

                    }

                    if(Length > 0)
                    {
                        for (int i = 0; i < Time_Index; i++)
                        {
                            Tr_In[Cursor_Index].Real_Time_Prices[i] = Prices_tmp[Length];
                            Tr_In[Cursor_Index].Real_Time_Amounts[i] = Amouns_temp[Length];

                            Length--;
                        }
                    }
                }
            }
            else if (e.sRQName.CompareTo("분봉버퍼_") == 0)
            {
                if (is_Load == true)
                {
                    for (int i = 0 + a; i < Time_Index; i++)
                    {

                        if (axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가").CompareTo("") == 0 ||
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").CompareTo("") == 0)
                        {
                            //log("Done!!");
                            Console.WriteLine("Done!!");
                            isDone = true;

                            Length = i - 1;
                            i = 900;
                        }
                        else
                        {
                            Console.WriteLine(i + ":" + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가") + " " +
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Prices_tmp[i] = Math.Abs(long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가")));
                            Amouns_temp[i] = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Length = i;
                        }

                    }

                    if (Length > 0)
                    {
                        for (int i = 0; i < Time_Index; i++)
                        {
                            Tr_In[Reg_Index].Real_Time_Prices[i] = Prices_tmp[Length];
                            Tr_In[Reg_Index].Real_Time_Amounts[i] = Amouns_temp[Length];

                            Length--;
                        }
                    }

                    if (is_buffer_Done)
                    {
                        System.Threading.Thread.Sleep(50);
                        Register_Tr_In_(true);
                    }
                }
            }
            else if (e.sRQName.CompareTo("일봉버퍼_") == 0)
            {
                if (is_Load == true)
                {
                    for (int i = 0 + a; i < 100; i++)
                    {

                        if (axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가").CompareTo("") == 0 ||
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").CompareTo("") == 0)
                        {
                            //log("Done!!");
                            Console.WriteLine("Done!!");
                            isDone = true;

                            Length = i - 1;
                            i = 900;
                        }
                        else
                        {
                            Console.WriteLine(i + ":" + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가") + " " +
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Prices_tmp[i] = Math.Abs(long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가")));
                            Amouns_temp[i] = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Length = i;
                        }

                    }

                    for (int i = 100; i >= 0; i--)
                    {
                        Tr_In[Reg_Index].Real_Time_Prices_day[i] = Prices_tmp[i];
                        Tr_In[Reg_Index].Real_Time_Amounts_day[i] = Amouns_temp[i];

                        Length--;
                    }

                    if (is_buffer_Done)
                    {
                        System.Threading.Thread.Sleep(50);
                        Register_Tr_In_(false);
                    }
                }
            }
            else if (e.sRQName.CompareTo("주봉버퍼_") == 0)
            {
                if (is_Load == true)
                {
                    for (int i = 100; i >= 0; i--)
                    {

                        if (axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가").CompareTo("") == 0 ||
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").CompareTo("") == 0)
                        {
                            //log("Done!!");
                            Console.WriteLine("Done!!");
                            isDone = true;

                            Length = i - 1;
                            i = 900;
                        }
                        else
                        {
                            Console.WriteLine(i + ":" + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가") + " " +
                            axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Prices_tmp[i] = Math.Abs(long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가")));
                            Amouns_temp[i] = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                            Length = i;
                        }

                    }

                    for (int i = 100; i >= 0; i--)
                    {
                        Tr_In[Reg_Index].Real_Time_Prices_week[i] = Prices_tmp[i];
                        Tr_In[Reg_Index].Real_Time_Amounts_week[i] = Amouns_temp[i];

                        Length--;
                    }
                }
            }
            else
            {
                for (int i = 0 + a; i < 900; i++)
                {

                    if (axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가").CompareTo("") == 0 ||
                        axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량").CompareTo("") == 0)
                    {
                        //log("Done!!");
                        Console.WriteLine("Done!!");
                        isDone = true;

                        Length = i - 1;
                        i = 900;
                    }
                    else
                    {
                        Console.WriteLine(i + ":" + axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가") + " " +
                        axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));

                        Prices_tmp[i] = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "시가"));
                        Amouns_temp[i] = long.Parse(axKHOpenAPI1.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));
                    }

                }

                if (!isDone && Count < 15)
                {
                    if (Cursor_Index != -1)
                    {
                        if (comboBox2.Text.CompareTo("분봉") == 0)
                        {
                            Tr_In[Cursor_Index].UpdateData(Prices_tmp, Amouns_temp);
                        }
                        else if (comboBox2.Text.CompareTo("일봉") == 0)
                        {
                            Tr_In[Cursor_Index].UpdateData(Prices_tmp, Amouns_temp, true);
                        }
                        if (comboBox2.Text.CompareTo("주봉") == 0)
                        {
                            Tr_In[Cursor_Index].UpdateData(Prices_tmp, Amouns_temp, false);
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                    Recieve();
                }
                else
                {
                    if (Cursor_Index != -1 && Length > 0)
                    {
                        long[] temp_pris = new long[Length];
                        long[] temp_amounts = new long[Length];

                        for (int i = 0; i < Length; i++)
                        {
                            temp_pris[i] = Prices_tmp[i];
                            temp_amounts[i] = Amouns_temp[i];
                        }

                        if (comboBox2.Text.CompareTo("분봉") == 0)
                        {
                            Tr_In[Cursor_Index].UpdateData(temp_pris, temp_amounts);
                        }
                        else if (comboBox2.Text.CompareTo("일봉") == 0)
                        {
                            Tr_In[Cursor_Index].UpdateData(temp_pris, temp_amounts, true);
                        }
                        else if (comboBox2.Text.CompareTo("주봉") == 0)
                        {
                            Tr_In[Cursor_Index].UpdateData(temp_pris, temp_amounts, false);
                        }
                    }

                    Console.WriteLine("");
                    log("완료되었습니다");
                }

                Console.WriteLine(Count);
            }
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (ConnectToServer("192.168.0.8", 15231))
            {
                
                Clients_Data_Recieve_Thread = new Thread(Recieve_Clients_Data);
                Clients_Data_Recieve_Thread.Start();

                Connected = true;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SendMessage("Test");
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count == 0)
            {
                Cursor_ID = "";
            }

            //graph update
            resetGraph();

            int index = -1;

            try
            {
                index = int.Parse(listView2.SelectedItems[0].Text);
                textBox1.Text = listView2.SelectedItems[0].SubItems[1].Text;
            }
            catch
            {

            }

            if (index >= 0)
            {
                Cursor_ID = listView2.SelectedItems[0].SubItems[1].Text;

                if (Tr_In[index].MinuteData && checkBox1.Checked)
                {
                    drawGraph(Tr_In[index].getDataArray(0), Color.Black);
                }

                if (Tr_In[index].DayData && checkBox2.Checked)
                {
                    drawGraph(Tr_In[index].getDataArray(1), Color.Blue);
                }

                if (Tr_In[index].WeekData && checkBox3.Checked)
                {
                    drawGraph(Tr_In[index].getDataArray(2), Color.Green);
                }
                
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string[] TK = DateTime.Now.ToString().Split(' ');
            int Time_ind = 0;
            
            if(TK[1].CompareTo("오후") == 0)
            {
                TK = TK[2].Split(':');

                int Hour = int.Parse(TK[0]);
                int Minute = int.Parse(TK[1]);

                if(Hour == 12)
                {
                    Hour = 0;
                }

                Hour += 3;

                Time_ind = Hour * 60 + Minute;

                label3.Text = DateTime.Now.ToString();
            }
            else
            {
                TK = TK[2].Split(':');

                int Hour = int.Parse(TK[0]);
                int Minute = int.Parse(TK[1]);

                if(Hour == 0)
                {
                    //
                }

                Hour -= -9;

                Time_ind = Hour * 60 + Minute;

                label3.Text = DateTime.Now.ToString();
            }

            if (Time_ind > 360 || Time_ind < 0)
            {
                label3.Text += " 장 마감시간 입니다.";

                Time_Index = 360;
            }
            else
            {
                Time_Index = Time_ind;
                label3.Text += " , " + Time_ind + "/ 360";
            }
             
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!checkBox4.Checked)
            {
                if (listView2.Items.Count > 0)
                {
                    drawGraph_(Tr_In[Cursor_Index].Real_Time_Prices, Color.Black);
                    drawGraph_(Tr_In[Cursor_Index].Real_Time_Amounts, Color.Red);
                }
            }
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (!checkBox4.Checked)
            {
                if (listView2.Items.Count > 0)
                {
                    Loop_Index++;
                    if (Loop_Index == listView2.Items.Count)
                    {
                        Loop_Index = 0;
                    }

                    get_Current_Price(Loop_Index);
                    if(listView2.Items[Loop_Index].SubItems[4].Text.CompareTo("Trained:N") != 0)
                    {
                        Register_Expectation_Value();
                    }
                    
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            int index = 0;

            if (comboBox2.Text.CompareTo("분봉") == 0)
            {
                index = 0;
            }
            else if (comboBox2.Text.CompareTo("일봉") == 0)
            {
                index = 1;
            }
            else if (comboBox2.Text.CompareTo("주봉") == 0)
            {
                index = 2;
            }

            string Msg = "";

            if (index == 0)
            {
                Msg = Tr_In[Cursor_Index].get_Data_Prices();
            }
            else if (index == 1)
            {
                Msg = Tr_In[Cursor_Index].get_Data_Prices(true);
            }
            else if (index == 2)
            {
                Msg = Tr_In[Cursor_Index].get_Data_Prices(false);
            }

            SendMessage(Msg);

            string Msg_Command = "@Train";
            int epoch = 1000;

            try
            {
                epoch = int.Parse(textBox3.Text);
            }
            catch
            {

            }

            string[] dic = { "min", "day", "week" };

            Msg_Command += "&" + textBox1.Text + "&" + dic[comboBox2.SelectedIndex] + "&" + epoch;
            SendMessage("");

            Thread.Sleep(500);
            SendMessage(Msg_Command);
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            if (!checkBox4.Checked)
            {
                if (listView2.Items.Count > 0)
                {
                    Exp_Loop_Index++;

                    if (Exp_Loop_Index == listView2.Items.Count + 1)
                    {
                        Exp_Loop_Index = 0;
                    }

                    if (listView2.Items[Exp_Loop_Index].SubItems[4].Text.CompareTo("Trained:N") != 0)
                    {
                        Register_Expectation_Value();
                    }

                }
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fw_ = System.IO.File.CreateText(filePath + "\\list");

            fw_.WriteLine(listView2.Items.Count + "");
            for(int i = 0; i < listView2.Items.Count; i++)
            {
                fw_.WriteLine(Favorite_Stock_List_Codes[i] + "");
            }
            fw_.Close();

            for (int i = 0; i < listView2.Items.Count; i++)
            {
                string mypath = folerPath + "\\" + Favorite_Stock_List_Codes[i];

                System.IO.StreamWriter fw = System.IO.File.CreateText(mypath + "_minute");
                System.IO.StreamWriter fw_am = System.IO.File.CreateText(mypath + "_minute_am");

                if (Tr_In[i].Trade_Amounts.Length < 10)
                {
                    fw.WriteLine("null");
                    fw_am.WriteLine("null");
                }
                else
                {
                    fw.WriteLine(Tr_In[i].get_Data_Prices());
                    fw.WriteLine(Tr_In[i].get_Data_Amounts());
                }
                fw.Close();
                fw_am.Close();


                System.IO.StreamWriter fw2 = System.IO.File.CreateText(mypath + "_day");
                System.IO.StreamWriter fw2_am = System.IO.File.CreateText(mypath + "_day_am");

                if (Tr_In[i].Trade_Amounts_Day.Length < 10)
                {
                    fw2.WriteLine("null");
                    fw2_am.WriteLine("null");
                }
                else
                {
                    fw2.WriteLine(Tr_In[i].get_Data_Prices(true));
                    fw2_am.WriteLine(Tr_In[i].get_Data_Amounts(true));
                }
                fw2.Close();
                fw2_am.Close();


                System.IO.StreamWriter fw3 = System.IO.File.CreateText(mypath + "_week");
                System.IO.StreamWriter fw3_am = System.IO.File.CreateText(mypath + "_week_am");

                if (Tr_In[i].Trade_Amounts_Week.Length < 10)
                {
                    fw3.WriteLine("null");
                    fw3_am.WriteLine("null");
                }
                else
                {
                    fw3.WriteLine(Tr_In[i].get_Data_Prices(false));
                    fw3_am.WriteLine(Tr_In[i].get_Data_Amounts(false));
                }
                fw3.Close();
                fw3_am.Close();
                
            }
        }

        private void timer5_Tick(object sender, EventArgs e)
        {
            bool isEnd = false;

            Type_Index++;

            if (Type_Index == 3)
            {
                Type_Index = 0;
                Reg_Index++;
            }
            if (listView2.Items.Count == Reg_Index)
            {
                isEnd = true;
            }

            if (!isEnd)
            {
                if (isBuffer)
                {
                    if (Type_Index == 0)
                    {
                        textBox1.Text = Favorite_Stock_List_Codes[Reg_Index];
                        Register_Tr_In_();
                    }
                    else if (Type_Index == 1)
                    {
                        textBox1.Text = Favorite_Stock_List_Codes[Reg_Index];
                        Register_Tr_In_(true);
                    }
                    else
                    {
                        textBox1.Text = Favorite_Stock_List_Codes[Reg_Index];
                        Register_Tr_In_(false);
                    }
                    Thread.Sleep(250);
                }
                else
                {
                    Exp_Loop_Index = Reg_Index;
                    Exp_Type_Index = Type_Index;

                    if (Type_Index == 0)
                    {
                        textBox1.Text = Favorite_Stock_List_Codes[Reg_Index];
                        Register_Expectation_Value();
                    }
                    else if (Type_Index == 1)
                    {
                        textBox1.Text = Favorite_Stock_List_Codes[Reg_Index];
                        Register_Expectation_Value(true);
                    }
                    else
                    {
                        textBox1.Text = Favorite_Stock_List_Codes[Reg_Index];
                        Register_Expectation_Value(false);
                    }
                    Thread.Sleep(250);
                }

                
                
            }
            else
            {
                if (isBuffer)
                {
                    isBuffer = false;
                    Type_Index = -1;
                    Reg_Index = 0;
                }
                else
                {
                    is_buffer_Done = true;
                    timer5.Stop();

                    timer2.Start();
                    timer3.Start();

                    Exp_Loop_Index = 0;
                    Exp_Type_Index = 0;
                }
                
            }
            
        }

        private void button10_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fw_ = System.IO.File.CreateText(filePath + "\\list");

            fw_.WriteLine(0 + "");
            fw_.Close();

            DirectoryInfo dir = new DirectoryInfo(folerPath);

            System.IO.FileInfo[] files = dir.GetFiles("*.*",
            SearchOption.AllDirectories);

            foreach (System.IO.FileInfo file in files)
                file.Attributes = FileAttributes.Normal;

            Directory.Delete(folerPath, true);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fw_ = System.IO.File.CreateText(filePath + "\\list");

            fw_.WriteLine(listView2.Items.Count + "");
            for (int i = 0; i < listView2.Items.Count; i++)
            {
                fw_.WriteLine(Favorite_Stock_List_Codes[i] + "");
            }
            fw_.Close();

            for (int i = 0; i < listView2.Items.Count; i++)
            {
                string mypath = folerPath + "\\" + Favorite_Stock_List_Codes[i];

                System.IO.StreamWriter fw = System.IO.File.CreateText(mypath + "_minute");
                System.IO.StreamWriter fw_am = System.IO.File.CreateText(mypath + "_minute_am");

                if (Tr_In[i].Trade_Amounts.Length < 10)
                {
                    fw.WriteLine("null");
                    fw_am.WriteLine("null");
                }
                else
                {
                    fw.WriteLine(Tr_In[i].get_Data_Prices());
                    fw.WriteLine(Tr_In[i].get_Data_Amounts());
                }
                fw.Close();
                fw_am.Close();


                System.IO.StreamWriter fw2 = System.IO.File.CreateText(mypath + "_day");
                System.IO.StreamWriter fw2_am = System.IO.File.CreateText(mypath + "_day_am");

                if (Tr_In[i].Trade_Amounts_Day.Length < 10)
                {
                    fw2.WriteLine("null");
                    fw2_am.WriteLine("null");
                }
                else
                {
                    fw2.WriteLine(Tr_In[i].get_Data_Prices(true));
                    fw2_am.WriteLine(Tr_In[i].get_Data_Amounts(true));
                }
                fw2.Close();
                fw2_am.Close();


                System.IO.StreamWriter fw3 = System.IO.File.CreateText(mypath + "_week");
                System.IO.StreamWriter fw3_am = System.IO.File.CreateText(mypath + "_week_am");

                if (Tr_In[i].Trade_Amounts_Week.Length < 10)
                {
                    fw3.WriteLine("null");
                    fw3_am.WriteLine("null");
                }
                else
                {
                    fw3.WriteLine(Tr_In[i].get_Data_Prices(false));
                    fw3_am.WriteLine(Tr_In[i].get_Data_Amounts(false));
                }
                fw3.Close();
                fw3_am.Close();

            }
        }

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Setting_Form setting_form = new Setting_Form();
            setting_form.Show();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fw_ = System.IO.File.CreateText(filePath + "\\list");

            fw_.WriteLine(0 + "");
            fw_.Close();

            DirectoryInfo dir = new DirectoryInfo(folerPath);

            System.IO.FileInfo[] files = dir.GetFiles("*.*",
            SearchOption.AllDirectories);

            foreach (System.IO.FileInfo file in files)
                file.Attributes = FileAttributes.Normal;

            Directory.Delete(folerPath, true);
        }
        
        private void timer6_Tick(object sender, EventArgs e)
        {
            //매도 결정 알고리즘이 동작하는 Timer
            for (int i = 0; i < listView3.Items.Count; i++) {
                bool isCell = Decision_Sell(i);

                if (isCell)
                {
                    //매도결정
                    create_order_chcek_box(true);
                }
            }
            
        }
        
        private void timer7_Tick(object sender, EventArgs e)
        {
            for(int i = 0; i < listView2.Items.Count; i++)
            {
                bool Buy_Decision = Decision_Buy(i);

                if (Buy_Decision)
                {
                    //매수 결정
                    create_order_chcek_box(false);
                }
            }
        }
    }
    
}
