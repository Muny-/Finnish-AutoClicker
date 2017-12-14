using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utilities;

namespace Finnish_AutoClicker
{
    public partial class Form1 : Form
    {
        globalKeyboardHook hook = new globalKeyboardHook();

        public Form1()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.FixedHeight, true);
            this.SetStyle(ControlStyles.FixedWidth, true);
        }

        void hook_KeyUp(object sender, KeyEventArgs e)
        {
            if (Finnish_AutoClicker.Properties.Settings.Default.ActivationMode == 1)
            {
                isClicking = false;
            }
        }

        void hook_KeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("key down");
            if (isClicking)
            {
                if (Finnish_AutoClicker.Properties.Settings.Default.ActivationMode == 2)
                {
                    isClicking = false;
                }
            }
            else
            {
                if (!isClicking)
                {
                    isClicking = true;
                    loopThread = new Thread(ClickLoop);
                    loopThread.Start();
                }
            }
        }

        bool isClicking = false;
        Thread loopThread;

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private int Mouse_Down = 0x0;
        private int Mouse_Up = 0x0;

        bool firstHidden = false;

        int clicks = 0;

        int tickCount = 0;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        void ClickLoop()
        {

            while (isClicking)
            {
                tickCount++;

                mouse_event((uint)Mouse_Down, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                Thread.Sleep(5);
                mouse_event((uint)Mouse_Up, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
                
                Thread.Sleep(Finnish_AutoClicker.Properties.Settings.Default.Interval);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!firstHidden)
            {
                firstHidden = true;
                notifyIcon1.ShowBalloonTip(2500, "Finnish AutoClicker", "You can reopen the settings window by double clicking on the tray icon here.", ToolTipIcon.Info);
            }

            Finnish_AutoClicker.Properties.Settings.Default.Interval = (int)numericUpDown1.Value;
            if (activationModeKeyHeld.Checked)
                Finnish_AutoClicker.Properties.Settings.Default.ActivationMode = 1;
            else
                Finnish_AutoClicker.Properties.Settings.Default.ActivationMode = 2;

            if (mouseButtonLeft.Checked)
            {
                Finnish_AutoClicker.Properties.Settings.Default.MouseButton = 1;
                Mouse_Down = MOUSEEVENTF_LEFTDOWN;
                Mouse_Up = MOUSEEVENTF_LEFTUP;
            }
            else
            {
                Finnish_AutoClicker.Properties.Settings.Default.MouseButton = 2;
                Mouse_Down = MOUSEEVENTF_RIGHTDOWN;
                Mouse_Up = MOUSEEVENTF_RIGHTUP;
            }

            Finnish_AutoClicker.Properties.Settings.Default.Save();

            ShowInTaskbar = false;
            this.Hide();
            hook.hook();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            KeySelectForm frm = new KeySelectForm();
            frm.ShowDialog();
            Finnish_AutoClicker.Properties.Settings.Default.Key = frm.selectedKey;
            textBox1.Text = Finnish_AutoClicker.Properties.Settings.Default.Key.ToString();
            hook.unhook();
            hook.HookedKeys.Clear();
            hook.HookedKeys.Add(Finnish_AutoClicker.Properties.Settings.Default.Key);
            hook.hook();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
            if (Finnish_AutoClicker.Properties.Settings.Default.MouseButton == 1)
            {
                mouseButtonLeft.Checked = true;
                Mouse_Down = MOUSEEVENTF_LEFTDOWN;
                Mouse_Up = MOUSEEVENTF_LEFTUP;
            }
            else
            {
                mouseButtonRight.Checked = true;
                Mouse_Down = MOUSEEVENTF_RIGHTDOWN;
                Mouse_Up = MOUSEEVENTF_RIGHTUP;
            }

            if (Finnish_AutoClicker.Properties.Settings.Default.ActivationMode == 1)
            {
                activationModeKeyHeld.Checked = true;
            }
            else
            {
                activationModeToggle.Checked = true;
            }

            textBox1.Text = Finnish_AutoClicker.Properties.Settings.Default.Key.ToString();
            numericUpDown1.Value = Finnish_AutoClicker.Properties.Settings.Default.Interval;

            hook.KeyDown += hook_KeyDown;
            hook.KeyUp += hook_KeyUp;
            hook.HookedKeys.Add(Finnish_AutoClicker.Properties.Settings.Default.Key);
            hook.unhook();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.Activate();
            hook.unhook();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isClicking = false;
            try
            {
                loopThread.Abort();
            }
            catch { }

            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClicking = false;
            try
            {
                loopThread.Abort();
            }
            catch { }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (tickCount % 10 == 0 && Finnish_AutoClicker.Properties.Settings.Default.ActivationMode == 1 && !Keyboard.IsKeyDown(Finnish_AutoClicker.Properties.Settings.Default.Key))
            {
                isClicking = false;

                try
                {
                    loopThread.Abort();
                    loopThread.Join();
                }
                catch { }
            }
        }
    }

    public abstract class Keyboard
    {
        [Flags]
        private enum KeyStates
        {
            None = 0,
            Down = 1,
            Toggled = 2
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        private static KeyStates GetKeyState(Keys key)
        {
            KeyStates state = KeyStates.None;

            short retVal = GetKeyState((int)key);

            //If the high-order bit is 1, the key is down
            //otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
                state |= KeyStates.Down;

            //If the low-order bit is 1, the key is toggled.
            if ((retVal & 1) == 1)
                state |= KeyStates.Toggled;

            return state;
        }

        public static bool IsKeyDown(Keys key)
        {
            return KeyStates.Down == (GetKeyState(key) & KeyStates.Down);
        }

        public static bool IsKeyToggled(Keys key)
        {
            return KeyStates.Toggled == (GetKeyState(key) & KeyStates.Toggled);
        }
    }
}
