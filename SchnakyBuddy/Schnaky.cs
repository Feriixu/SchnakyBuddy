using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("User32.dll")]
        private static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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

        public Schnaky()
        {
            this.InitializeComponent();
            StartNewScreenMeltTimer();
            this.Size = new Size(283, 283);
            this.SchnakyPic = Bitmap.FromFile(Environment.CurrentDirectory + @"\schnake2.png");
            this.SchnakyPicRotated = Bitmap.FromFile(Environment.CurrentDirectory + @"\schnake2.png");

            SchnakyHandle = this.Handle;

            // MouseHooks
            MouseHook.MouseAction_WM_LBUTTONUP += new EventHandler(this.MouseEvent_LeftUp);
            MouseHook.MouseAction_WM_LBUTTONDOWN += new EventHandler(this.MouseEvent_LeftDown);
            MouseHook.Start();

            // Hide window from taskbar and alt tab and processes list
            var exStyle = (int)GetWindowLong(this.Handle, (int)GetWindowLongFields.GWL_EXSTYLE); // Get initial style
            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW | 0x80000 | 0x20; // Change style
            SetWindowLong(this.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle); // Apply changes

            // Set initial position

            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            this.SchnakyLocation = new Vector2(this.Location.X, this.Location.Y);
            // Beepy boi
            //new Task(this.ChangeVolume).Start();
            this.TopMost = true;
            ShowWindow(this.Handle, 1);
            //notifyIconSchnaky.ShowBalloonTip(2, "Schnaky", "is running", ToolTipIcon.Info);
            var item = new ToolStripItem();
        }

        private void StartNewScreenMeltTimer()
        {
            timerScreenMelt.Stop();
            Random r = new Random();
            timerScreenMelt.Interval = r.Next(10000, 60000);
            timerScreenMelt.Start();
        }

        private readonly Image SchnakyPic;
        private Image SchnakyPicRotated;

        public static Bitmap RotateImage(Image inputImage, float angleDegrees, bool upsizeOk,
                                 bool clipOk, Color backgroundColor)
        {
            if (inputImage is null)
                throw new ArgumentNullException(nameof(inputImage));
            // Test for zero rotation and return a clone of the input image
            if (angleDegrees == 0f)
                return (Bitmap)inputImage.Clone();

            // Set up old and new image dimensions, assuming upsizing not wanted and clipping OK
            var oldWidth = inputImage.Width;
            var oldHeight = inputImage.Height;
            var newWidth = oldWidth;
            var newHeight = oldHeight;
            var scaleFactor = 1f;

            // If upsizing wanted or clipping not OK calculate the size of the resulting bitmap
            if (upsizeOk || !clipOk)
            {
                var angleRadians = angleDegrees * Math.PI / 180d;

                var cos = Math.Abs(Math.Cos(angleRadians));
                var sin = Math.Abs(Math.Sin(angleRadians));
                newWidth = (int)Math.Round(oldWidth * cos + oldHeight * sin);
                newHeight = (int)Math.Round(oldWidth * sin + oldHeight * cos);
            }

            // If upsizing not wanted and clipping not OK need a scaling factor
            if (!upsizeOk && !clipOk)
            {
                scaleFactor = Math.Min((float)oldWidth / newWidth, (float)oldHeight / newHeight);
                newWidth = oldWidth;
                newHeight = oldHeight;
            }

            // Create the new bitmap object. If background color is transparent it must be 32-bit, 
            //  otherwise 24-bit is good enough.
            var newBitmap = new Bitmap(newWidth, newHeight, backgroundColor == Color.Transparent ?
                                             PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
            newBitmap.SetResolution(inputImage.HorizontalResolution, inputImage.VerticalResolution);

            // Create the Graphics object that does the work
            using (var graphicsObject = Graphics.FromImage(newBitmap))
            {
                graphicsObject.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsObject.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphicsObject.SmoothingMode = SmoothingMode.HighQuality;

                // Fill in the specified background color if necessary
                if (backgroundColor != Color.Transparent)
                    graphicsObject.Clear(backgroundColor);

                // Set up the built-in transformation matrix to do the rotation and maybe scaling
                graphicsObject.TranslateTransform(newWidth / 2f, newHeight / 2f);

                if (scaleFactor != 1f)
                    graphicsObject.ScaleTransform(scaleFactor, scaleFactor);

                graphicsObject.RotateTransform(angleDegrees);
                graphicsObject.TranslateTransform(-oldWidth / 2f, -oldHeight / 2f);

                // Draw the result 
                graphicsObject.DrawImage(inputImage, 0, 0);
            }

            return newBitmap;
        }

        private Vector2 SchnakyLocation;
        private Vector2 SchnakyVelocity = Vector2.Zero;

        private void TimerMouseCheck_Tick(object sender, EventArgs e)
        {
            //var target = new Vector2(Screen.PrimaryScreen.Bounds.Width - (this.Size.Width), Screen.PrimaryScreen.Bounds.Height - (this.Size.Height));
            var target = RandomTargetPos;
            var mousePos = new Vector2(Cursor.Position.X, Cursor.Position.Y);

            var currentMiddle = new Vector2(this.SchnakyLocation.X + (this.Size.Width / 2), this.SchnakyLocation.Y + (this.Size.Height / 2));
            //var currentPos = new Vector2(this.Location.X, this.Location.Y);

            var distance = Vector2.Distance(currentMiddle, mousePos);

            if (LeftButtonDown)
            {
                var direction = Vector2.Normalize(mousePos - currentMiddle);
                this.SchnakyVelocity = Vector2.Lerp(this.SchnakyVelocity, Vector2.Multiply(direction, 40), 0.1f);
            }
            else if (distance <= 250) // Run away from mouse
            {
                this.GetNewRandomPos();
                var direction = Vector2.Normalize(currentMiddle - mousePos);
                this.SchnakyVelocity = Vector2.Lerp(this.SchnakyVelocity, Vector2.Multiply(direction, (8 * 255) / (distance + 1)), 0.1f);
            }
            else if (this.TargetWindow)
            {
                var wRect = new RECT();
                GetWindowRect(this.SchnakyTargetWindow.windowHandle, ref wRect);

                var windowMiddle = new Vector2((wRect.Left + wRect.Right) / 2, (wRect.Top + wRect.Bottom) / 2);

                //Check if schnaky is at the middle of the window
                if (Vector2.Distance(windowMiddle, currentMiddle) < 20)
                {
                    if (!timerDragWindow.Enabled)
                    {
                        timerDragWindow.Start();
                        GetNewRandomPos();
                    }

                    SetForegroundWindow(SchnakyTargetWindow.windowHandle);

                    // Go to random target
                    var direction = Vector2.Normalize(target - this.SchnakyLocation);
                    this.SchnakyVelocity = Vector2.Lerp(this.SchnakyVelocity, Vector2.Multiply(direction, 8), 0.01f);
                    // Drag window
                    MoveWindow(SchnakyTargetWindow.windowHandle, (int)(wRect.Left + this.SchnakyVelocity.X), (int)(wRect.Top + SchnakyVelocity.Y), wRect.Right, wRect.Bottom, true);
                }
                else
                {
                    var direction = Vector2.Normalize(windowMiddle - currentMiddle);
                    this.SchnakyVelocity = Vector2.Lerp(this.SchnakyVelocity, Vector2.Multiply(direction, 8), 0.02f);
                }



            }
            else if (Vector2.Distance(this.SchnakyLocation, target) > 20) // Go to random target
            {
                var direction = Vector2.Normalize(target - this.SchnakyLocation);
                this.SchnakyVelocity = Vector2.Lerp(this.SchnakyVelocity, Vector2.Multiply(direction, 8), 0.01f);
            }
            else
            {
                this.GetNewRandomPos();
            }

            this.SchnakyPicRotated = RotateImage(this.SchnakyPic, CalcSchnakyAngle(this.SchnakyVelocity), false, true, Color.Transparent);
            this.SchnakyLocation += this.SchnakyVelocity;

            // Set new location
            this.Invoke(new MethodInvoker(delegate ()
            {
                this.Location = new Point((int)this.SchnakyLocation.X, (int)this.SchnakyLocation.Y);
                this.Refresh();
            }));
        }

        private static float CalcSchnakyAngle(Vector2 direction) => (float)AngleBetween(new Vector2(0, -1), direction);

        /// <summary>
        /// Calculates the angle between two 2D Vectors. Returns angle in degrees.
        /// </summary>
        public static double AngleBetween(Vector2 vector1, Vector2 vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
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
        }

        private void MouseEvent_LeftDown(object sender, EventArgs e)
        {
            LeftButtonDown = true;
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
            using (var image = this.SchnakyPicRotated)
            {
                var destRect = new Rectangle(0, 0, this.Size.Width - 1, this.Size.Height - 1);
                var srcRect = new Rectangle(0, 0, image.Width, image.Height);
                e.Graphics.Clear(Color.Transparent);
                if (this.TargetWindow)
                {
                    using (var pen = new Pen(Color.Red))
                    {
                        e.Graphics.DrawRectangle(pen, destRect);
                    }
                }

                e.Graphics.DrawImage(image, destRect, srcRect, GraphicsUnit.Pixel);
            }
        }

        private readonly Random r = new Random();
        private static Vector2 RandomTargetPos = Vector2.Zero;
        private void GetNewRandomPos() => RandomTargetPos = new Vector2(this.r.Next(Screen.PrimaryScreen.Bounds.Width - this.Size.Width), this.r.Next(Screen.PrimaryScreen.Bounds.Height - this.Size.Height));

        private void Schnaky_FormClosing(object sender, FormClosingEventArgs e) => this.notifyIconSchnaky = null;

        private bool TargetWindow = false;
        private WindowInfo SchnakyTargetWindow;

        private void timerGrabWindow_Tick(object sender, EventArgs e)
        {
            var windows = WindowEnumerator.GetWindows(true);
            var window = windows[this.r.Next(windows.Count)];
            SchnakyTargetWindow = window;
            this.TargetWindow = true;
            ShowWindow(window.windowHandle, 1);
            Debug.WriteLine(window.name);
            timerMaxGrabTime.Start();
        }

        private void timerDragWindow_Tick(object sender, EventArgs e)
        {
            timerDragWindow.Stop();
            TargetWindow = false;
        }

        private void timerMaxGrabTime_Tick(object sender, EventArgs e)
        {
            this.TargetWindow = false;
        }

        private void timerScreenMelt_Tick(object sender, EventArgs e)
        {
            new ScreenMeltForm(0, 10, r.Next(5000, 15000)).Show();
            StartNewScreenMeltTimer();
        }
    }
}
