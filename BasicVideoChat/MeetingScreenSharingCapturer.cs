using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTok;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace BasicVideoChat
{
    public class MeetingScreenSharingCapturer : IVideoCapturer
    {
        private readonly Process _process;
        private Action _closeProcessHandler;
        const int Fps = 15;
        private int _width;
        private int _height;
        private Task _task;
        private IVideoFrameConsumer _frameConsumer;
#pragma warning disable CS0649
        private Texture2D _screenTexture;
        private OutputDuplication _duplicatedOutput;
#pragma warning restore CS0649
        private bool _isSharing;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDc, uint nFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public MeetingScreenSharingCapturer()
        {
        }

        public void Init(IVideoFrameConsumer frameConsumer)
        {
            _frameConsumer = frameConsumer;
        }

        public void Start()
        {
            if (_task != null) return;

            ShareScreen();
        }
        public void ShareScreen()
        {
            _isSharing = true;

            _task = Task.Factory.StartNew(() =>
            {
                while (_isSharing)
                {
                    try
                    {
                        var captureRectangle = Screen.AllScreens[0].Bounds;

                        _width = captureRectangle.Right - captureRectangle.Left;
                        _height = captureRectangle.Bottom - captureRectangle.Top;
                        using (var bmp = new Bitmap(_width, _height))
                        {
                            using (var memoryGraphics = Graphics.FromImage(bmp))
                            {
                                memoryGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0,
                                    captureRectangle.Size);

                                using (var bmpConverted = new Bitmap(bmp, new Size(_height / 10 * 16, _height)))
                                {
                                    CreateYuv420PFrameFromBitmap(bmpConverted);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    Task.Delay(100);
                }
            });
        }

        /// <summary>Creates a YUV420p video frame from a bitmap.</summary>
        /// <param name="bitmap">The source bitmap for the frame.</param>
        public void CreateYuv420PFrameFromBitmap(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var strides = new[] { bitmapData.Stride };
            var planes = new[]
            {
                bitmapData.Scan0
            };
            using (var frame = VideoFrame.CreateFrameFromBuffer(OpenTok.PixelFormat.FormatArgb32, bitmapData.Width, bitmapData.Height, planes, strides))
            {
                _frameConsumer.Consume(frame);
                bitmap.UnlockBits(bitmapData);
            }
        }

        public void Stop()
        {
            _isSharing = false;
            _task = null;
        }

        public void Destroy()
        {
            _isSharing = false;
            _task?.Dispose();
            _task = null;
            _duplicatedOutput?.Dispose();
            _screenTexture?.Dispose();
        }

        public VideoCaptureSettings GetCaptureSettings()
        {
            VideoCaptureSettings settings = new VideoCaptureSettings();
            settings.Width = _width;
            settings.Height = _height;
            settings.Fps = Fps;
            settings.MirrorOnLocalRender = false;
            settings.PixelFormat = OpenTok.PixelFormat.FormatYuv420p;
            return settings;
        }

        public void SetVideoContentHint(VideoContentHint contentHint)
        {
        }

        public VideoContentHint GetVideoContentHint()
        {
            return VideoContentHint.NONE;
        }

    }
}
