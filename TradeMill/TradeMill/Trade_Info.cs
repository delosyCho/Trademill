using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeMill
{
    public class Trade_Info
    {
        static int Sequence_Length = 20;

        public bool MinuteData = false;
        public bool DayData = false;
        public bool WeekData = false;

        public bool isTrained = false;
        public bool isTrained_day = false;
        public bool isTrained_week = false;

        public long[] realtime_Price = new long[360];

        String Tr_ID = "";

        public long[] Real_Time_Prices = new long[360 + Sequence_Length];
        public long[] Real_Time_Amounts = new long[360 + Sequence_Length];

        public long[] Real_Time_Prices_day = new long[100 + Sequence_Length];
        public long[] Real_Time_Amounts_day = new long[100 + Sequence_Length];

        public long[] Real_Time_Prices_week = new long[100 + Sequence_Length];
        public long[] Real_Time_Amounts_week = new long[100 + Sequence_Length];

        long[] Prices = new long[0];
        long[] Prices_Day = new long[0];
        long[] Prices_Week = new long[0];

        public long[] Trade_Amounts = new long[0];
        public long[] Trade_Amounts_Day = new long[0];
        public long[] Trade_Amounts_Week = new long[0];

        long[] temp_Pr = new long[0];
        long[] temp_Am = new long[0];

        public void Exp_Data(string[] TK, int time_index)
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    Real_Time_Prices[time_index + i] = long.Parse(TK[i]);
                }
            }
            catch
            {

            }
        }

        public void Exp_Data(string[] TK, bool isDay)
        {
            try
            {
                for(int i = 0; i < 20; i++)
                {
                    if (isDay)
                    {
                        Real_Time_Prices_day[i + 100] = long.Parse(TK[i]);
                    }
                    else
                    {
                        Real_Time_Prices_week[i + 100] = long.Parse(TK[i]);
                    }
                }
            }
            catch
            {

            }
        }

        public void reset_Data()
        {
            Prices = new long[0];
            Trade_Amounts = new long[0];
        }

        public void reset_Data(bool isDay)
        {
            if (isDay)
            {
                Prices_Day = new long[0];
                Trade_Amounts_Day = new long[0];
            }
            else
            {
                Prices_Week = new long[0];
                Trade_Amounts_Week = new long[0];
            }
        }

        public Trade_Info(String tr_id)
        {
            Tr_ID = tr_id;
        }

        public long[] getDataArray(int index)
        {
            if(index == 0)
            {
                return Prices;
            }
            else if (index == 1)
            {
                return Prices_Day;
            }
            else if (index == 2)
            {
                return Prices_Week;
            }
            else
            {
                return null;
            }
        }

        public void update_realtime_price(long price, int time_index)
        {
            realtime_Price[time_index] = price;
        }

        public string get_Data_Prices()
        {
            string result = "";

            int Length = 0;

            Length = Prices.Length;

            for (int i = 0; i < Length; i++)
            {
                result += Prices[i] + "@";
            }

            return result;
        }

        public string get_Data_Amounts()
        {
            string result = "";

            int Length = 0;

            Length = Trade_Amounts.Length;

            for (int i = 0; i < Length; i++)
            {
                result += Trade_Amounts[i] + "@";
            }

            return result;
        }

        public string get_Data_Prices(bool isDay)
        {
            string result = "";

            int Length = 0;

            if (isDay)
            {
                Length = Prices_Day.Length;
            }
            else
            {
                Length = Prices_Week.Length;
            }

            for(int i = Length - 1; i >= 0; i--)
            {
                if (isDay)
                {
                    result += Prices_Day[i] + "@";
                }
                else
                {
                    result += Prices_Week[i] + "@";
                }
            }

            return result;
        }

        public string get_Data_Amounts(bool isDay)
        {
            string result = "";

            int Length = 0;

            if (isDay)
            {
                Length = Trade_Amounts_Day.Length;
            }
            else
            {
                Length = Trade_Amounts_Week.Length;
            }

            for (int i = Length - 1; i >= 0; i--)
            {
                if (isDay)
                {
                    result += Trade_Amounts_Day[i] + "@";
                }
                else
                {
                    result += Trade_Amounts_Week[i] + "@";
                }
            }

            return result;
        }

        public void UpdateData(long[] pris, long[] amounts)
        {
            temp_Pr = Prices;
            temp_Am = Trade_Amounts;

            Prices = new long[temp_Pr.Length + pris.Length];
            Trade_Amounts = new long[temp_Am.Length + amounts.Length];

            for (int i = 0; i < temp_Am.Length; i++)
            {
                Prices[i] = temp_Pr[i];
                Trade_Amounts[i] = temp_Am[i];
            }

            for (int i = temp_Am.Length; i < temp_Am.Length + pris.Length; i++)
            {
                Prices[i] = Math.Abs(pris[i - temp_Am.Length]);
                Trade_Amounts[i] = amounts[i - temp_Am.Length];
            }

        }

        public void UpdateData(long[] pris, long[] amounts, bool isDay)
        {

            if (isDay)
            {
                temp_Pr = Prices_Day;
                temp_Am = Trade_Amounts_Day;

                Prices_Day = new long[temp_Pr.Length + pris.Length];
                Trade_Amounts_Day = new long[temp_Am.Length + amounts.Length];

                for (int i = 0; i < temp_Am.Length; i++)
                {
                    Prices_Day[i] = temp_Pr[i];
                    Trade_Amounts_Day[i] = temp_Am[i];
                }

                for (int i = temp_Am.Length; i < temp_Am.Length + pris.Length; i++)
                {
                    Prices_Day[i] = Math.Abs(pris[i - temp_Am.Length]);
                    Trade_Amounts_Day[i] = amounts[i - temp_Am.Length];
                }
            }
            else
            {
                temp_Pr = Prices_Week;
                temp_Am = Trade_Amounts_Week;

                Prices_Week = new long[temp_Pr.Length + pris.Length];
                Trade_Amounts_Week = new long[temp_Am.Length + amounts.Length];

                for (int i = 0; i < temp_Am.Length; i++)
                {
                    Prices_Week[i] = temp_Pr[i];
                    Trade_Amounts_Week[i] = temp_Am[i];
                }

                for (int i = temp_Am.Length; i < temp_Am.Length + pris.Length; i++)
                {
                    Prices_Week[i] = Math.Abs(pris[i - temp_Am.Length]);
                    Trade_Amounts_Week[i] = amounts[i - temp_Am.Length];
                }
            }

        }

    }
}
