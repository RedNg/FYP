using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.CvEnum;
using System.Threading;

namespace ICAO_Conformance_Photo_Capturing
{
    public partial class Form1 : Form
    {
        Capture capture = new Capture(0);
        CascadeClassifier faces;
        CascadeClassifier eyes;
        CascadeClassifier leftEye;
        CascadeClassifier rightEye;
        CascadeClassifier mouth;
        CascadeClassifier smile;
        public Form1()
        {
            InitializeComponent();
            //faces = new CascadeClassifier("lbpcascade_frontalface.xml");
            faces = new CascadeClassifier("haarcascade_frontalface_alt.xml");
            eyes = new CascadeClassifier("haarcascade_mcs_eyepair_big.xml");
            leftEye = new CascadeClassifier("haarcascade_mcs_lefteye.xml");
            rightEye = new CascadeClassifier("haarcascade_mcs_righteye.xml");
            mouth = new CascadeClassifier("nonsmilecascade.xml"); 
            smile = new CascadeClassifier("smilecascade.xml"); //("haarcascade_smile.xml"); 
            Application.Idle += new EventHandler(FrameGrabber);
        }

        void FrameGrabber(object sender, EventArgs e)
        {
            
            //Get frame from webcam
            Image <Bgr, Byte> frame = capture.QueryFrame();
            
            //Bitmap bmp = frame.ToBitmap();
            //Convert frame to grayscale
            if (frame == null)
                return;
            Image<Hsv, Byte> hsvFrame = frame.Convert<Hsv, byte>();
            Image<Gray, Byte> grayFrame = frame.Convert<Gray, Byte>();
            //Equalization step
            grayFrame._EqualizeHist();

            

            //Detect face(s) in the grayscaled frame
            Rectangle[] facesDetected = faces.DetectMultiScale(grayFrame, 1.5, 2, new Size(30, 30),new Size(500, 500));
            Rectangle[] leftEyeDetected;
            Rectangle[] rightEyeDetected;
            Rectangle[] mouthDetected;
            Rectangle[] smileDetected;
            if (facesDetected.Length > 0)
            {
                
                Rectangle biggestRect = new Rectangle();
                Rectangle rect = new Rectangle();
                int foreheadWidth, foreheadHeight;
                //drawing face rectangle on frame
                biggestRect = facesDetected.Aggregate((r1, r2) => (r1.Height * r1.Width) > (r2.Height * r2.Width) ? r1 : r2);
                #region Luca Del Tongo Search Roi based on Face Metric Estimation --- based on empirical measuraments on a couple of photos ---  a really trivial heuristic

                // Our Region of interest where find eyes will start with a sample estimation using face metric
                Int32 yCoordStartSearchEyes = biggestRect.Top + (biggestRect.Height * 3 / 11);
                Point startingPointSearchEyes = new Point(biggestRect.X, yCoordStartSearchEyes);
                Point endingPointSearchEyes = new Point((biggestRect.X + biggestRect.Width), yCoordStartSearchEyes);

                Size searchEyesAreaSize = new Size(biggestRect.Width, (biggestRect.Height * 2 / 9));
                Point lowerEyesPointOptimized = new Point(biggestRect.X, yCoordStartSearchEyes + searchEyesAreaSize.Height);
                Size eyeAreaSize = new Size(biggestRect.Width / 2, (biggestRect.Height * 2 / 9));
                Point startingLeftEyePointOptimized = new Point(biggestRect.X + biggestRect.Width / 2, yCoordStartSearchEyes);

                Rectangle possibleROI_eyes = new Rectangle(startingPointSearchEyes, searchEyesAreaSize);
                Rectangle possibleROI_rightEye = new Rectangle(startingPointSearchEyes, eyeAreaSize);
                Rectangle possibleROI_leftEye = new Rectangle(startingLeftEyePointOptimized, eyeAreaSize);
                
                //Region of interest of mouth
                Int32 yCoordEndSearchMouth = biggestRect.Bottom - (biggestRect.Height * 4 / 11);
                Point startingPointSearchMouth = new Point(biggestRect.X+40, yCoordEndSearchMouth );
                Point endingPointSearchMouth = new Point((biggestRect.X + biggestRect.Width), yCoordEndSearchMouth);

                Size searchMouthAreaSize = new Size(biggestRect.Width*3/5, (biggestRect.Height *  3 / 9));
                Point mouthPointOptimized = new Point(biggestRect.X, yCoordEndSearchMouth -5 );
                Rectangle possibleROI_mouth = new Rectangle(startingPointSearchMouth,searchMouthAreaSize);


                #endregion

                //Draw square on face detected
                frame.Draw(biggestRect, new Bgr(Color.GreenYellow), 1);

                //Get ROI of Face
                grayFrame.ROI = possibleROI_leftEye;
                leftEyeDetected = leftEye.DetectMultiScale(grayFrame, 1.15, 3, new Size(20, 20), new Size(50,50));
                grayFrame.ROI = Rectangle.Empty;
                grayFrame.ROI = possibleROI_rightEye;
                rightEyeDetected = rightEye.DetectMultiScale(grayFrame, 1.15, 3, new Size(20, 20), new Size(50, 50));
                grayFrame.ROI = Rectangle.Empty;
                grayFrame.ROI = possibleROI_mouth;
                CvInvoke.cvShowImage("Mouth Section", grayFrame);
                mouthDetected = mouth.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty, Size.Empty);
                smileDetected = smile.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty, Size.Empty);
                grayFrame.ROI = Rectangle.Empty;

                #region mouth detection
                if (smileDetected.Length > 0)
                {
                    Rectangle smileDetect = smileDetected.Aggregate((r1, r2) => (r1.Height * r1.Width) > (r2.Height * r2.Width) ? r1 : r2);
                    Rectangle smileRect = smileDetect;

                   // Matrix<float> I = new Matrix<float>(N,M, 1);

                    smileRect.Offset(mouthPointOptimized.X, mouthPointOptimized.Y);
                    //grayFrame.ROI = mouthRect;
                    //smileDetected = smile.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty, Size.Empty);//new Size(24, 24), new Size(24, 24));
                    //if (smileDetected.Length > 0)
                    //{
                    frame.Draw(smileRect, new Bgr(Color.Red), 4);
                    //}
                    
                    //frame.Draw(mouthRect, new Bgr(Color.Red), 2);
                   // CvInvoke.cvShowImage("Mouth Section 2", grayFrame);
                }
                else if (mouthDetected.Length > 0)
                {
                    Rectangle mouthDetect = mouthDetected.Aggregate((r1, r2) => (r1.Height * r1.Width) > (r2.Height * r2.Width) ? r1 : r2);
                    Rectangle mouthRect = mouthDetect;

                    // Matrix<float> I = new Matrix<float>(N,M, 1);

                    mouthRect.Offset(mouthPointOptimized.X, mouthPointOptimized.Y);
                    //grayFrame.ROI = mouthRect;
                    //smileDetected = smile.DetectMultiScale(grayFrame, 1.1, 10, Size.Empty, Size.Empty);//new Size(24, 24), new Size(24, 24));
                    //if (smileDetected.Length > 0)
                    //{
                    frame.Draw(mouthRect, new Bgr(Color.Yellow), 4);
                }

                grayFrame.ROI = Rectangle.Empty;
                #endregion
                          
                #region Rectangle split eyes
                
                if (leftEyeDetected.Length > 0 && rightEyeDetected.Length > 0)
                {
                    Rectangle eyeLeft = leftEyeDetected.Aggregate((r1, r2) => (r1.Height * r1.Width) > (r2.Height * r2.Width) ? r1 : r2);
                    Rectangle eyeRect = eyeLeft;
                    
                    eyeRect.Offset(startingLeftEyePointOptimized.X, startingLeftEyePointOptimized.Y);
                    possibleROI_leftEye = eyeRect;
                    frame.Draw(eyeRect, new Bgr(Color.Red), 2);

                    //getting forehead information on ROI
                    foreheadWidth = eyeRect.X + eyeRect.Width;
                    foreheadHeight = 60;
                    Hsv skinColor = hsvFrame.Convert<Hsv,byte>()[ eyeRect.X, eyeRect.Y+ eyeRect.Height + 100];


                    Rectangle eyeRight = rightEyeDetected.Aggregate((r1, r2) => (r1.Height * r1.Width) > (r2.Height * r2.Width) ? r1 : r2);
                    
                    eyeRect = eyeRight;
                    
                    eyeRect.Offset(startingPointSearchEyes.X, startingPointSearchEyes.Y);
                    possibleROI_rightEye = eyeRect;
                    frame.Draw(eyeRect, new Bgr(Color.Red), 2);

                    //Forehead
                    foreheadWidth = foreheadWidth - eyeRect.X ;
                    Point startingPointForeHead = new Point(eyeRect.X, eyeRect.Y - 40);
                    Size foreheadArea = new Size(foreheadWidth,foreheadHeight);
                    Rectangle foreheadRect = new Rectangle(startingPointForeHead, foreheadArea);
                    hsvFrame.ROI = foreheadRect;
                    grayFrame.ROI = foreheadRect;
                    //Image<Gray, byte>[] channels = grayFrame.Split();
                    
                    int colorValue;

                    if (skinColor.Value > 150)
                        colorValue = 25;
                    else
                        colorValue = 50;
                    Hsv lowerLimit = new Hsv(skinColor.Hue - 25, skinColor.Satuation - 25, skinColor.Value - colorValue); //50 for dark(value <150) 25for white 

                    Hsv upperLimit = new Hsv(skinColor.Hue + 25, skinColor.Satuation + 25, skinColor.Value + 40);

                    Image<Gray, byte> imageHSVDest = hsvFrame.InRange(lowerLimit, upperLimit);
                    CvInvoke.cvShowImage("forehead?", imageHSVDest);
                    CvInvoke.cvShowImage("forehead2?", grayFrame);
                    grayFrame.ROI = Rectangle.Empty;
                    hsvFrame.ROI = Rectangle.Empty;
                }
           
                #endregion

                #region Hough Circles Eye Detection

                //smooth
                grayFrame.PyrDown().PyrUp();
                //apply inverse suppression
                grayFrame = grayFrame.ThresholdBinaryInv(new Gray(20), new Gray(255));

                grayFrame.ROI = possibleROI_leftEye;


                //CircleF[] leftEyecircles = grayFrame.HoughCircles(new Gray(180), new Gray(70), 5.0, 10.0, 1, 20)[0];
                CircleF[] leftEyecircles = grayFrame.HoughCircles(new Gray(180), new Gray(20), 5.0, 20.0, 9, 20)[0];
                grayFrame.ROI = Rectangle.Empty;

                if (leftEyecircles.Length > 0 && leftEyeDetected.Length > 0)
                {
                    //CvInvoke.cvShowImage("Threshold Image", grayFrame);
                    CircleF circle = leftEyecircles.Aggregate((r1, r2) => (r1.Area) > (r2.Area) ? r2 : r1);
                    float x = circle.Center.X + possibleROI_leftEye.X;//startingLeftEyePointOptimized.X;
                    float y = circle.Center.Y + possibleROI_leftEye.Y;//startingLeftEyePointOptimized.Y;
                    frame.Draw(new CircleF(new PointF(x, y), circle.Radius), new Bgr(Color.RoyalBlue), 1);

                }
                //foreach (CircleF circle in leftEyecircles)
                //{
                //    float x = circle.Center.X + startingLeftEyePointOptimized.X;
                //    float y = circle.Center.Y + startingLeftEyePointOptimized.Y;
                //    frame.Draw(new CircleF(new PointF(x, y), circle.Radius), new Bgr(Color.RoyalBlue), 4);
                //}

                grayFrame.ROI = Rectangle.Empty;
                grayFrame.ROI = possibleROI_rightEye;
                ////smooth
                //grayFrame.PyrDown().PyrUp();
                ////apply inverse suppression
                //grayFrame = grayFrame.ThresholdBinaryInv(new Gray(20), new Gray(255));

                //bmp = frame.ToBitmap().Clone(possibleROI_rightEye, frame.ToBitmap().PixelFormat);
                ////CircleF[] rightEyecircles = grayFrame.HoughCircles(new Gray(180), new Gray(70), 5.0, 10.0, 1, 200)[0];
                CircleF[] rightEyecircles = grayFrame.HoughCircles(new Gray(180), new Gray(20), 5.0, 20.0, 9, 20)[0];
                //CvInvoke.cvShowImage("Threshold Image", grayFrame);
                grayFrame.ROI = Rectangle.Empty;

                ////foreach (CircleF circle in rightEyecircles)
                ////{
                ////float x1 = circle1.Center.X + startingPointSearchEyes.X;
                ////float y1 = circle1.Center.Y + startingPointSearchEyes.Y;
                ////frame.Draw(new CircleF(new PointF(x1, y1), circle1.Radius), new Bgr(Color.RoyalBlue), 4);
                ////}
                if (rightEyecircles.Length > 0 && rightEyeDetected.Length > 0)
                {
                    //CvInvoke.cvShowImage("Threshold Image", grayFrame);
                    CircleF circle = rightEyecircles.Aggregate((r1, r2) => (r1.Area) > (r2.Area) ? r2 : r1);
                    float x = circle.Center.X + possibleROI_rightEye.X;//startingLeftEyePointOptimized.X;
                    float y = circle.Center.Y + possibleROI_rightEye.Y;// startingLeftEyePointOptimized.Y;
                    frame.Draw(new CircleF(new PointF(x, y), circle.Radius), new Bgr(Color.RoyalBlue), 1);

                }

                #endregion
            }

            
            liveFeed.Image = frame.ToBitmap();
            liveFeed.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
        }

        private void timer_tick(object sender, EventArgs e)
        {

        }

        private void paintOutline(object sender, PaintEventArgs e)
        {
            

            e.Graphics.DrawEllipse(
                new Pen(Color.YellowGreen, 2f),
                liveFeed.Width/4, liveFeed.Height*2/11, 230, 280);
            float[] dashValues = { 5, 2, 5, 2 };
            Pen yellowPen = new Pen(Color.Yellow, 5);
            yellowPen.DashPattern = dashValues;
            e.Graphics.DrawLine(
            yellowPen,
            new Point(liveFeed.Width / 4, liveFeed.Height * 8 / 20),
            new Point(liveFeed.Width* 3 / 4, liveFeed.Height * 8 / 20));
            e.Graphics.DrawLine(
            yellowPen,
            new Point(liveFeed.Width / 4 , liveFeed.Height * 8 / 17+10),
            new Point(liveFeed.Width * 3 / 4, liveFeed.Height * 8 / 17+10));
        }
    }
}
