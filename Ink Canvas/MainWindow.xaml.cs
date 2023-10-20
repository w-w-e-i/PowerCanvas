using Ink_Canvas.Helpers;
using IWshRuntimeLibrary;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using ModernWpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using Point = System.Windows.Point;
using Timer = System.Timers.Timer;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Window Initialization

        public MainWindow()
        {
            InitializeComponent();

            MessageBox.Show("您正在使用开发版本，不具有自动更新功能且可能有异常功能。请知悉。");

            LeftPPTSwitchPanel.Visibility = Visibility.Collapsed;
            RightPPTSwitchPanel.Visibility = Visibility.Collapsed;

            if (!App.StartArgs.Contains("-o")) //-old ui
            {
                ViewBoxStackPanelMain.Visibility = Visibility.Collapsed;

                HideSubPanels();

                ViewboxFloatingBar.Margin = new Thickness((SystemParameters.WorkArea.Width - 284) / 2, SystemParameters.WorkArea.Height - 80, -2000, -200);
            }
            else
            {
                ViewboxFloatingBar.Visibility = Visibility.Collapsed;
                GridForRecoverOldUI.Visibility = Visibility.Collapsed;
            }


            InitTimers();
            timerCheckPPT.Start();
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }

        #endregion

        #region PPTbutton
        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (StackPanelPPTControls.Visibility != Visibility.Visible || currentMode != 0) return;

            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N)
            {
                BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
            }
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
            {
                BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
            }
        }
        #endregion

        #region Ink Canvas Functions

        Color Ink_DefaultColor = Colors.Red;

        DrawingAttributes drawingAttributes;
        private void loadPenCanvas()
        {
            try
            {
                drawingAttributes = inkCanvas.DefaultDrawingAttributes;
                drawingAttributes.Color = Ink_DefaultColor;

                drawingAttributes.Height = 2.5;
                drawingAttributes.Width = 2.5;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                inkCanvas.Gesture += InkCanvas_Gesture;
            }
            catch { }
        }
        DateTime lastGestureTime = DateTime.Now;
        private void InkCanvas_Gesture(object sender, InkCanvasGestureEventArgs e)
        {
            ReadOnlyCollection<GestureRecognitionResult> gestures = e.GetGestureRecognitionResults();
            try
            {
                foreach (GestureRecognitionResult gest in gestures)
                {
                    if (StackPanelPPTControls.Visibility == Visibility.Visible)
                    {
                        if (gest.ApplicationGesture == ApplicationGesture.Left)
                        {
                            BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
                        }
                        if (gest.ApplicationGesture == ApplicationGesture.Right)
                        {
                            BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
                        }
                    }
                }
            }
            catch { }
        }

        private void inkCanvas_EditingModeChanged(object sender, RoutedEventArgs e)
        {
            var inkCanvas1 = sender as InkCanvas;
            if (inkCanvas1 == null) return;
            if (Settings.Canvas.IsShowCursor)
            {
                if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink)
                {
                    inkCanvas1.ForceCursor = true;
                }
                else
                {
                    inkCanvas1.ForceCursor = false;
                }
            }
            else
            {
                inkCanvas1.ForceCursor = false;
            }
            if (inkCanvas1.EditingMode == InkCanvasEditingMode.Ink) forcePointEraser = !forcePointEraser;


        }

        #endregion Ink Canvas


        #region TimeMachine

        private enum CommitReason
        {
            UserInput,
            CodeInput,
            ShapeDrawing,
            ShapeRecognition,
            ClearingCanvas,
            Rotate
        }

        private CommitReason _currentCommitType = CommitReason.UserInput;
        private bool IsEraseByPoint => inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint;
        private StrokeCollection ReplacedStroke;
        private StrokeCollection AddedStroke;
        private TimeMachine timeMachine = new TimeMachine();

       

        #endregion

        #region Definations and Loading

        public static Settings Settings = new Settings();
        public static string settingsFileName = "Settings.json";
        bool isLoaded = false;
        //bool isAutoUpdateEnabled = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadPenCanvas();
            LoadSettings();
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            SystemEvents_UserPreferenceChanged(null, null);
            
            new WelcomeWindow().ShowDialog();
            isLoaded = true;
        }




        private void LoadSettings(bool isStartup = true)
        {
            if (File.Exists(App.RootPath + settingsFileName))
            {
                try
                {
                    string text = File.ReadAllText(App.RootPath + settingsFileName);
                    Settings = JsonConvert.DeserializeObject<Settings>(text);
                }
                catch { }
            }

         


            BtnHideInkCanvas_Click(BtnHideInkCanvas, null);


            if (Settings.Gesture == null)
            {
                Settings.Gesture = new Gesture();
            }

          
       

            if (Settings.Canvas != null)
            {
                drawingAttributes.Height = Settings.Canvas.InkWidth;
                drawingAttributes.Width = Settings.Canvas.InkWidth;

                InkWidthSlider.Value = Settings.Canvas.InkWidth * 2;

                if (Settings.Canvas.IsShowCursor)
                {
                    ToggleSwitchShowCursor.IsOn = true;
                    inkCanvas.ForceCursor = true;
                }
                else
                {
                    ToggleSwitchShowCursor.IsOn = false;
                    inkCanvas.ForceCursor = false;
                }


            }
            else
            {
                Settings.Canvas = new Canvas();
            }

         
               Settings.Advanced = new Advanced();
            


        }

        #endregion Definations and Loading

        #region Right Side Panel

        public static bool CloseIsFromButton = false;
        


        bool forceEraser = false;

       

        int currentMode = 0;


        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (Main_Grid.Background == Brushes.Transparent)
            {
                Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));

                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;

                if (ImageEraserMask.Visibility == Visibility.Visible)
                    BtnColorRed_Click(sender, null);


            }
            else
            {


                if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
                {
                  

                    inkCanvas.IsHitTestVisible = false;
                    inkCanvas.Visibility = Visibility.Visible;

                }
                else
                {
                   

                    if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                    {
                        inkCanvas.Visibility = Visibility.Visible;
                        inkCanvas.IsHitTestVisible = true;
                    }
                    else
                    {

                        inkCanvas.IsHitTestVisible = false;
                        inkCanvas.Visibility = Visibility.Visible;

                    }
                }



                Main_Grid.Background = Brushes.Transparent;


                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;
                if (currentMode != 0)
                {
                    SaveStrokes();
                    RestoreStrokes(true);
                }

                StackPanelPPTButtons.Visibility = Visibility.Visible;
              
            }

            if (Main_Grid.Background == Brushes.Transparent)
            {
                StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                StackPanelCanvacMain.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanelCanvasControls.Visibility = Visibility.Visible;
                StackPanelCanvacMain.Visibility = Visibility.Collapsed;
            }
        }





        #endregion

        #region Right Side Panel (Buttons - Color)

        int inkColor = 1;

        private void ColorSwitchCheck()
        {
            ImageEraser.Visibility = Visibility.Visible;
            if (Main_Grid.Background == Brushes.Transparent)
            {
                if (currentMode == 1)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                }
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
            }

            StrokeCollection strokes = inkCanvas.GetSelectedStrokes();
            if (strokes.Count != 0)
            {
                foreach (Stroke stroke in strokes)
                {
                    try
                    {
                        stroke.DrawingAttributes.Color = inkCanvas.DefaultDrawingAttributes.Color;
                    }
                    catch { }
                }
            }
            else
            {
                inkCanvas.IsManipulationEnabled = true;

                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;

                forceEraser = false;
                ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
                ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;
                switch (inkColor)
                {
                    case 0:
                        ViewboxBtnColorBlackContent.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        ViewboxBtnColorRedContent.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        ViewboxBtnColorGreenContent.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        ViewboxBtnColorBlueContent.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        ViewboxBtnColorYellowContent.Visibility = Visibility.Visible;
                        break;
                    case 5:
                        ViewboxBtnColorWhiteContent.Visibility = Visibility.Visible;
                        break;
                }
            }

        }

        private void BtnColorBlack_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 0;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Black;

            ColorSwitchCheck();
        }

        private void BtnColorRed_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 1;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = Colors.Red;

            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFF3333");
            BtnColorRed.Background = new SolidColorBrush(StringToColor("#FFFF3333"));


            ColorSwitchCheck();
        }

        private void BtnColorGreen_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 2;
            forceEraser = false;

            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF169141");
            BtnColorGreen.Background = new SolidColorBrush(StringToColor("#FF169141"));


            ColorSwitchCheck();
        }

        private void BtnColorBlue_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 3;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FF239AD6");

            ColorSwitchCheck();
        }

        private void BtnColorYellow_Click(object sender, RoutedEventArgs e)
        {
            inkColor = 4;
            forceEraser = false;
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFF38B00");
            BtnColorYellow.Background = new SolidColorBrush(StringToColor("#FFF38B00"));


            ColorSwitchCheck();
        }

        private Color StringToColor(string colorStr)
        {
            Byte[] argb = new Byte[4];
            for (int i = 0; i < 4; i++)
            {
                char[] charArray = colorStr.Substring(i * 2 + 1, 2).ToCharArray();
                //string str = "11";
                Byte b1 = toByte(charArray[0]);
                Byte b2 = toByte(charArray[1]);
                argb[i] = (Byte)(b2 | (b1 << 4));
            }
            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);//#FFFFFFFF
        }

        private static byte toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }

        #endregion

        #region Touch Events


        private bool forcePointEraser = true;

        private void Main_Grid_TouchDown(object sender, TouchEventArgs e)
        {


            inkCanvas.Opacity = 1;
                 inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            
        }

        public double GetTouchBoundWidth(TouchEventArgs e)
        {
            var args = e.GetTouchPoint(null).Bounds;
            double value;
            if (!Settings.Advanced.IsQuadIR) value = args.Width;
            else value = Math.Sqrt(args.Width * args.Height); //四边红外
            if (Settings.Advanced.IsSpecialScreen) value *= Settings.Advanced.TouchMultiplier;
            return value;
        }

        private List<int> dec = new List<int>();
        System.Windows.Point centerPoint;
        InkCanvasEditingMode lastInkCanvasEditingMode = InkCanvasEditingMode.Ink;


        private void inkCanvas_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
           
                TouchPoint touchPoint = e.GetTouchPoint(inkCanvas);
                centerPoint = touchPoint.Position;
                lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            
      
        }

        private void inkCanvas_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (dec.Count > 1)
            {
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    inkCanvas.EditingMode = lastInkCanvasEditingMode;
                }
            }
            dec.Remove(e.TouchDevice.Id);
            inkCanvas.Opacity = 1;
            if (dec.Count == 0)
            {
                if (lastTouchDownStrokeCollection.Count() != inkCanvas.Strokes.Count())
                {
                    int whiteboardIndex = CurrentWhiteboardIndex;
                    if (currentMode == 0)
                    {
                        whiteboardIndex = 0;
                    }
                    strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
                }
            }
        }
        private void inkCanvas_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void inkCanvas_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {

        }

        private void Main_Grid_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (e.Manipulators.Count() == 0)
            {
                if (forceEraser) return;
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }
        }


        #endregion Touch Events

        #region PowerPoint
        Timer timerCheckPPT = new Timer();

        private void InitTimers()
        {
            timerCheckPPT.Elapsed += TimerCheckPPT_Elapsed;
            timerCheckPPT.Interval = 1000;

        }
        public static Microsoft.Office.Interop.PowerPoint.Application pptApplication = null;
        public static Microsoft.Office.Interop.PowerPoint.Presentation presentation = null;
        public static Microsoft.Office.Interop.PowerPoint.Slides slides = null;
        public static Microsoft.Office.Interop.PowerPoint.Slide slide = null;
        public static int slidescount = 0;
        private void BtnCheckPPT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)Marshal.GetActiveObject("PowerPoint.Application");
                //pptApplication.SlideShowWindows[1].View.Next();
                if (pptApplication != null)
                {
                    //获得演示文稿对象
                    presentation = pptApplication.ActivePresentation;
                    pptApplication.SlideShowBegin += PptApplication_SlideShowBegin;
                    pptApplication.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
                    pptApplication.SlideShowEnd += PptApplication_SlideShowEnd;
                    // 获得幻灯片对象集合
                    slides = presentation.Slides;
                    // 获得幻灯片的数量
                    slidescount = slides.Count;
                    memoryStreams = new MemoryStream[slidescount + 2];
                    // 获得当前选中的幻灯片
                    try
                    {
                        // 在普通视图下这种方式可以获得当前选中的幻灯片对象
                        // 然而在阅读模式下，这种方式会出现异常
                        slide = slides[pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch
                    {
                        // 在阅读模式下出现异常时，通过下面的方式来获得当前选中的幻灯片对象
                        slide = pptApplication.SlideShowWindows[1].View.Slide;
                    }
                }

                if (pptApplication == null) throw new Exception();
                StackPanelPPTControls.Visibility = Visibility.Visible;
                LeftPPTSwitchPanel.Visibility = Visibility.Visible;
                RightPPTSwitchPanel.Visibility = Visibility.Visible;
            }
            catch
            {
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                LeftPPTSwitchPanel.Visibility = Visibility.Collapsed;
                RightPPTSwitchPanel.Visibility = Visibility.Collapsed;
                MessageBox.Show("未找到幻灯片");
            }
        }


        public static bool IsShowingRestoreHiddenSlidesWindow = false;

        private void TimerCheckPPT_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsShowingRestoreHiddenSlidesWindow) return;
            try
            {
                pptApplication = (Microsoft.Office.Interop.PowerPoint.Application)Marshal.GetActiveObject("PowerPoint.Application");

                if (pptApplication != null)
                {
                    timerCheckPPT.Stop();
                    presentation = pptApplication.ActivePresentation;
                    pptApplication.PresentationClose += PptApplication_PresentationClose;
                    pptApplication.SlideShowBegin += PptApplication_SlideShowBegin;
                    pptApplication.SlideShowNextSlide += PptApplication_SlideShowNextSlide;
                    pptApplication.SlideShowEnd += PptApplication_SlideShowEnd;
                    slides = presentation.Slides;

                    slidescount = slides.Count;
                    memoryStreams = new MemoryStream[slidescount + 2];
                    try
                    {
                        slide = slides[pptApplication.ActiveWindow.Selection.SlideRange.SlideNumber];
                    }
                    catch
                    {
                        slide = pptApplication.SlideShowWindows[1].View.Slide;
                    }
                }

                if (pptApplication == null) return;

                //如果检测到已经开始放映，则立即进入画板模式
                if (pptApplication.SlideShowWindows.Count >= 1)
                {
                    PptApplication_SlideShowBegin(pptApplication.SlideShowWindows[1]);
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                });
                timerCheckPPT.Start();
            }
        }

        private void PptApplication_PresentationClose(Presentation Pres)
        {
            pptApplication.PresentationClose -= PptApplication_PresentationClose;
            pptApplication.SlideShowBegin -= PptApplication_SlideShowBegin;
            pptApplication.SlideShowNextSlide -= PptApplication_SlideShowNextSlide;
            pptApplication.SlideShowEnd -= PptApplication_SlideShowEnd;
            pptApplication = null;
            timerCheckPPT.Start();
            Application.Current.Dispatcher.Invoke(() =>
            {
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
            });
        }



        private string pptName = null;
        private void PptApplication_SlideShowBegin(SlideShowWindow Wn)
        {
            LogHelper.WriteLogToFile("PowerPoint Application Slide Show Begin", LogHelper.LogType.Event);
            Application.Current.Dispatcher.Invoke(() =>
            {
                //调整颜色
                double screenRatio = SystemParameters.PrimaryScreenWidth / SystemParameters.PrimaryScreenHeight;
                if (Math.Abs(screenRatio - 16.0 / 9) <= -0.01)
                {
                    if (Wn.Presentation.PageSetup.SlideWidth / Wn.Presentation.PageSetup.SlideHeight < 1.65)
                    {
                   
                        SymbolIconBtnColorBlackContent.Foreground = Brushes.White;
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;

                    }
                }
                else if (screenRatio == -256 / 135)
                {

                }

                slidescount = Wn.Presentation.Slides.Count;
                previousSlideID = 0;
                memoryStreams = new MemoryStream[slidescount + 2];

                pptName = Wn.Presentation.Name;
                LogHelper.NewLog("Name: " + Wn.Presentation.Name);
                LogHelper.NewLog("Slides Count: " + slidescount.ToString());

                //检查是否有已有墨迹，并加载
              
                    string defaultFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\Auto Saved\Presentations\";
                    if (Directory.Exists(defaultFolderPath + Wn.Presentation.Name + "_" + Wn.Presentation.Slides.Count))
                    {
                        LogHelper.WriteLogToFile("Found saved strokes", LogHelper.LogType.Trace);
                        FileInfo[] files = new DirectoryInfo(defaultFolderPath + Wn.Presentation.Name + "_" + Wn.Presentation.Slides.Count).GetFiles();
                        int count = 0;
                        foreach (FileInfo file in files)
                        {
                            int i = -1;
                            try
                            {
                                i = int.Parse(System.IO.Path.GetFileNameWithoutExtension(file.Name));
                                memoryStreams[i] = new MemoryStream(File.ReadAllBytes(file.FullName));
                                memoryStreams[i].Position = 0;
                                count++;
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile(string.Format("Failed to load strokes on Slide {0}\n{1}", i, ex.ToString()), LogHelper.LogType.Error);
                            }
                        }
                        LogHelper.WriteLogToFile(string.Format("Loaded {0} saved strokes", count.ToString()));
                    }
                

                pointDesktop = new Point(ViewboxFloatingBar.Margin.Left, ViewboxFloatingBar.Margin.Top);
                pointPPT = new Point(-1, -1);

                StackPanelPPTControls.Visibility = Visibility.Visible;
                LeftPPTSwitchPanel.Visibility = Visibility.Visible;
                RightPPTSwitchPanel.Visibility = Visibility.Visible;
                BtnPPTSlideShow.Visibility = Visibility.Collapsed;
                BtnPPTSlideShowEnd.Visibility = Visibility.Visible;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 10);


                if (currentMode != 0)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;

                    ClearStrokes(true);


                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);



                ClearStrokes(true);

                BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                BorderPenColorRed_MouseUp(BorderPenColorRed, null);


                isEnteredSlideShowEndEvent = false;
                PptNavigationTextBlock.Text = $"{Wn.View.CurrentShowPosition}/{Wn.Presentation.Slides.Count}";
                LogHelper.NewLog("PowerPoint Slide Show Loading process complete");

                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                    });
                })).Start();
            });

        }

        bool isEnteredSlideShowEndEvent = false; //防止重复调用本函数导致墨迹保存失效
        private void PptApplication_SlideShowEnd(Presentation Pres)
        {
            LogHelper.WriteLogToFile(string.Format("PowerPoint Slide Show End"), LogHelper.LogType.Event);
            if (isEnteredSlideShowEndEvent)
            {
                LogHelper.WriteLogToFile("Detected previous entrance, returning");
                return;
            }
            isEnteredSlideShowEndEvent = true;
           
                string defaultFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Ink Canvas Strokes\Auto Saved\Presentations\";
                string folderPath = defaultFolderPath + Pres.Name + "_" + Pres.Slides.Count;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                File.WriteAllText(folderPath + "/Position", previousSlideID.ToString());
                for (int i = 1; i <= Pres.Slides.Count; i++)
                {
                    if (memoryStreams[i] != null)
                    {
                        try
                        {
                            if (memoryStreams[i].Length > 8)
                            {
                                byte[] srcBuf = new Byte[memoryStreams[i].Length];
                                //MessageBox.Show(memoryStreams[i].Length.ToString());
                                int byteLength = memoryStreams[i].Read(srcBuf, 0, srcBuf.Length);
                                File.WriteAllBytes(folderPath + @"\" + i.ToString("0000") + ".icstk", srcBuf);
                                LogHelper.WriteLogToFile(string.Format("Saved strokes for Slide {0}, size={1}, byteLength={2}", i.ToString(), memoryStreams[i].Length, byteLength));
                            }
                            else
                            {
                                File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile(string.Format("Failed to save strokes for Slide {0}\n{1}", i, ex.ToString()), LogHelper.LogType.Error);
                            File.Delete(folderPath + @"\" + i.ToString("0000") + ".icstk");
                        }
                    }
                }
            

            Application.Current.Dispatcher.Invoke(() =>
            {



                BtnPPTSlideShow.Visibility = Visibility.Visible;
                BtnPPTSlideShowEnd.Visibility = Visibility.Collapsed;
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                LeftPPTSwitchPanel.Visibility = Visibility.Collapsed;
                RightPPTSwitchPanel.Visibility = Visibility.Collapsed;
                ViewBoxStackPanelMain.Margin = new Thickness(10, 10, 10, 55);

                if (currentMode != 0)
                {
                    currentMode = 0;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;

                    ClearStrokes(true);


                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
     

                ClearStrokes(true);

                if (Main_Grid.Background != Brushes.Transparent)
                {
                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }

                if (pointDesktop != new Point(-1, -1))
                {
                    ViewboxFloatingBar.Margin = new Thickness(pointDesktop.X, pointDesktop.Y, -2000, -200);
                }
            });
        }

        int previousSlideID = 0;
        MemoryStream[] memoryStreams = new MemoryStream[50];

        private void PptApplication_SlideShowNextSlide(SlideShowWindow Wn)
        {
            LogHelper.WriteLogToFile(string.Format("PowerPoint Next Slide (Slide {0})", Wn.View.CurrentShowPosition), LogHelper.LogType.Event);
            if (Wn.View.CurrentShowPosition != previousSlideID)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[previousSlideID] = ms;

                 
                    _isPptClickingBtnTurned = false;

                    ClearStrokes(true);
                    timeMachine.ClearStrokeHistory();

                    try
                    {
                        if (memoryStreams[Wn.View.CurrentShowPosition] != null && memoryStreams[Wn.View.CurrentShowPosition].Length > 0)
                        {
                            inkCanvas.Strokes.Add(new StrokeCollection(memoryStreams[Wn.View.CurrentShowPosition]));
                        }
                    }
                    catch
                    { }

                    PptNavigationTextBlock.Text = $"{Wn.View.CurrentShowPosition}/{Wn.Presentation.Slides.Count}";
                });
                previousSlideID = Wn.View.CurrentShowPosition;

            }
        }

        private bool _isPptClickingBtnTurned = false;

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            

            _isPptClickingBtnTurned = true;
                  try
            {
                new Thread(new ThreadStart(() =>
                {
                    pptApplication.SlideShowWindows[1].Activate();
                    pptApplication.SlideShowWindows[1].View.Previous();
                })).Start();
            }
            catch
            {
                
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                LeftPPTSwitchPanel.Visibility = Visibility.Collapsed;
                RightPPTSwitchPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
           
            _isPptClickingBtnTurned = true;
              try
            {
                new Thread(new ThreadStart(() =>
                {
                    pptApplication.SlideShowWindows[1].Activate();
                    pptApplication.SlideShowWindows[1].View.Next();
                })).Start();
            }
            catch
            {
                StackPanelPPTControls.Visibility = Visibility.Collapsed;
                LeftPPTSwitchPanel.Visibility = Visibility.Collapsed;
                RightPPTSwitchPanel.Visibility = Visibility.Collapsed;
            }
        }


        private async void PPTNavigationBtn_Click(object sender, MouseButtonEventArgs e)
        {
            Main_Grid.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
            BtnHideInkCanvas_Click(sender, e);
            pptApplication.SlideShowWindows[1].SlideNavigation.Visible = true;
           
            if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                if (ViewboxFloatingBar.Margin == new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200))
                {
                    await Task.Delay(100);
                    ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                }
            }
        }

        private void BtnPPTSlideShow_Click(object sender, RoutedEventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    presentation.SlideShowSettings.Run();
                }
                catch { }
            })).Start();
        }

        private void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    MemoryStream ms = new MemoryStream();
                    inkCanvas.Strokes.Save(ms);
                    ms.Position = 0;
                    memoryStreams[pptApplication.SlideShowWindows[1].View.CurrentShowPosition] = ms;
                    timeMachine.ClearStrokeHistory();
                }
                catch { }
            });
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    pptApplication.SlideShowWindows[1].View.Exit();
                }
                catch { }
            })).Start();
        }

        #endregion

        #region Settings

      

        

        #region Appearance



        private void ToggleSwitchShowCursor_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.Canvas.IsShowCursor = ToggleSwitchShowCursor.IsOn;
            inkCanvas_EditingModeChanged(inkCanvas, null);

        }

        #endregion

        #region Canvas

       

       

     

        private void InkWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!isLoaded) return;

            drawingAttributes.Height = ((Slider)sender).Value / 2;
            drawingAttributes.Width = ((Slider)sender).Value / 2;

            Settings.Canvas.InkWidth = ((Slider)sender).Value / 2;

        }


        #endregion




       

       

        private void SCManipulationBoundaryFeedback(object sender, ManipulationBoundaryFeedbackEventArgs e)
        {
            e.Handled = true;
        }

       

        #endregion

        #region Left Side Panel


        private void Btn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isLoaded) return;
            try
            {
                if (((Button)sender).IsEnabled)
                {
                    ((UIElement)((Button)sender).Content).Opacity = 1;
                }
                else
                {
                    ((UIElement)((Button)sender).Content).Opacity = 0.25;
                }
            }
            catch { }
        }

        #endregion Left Side Panel

        #region Whiteboard Controls

        StrokeCollection[] strokeCollections = new StrokeCollection[101];
        bool[] whiteboadLastModeIsRedo = new bool[101];
        StrokeCollection lastTouchDownStrokeCollection = new StrokeCollection();

        int CurrentWhiteboardIndex = 1;
        int WhiteboardTotalCount = 1;
        TimeMachineHistory[][] TimeMachineHistories = new TimeMachineHistory[101][]; //最多99页，0用来存储非白板时的墨迹以便还原

        private void SaveStrokes(bool isBackupMain = false)
        {
            if (isBackupMain)
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[0] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();

            }
            else
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[CurrentWhiteboardIndex] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
        }

        private void ClearStrokes(bool isErasedByCode)
        {

            _currentCommitType = CommitReason.ClearingCanvas;
            if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
            inkCanvas.Strokes.Clear();
            _currentCommitType = CommitReason.UserInput;
        }

        private void RestoreStrokes(bool isBackupMain = false)
        {
            try
            {
                if (TimeMachineHistories[CurrentWhiteboardIndex] == null) return; //防止白板打开后不居中
                if (isBackupMain)
                {
                    _currentCommitType = CommitReason.CodeInput;
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[0]);
                    foreach (var item in TimeMachineHistories[0])
                    {
                        if (item.CommitType == TimeMachineHistoryType.UserInput)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Rotate)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Clear)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Add(currentStroke);
                                    }

                                }
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Remove(replacedStroke);
                                    }
                                }

                            }
                            else
                            {
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Add(replacedStroke);
                                    }
                                }
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Remove(currentStroke);
                                    }
                                }
                            }
                        }
                        _currentCommitType = CommitReason.UserInput;
                    }
                }
                else
                {
                    _currentCommitType = CommitReason.CodeInput;
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[CurrentWhiteboardIndex]);
                    foreach (var item in TimeMachineHistories[CurrentWhiteboardIndex])
                    {
                        if (item.CommitType == TimeMachineHistoryType.UserInput)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.ShapeRecognition)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Rotate)
                        {
                            if (item.StrokeHasBeenCleared)
                            {

                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                            }
                            else
                            {
                                foreach (var strokes in item.CurrentStroke)
                                {
                                    if (!inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Add(strokes);
                                }
                                foreach (var strokes in item.ReplacedStroke)
                                {
                                    if (inkCanvas.Strokes.Contains(strokes))
                                        inkCanvas.Strokes.Remove(strokes);
                                }
                            }
                        }
                        else if (item.CommitType == TimeMachineHistoryType.Clear)
                        {
                            if (!item.StrokeHasBeenCleared)
                            {
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Add(currentStroke);
                                    }

                                }
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Remove(replacedStroke);
                                    }
                                }

                            }
                            else
                            {
                                if (item.ReplacedStroke != null)
                                {
                                    foreach (var replacedStroke in item.ReplacedStroke)
                                    {
                                        if (!inkCanvas.Strokes.Contains(replacedStroke)) inkCanvas.Strokes.Add(replacedStroke);
                                    }
                                }
                                if (item.CurrentStroke != null)
                                {
                                    foreach (var currentStroke in item.CurrentStroke)
                                    {
                                        if (inkCanvas.Strokes.Contains(currentStroke)) inkCanvas.Strokes.Remove(currentStroke);
                                    }
                                }
                            }
                        }
                    }
                    _currentCommitType = CommitReason.UserInput;
                }
            }
            catch { }
        }



        #endregion Whiteboard Controls

        #region Theme 主题代码

        Color toolBarForegroundColor = Color.FromRgb(102, 102, 102);
        private void SetTheme(string theme)
        {
            
                ResourceDictionary rd1 = new ResourceDictionary() { Source = new Uri("Resources/Styles/Light.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd1);

                ResourceDictionary rd2 = new ResourceDictionary() { Source = new Uri("Resources/DrawShapeImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd2);

                ResourceDictionary rd3 = new ResourceDictionary() { Source = new Uri("Resources/SeewoImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd3);

                ResourceDictionary rd4 = new ResourceDictionary() { Source = new Uri("Resources/IconImageDictionary.xaml", UriKind.Relative) };
                Application.Current.Resources.MergedDictionaries.Add(rd4);

                ThemeManager.SetRequestedTheme(window, ElementTheme.Light);

                toolBarForegroundColor = (Color)Application.Current.FindResource("ToolBarForegroundColor");
         
           

        }

        private void SystemEvents_UserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            switch (Settings.Appearance.Theme)
            {
                case 0:
                    SetTheme("Light");
                    break;
                case 1:
                    SetTheme("Dark");
                    break;
                case 2:
                    if (IsSystemThemeLight()) SetTheme("Light");
                    else SetTheme("Dark");
                    break;
            }
        }

        private bool IsSystemThemeLight()
        {
            bool light = false;
            try
            {
                RegistryKey registryKey = Registry.CurrentUser;
                RegistryKey themeKey = registryKey.OpenSubKey("software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                int keyValue = 0;
                if (themeKey != null)
                {
                    keyValue = (int)themeKey.GetValue("SystemUsesLightTheme");
                }
                if (keyValue == 1) light = true;
            }
            catch { }
            return light;
        }
        #endregion

        #region Screenshot 截屏工作代码
        private void SymbolIconScreenshot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            bool isHideNotification = false;
            if (sender is bool) isHideNotification = (bool)sender;

            GridNotifications.Visibility = Visibility.Collapsed;

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(20);
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                            SaveScreenShot(isHideNotification, $"{pptName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                        else
                            SaveScreenShot(isHideNotification);
                    });
                }
                catch
                {
                    if (!isHideNotification)
                    {
                        ShowNotification("截图保存失败");
                    }
                }



                if (isHideNotification)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        forceEraser = false;
                        if (inkCanvas.Strokes.Count != 0)
                        {
                            int whiteboardIndex = CurrentWhiteboardIndex;
                            if (currentMode == 0)
                            {
                                whiteboardIndex = 0;
                            }
                            strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();

                        }

                        ClearStrokes(false);
                        inkCanvas.Children.Clear();
                    });
                }
            })).Start();
        }

        private void SaveScreenShot(bool isHideNotification, string fileName = null)
        {
            System.Drawing.Rectangle rc = System.Windows.Forms.SystemInformation.VirtualScreen;
            var bitmap = new System.Drawing.Bitmap(rc.Width, rc.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics memoryGrahics = System.Drawing.Graphics.FromImage(bitmap))
            {
                memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }


            bitmap.Save(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "/" + DateTime.Now.ToString("u").Replace(':', '-') + ".png", ImageFormat.Png);

            if (!isHideNotification)
            {
                ShowNotification("截图成功保存至 " + Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\" + DateTime.Now.ToString("u").Replace(':', '-') + ".png");


            }

        }

        #endregion

        #region Notification 信息弹窗控制代码

        int lastNotificationShowTime = 0;
        int notificationShowTime = 2500;

        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)?.ShowNotification(notice, isShowImmediately);
        }

        public void ShowNotification(string notice, bool isShowImmediately = true)
        {
            lastNotificationShowTime = Environment.TickCount;

            GridNotifications.Visibility = Visibility.Visible;
            //GridNotifications.Opacity = 1;
            TextBlockNotice.Text = notice;

            new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(notificationShowTime + 200);
                if (Environment.TickCount - lastNotificationShowTime >= notificationShowTime)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GridNotifications.Visibility = Visibility.Collapsed;
                        //DoubleAnimation daV = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.15)));
                        //GridNotifications.BeginAnimation(UIElement.OpacityProperty, daV);

                        //new Thread(new ThreadStart(() => {
                        //    Thread.Sleep(200);
                        //    Application.Current.Dispatcher.Invoke(() =>
                        //    {
                        //        if (GridNotifications.Opacity == 0)
                        //        {
                        //            GridNotifications.Visibility = Visibility.Collapsed;
                        //            GridNotifications.Opacity = 1;
                        //        }
                        //    });
                        //})).Start();
                    });
                }
            })).Start();
        }

        private void AppendNotification(string notice)
        {
            TextBlockNotice.Text = TextBlockNotice.Text + Environment.NewLine + notice;
        }

        #endregion

        #region Float Bar 浮动栏控制代码

        private void HideSubPanels()
        {
            BorderTools.Visibility = Visibility.Collapsed;
        }

        private void BorderPenColorBlack_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorBlack_Click(BtnColorBlack, null);
            HideSubPanels();
        }

        private void BorderPenColorRed_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorRed_Click(BtnColorRed, null);
            HideSubPanels();
        }

        private void BorderPenColorGreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorGreen_Click(BtnColorGreen, null);
            HideSubPanels();
        }

        private void BorderPenColorBlue_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorBlue_Click(BtnColorBlue, null);
            HideSubPanels();
        }

        private void BorderPenColorYellow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnColorYellow_Click(BtnColorYellow, null);
            HideSubPanels();
        }

        private void BorderPenColorWhite_MouseUp(object sender, MouseButtonEventArgs e)
        {
            inkCanvas.DefaultDrawingAttributes.Color = StringToColor("#FFFEFEFE");
            inkColor = 5;
            ColorSwitchCheck();
            HideSubPanels();
        }

       
        private async void SymbolIconCursor_Click(object sender, RoutedEventArgs e)
        {
            BtnHideInkCanvas_Click(BtnHideInkCanvas, null);

            if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                if (ViewboxFloatingBar.Margin == new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200))
                {
                    await Task.Delay(100);
                    ViewboxFloatingBar.Margin = new Thickness((SystemParameters.PrimaryScreenWidth - ViewboxFloatingBar.ActualWidth) / 2, SystemParameters.PrimaryScreenHeight - 60, -2000, -200);
                }
            }

        }

        private void SymbolIconDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {


            if (inkCanvas.Strokes.Count > 0)
            {
                if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                {
                    if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                        SaveScreenShot(true, $"{pptName}/{previousSlideID}_{DateTime.Now:HH-mm-ss}");
                    else
                        SaveScreenShot(true);
                }
                forceEraser = false;
                if (inkCanvas.Strokes.Count != 0)
                {
                    int whiteboardIndex = CurrentWhiteboardIndex;
                    if (currentMode == 0)
                    {
                        whiteboardIndex = 0;
                    }
                    strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();

                }

                ClearStrokes(false);
                inkCanvas.Children.Clear();
            }
          
        }



     

        Point pointDesktop = new Point(-1, -1); //用于记录上次进入PPT或白板时的坐标
        Point pointPPT = new Point(-1, -1); //用于记录上次在PPT中打开白板时的坐标


        private void ImageEraser_MouseUp(object sender, MouseButtonEventArgs e)
        {
            forceEraser = true;
            forcePointEraser = !forcePointEraser;

            inkCanvas.EditingMode =
                forcePointEraser ? InkCanvasEditingMode.EraseByPoint : InkCanvasEditingMode.EraseByStroke;
            inkCanvas.EraserShape = forcePointEraser ? new EllipseStylusShape(50, 50) : new EllipseStylusShape(5, 5);

            GeometryDrawingEraser.Brush = forcePointEraser
                ? new SolidColorBrush(Color.FromRgb(0x23, 0xA9, 0xF2))
                : new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            ImageEraser.Visibility = Visibility.Collapsed;
            inkCanvas_EditingModeChanged(inkCanvas, null);


            ViewboxBtnColorBlackContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorBlueContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorGreenContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorRedContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorYellowContent.Visibility = Visibility.Collapsed;
            ViewboxBtnColorWhiteContent.Visibility = Visibility.Collapsed;

            HideSubPanels();
        }



        private void SymbolIconTools_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (BorderTools.Visibility == Visibility.Visible)
            {
                BorderTools.Visibility = Visibility.Collapsed;
            }
            else
            {
                BorderTools.Visibility = Visibility.Visible;
            }
        }


        private void MoreInfo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            new ChangeLogWindow().ShowDialog();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        #region Drag

        bool isDragDropInEffect = false;
        Point pos = new Point();
        Point downPos = new Point();

        void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                FrameworkElement currEle = sender as FrameworkElement;
                double xPos = e.GetPosition(null).X - pos.X + currEle.Margin.Left;
                double yPos = e.GetPosition(null).Y - pos.Y + currEle.Margin.Top;
                currEle.Margin = new Thickness(xPos, yPos, 0, 0);
                pos = e.GetPosition(null);
            }
        }

        void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            FrameworkElement fEle = sender as FrameworkElement;
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            fEle.CaptureMouse();
            fEle.Cursor = Cursors.Hand;
        }

        void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragDropInEffect)
            {
                FrameworkElement ele = sender as FrameworkElement;
                isDragDropInEffect = false;
                ele.ReleaseMouseCapture();
            }
        }


        void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                double xPos = e.GetPosition(null).X - pos.X + ViewboxFloatingBar.Margin.Left;
                double yPos = e.GetPosition(null).Y - pos.Y + ViewboxFloatingBar.Margin.Top;
                ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);
                pos = e.GetPosition(null);
            }
        }

        void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            downPos = e.GetPosition(null);
            GridForFloatingBarDraging.Visibility = Visibility.Visible;

            SymbolIconEmoji.Symbol = ModernWpf.Controls.Symbol.Emoji;
        }

        void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = false;

            if (e is null || (downPos.X == e.GetPosition(null).X && downPos.Y == e.GetPosition(null).Y))
            {
                if (BorderFloatingBarMainControls.Visibility == Visibility.Visible)
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                }
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
            SymbolIconEmoji.Symbol = ModernWpf.Controls.Symbol.Emoji2;
        }

        #endregion


        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
        }

        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
        }

        private void ImagePPTControlEnd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlideShowEnd_Click(BtnPPTSlideShowEnd, null);
        }

        #endregion


    }

}
