using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Finnish_AutoClicker
{
    public partial class KeySelectForm : Form
    {
        public Keys selectedKey;

        public KeySelectForm()
        {
            InitializeComponent();
        }

        private void KeySelectForm_KeyUp(object sender, KeyEventArgs e)
        {
            selectedKey = e.KeyCode;
            this.Close();
        }
    }
}
