using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradeMill
{
    class Order_Info
    {
        public long ordered_Price;
        public long ordered_Amount;

        public int Cursor_Index = 0;

        public Order_Info(int ci, long pr, long am)
        {
            Cursor_Index = ci;
            ordered_Price = pr;
            ordered_Amount = am;
        }
    }
}
