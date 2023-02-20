using OpenTok;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace BasicVideoChat
{
    public partial class MainWindow : Window
    {
        public const string API_KEY = "";
        public const string SESSION_ID = "";
        public const string TOKEN = "";

        private Session Session;
        private Publisher Publisher;
        private bool _isSharedScreen;
        private Publisher _publisherShare;
        private bool isButton2Click = false;
        private bool isButtonClick = false;

        public static class Logger
        {
            public static string Log { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Closed += OnClosed;

            // Uncomment following line to get debug logging
            // LogUtil.Instance.EnableLogging();

            var context = new Context(new WPFDispatcher());
            Publisher = new Publisher.Builder(context)
            {
                Renderer = PublisherVideo
            }.Build();

            Session = new Session.Builder(context, API_KEY, SESSION_ID).Build();
            Session.Connected += Session_Connected;
            Session.Disconnected += Session_Disconnected;
            Session.Error += Session_Error;
            Session.Connect(TOKEN);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            File.WriteAllText(@".\Log.txt", Logger.Log);
        }

        private void Session_Connected(object sender, System.EventArgs e)
        {
            Session.Publish(Publisher);
        }

        private void Session_Disconnected(object sender, System.EventArgs e)
        {
            Trace.WriteLine("Session disconnected.");
        }

        private void Session_Error(object sender, Session.ErrorEventArgs e)
        {
            Trace.WriteLine("Session error:" + e.ErrorCode);
        }

        public void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var delayActionUser = 3000;
            isButtonClick = !isButtonClick;
            while (isButtonClick)
            {
                Logger.Log += Environment.NewLine + "Publisher.PublishAudio = false" + Environment.NewLine;
                Publisher.PublishAudio = false;
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "Publisher.PublishAudio = true" + Environment.NewLine;
                Publisher.PublishAudio = true;
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "Publisher.PublishVideo = false" + Environment.NewLine;
                Publisher.PublishVideo = false;
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "Publisher.PublishVideo = true" + Environment.NewLine;
                Publisher.PublishVideo = true;
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "SelectFirstCamera();" + Environment.NewLine;
                SelectFirstCamera();
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "SelectLastCamera();" + Environment.NewLine;
                SelectLastCamera();
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);
            }
        }

        public void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
        {
            var delayActionUser = 3000;
            isButton2Click = !isButton2Click;
            while (isButton2Click)
            {
                Logger.Log += Environment.NewLine + "StartShare" + Environment.NewLine;
                StartShare();
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);
                
                Logger.Log += Environment.NewLine + "StopSharing" + Environment.NewLine;
                StopSharing();
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "SelectFirstCamera();" + Environment.NewLine;
                SelectFirstCamera();
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);

                Logger.Log += Environment.NewLine + "SelectLastCamera();" + Environment.NewLine;
                SelectLastCamera();
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
                Task.Delay(delayActionUser);
            }
        }

        private void StartShare()
        {
            if (_isSharedScreen) return;

            _isSharedScreen = true;

            try
            {
                var builder = new Publisher.Builder(new Context(new WPFDispatcher()));
                builder.Capturer = new MeetingScreenSharingCapturer();
                builder.Renderer = ShareVideo;
                _publisherShare = builder.Build();
                _publisherShare.PublishAudio = false;
                _publisherShare.PublishVideo = true;
                _publisherShare.VideoSourceType = VideoSourceType.Screen;
                Session.Publish(_publisherShare);
            }
            catch (Exception ex)
            {
            }
        }

        private void StopSharing()
        {
            if (!_isSharedScreen) return;

            try
            {
                Session.Unpublish(_publisherShare);
                _publisherShare.Dispose();
            }
            catch (Exception ex)
            {

            }
        }

        public void SelectCamera(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new Exception("id is null");
            var isPublishAudio = Publisher.PublishAudio;
            var isPublishVideo = Publisher.PublishVideo;
            UnPublish();

            var videoCapturer = GetVideoCapturer(id);
            Publish(isPublishAudio, isPublishVideo, videoCapturer);
        }

        public void SelectFirstCamera()
        {
            try
            {
                var devices = VideoCapturer.EnumerateDevices();
                var id = devices.FirstOrDefault()?.Id;
                SelectCamera(id);
            }
            catch (Exception ex)
            {
            }
        }

        public void SelectLastCamera()
        {
            try
            {
                var devices = VideoCapturer.EnumerateDevices();
                var id = devices.LastOrDefault()?.Id;
                SelectCamera(id);
            }
            catch (Exception ex)
            {
            }
        }

        private void Publish(bool isPublishAudio, bool isPublishVideo, IVideoCapturer capture)
        {
            var builder = new Publisher.Builder(new Context(new WPFDispatcher()));

            if (capture != null)
            {
                builder.Capturer = capture;
            }

            builder.Renderer = PublisherVideo;

            Publisher = builder.Build();
            Publisher.PublishAudio = isPublishAudio;
            Publisher.PublishVideo = isPublishVideo;
            Publisher.VideoSourceType = VideoSourceType.Camera;

            Session.Publish(Publisher);
        }

        private void UnPublish()
        {
            Session.Unpublish(Publisher);
            Publisher?.Dispose();
            Publisher = null;
        }

        private VideoCapturer GetVideoCapturer(string id)
        {
            try
            {
                VideoCapturer videoCapturer = null;

                var cameraList = GetVideoDevices();
                var device = cameraList?.FirstOrDefault(t => t?.Id?.Equals(id) ?? false);
                var videoFormat = GetFormatVideoDevices(device)?.FirstOrDefault();

                if (device != null && videoFormat != null)
                    videoCapturer = device.CreateVideoCapturer(videoFormat);

                return videoCapturer;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private IList<VideoCapturer.VideoDevice> GetVideoDevices()
        {
            return VideoCapturer.EnumerateDevices();
        }

        private IList<VideoCapturer.VideoFormat> GetFormatVideoDevices(VideoCapturer.VideoDevice device)
        {
            return device.ListFormats();
        }

    }
}
