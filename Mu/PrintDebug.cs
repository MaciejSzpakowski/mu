using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mu
{
    public partial class PrintDebug : Form
    {
        string mDebugString;

        public PrintDebug()
        {
            InitializeComponent();
        }

        private void PrintDebug_Load(object sender, EventArgs e)
        {
            label1.Text = string.Empty;
            mDebugString = string.Empty;
            Location = new Point(Location.X, Debug.Console.Location.Y + Debug.Console.Size.Height);
        }

        /// <summary>
        /// Print something in real time
        /// </summary>
        /// <param name="item">What to print, this item will be convertex to string by ToString() method</param>
        /// <param name="desc">Optional description</param>
        public void Print(object item, string desc = "")
        {
            if (desc != string.Empty)
                mDebugString += desc + ": ";
            mDebugString += item.ToString() + Environment.NewLine;
        }

        public void PrintDebugString()
        {
            label1.Text = mDebugString;
            mDebugString = string.Empty;
        }
    }
}
