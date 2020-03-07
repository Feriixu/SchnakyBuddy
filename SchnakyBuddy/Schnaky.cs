using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static SchnakyBuddy.WindowStyle;

namespace SchnakyBuddy
{
    public partial class Schnaky : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("User32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const short SWP_NOMOVE = 0X2;
        private const short SWP_NOSIZE = 1;
        private const short SWP_NOZORDER = 0X4;
        private const int SWP_SHOWWINDOW = 0x0040;

        private struct WindowSpecs
        {
            public int X;
            public int Y;
            public int width;
            public int height;
        }

        private readonly SchnakyAction action;

        public Schnaky(SchnakyAction action = SchnakyAction.Default)
        {
            this.InitializeComponent();

            this.SchnakyPic = Bitmap.FromFile(Environment.CurrentDirectory + @"\schnake.png");
            this.SchnakyPicRot = Bitmap.FromFile(Environment.CurrentDirectory + @"\schnake.png");

            this.action = action;
            SchnakyHandle = this.Handle;

            // MouseHooks
            MouseHook.MouseAction_WM_LBUTTONUP += new EventHandler(this.MouseEvent_LeftUp);
            MouseHook.MouseAction_WM_LBUTTONDOWN += new EventHandler(this.MouseEvent_LeftDown);
            MouseHook.Start();

            // Hide window from taskbar and alt tab and processes list
            var exStyle = (int)GetWindowLong(this.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(this.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            // Set initial position

            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            SchnakyLocation = new Vector2(this.Location.X, this.Location.Y);
            // Beepy boi
            //new Task(this.ChangeVolume).Start();

            //notifyIconSchnaky.ShowBalloonTip(2, "Schnaky", "is running", ToolTipIcon.Info);
        }

        private readonly Image SchnakyPic;
        private Image SchnakyPicRot;

        private Bitmap RotateImage(Bitmap b, float angle)
        {
            var returnBitmap = new Bitmap(b.Width, b.Height);
            var g = Graphics.FromImage(returnBitmap);
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
            g.RotateTransform(angle);
            g.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
            g.DrawImage(b, new Point(0, 0));
            return returnBitmap;
        }

        private void DoAction()
        {
            switch (this.action)
            {
                case SchnakyAction.Talk:
                    break;
                case SchnakyAction.Dance:
                    break;
                case SchnakyAction.DoTask:
                    break;
                case SchnakyAction.JustMove:
                    break;
                default:
                    break;
            }

        }

        Vector2 SchnakyLocation;

        private void TimerMouseCheck_Tick(object sender, EventArgs e)
        {
            //var target = new Vector2(Screen.PrimaryScreen.Bounds.Width - (this.Size.Width), Screen.PrimaryScreen.Bounds.Height - (this.Size.Height));
            var target = RandomTargetPos;
            var mousePos = new Vector2(Cursor.Position.X, Cursor.Position.Y);

            var currentMiddle = new Vector2(SchnakyLocation.X + (this.Size.Width / 2), SchnakyLocation.Y + (this.Size.Height / 2));
            //var currentPos = new Vector2(this.Location.X, this.Location.Y);

            var distance = Vector2.Distance(currentMiddle, mousePos);

            if (LeftButtonDown)
            {
                var direction = Vector2.Normalize(mousePos - currentMiddle);
                var newPos = SchnakyLocation + Vector2.Multiply(direction, 40);
                this.SchnakyLocation = newPos;
                this.SchnakyPicRot = this.RotateImage((Bitmap)this.SchnakyPic, CalcSchnakyAngle(direction));
            }
            else if (distance <= 250)
            {
                this.GetNewRandomPos();
                var direction = Vector2.Normalize(currentMiddle - mousePos);
                var newPos = SchnakyLocation + Vector2.Multiply(direction, (8 * 255) / distance);
                this.SchnakyLocation = newPos;
                this.SchnakyPicRot = this.RotateImage((Bitmap)this.SchnakyPic, CalcSchnakyAngle(direction));
            }
            else if (Vector2.Distance(SchnakyLocation, target) > 2)
            {
                var direction = Vector2.Normalize(target - SchnakyLocation);
                var newPos = SchnakyLocation + Vector2.Multiply(direction, 4);
                this.SchnakyLocation = newPos;
                this.SchnakyPicRot = this.RotateImage((Bitmap)this.SchnakyPic, CalcSchnakyAngle(direction));
            }
            else
            {
                this.GetNewRandomPos();
                this.SchnakyPicRot = this.RotateImage((Bitmap)this.SchnakyPic, 0);
            }

            // Set new location
            this.Invoke(new MethodInvoker(delegate ()
            {
                this.Location = new Point((int)SchnakyLocation.X, (int)SchnakyLocation.Y);
                this.Refresh();
            }));
        }

        private static float CalcSchnakyAngle(Vector2 direction) => (float)AngleBetween(new Vector2(0, -1), direction);

        public static double AngleBetween(Vector2 vector1, Vector2 vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }

        private void TimerCheckWindow_Tick(object sender, EventArgs e)
        {
            var handle = GetForegroundWindow();

            if (handle != this.Handle)
            {
                var lastPosX = System.Windows.Forms.Cursor.Position.X;
                var lastPosY = System.Windows.Forms.Cursor.Position.Y;
                var specs = this.GetWindowPos(handle);
                Thread.Sleep(100);
                var specs2 = this.GetWindowPos(handle);

                if (specs.X != specs2.X || specs.Y != specs2.Y && handle != IntPtr.Zero)
                {
                    var newX = (specs.X - specs2.X);
                    var newY = (specs.Y - specs2.Y);

                    SetCursorPos(lastPosX, lastPosY);
                }
            }
        }

        private WindowSpecs GetWindowPos(IntPtr handle)
        {
            WindowSpecs specs;
            var winRec = new RECT();
            GetWindowRect(handle, ref winRec);
            specs.X = winRec.Left;
            specs.Y = winRec.Top;
            specs.width = winRec.Right - winRec.Left;
            specs.height = winRec.Bottom - winRec.Top;
            return specs;
        }

        private static volatile bool LeftButtonDown = false;
        private static Point lastMouseClickPos = new Point();
        private static WindowSpecs lastWindowPos = new WindowSpecs();
        private static IntPtr SchnakyHandle;

        private void MouseEvent_LeftUp(object sender, EventArgs e)
        {
            LeftButtonDown = false;
            Console.WriteLine("Left mouse up!");
        }

        private void MouseEvent_LeftDown(object sender, EventArgs e)
        {
            try
            {
                LeftButtonDown = true;
                Console.WriteLine("Left mouse down!");
                lastMouseClickPos = Cursor.Position;

                var handle = GetForegroundWindow();
                if (handle != SchnakyHandle)
                {
                    lastWindowPos = this.GetWindowPos(handle);
                }

                while (LeftButtonDown)
                {
                    var newWindowPos = this.GetWindowPos(handle);
                    if ((lastWindowPos.X != newWindowPos.X)
                        || (lastWindowPos.Y != newWindowPos.Y))
                    {
                        //float factor = 0.5;
                        //var interX = (int)(Cursor.Position.X*factor + lastMouseClickPos.X*(1-factor));
                        //var interY = (int)(Cursor.Position.Y * factor + lastMouseClickPos.Y * (1 - factor));

                        SetCursorPos(lastMouseClickPos.X, lastMouseClickPos.Y);
                        //SetCursorPos(interX, interY);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void ChangeVolume()
        {
            //CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
            //defaultPlaybackDevice.Volume = 80;
            Console.Beep(1320, 500); Console.Beep(990, 250); Console.Beep(1056, 250); Console.Beep(1188, 250); Console.Beep(1320, 125); Console.Beep(1188, 125); Console.Beep(1056, 250); Console.Beep(990, 250); Console.Beep(880, 500); Console.Beep(880, 250); Console.Beep(1056, 250); Console.Beep(1320, 500); Console.Beep(1188, 250); Console.Beep(1056, 250); Console.Beep(990, 750); Console.Beep(1056, 250); Console.Beep(1188, 500); Console.Beep(1320, 500); Console.Beep(1056, 500); Console.Beep(880, 500); Console.Beep(880, 500); System.Threading.Thread.Sleep(250); Console.Beep(1188, 500); Console.Beep(1408, 250); Console.Beep(1760, 500); Console.Beep(1584, 250); Console.Beep(1408, 250); Console.Beep(1320, 750); Console.Beep(1056, 250); Console.Beep(1320, 500); Console.Beep(1188, 250); Console.Beep(1056, 250); Console.Beep(990, 500); Console.Beep(990, 250); Console.Beep(1056, 250); Console.Beep(1188, 500); Console.Beep(1320, 500); Console.Beep(1056, 500); Console.Beep(880, 500); Console.Beep(880, 500); System.Threading.Thread.Sleep(500);
            Console.Beep(659, 125); Console.Beep(659, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(523, 125); Console.Beep(659, 125); Thread.Sleep(125); Console.Beep(784, 125); Thread.Sleep(375); Console.Beep(392, 125); Thread.Sleep(375); Console.Beep(523, 125); Thread.Sleep(250); Console.Beep(392, 125); Thread.Sleep(250); Console.Beep(330, 125); Thread.Sleep(250); Console.Beep(440, 125); Thread.Sleep(125); Console.Beep(494, 125); Thread.Sleep(125); Console.Beep(466, 125); Thread.Sleep(42); Console.Beep(440, 125); Thread.Sleep(125); Console.Beep(392, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(125); Console.Beep(784, 125); Thread.Sleep(125); Console.Beep(880, 125); Thread.Sleep(125); Console.Beep(698, 125); Console.Beep(784, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(125); Console.Beep(523, 125); Thread.Sleep(125); Console.Beep(587, 125); Console.Beep(494, 125); Thread.Sleep(125); Console.Beep(523, 125); Thread.Sleep(250); Console.Beep(392, 125); Thread.Sleep(250); Console.Beep(330, 125); Thread.Sleep(250); Console.Beep(440, 125); Thread.Sleep(125); Console.Beep(494, 125); Thread.Sleep(125); Console.Beep(466, 125); Thread.Sleep(42); Console.Beep(440, 125); Thread.Sleep(125); Console.Beep(392, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(125); Console.Beep(784, 125); Thread.Sleep(125); Console.Beep(880, 125); Thread.Sleep(125); Console.Beep(698, 125); Console.Beep(784, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(125); Console.Beep(523, 125); Thread.Sleep(125); Console.Beep(587, 125); Console.Beep(494, 125); Thread.Sleep(375); Console.Beep(784, 125); Console.Beep(740, 125); Console.Beep(698, 125); Thread.Sleep(42); Console.Beep(622, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(415, 125); Console.Beep(440, 125); Console.Beep(523, 125); Thread.Sleep(125); Console.Beep(440, 125); Console.Beep(523, 125); Console.Beep(587, 125); Thread.Sleep(250); Console.Beep(784, 125); Console.Beep(740, 125); Console.Beep(698, 125); Thread.Sleep(42); Console.Beep(622, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(698, 125); Thread.Sleep(125); Console.Beep(698, 125); Console.Beep(698, 125); Thread.Sleep(625); Console.Beep(784, 125); Console.Beep(740, 125); Console.Beep(698, 125); Thread.Sleep(42); Console.Beep(622, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(415, 125); Console.Beep(440, 125); Console.Beep(523, 125); Thread.Sleep(125); Console.Beep(440, 125); Console.Beep(523, 125); Console.Beep(587, 125); Thread.Sleep(250); Console.Beep(622, 125); Thread.Sleep(250); Console.Beep(587, 125); Thread.Sleep(250); Console.Beep(523, 125); Thread.Sleep(1125); Console.Beep(784, 125); Console.Beep(740, 125); Console.Beep(698, 125); Thread.Sleep(42); Console.Beep(622, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(415, 125); Console.Beep(440, 125); Console.Beep(523, 125); Thread.Sleep(125); Console.Beep(440, 125); Console.Beep(523, 125); Console.Beep(587, 125); Thread.Sleep(250); Console.Beep(784, 125); Console.Beep(740, 125); Console.Beep(698, 125); Thread.Sleep(42); Console.Beep(622, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(698, 125); Thread.Sleep(125); Console.Beep(698, 125); Console.Beep(698, 125); Thread.Sleep(625); Console.Beep(784, 125); Console.Beep(740, 125); Console.Beep(698, 125); Thread.Sleep(42); Console.Beep(622, 125); Thread.Sleep(125); Console.Beep(659, 125); Thread.Sleep(167); Console.Beep(415, 125); Console.Beep(440, 125); Console.Beep(523, 125); Thread.Sleep(125); Console.Beep(440, 125); Console.Beep(523, 125); Console.Beep(587, 125); Thread.Sleep(250); Console.Beep(622, 125); Thread.Sleep(250); Console.Beep(587, 125); Thread.Sleep(250); Console.Beep(523, 125);
        }

        private void notifyIconSchnaky_Click(object sender, EventArgs e)
        {
            this.notifyIconSchnaky = null;
            this.Close();
        }

        private void Schnaky_Paint(object sender, PaintEventArgs e)
        {
            var image = this.SchnakyPicRot;
            e.Graphics.DrawImage(image, new Rectangle(0, 0, 200, 200), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
        }

        private readonly Random r = new Random();
        private static Vector2 RandomTargetPos = Vector2.Zero;
        private void GetNewRandomPos() => RandomTargetPos = new Vector2(this.r.Next(Screen.PrimaryScreen.Bounds.Width - this.Size.Width), this.r.Next(Screen.PrimaryScreen.Bounds.Height-this.Size.Height));

        private void timerRandomPos_Tick(object sender, EventArgs e) => this.GetNewRandomPos();

        private void Schnaky_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.notifyIconSchnaky = null;
        }
    }
}
