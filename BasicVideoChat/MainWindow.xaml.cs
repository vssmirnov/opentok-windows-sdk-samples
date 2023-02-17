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

        Session Session;
        Publisher Publisher;

        public static class Logger
        {
            public static string Log { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Closed += OnClosed;

            // Uncomment following line to get debug logging
            LogUtil.Instance.EnableLogging();

            var context = new Context(new WPFDispatcher());
            Publisher = new Publisher.Builder(context)
            {
                Renderer = PublisherVideo
            }.Build();

            Session = new Session.Builder(context, API_KEY, SESSION_ID).Build();
            Session.Connected += Session_Connected;
            Session.Disconnected += Session_Disconnected;
            Session.Error += Session_Error;
            Session.StreamReceived += Session_StreamReceived;
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

        private void Session_StreamReceived(object sender, Session.StreamEventArgs e)
        {
            Subscriber subscriber = new Subscriber.Builder(Context.Instance, e.Stream)
            {
                Renderer = SubscriberVideo
            }.Build();
            Session.Subscribe(subscriber);
        }

        public void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var delayActionUser = 3000;
            while (true)
            {
                Logger.Log += Environment.NewLine + "Publisher.PublishAudio = false" + Environment.NewLine;
                Publisher.PublishAudio = false;
                Task.Delay(delayActionUser);
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;

                Logger.Log += Environment.NewLine + "Publisher.PublishAudio = true" + Environment.NewLine;
                Publisher.PublishAudio = true;
                Task.Delay(delayActionUser);
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;

                Logger.Log += Environment.NewLine + "Publisher.PublishVideo = false" + Environment.NewLine;
                Publisher.PublishVideo = false;
                Task.Delay(delayActionUser);
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;

                Logger.Log += Environment.NewLine + "Publisher.PublishVideo = true" + Environment.NewLine;
                Publisher.PublishVideo = true;
                Task.Delay(delayActionUser);
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;

                Logger.Log += Environment.NewLine + "SelectFirstCamera();" + Environment.NewLine;
                SelectFirstCamera();
                Task.Delay(delayActionUser);
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;

                Logger.Log += Environment.NewLine + "SelectLastCamera();" + Environment.NewLine;
                SelectLastCamera();
                Task.Delay(delayActionUser);
                File.WriteAllText(@".\Log.txt", Logger.Log);
                Logger.Log = String.Empty;
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
            Publisher.Dispose();
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
