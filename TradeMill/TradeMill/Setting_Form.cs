using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TradeMill
{
    public partial class Setting_Form : Form
    {
        public Setting_Form()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filePath = "C:\\Users\\Administrator\\Documents\\Trademill_backup";
            System.IO.StreamWriter fw_ = System.IO.File.CreateText(filePath + "\\trademill_settings");

            if (radioButton1.Checked)
            {
                fw_.WriteLine("0");
            }
            else
            {
                fw_.WriteLine("1");
            }

            fw_.WriteLine(textBox1.Text);
            fw_.WriteLine(textBox2.Text);

            fw_.Close();
        }

        private void Setting_Form_Load(object sender, EventArgs e)
        {

        }
    }
}
