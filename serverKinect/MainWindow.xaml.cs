/////////////////////////////////////////////////////////////////////////
//
// This module contains code to do Kinect NUI initialization and
// processing and also to display NUI streams on screen.
//
// Copyright © Microsoft Corporation.  All rights reserved.  
// This code is licensed under the terms of the 
// Microsoft Kinect for Windows SDK (Beta) from Microsoft Research 
// License Agreement: http://research.microsoft.com/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;


namespace KinectServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Runtime nui;
        Server server;
        KinectAudioSource audioSource;
        Stream audioStream;
        RecognizerInfo ri;
        SpeechRecognitionEngine sre;
        
        Grammar commonGrammar;
        Grammar additionalGrammar;
        string lastRecognizedCommand = "";
        int lastRecognizedAngle = 0;
        float lastRecognizedConfidence = 0.0f;
        
        
        // - - - - - - - - - - - - - - - - - - - - - - - -

        const string RecognizerId = "SR_MS_en-US_Kinect_10.0";

        int totalFrames = 0;
        int lastFrames = 0;

        DateTime lastTime = DateTime.MaxValue;
        DateTime lastAngleTime = DateTime.MaxValue;
        DateTime lastSkeletonTime = DateTime.MaxValue;

        byte[] depthFrame32 = new byte[320 * 240 * 4];
        
        
        Dictionary<JointID,Brush> jointColors = new Dictionary<JointID,Brush>() { 
            {JointID.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointID.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointID.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointID.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointID.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointID.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointID.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointID.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointID.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointID.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointID.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointID.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointID.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointID.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointID.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointID.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointID.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };

        Brush[] brushes =  new Brush[6] {
            new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            new SolidColorBrush(Color.FromRgb(0, 255, 0)),
            new SolidColorBrush(Color.FromRgb(64, 255, 255)),
            new SolidColorBrush(Color.FromRgb(255, 255, 64)),
            new SolidColorBrush(Color.FromRgb(255, 64, 255)),
            new SolidColorBrush(Color.FromRgb(128, 128, 255))
        };

        // - - - - - - - - - - - - - - - - - - - - - - - -

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            // kinect runtime
            try
            {
                kinectInit();
            }
            catch (Exception exp)
            {
                log.AppendText("Kinect Init failed:" + exp.Message);
            }

            // audio
            try
            {
                audioInit();
            }
            catch (Exception exp)
            {
                log.AppendText("Speech Recognition Init failed:" + exp.Message);
            }
            // tcp server create

            serverInit();
            
        }

        // audio - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        private void audioInit()
        {
            // recognizer

            ri = SpeechRecognitionEngine.InstalledRecognizers().Where(r => r.Id == RecognizerId).FirstOrDefault();

            if (ri == null)
            {
                //Console.WriteLine("Could not find speech recognizer: {0}. Please refer to the sample requirements.", RecognizerId);
                return;
            }

            //Console.WriteLine("Using: {0}", ri.Name);

            // engine

            sre = new SpeechRecognitionEngine(ri.Id);

            // common dictionary

            Choices commonCommands = new Choices();

            commonCommands.Add("kinect play");
            commonCommands.Add("kinect stop");

            // grammar builder
            var gb = new GrammarBuilder();
            gb.Culture = ri.Culture;

            gb.Append(commonCommands);
            
            // grammar
            commonGrammar = new Grammar(gb);
            sre.LoadGrammar(commonGrammar);
            
            // add event handler

            sre.SpeechRecognized += SreSpeechRecognized;
            
            // new thread
            var t = new Thread(audioRun);
            t.Name = "audio";
            t.Start();
        }

        private void audioRun()
        {
            audioSource = new KinectAudioSource();

            audioSource.FeatureMode = true;
            
            audioSource.SystemMode = SystemMode.OptibeamArrayOnly;
            audioSource.MicArrayMode = MicArrayMode.MicArrayAdaptiveBeam;

            audioSource.AutomaticGainControl = true;
            audioSource.CenterClip = true;
            audioSource.NoiseFill = true;
            audioSource.NoiseSuppression = true;
            audioSource.GainBounder = true;
            
            audioStream = audioSource.Start();
            
            sre.SetInputToAudioStream(audioStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            int angle = (int) (audioSource.MicArrayBeamAngle * 180/Math.PI);

            lastRecognizedCommand = e.Result.Text;
            lastRecognizedAngle = angle;
            lastRecognizedConfidence = e.Result.Confidence;
            
            //Console.WriteLine("Speech Recognized: \ttext: {0}\tangle: {1}\tconfidence: {2}\n\n", e.Result.Text, angle, e.Result.Confidence);
            
            using (MemoryStream speechStream = new MemoryStream())
            {
                using (BinaryWriter speechWriter = new BinaryWriter(speechStream))
                {

                    speechWriter.Write(e.Result.Text);
                    uint packetLength = (uint)(speechStream.Length + 8); // word + confidence + beamAngle
                    speechStream.Position = 0;

                    speechWriter.Write(KinectConst.MS_DATA);
                    speechWriter.Write(KinectConst.OUT_SPEECH);
                    speechWriter.Write(packetLength);

                    speechWriter.Write(e.Result.Text);// write byte length of text than text
                    speechWriter.Write((float)e.Result.Confidence);
                    speechWriter.Write((float)audioSource.MicArrayBeamAngle);

                    if (server != null) server.Send(speechStream.ToArray());
                }
            }
        }

        // server - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        private void serverInit()
        {
            server = new Server();
            server.Start(6001);

            server.Connected += server_Connected;
            server.Disconnected += server_Disconnected;
            server.Recieved += server_Recieved;
        }

        private void server_Connected(object sender, ConnectionEventArgs e)
        {
            //Console.WriteLine("Client {0}:{1} was connected", e.IpAddress, e.Port);
        }

        private void server_Disconnected(object sender, ConnectionEventArgs e)
        {
            //Console.WriteLine("Client {0}:{1} was disconnected", e.IpAddress, e.Port);
        }

        private void server_Recieved(object sender, MessageEventArgs e)
        {
            //Console.WriteLine("COMMAND 0x{0:X}", e.Command);
            
            switch (e.Command) {

                case KinectConst.IN_ADD_WORD:

                    if (additionalGrammar != null)
                    {
                        sre.UnloadGrammar(additionalGrammar);
                    }

                    List<string> words = (List<string>)e.Arguments["words"];
                    Choices additionalCommands = new Choices();

                    //Console.Write("ADD {0} WORDS:\t", words.Count);

                    words.ForEach(delegate(string word)
                    {
                        //Console.Write("{0}\t", word);
                        additionalCommands.Add(word);
                    });

                    // grammar builder

                    // grammar builder
                    var gb = new GrammarBuilder();
                    gb.Culture = ri.Culture;

                    if (additionalCommands != null) gb.Append(additionalCommands);

                    // grammar
                    additionalGrammar = new Grammar(gb);
                    sre.LoadGrammar(additionalGrammar);

                    //Console.WriteLine();
                    break;
                    
            }
        }

        // kinect - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        private void kinectInit()
        {
            nui = new Runtime();

            try
            {
                nui.Initialize(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                return;
            }

            try
            {
                nui.VideoStream.Open(ImageStreamType.Video, 2, ImageResolution.Resolution640x480, ImageType.Color);
                nui.DepthStream.Open(ImageStreamType.Depth, 2, ImageResolution.Resolution320x240, ImageType.DepthAndPlayerIndex);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                return;
            }

            nui.SkeletonEngine.TransformSmooth = true;

            TransformSmoothParameters smoothParameters = new TransformSmoothParameters();
            smoothParameters.JitterRadius = 0.1f;
            smoothParameters.MaxDeviationRadius = 0.1f;
            smoothParameters.Prediction = 0.05f;
            smoothParameters.Smoothing = 0.75f;

            nui.SkeletonEngine.SmoothParameters = smoothParameters;

            // lastTime for FPS

            lastTime = DateTime.Now;
            
            // angleSlider presets
            lastAngleTime = DateTime.Now;

            // add listeners

            nui.DepthFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_DepthFrameReady);
            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            nui.VideoFrameReady += new EventHandler<ImageFrameReadyEventArgs>(nui_ColorFrameReady);
        }

        // Converts a 16-bit grayscale depth frame which includes player indexes into a 32-bit frame
        // that displays different players in different colors
        byte[] convertDepthFrame(byte[] depthFrame16)
        {
            for (int i16 = 0, i32 = 0; i16 < depthFrame16.Length && i32 < depthFrame32.Length; i16 += 2, i32 += 4)
            {
                int player = depthFrame16[i16] & 0x07;
                int realDepth = (depthFrame16[i16+1] << 5) | (depthFrame16[i16] >> 3);
                // transform 13-bit depth information into an 8-bit intensity appropriate
                // for display (we disregard information in most significant bit)
                byte intensity = (byte)(255 - (255 * realDepth / 0x0fff));

                depthFrame32[i32 + KinectConst.RED_IDX] = 0;
                depthFrame32[i32 + KinectConst.GREEN_IDX] = 0;
                depthFrame32[i32 + KinectConst.BLUE_IDX] = 0;

                // choose different display colors based on player
                switch (player)
                {
                    case 0:
                        depthFrame32[i32 + KinectConst.RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + KinectConst.BLUE_IDX] = (byte)(intensity / 2);
                        break;
                    case 1:
                        depthFrame32[i32 + KinectConst.RED_IDX] = intensity;
                        break;
                    case 2:
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = intensity;
                        break;
                    case 3:
                        depthFrame32[i32 + KinectConst.RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + KinectConst.BLUE_IDX] = (byte)(intensity);
                        break;
                    case 4:
                        depthFrame32[i32 + KinectConst.RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = (byte)(intensity);
                        depthFrame32[i32 + KinectConst.BLUE_IDX] = (byte)(intensity / 2);
                        break;
                    case 5:
                        depthFrame32[i32 + KinectConst.RED_IDX] = (byte)(intensity);
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + KinectConst.BLUE_IDX] = (byte)(intensity);
                        break;
                    case 6:
                        depthFrame32[i32 + KinectConst.RED_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = (byte)(intensity / 2);
                        depthFrame32[i32 + KinectConst.BLUE_IDX] = (byte)(intensity);
                        break;
                    case 7:
                        depthFrame32[i32 + KinectConst.RED_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + KinectConst.GREEN_IDX] = (byte)(255 - intensity);
                        depthFrame32[i32 + KinectConst.BLUE_IDX] = (byte)(255 - intensity);
                        break;
                }
            }
            return depthFrame32;
        }

        // draw depth frame and calculate FPS
        void nui_DepthFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            PlanarImage Image = e.ImageFrame.Image;
            byte[] convertedDepthFrame = convertDepthFrame(Image.Bits);

            depth.Source = BitmapSource.Create(
                Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, Image.Width * 4);

            ++totalFrames;

            DateTime cur = DateTime.Now;
            if (cur.Subtract(lastTime) > TimeSpan.FromSeconds(1))
            {
                int frameDiff = totalFrames - lastFrames;
                lastFrames = totalFrames;
                lastTime = cur;
                frameRate.Text = frameDiff.ToString() + " fps";
            }

            if (cur.Subtract(lastSkeletonTime) > TimeSpan.FromMilliseconds(200))
            {
                skeleton.Children.Clear();
                log.Clear();
            }
            
            convertedDepthFrame = null;
        }

        // joint to point
        private Point getDisplayPosition(Joint joint)
        {
            float depthX, depthY;
            nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
            depthX = Math.Max(0, Math.Min(depthX * 320, 320));  //convert to 320, 240 space
            depthY = Math.Max(0, Math.Min(depthY * 240, 240));  //convert to 320, 240 space
            int colorX, colorY;
            ImageViewArea iv = new ImageViewArea();
            // only ImageResolution.Resolution640x480 is supported at this point
            nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);

            // map back to skeleton.Width & skeleton.Height
            return new Point((int)(skeleton.Width * colorX / 640.0), (int)(skeleton.Height * colorY / 480));
        }

        // draw body segment (hand, foot, body, head)
        Polyline getBodySegment(Microsoft.Research.Kinect.Nui.JointsCollection joints, Brush brush, params JointID[] ids)
        {
            PointCollection points = new PointCollection(ids.Length);
            
            for (int i = 0; i < ids.Length; ++i)
            {
                points.Add(getDisplayPosition(joints[ids[i]]));
            }
            
            Polyline polyline = new Polyline();
            polyline.Points = points;
            polyline.Stroke = brush;
            polyline.StrokeThickness = 1;

            return polyline;
        }

        // skeleton handler
        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;
            lastSkeletonTime = DateTime.Now;

            uint absoluteTime = (uint)(lastSkeletonTime.Ticks / TimeSpan.TicksPerMillisecond); // 32-bit uint
                        
            log.Text = "";
            
            skeleton.Children.Clear();

            byte skeletonNum = 0;

            using (MemoryStream skeletonStream = new MemoryStream())
            {
                using (BinaryWriter skeletonWriter = new BinaryWriter(skeletonStream))
                {

                    foreach (SkeletonData data in skeletonFrame.Skeletons)
                    {
                        if (SkeletonTrackingState.Tracked == data.TrackingState)
                        {
                            // skeleton id write to buffer

                            skeletonWriter.Write(KinectConst.MS_DATA);
                            skeletonWriter.Write(KinectConst.OUT_SKELETON);
                            skeletonWriter.Write(KinectConst.SKELETON_BUFFER_LENGTH);
                            skeletonWriter.Write(data.TrackingID);

                            // Draw bones
                            Brush brush = brushes[skeletonNum % brushes.Length];
                            skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.Spine, JointID.ShoulderCenter, JointID.Head));
                            skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ElbowLeft, JointID.WristLeft, JointID.HandLeft));
                            skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.ShoulderCenter, JointID.ShoulderRight, JointID.ElbowRight, JointID.WristRight, JointID.HandRight));
                            skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipLeft, JointID.KneeLeft, JointID.AnkleLeft, JointID.FootLeft));
                            skeleton.Children.Add(getBodySegment(data.Joints, brush, JointID.HipCenter, JointID.HipRight, JointID.KneeRight, JointID.AnkleRight, JointID.FootRight));

                            // Draw and write to buffer joints

                            foreach (Joint joint in data.Joints)
                            {
                                Point jointPos = getDisplayPosition(joint);
                                Ellipse jointPoint = new Ellipse();
                                jointPoint.Width = 6;
                                jointPoint.Height = 6;
                                jointPoint.HorizontalAlignment = HorizontalAlignment.Center;
                                jointPoint.VerticalAlignment = VerticalAlignment.Center;
                                jointPoint.Fill = jointColors[joint.ID];
                                jointPoint.Margin = new Thickness(jointPos.X - 3, jointPos.Y - 3, 0, 0);
                                skeleton.Children.Add(jointPoint);

                                // write to buffer

                                skeletonWriter.Write(joint.Position.X);
                                skeletonWriter.Write(joint.Position.Y);
                                skeletonWriter.Write(joint.Position.Z);
                            }

                            skeletonWriter.Write(absoluteTime);

                            skeletonNum++;
                            Microsoft.Research.Kinect.Nui.Vector position = data.Joints[JointID.ShoulderCenter].Position;
                            log.AppendText("skeleton " + data.TrackingID + "\t(" + position.X.ToString("000.00") + "\t" + position.Y.ToString("000.00") + "\t" + position.Z.ToString("000.00") + ")\n");
                            position = data.Joints[JointID.HandLeft].Position;
                            log.AppendText("leftHand\t\t(" + position.X.ToString("000.00") + "\t" + position.Y.ToString("000.00") + "\t" + position.Z.ToString("000.00") + ")\n");
                            position = data.Joints[JointID.HandRight].Position;
                            log.AppendText("rightHand\t(" + position.X.ToString("000.00") + "\t" + position.Y.ToString("000.00") + "\t" + position.Z.ToString("000.00") + ")\n");
                            if (sre == null) log.AppendText("Speech Recognition Platform x86 is not instalted!\n");
                        }

                    } // for each skeleton

                    log.AppendText("skeletons : " + skeletonNum + "\n");
                    if (lastRecognizedCommand != "") log.AppendText("cmd : " + lastRecognizedCommand + "\t" + lastRecognizedAngle + "\t" + lastRecognizedConfidence + "\n");
                    log.AppendText("clients : " + ClientConnection.Count() + "\n");

                    if (skeletonNum > 0 && server != null) server.Send(skeletonStream.ToArray());

                }
            }
        }

        void nui_ColorFrameReady(object sender, ImageFrameReadyEventArgs e)
        {
            // 32-bit per pixel, RGBA image
            PlanarImage Image = e.ImageFrame.Image;
            video.Source = BitmapSource.Create(
                Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, Image.Bits, Image.Width * Image.BytesPerPixel);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // audio stop

            sre.RecognizeAsyncCancel();
            sre.RecognizeAsyncStop();
            audioStream.Close();
            audioStream.Dispose();
            audioSource.Dispose();

            // server stop

            server.Stop();

            // kinect stop

            nui.Uninitialize();

            // exit
            
            Environment.Exit(0);
        }

        private void log_TextChanged(object sender, TextChangedEventArgs e)
        {
            log.ScrollToEnd();
        }

        private void upButton_Click(object sender, RoutedEventArgs e)
        {
            int newAngle = 5 * ((int) (nui.NuiCamera.ElevationAngle)/5) + 5;
            if (newAngle > Camera.ElevationMaximum) newAngle = Camera.ElevationMaximum;

            SetAngle(newAngle);
        }

        private void downButton_Click(object sender, RoutedEventArgs e)
        {
            int newAngle = 5 * ((int) (nui.NuiCamera.ElevationAngle) / 5) - 5;
            if (newAngle < Camera.ElevationMinimum) newAngle = Camera.ElevationMinimum;

            SetAngle(newAngle);
        }

        private void SetAngle(int angle)
        {
            DateTime cur = DateTime.Now;

            if (angle == nui.NuiCamera.ElevationAngle || cur.Subtract(lastAngleTime) < TimeSpan.FromMilliseconds(1000)) return;

            nui.NuiCamera.ElevationAngle = angle;
            lastAngleTime = DateTime.Now;
        }
    }
}
