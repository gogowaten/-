using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;//Imagingで使っている
using System.Windows.Interop;//CreateBitmapSourceFromHBitmapで使っている
//using System.Windows.Threading;//DispatcherTimerで使っている
using System.Runtime.Serialization;
using System.Xml;
using Microsoft.Win32;

namespace Gourenga
{
    public partial class MainWindow : Window
    {
        //アプリ情報表示用、名前、バージョン、実行ファイルパス
        private const string APP_NAME = "絶対画連合連画";
        private string AppVersion;
        private string AppDir;
        private const string APP_DATA_FILE_NAME = "AppData.xml";

        private ObservableCollection<ImageThumb> MyThumbs;

        private ImageThumb myActiveThumb;
        private ImageThumb MyActiveThumb
        {
            get => myActiveThumb;
            set
            {
                //古い方は枠表表示なし
                if (myActiveThumb != null)
                {
                    MyActiveThumb.MyStrokeRectangle.Visibility = Visibility.Collapsed;
                }

                myActiveThumb = value;

                //新しいものには枠表示する
                if (value != null)
                {
                    value.MyStrokeRectangle.Visibility = Visibility.Visible;
                    MyStatusItemSelectedImageSize.Content = $"サイズ(横 {value.MyBitmapSource.PixelWidth}, 縦 {value.MyBitmapSource.PixelHeight})";
                }
                else if (value == null)
                {
                    MyStatusItemSelectedImageSize.Content = $"サイズ(横 0, 縦 0)";
                }
                //初期Indexの変更
                OriginalIndex = MyThumbs.IndexOf(value);
            }
        }



        private List<Point> MyLocate = new();//座標リスト
        public Data MyData;//DataContextに指定する
        private string LastDirectory;//ドロップしたファイルのフォルダパス
        private string LastFileName;//ドロップしたファイル名
        private int LastFileExtensionIndex;//ドロップしたファイルの拡張子判別用インデックス
        private int OriginalIndex;//移動前のIndex
        public Preview MyPreviewWindow;

        public MainWindow()
        {
            InitializeComponent();
            MyInitialize();

        }



        //初期設定
        private void MyInitialize()
        {
#if DEBUG
            this.Left = 0;
            this.Top = 0;
            MyButtonTest.Visibility = Visibility.Visible;
#endif

            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);


            //アプリ情報収集
            string[] coms = Environment.GetCommandLineArgs();
            AppVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(coms[0]).FileVersion;
            this.Title = APP_NAME + AppVersion;
            AppDir = Environment.CurrentDirectory;

            //設定ファイル読み込み
            string filePath = System.IO.Path.Combine(AppDir, APP_DATA_FILE_NAME);
            if (System.IO.File.Exists(filePath))
            {
                MyData = LoadData(filePath);
            }
            else
            {
                MyData = new Data();
            }
            this.DataContext = MyData;
            MyThumbs = MyData.MyThumbs;

        }






        /// <summary>
        /// PixelFormatをBgar32固定、dpiは指定してファイルから画像読み込み
        /// </summary>
        /// <param name="filePath">フルパス</param>
        /// <param name="dpiX"></param>
        /// <param name="dpiY"></param>
        /// <returns></returns>
        private BitmapSource MakeBitmapSourceBgra32FromFile(string filePath, double dpiX = 96, double dpiY = 96)
        {
            BitmapSource source = null;
            try
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    source = BitmapFrame.Create(stream);
                    if (source.Format != PixelFormats.Bgra32)
                    {
                        source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
                    }
                    int w = source.PixelWidth;
                    int h = source.PixelHeight;
                    int stride = (w * source.Format.BitsPerPixel + 7) / 8;
                    var pixels = new byte[h * stride];
                    source.CopyPixels(pixels, stride, 0);
                    source = BitmapSource.Create(w, h, dpiX, dpiY, PixelFormats.Bgra32, source.Palette, pixels, stride);
                };
            }
            catch (Exception)
            { }
            return source;
        }

        //ファイルドロップ時
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) return;
            //ファイルパス取得
            var datas = (string[])e.Data.GetData(DataFormats.FileDrop);
            var paths = datas.ToList();
            paths.Sort();

            //画像ファイルからBitmapsource取得してImageThumb作成
            //Zオーダー指定
            for (int i = 0; i < paths.Count; i++)
            {
                AddImageThumb(MakeImage(MakeBitmapSourceBgra32FromFile(paths[i])));
            }
            Panel.SetZIndex(MyRectangle, MyThumbs.Count + 1);

            ChangeLocate();

            if (MyActiveThumb == null)
            {
                MyActiveThumb = MyThumbs[MyThumbs.Count - 1];
            }

            //最初のファイルのフォルダパス、ファイル名、拡張子を記録
            //これらは保存ダイアログ表示のときに使う
            LastDirectory = System.IO.Path.GetDirectoryName(paths[0]);
            LastFileName = System.IO.Path.GetFileNameWithoutExtension(paths[0]);
            SetLastExtentionIndex(paths[0]);


        }

        //ファイルドロップされたパスから拡張子取得して決められたIndexを記録
        //ファイル保存時に使う用
        private void SetLastExtentionIndex(string path)
        {
            string ext = System.IO.Path.GetExtension(path);
            switch (ext)
            {
                case ".png":
                case ".PNG":
                case ".Png":
                    LastFileExtensionIndex = 1;
                    break;
                case ".bmp":
                case ".Bmp":
                case ".BMP":
                    LastFileExtensionIndex = 3;
                    break;
                case ".jpg":
                case ".Jpg":
                case ".JPG":
                case ".jpeg":
                case ".JPEG":
                    LastFileExtensionIndex = 2;
                    break;
                case ".gif":
                case ".Gif":
                case ".GIF":
                    LastFileExtensionIndex = 4;
                    break;
                case ".tif":
                case ".Tif":
                case ".TIF":
                case ".tiff":
                case ".Tiff":
                case ".TIFF":
                    LastFileExtensionIndex = 5;
                    break;
                case ".hdp":
                case ".Hdp":
                case ".HDP":
                case ".wdp":
                case ".Wdp":
                case ".WDP":
                case ".jxr":
                case ".Jxr":
                case ".JXR":
                    LastFileExtensionIndex = 6;
                    break;
                default:
                    LastFileExtensionIndex = 1;
                    break;
            }
        }

        //ImageThumbを作成
        private Image MakeImage(BitmapSource source)
        {
            if (source == null) return null;

            Image img = new() { Source = source, StretchDirection = StretchDirection.DownOnly };
            img.Width = MyData.Size;
            img.Height = MyData.Size;
            return img;
        }

        //ImageThumb作成してリストとCanvasに追加
        private void AddImageThumb(Image img)
        {
            if (img == null) return;

            //作成、Zオーダー、サイズBinding、Canvasに追加、管理リストに追加、マウス移動イベント
            ImageThumb thumb = new(img);
            //枠表示用の
            thumb.GotFocus += Thumb_GotFocus;
            thumb.LostFocus += Thumb_LostFocus;
            thumb.PreviewKeyDown += Thumb_PreviewKeyDown;
            Panel.SetZIndex(thumb, MyThumbs.Count);

            SetThumbSizeBinding(thumb);

            MyCanvas.Children.Add(thumb);
            MyThumbs.Add(thumb);

            //ステータスバーの表示更新
            ChangedSaveImageSize();

            //マウスドラッグ移動
            //開始時
            thumb.DragStarted += (s, e) =>
            {
                //最上面表示、インデックス取得
                Panel.SetZIndex(thumb, MyThumbs.Count);

                thumb.Opacity = 0.5;
                MyActiveThumb = thumb;
                //今のIndexを記録
                OriginalIndex = MyThumbs.IndexOf(thumb);
            };

            //移動中
            thumb.DragDelta += Thumb_DragDelta;

            //終了後
            thumb.DragCompleted += (s, e) =>
            {
                //インデックス取得、インデックスに合わせたZオーダー
                thumb.Opacity = 1.0;
                int index = MyThumbs.IndexOf(thumb);
                Panel.SetZIndex(thumb, index);
                Canvas.SetLeft(thumb, MyLocate[index].X);
                Canvas.SetTop(thumb, MyLocate[index].Y);
                //ステータスバーの表示更新
                ChangedSaveImageSize();
            };

        }


        private void Thumb_LostFocus(object sender, RoutedEventArgs e)
        {
            var t = sender as ImageThumb;
            t.MyStrokeRectangle.Visibility = Visibility.Collapsed;
        }

        private void Thumb_GotFocus(object sender, RoutedEventArgs e)
        {
            var t = sender as ImageThumb;
            MyActiveThumb = t;
            t.MyStrokeRectangle.Visibility = Visibility.Visible;

        }


        //ImageThumbのサイズとUpDownとBinding
        private void SetThumbSizeBinding(ImageThumb t)
        {
            Binding b = new();
            b.Source = MyUpDownSize;
            b.Path = new PropertyPath(ControlLibraryCore20200620.NumericUpDown.MyValueProperty);
            t.MyImage.SetBinding(WidthProperty, b);
            t.MyImage.SetBinding(HeightProperty, b);
        }

        //座標リスト刷新、ImageThumb再配置、Canvasサイズ変更
        private void ChangeLocate()
        {
            if (MyThumbs == null) return;

            MyLocate.Clear();

            for (int i = 0; i < MyThumbs.Count; i++)
            {
                int x = i % MyData.Col * MyData.Size;
                int y = (int)((double)i / MyData.Col) * MyData.Size;
                MyLocate.Add(new Point(x, y));
            }

            SetLocate();
            SetMyCanvasSize();
        }

        /// <summary>
        /// すべてのThumbを再配置、移動中のThumbは変更しない
        /// </summary>
        /// <param name="avoidIndex">位置変更したくない移動中ThumbのIndex</param>
        private void SetLocate(int avoidIndex = -1)
        {
            for (int i = 0; i < avoidIndex; i++)
            {
                Canvas.SetLeft(MyThumbs[i], MyLocate[i].X);
                Canvas.SetTop(MyThumbs[i], MyLocate[i].Y);
            }
            for (int i = avoidIndex + 1; i < MyThumbs.Count; i++)
            {
                Canvas.SetLeft(MyThumbs[i], MyLocate[i].X);
                Canvas.SetTop(MyThumbs[i], MyLocate[i].Y);
            }
        }

        //MyCanvasのサイズ変更
        private void SetMyCanvasSize()
        {
            if (MyThumbs.Count == 0) return;


            int c = MyData.Col;
            int r = MyData.Row;
            int size = MyData.Size;
            int w = c * size;
            int h = r * size;

            int hh = (int)Math.Ceiling((double)MyThumbs.Count / c) * size;
            if (hh > h) h = hh;

            MyCanvas.Width = w;
            MyCanvas.Height = h;
        }


        #region ドラッグ移動系


        /// <summary>
        /// ドラッグ移動中のThumbとその他のThumbとの重なり合う部分の面積を計算、
        /// 一定以上の面積があった場合、場所を入れ替えて整列
        /// </summary>
        /// <param name="imaT">ドラッグ移動中のThumb</param>
        /// <param name="x">Canvas上でのX座標</param>
        /// <param name="y">Canvas上でのY座標</param>
        private void Idou移動中処理(ImageThumb imaT, double x, double y)
        {
            int imaIndex = MyThumbs.IndexOf(imaT);//ドラッグ移動中ThumbのIndex


            //最寄りのPoint
            int moyoriIndex = 0;
            double moyori距離 = double.MaxValue;
            for (int i = 0; i < MyLocate.Count; i++)
            {
                double distance = GetDistance(MyLocate[i], new Point(x, y));
                if (distance < moyori距離)
                {
                    moyori距離 = distance;
                    moyoriIndex = i;
                }
            }

            //最短距離のThumbIndexと移動中のThumbのIndexが違うなら入れ替え処理
            if (moyoriIndex != imaIndex)
            {
                //リスト内で移動
                MoveThumb(imaIndex, moyoriIndex, imaT);

                //indexに従って表示位置変更、ドラッグ移動中のThumbはそのままの位置を保つ
                SetLocate(moyoriIndex);
            }
        }


        /// <summary>
        /// Thumbの入れ替え、ListのIndexを入れ替え
        /// </summary>
        /// <param name="imaIndex">移動中ThumbのIndex</param>
        /// <param name="moyoriIndex">入れ替え先のIndex</param>
        /// <param name="imaT">移動中のThumb</param>
        private void MoveThumb(int imaIndex, int moyoriIndex, ImageThumb imaT)
        {
            //挿入モードのとき
            if (MyData.IsSwap == false)
            {
                MyThumbs.Move(imaIndex, moyoriIndex);
            }
            //入れ替えモードのとき
            else
            {
                ImageThumb moyoriT = MyThumbs[moyoriIndex];
                ImageThumb originT = MyThumbs[OriginalIndex];

                //移動開始地点のThumbIndexと今移動中のThumbIndexが同じ時
                if (OriginalIndex == imaIndex)
                {
                    //今移動中と最寄りのThumbを入れ替え
                    MyThumbs.Move(MyThumbs.IndexOf(imaT), moyoriIndex);
                    MyThumbs.Move(MyThumbs.IndexOf(moyoriT), OriginalIndex);
                }

                //違うとき
                else
                {
                    //最寄りのThumbIndexと移動開始時のIndexが同じ時
                    if (moyoriIndex == OriginalIndex)
                    {
                        //移動開始地点と今移動中のThumbを入れ替え
                        MyThumbs.Move(MyThumbs.IndexOf(originT), imaIndex);
                        MyThumbs.Move(MyThumbs.IndexOf(imaT), OriginalIndex);
                    }

                    //違うとき
                    else
                    {
                        //移動開始地点と今移動中のThumbを入れ替え
                        MyThumbs.Move(MyThumbs.IndexOf(originT), imaIndex);
                        MyThumbs.Move(MyThumbs.IndexOf(imaT), OriginalIndex);
                        //今移動中と最寄りのThumbを入れ替え
                        MyThumbs.Move(MyThumbs.IndexOf(imaT), moyoriIndex);
                        MyThumbs.Move(MyThumbs.IndexOf(moyoriT), OriginalIndex);
                    }
                }
            }
        }


        //ドラッグ移動イベント時
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            //移動
            ImageThumb t = sender as ImageThumb;
            double x = Canvas.GetLeft(t) + e.HorizontalChange;
            double y = Canvas.GetTop(t) + e.VerticalChange;
            Canvas.SetLeft(t, x);
            Canvas.SetTop(t, y);


            //入れ替え発生判定と入れ替え
            Idou移動中処理(t, x, y);
        }

        //2点間距離
        private double GetDistance(Point a, Point b)
        {
            return Math.Sqrt((a - b) * (a - b));
        }
        #endregion ドラッグ移動系





        #region 保存

        //描画サイズと座標の計算
        //E:\オレ\エクセル\作りたいアプリのメモ.xlsm_2021031_$A$214
        //左上から右下へ並べる
        //アスペクト比は元画像から変更しない、サイズは横幅を基準に縦幅を変更
        //行の高さはその行の最大縦幅にする(上下のムダな領域を消す)


        //指定画像の縮小率を計算
        private double GetScaleRatio(double w, double h, double size)
        {
            //縮小サイズ決定、指定サイズの正方形に収まるようにアスペクト比保持で縮小
            //縦横それぞれの縮小率計算して小さい方に合わせる
            //拡大はしないので縮小率が1以上なら1に抑える
            double widthRatio = size / w;
            double heightRatio = size / h;
            //double ratio = Math.Max(widthRatio, heightRatio);
            double ratio = Math.Min(widthRatio, heightRatio);
            if (ratio > 1) ratio = 1;
            return ratio;
        }


        //指定画像の縮小率を計算
        private double GetScaleRatio(double sizeW, double sizeH, double bmpW, double bmpH)
        {
            //縮小サイズ決定、アスペクト比保持で縮小するため縦横どちらかの比率にする
            //縦横それぞれの縮小率計算して小さい方に合わせる
            //拡大はしないので縮小率が1以上なら1に抑える
            double widthRatio = sizeW / bmpW;
            double heightRatio = sizeH / bmpH;
            //double ratio = Math.Max(widthRatio, heightRatio);
            double ratio = Math.Min(widthRatio, heightRatio);
            if (ratio > 1) ratio = 1;
            return ratio;
        }

        //保存サイズ決め打ちの場合
        private List<Rect> MakeRectsForOverallType()
        {
            //保存対象画像数と実際の縦横個数を取得
            (int imageCount, int areaRows, int areaCols) = GetMakeRectParam();

            List<Rect> drawRects = new();

            //サイズとX座標
            //各画像の縮小サイズを計算
            double wOne = MyData.SaveWidth / areaCols;
            double hOne = MyData.SaveHeight / areaRows;

            for (int iRow = 0; iRow < MyData.Row; iRow++)
            {
                for (int iCol = 0; iCol < areaCols; iCol++)
                {
                    int index = iRow * MyData.Col + iCol;
                    if (imageCount <= index) break;

                    double w = MyThumbs[index].MyBitmapSource.PixelWidth;
                    double h = MyThumbs[index].MyBitmapSource.PixelHeight;
                    double ratio = GetScaleRatio(wOne, hOne, w, h);
                    w *= ratio;
                    h *= ratio;
                    double x = iCol * wOne;
                    x += (wOne - w) / 2;
                    x = (int)x;//小数切り捨て
                    double y = iRow * hOne;
                    y += (hOne - h) / 2;
                    y = (int)y;//小数切り捨て
                    drawRects.Add(new Rect(x, y, w, h));
                }
            }

            return drawRects;
        }

        //保存サイズ決め打ち + 余白付加の場合
        private List<Rect> MakeRectsForOverallTypeWithMargin()
        {
            //保存対象画像数と実際の縦横個数を取得
            (int imageCount, int areaRows, int areaCols) = GetMakeRectParam();

            List<Rect> drawRects = new();

            //サイズとX座標
            //各画像の縮小サイズを計算
            int margin = MyData.Margin;
            double wMargin = (areaCols + 1) * margin;
            double hMargin = (areaRows + 1) * margin;
            double wOne = (MyData.SaveWidth - wMargin) / areaCols;
            double hOne = (MyData.SaveHeight - hMargin) / areaRows;

            for (int y = 0; y < MyData.Row; y++)
            {
                for (int x = 0; x < areaCols; x++)
                {
                    int i = y * MyData.Col + x;
                    if (imageCount <= i) break;
                    BitmapSource bmp = MyThumbs[i].MyBitmapSource;
                    double ratio = GetScaleRatio(wOne, hOne, bmp.PixelWidth, bmp.PixelHeight);
                    double w = bmp.PixelWidth * ratio;
                    double h = bmp.PixelHeight * ratio;

                    double xx = margin + (x * (wOne + margin));
                    xx += (wOne - w) / 2;
                    xx = (int)xx;//小数切り捨て
                    double yy = margin + (y * (margin + hOne));
                    yy += (hOne - h) / 2;
                    yy = (int)yy;//小数切り捨て
                    drawRects.Add(new Rect(xx, yy, w, h));
                }
            }

            return drawRects;
        }

        //
        /// <summary>
        /// Rect作成に必要な要素を返す
        /// </summary>
        /// <returns></returns>
        private (int imageCount, int areaRows, int areaCols) GetMakeRectParam()
        {
            if (MyData == null)
            {
                return (0, 0, 0);
            }
            //保存対象画像数と横に並べる個数
            int imageCount = MyData.Row * MyData.Col;
            int areaRows = MyData.Row;
            int areaCols = MyData.Col;
            //連結範囲のマス数よりよりThumb数が少なければ枠を縮める必要がある
            if (MyData.Row * MyData.Col > MyThumbs.Count)
            {
                imageCount = MyThumbs.Count;
                areaRows = (int)Math.Ceiling((double)MyThumbs.Count / MyData.Col);
                if (MyData.Col > MyThumbs.Count) areaCols = MyThumbs.Count;
            }
            return (imageCount, areaRows, areaCols);
        }


        private List<Rect> MakeRectsForSpesial(int margin)
        {
            //保存対象画像数と実際の縦横個数を取得
            (int imageCount, int areaRows, int areaCols) = GetMakeRectParam();

            List<Rect> drawRects = new();

            for (int y = 0; y < areaRows; y++)
            {
                for (int x = 0; x < areaCols; x++)
                {
                    if (imageCount <= y * areaCols + x) break;
                    int xx = margin + (x * 640) + (x * margin);
                    int yy = margin + (y * 480) + (y * margin);
                    drawRects.Add(new Rect(xx, yy, 640, 480));
                }
            }
            return drawRects;
        }

        //保存サイズは1画像の横幅を基準にする場合
        private List<Rect> MakeRectsForWidthType()
        {
            //保存対象画像数と実際の縦横個数を取得
            (int imageCount, int areaRows, int areaCols) = GetMakeRectParam();

            List<Rect> drawRects = new();

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            double pieceW = MyData.SaveOneWidth;
            switch (MyData.SaveScaleSizeType)
            {
                case SaveScaleSizeType.OneWidth:
                    pieceW = MyData.SaveOneWidth;
                    break;
                case SaveScaleSizeType.TopLeft:
                    pieceW = MyThumbs[0].MyBitmapSource.PixelWidth;
                    break;
                default:
                    break;
            }

            double yKijun = 0;
            List<Rect> tempRect = new();
            for (int iRow = 0; iRow < areaRows; iRow++)
            {
                double maxH = 0;
                tempRect.Clear();
                for (int iCol = 0; iCol < areaCols; iCol++)
                {
                    int index = iRow * MyData.Col + iCol;
                    if (imageCount <= index) break;

                    double w = MyThumbs[index].MyBitmapSource.PixelWidth;
                    double h = MyThumbs[index].MyBitmapSource.PixelHeight;
                    //縮尺取得
                    double ratio = GetScaleRatio(w, h, pieceW);
                    //ratio = 1;
                    w *= ratio;
                    h *= ratio;
                    if (h > maxH) maxH = h;
                    double x = iCol * pieceW;
                    x += (pieceW - w) / 2;
                    x = (int)x;//小数切り捨て
                    w = (int)(w + 0.5);//四捨五入
                    tempRect.Add(new Rect(x, 0, w, h));
                }
                for (int i = 0; i < tempRect.Count; i++)
                {
                    Rect r = tempRect[i];
                    double y = yKijun + (maxH - r.Height) / 2;
                    y = (int)y;//小数切り捨て
                    int h = (int)(r.Height + 0.5);//四捨五入
                    drawRects.Add(new Rect(r.X, y, r.Width, h));
                }
                yKijun += maxH;
            }

            return drawRects;
        }

        //保存サイズは1画像の横幅を基準にする場合、サイズは四捨五入で整数にする
        private List<Rect> MakeRectsForWidthTypeWithMargin()
        {
            //保存対象画像数と実際の縦横個数を取得
            (int imageCount, int areaRows, int areaCols) = GetMakeRectParam();

            List<Rect> drawRects = new();

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            double pieceW = MyData.SaveOneWidth;
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.TopLeft)
            {
                pieceW = MyThumbs[0].MyBitmapSource.PixelWidth;
            }

            double yKijun = 0;
            int margin = MyData.Margin;
            List<Rect> tempRect = new();
            for (int y = 0; y < areaRows; y++)
            {
                double maxH = 0;
                tempRect.Clear();
                for (int x = 0; x < areaCols; x++)
                {
                    int index = y * MyData.Col + x;
                    if (imageCount <= index) break;

                    double w = MyThumbs[index].MyBitmapSource.PixelWidth;
                    double h = MyThumbs[index].MyBitmapSource.PixelHeight;
                    //縮尺取得
                    double ratio = GetScaleRatio(w, h, pieceW);
                    w *= ratio;
                    h *= ratio;
                    if (h > maxH) maxH = h;
                    double xx = margin + (x * (pieceW + margin));
                    xx += (pieceW - w) / 2;
                    xx = (int)xx;//小数切り捨て
                    w = (int)(w + 0.5);//四捨五入
                    tempRect.Add(new Rect(xx, 0, w, h));
                }
                for (int i = 0; i < tempRect.Count; i++)
                {
                    Rect r = tempRect[i];
                    double yy = margin + yKijun + (maxH - r.Height) / 2;
                    yy = (int)yy;//小数切り捨て
                    int h = (int)(r.Height + 0.5);//四捨五入
                    drawRects.Add(new Rect(r.X, yy, r.Width, h));
                }
                yKijun += maxH + margin;
            }

            return drawRects;
        }



        //日時をstringで取得
        private string MakeFullDateString()
        {
            DateTime time = DateTime.Now;
            string str = time.ToString("yyyyMMdd_HHmmssfff");
            return str;
        }
        //日時をstringで取得
        private string MakeTimeString()
        {
            DateTime time = DateTime.Now;
            string str = time.ToString("HH:mm:ss.fff");
            return str;
        }

        //日時のファイル名作成＋PNG拡張子
        private string MakeSavePath()
        {
            DateTime time = DateTime.Now;
            string ts = time.ToString("yyyyMMdd_HHmmssfff");
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            path = System.IO.Path.Combine(path, ts);
            path += ".png";
            return path;
        }



        //保存画像作成        
        //DrawingContextに描画座標とサイズに従って1枚ずつ描画して
        //RenderTargetBitmap作成
        private BitmapSource MakeSaveBitmap()
        {
            if (MyThumbs.Count == 0) return null;
            //List<Rect> drawRects = MyData.SaveScaleSizeType switch
            //{
            //    SaveScaleSizeType.OneWidth => MakeRectsForWidthType(),
            //    SaveScaleSizeType.Overall => MakeRectsForOverallType(),
            //    SaveScaleSizeType.MatchTopLeftImage => MakeRectsForWidthType(),
            //    _ => MakeRectsForOverallType(),
            //};

            List<Rect> drawRects;
            switch (MyData.SaveScaleSizeType, MyData.IsMargin)
            {
                case (SaveScaleSizeType.All, true):
                    drawRects = MakeRectsForOverallTypeWithMargin();
                    break;
                case (SaveScaleSizeType.All, false):
                    drawRects = MakeRectsForOverallType();
                    break;

                case (SaveScaleSizeType.OneWidth, true):
                    drawRects = MakeRectsForWidthTypeWithMargin();
                    break;
                case (SaveScaleSizeType.OneWidth, false):
                    drawRects = MakeRectsForWidthType();
                    break;

                case (SaveScaleSizeType.TopLeft, true):
                    drawRects = MakeRectsForWidthTypeWithMargin();
                    break;
                case (SaveScaleSizeType.TopLeft, false):
                    drawRects = MakeRectsForWidthType();
                    break;

                case (SaveScaleSizeType.Special, true):
                    drawRects = MakeRectsForSpesial(MyData.Margin);
                    break;
                case (SaveScaleSizeType.Special, false):
                    drawRects = MakeRectsForSpesial(0);
                    break;

                default:
                    drawRects = MakeRectsForOverallType();
                    break;
            }
            //最終的な全体画像サイズ取得
            Size renderBitmapSize = GetRenderSize(drawRects);
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.All)
            {
                renderBitmapSize.Width = MyData.SaveWidth;
                renderBitmapSize.Height = MyData.SaveHeight;
            }

            DrawingVisual dv = new();
            using (DrawingContext dc = dv.RenderOpen())
            {
                //隙間を白で塗る(下地を白で塗りつぶしておく)
                if (MyData.IsSaveBackgroundWhite || MyData.IsMargin)
                {
                    dc.DrawRectangle(
                        Brushes.White, null,
                        new Rect(0, 0,
                        (int)(renderBitmapSize.Width + 0.5),
                        (int)(renderBitmapSize.Height + 0.5)));
                }
                //各画描画
                for (int i = 0; i < drawRects.Count; i++)
                {
                    int w = (int)Math.Ceiling(drawRects[i].Width);
                    int h = (int)Math.Ceiling(drawRects[i].Height);
                    BitmapSource source;
                    //縮小処理
                    if (MyThumbs[i].MyBitmapSource.PixelWidth > drawRects[i].Width)
                    {
                        //バイキュービックで補完
                        source = BicubicBgra32Ex(MyThumbs[i].MyBitmapSource, w, h, -0.5);
                        //ランチョス法で補完
                        //source = LanczosBgra32Ex(MyThumbs[i].MyBitmapSource, w, h, 4);
                        //source = LanczosBgra32TypeX(MyThumbs[i].MyBitmapSource, w, h);
                        //窓関数で補完
                        //source = LanczosBgra32(MyThumbs[i].MyBitmapSource, w, h);
                    }
                    else if (MyData.SaveScaleSizeType == SaveScaleSizeType.Special)
                    {
                        //PS1スクショ専用、640x480にきれいにリサイズ
                        source = BitmapPS1(MyThumbs[i].MyBitmapSource);
                    }
                    //縮小処理なし、そのまま描画
                    else
                    {
                        source = MyThumbs[i].MyBitmapSource;
                    }
                    dc.DrawImage(source, drawRects[i]);
                }
            }
            //var neko = dv.ContentBounds;

            //Bitmap作成
            RenderTargetBitmap renderBitmap;
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.OneWidth ||
                MyData.SaveScaleSizeType == SaveScaleSizeType.TopLeft)
            {
                //サイズは四捨五入
                int width = (int)(renderBitmapSize.Width + 0.5);
                int height = (int)(renderBitmapSize.Height + 0.5);
                renderBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(dv);
            }
            else if (MyData.SaveScaleSizeType == SaveScaleSizeType.All)
            {
                //決め打ちサイズ
                renderBitmap = new(MyData.SaveWidth, MyData.SaveHeight, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(dv);
            }
            else if (MyData.SaveScaleSizeType == SaveScaleSizeType.Special)
            {
                renderBitmap = new((int)renderBitmapSize.Width, (int)renderBitmapSize.Height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(dv);
            }
            else
            {
                renderBitmap = null;
            }
            return renderBitmap;
        }

        //描画サイズ取得
        //最終的な全体画像サイズ計算、RectのUnionを使う
        private Size GetRenderSize(List<Rect> drawRects)
        {
            Rect dRect = new();
            for (int i = 0; i < drawRects.Count; i++)
            {
                dRect = Rect.Union(dRect, drawRects[i]);
            }

            //横幅は指定サイズ*横個数
            var temp = GetMakeRectParam();
            int margin = MyData.Margin;
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.OneWidth)
            {
                dRect.Width = MyData.SaveOneWidth * temp.areaCols;
                //余白分を足す
                if (MyData.IsMargin)
                {
                    dRect.Width += (temp.areaCols + 1) * margin;
                    dRect.Height += margin;
                }
            }
            else if (MyData.SaveScaleSizeType == SaveScaleSizeType.TopLeft && MyData.IsMargin
                || MyData.SaveScaleSizeType == SaveScaleSizeType.Special && MyData.IsMargin)
            {
                dRect.Width += margin;
                dRect.Height += margin;
            }
            return dRect.Size;
        }

        //初期ファイル名を更新する
        //重複していたら "_" を末尾に追加する
        private void SetDefaultFileName(string extensionFilter)
        {
            var extensions = extensionFilter.Split("|");
            extensions = extensions.Select(x => x.Replace("*", "")).ToArray();
            int i = LastFileExtensionIndex * 2 - 2;

            string path = System.IO.Path.Combine(LastDirectory, LastFileName) + extensions[i];
            int count = 0;
            while (System.IO.File.Exists(path) && count < 100)
            {
                LastFileName += "_";
                path = System.IO.Path.Combine(LastDirectory, LastFileName) + extensions[i];
                count++;
            }
        }
        private void SaveImage()
        {
            //サイズが一定以上になる場合は確認
            Size size = GetSaveBitmapSize();
            if (size.Width > 10000 || size.Height > 10000)
            {
                string str = $"保存画像サイズが横縦({size})と大きい、保存する？\nはい : 処理続行\nいいえ : 中止";
                if (MessageBox.Show($"{str}", "確認", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    return;
                }
            }

            //初期ファイル名更新
            string extensionFilter = "*.png|*.png|*.jpg|*.jpg|*.bmp|*.bmp|*.gif|*.gif|*.tiff|*.tiff|*.wdp|*.wdp;*jxr";
            SetDefaultFileName(extensionFilter);

            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Filter = extensionFilter,
                AddExtension = true,//ファイル名に拡張子追加

                //初期フォルダ指定、開いている画像と同じフォルダ
                InitialDirectory = LastDirectory,
                FileName = LastFileName,
                //saveFileDialog.FileName = MakeDateString();
                FilterIndex = LastFileExtensionIndex
            };


            if (saveFileDialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = MakeBitmapEncoder(saveFileDialog.FilterIndex);
                encoder.Frames.Add(BitmapFrame.Create(MakeSaveBitmap(), null, MakeMetadata(encoder), null));
                using var fs = new System.IO.FileStream(
                    saveFileDialog.FileName,
                    System.IO.FileMode.Create,
                    System.IO.FileAccess.Write);
                //保存
                encoder.Save(fs);
                //保存フォルダのパスと拡張子を記録
                LastDirectory = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                LastFileExtensionIndex = saveFileDialog.FilterIndex;
                if (MyData.IsSavedBitmapRemove)
                {
                    RemoveAreaThumb();
                }

                RenewStatusProcessed($"{System.IO.Path.GetFileName(saveFileDialog.FileName)}を保存した");
            }
        }
        private void RenewStatusProcessed(string message)
        {
            MyStatusItemPrecessed.Content = MakeTimeString() + " " + message;
        }
        private BitmapEncoder MakeBitmapEncoder(int filterIndex)
        {
            BitmapEncoder encoder = null;
            switch (filterIndex)
            {
                case 1:
                    encoder = new PngBitmapEncoder();
                    break;
                case 2:
                    JpegBitmapEncoder j = new();
                    j.QualityLevel = MyData.JpegQuality;
                    encoder = j;
                    break;
                case 3:
                    encoder = new BmpBitmapEncoder();
                    break;
                case 4:
                    encoder = new GifBitmapEncoder();
                    break;
                case 5:
                    encoder = new TiffBitmapEncoder();
                    break;
                case 6:
                    //wmpはロスレス指定、じゃないと1bppで保存時に画像が崩れるしファイルサイズも大きくなる
                    WmpBitmapEncoder wmp = new();
                    wmp.ImageQualityLevel = 1.0f;
                    encoder = wmp;
                    break;
                default:
                    break;
            }
            return encoder;
        }

        //メタデータ作成
        private BitmapMetadata MakeMetadata(BitmapEncoder encoder)
        {
            BitmapMetadata data = null;
            string software = APP_NAME + "_" + AppVersion;
            switch (encoder.CodecInfo.FriendlyName)
            {
                case "BMP Encoder":
                    break;
                case "PNG Encoder":
                    data = new BitmapMetadata("png");
                    data.SetQuery("/tEXt/Software", software);
                    break;
                case "JPEG Encoder":
                    data = new BitmapMetadata("jpg");
                    data.SetQuery("/app1/ifd/{ushort=305}", software);
                    break;
                case "GIF Encoder":
                    data = new BitmapMetadata("Gif");
                    data.SetQuery("/XMP/XMP:CreatorTool", software);
                    break;
                case "TIFF Encoder":
                    data = new BitmapMetadata("tiff")
                    {
                        ApplicationName = software
                    };
                    break;
                case "WMPhoto Encoder":

                    break;
                default:
                    break;
            }

            return data;
        }

        #endregion 保存

        #region 特殊保存PS1スクショ用

        //窓関数
        private double SincA(double d)
        {
            if (d == 0) return 1.0;
            return Math.Sin(Math.PI * d) / (Math.PI * d);
        }


        //これのn=4が一番キレイに見える
        /// <summary>
        /// 画像のリサイズ、窓関数で補完、PixelFormats.Bgra32専用)
        /// 高速化なし
        /// </summary>
        /// <param name="source">PixelFormats.Bgra32のBitmap</param>
        /// <param name="width">変換後の横ピクセル数を指定</param>
        /// <param name="height">変換後の縦ピクセル数を指定</param>
        /// <param name="n">最大参照距離、3か4がいい</param>
        /// <returns></returns>
        private BitmapSource SincBgra32(BitmapSource source, int width, int height, int n)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;
            int sourceStride = sourceWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[sourceHeight * sourceStride];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            //変換後の画像の画素値の配列用
            double widthScale = (double)sourceWidth / width;//横倍率(逆倍率)
            double heightScale = (double)sourceHeight / height;
            int stride = width * pByte;
            byte[] pixels = new byte[height * stride];

            //倍率
            double scale = width / (double)sourceWidth;
            //逆倍率
            double inScale = widthScale;
            //最大参照距離 = 逆倍率 * n
            double limitD = widthScale * n;
            //実際の参照距離、は指定距離*逆倍率の切り上げにしたけど、切り捨てでも見た目の変化なし            
            int actD = (int)Math.Ceiling(limitD);
            //int actD = (int)(limitD);


            //拡大時の調整(これがないと縮小専用)
            if (1.0 < scale)
            {
                scale = 1.0;//重み計算に使う、拡大時は1固定
                actD = n;//拡大時の実際の参照距離は指定距離と同じ
                inScale = 1.0;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //参照点
                    double rx = (x + 0.5) * widthScale;
                    double ry = (y + 0.5) * heightScale;
                    //参照点四捨五入で基準
                    int xKijun = (int)(rx + 0.5);
                    int yKijun = (int)(ry + 0.5);
                    //修正した重み取得
                    double[,] ws = GetFixWeights(rx, ry, actD, inScale);

                    double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                    double alphaFix = 0;
                    //参照範囲は基準から上(xは左)へnn、下(xは右)へnn-1の範囲
                    for (int yy = -actD; yy < actD; yy++)
                    {
                        int yc = yKijun + yy;
                        //マイナス座標や画像サイズを超えていたら、収まるように修正
                        yc = yc < 0 ? 0 : yc > sourceHeight - 1 ? sourceHeight - 1 : yc;
                        for (int xx = -actD; xx < actD; xx++)
                        {
                            int xc = xKijun + xx;
                            xc = xc < 0 ? 0 : xc > sourceWidth - 1 ? sourceWidth - 1 : xc;
                            int pp = (yc * sourceStride) + (xc * pByte);
                            double weight = ws[xx + actD, yy + actD];
                            //完全透明ピクセル(a=0)だった場合はRGBは計算しないで
                            //重みだけ足し算して後で使う
                            if (sourcePixels[pp + 3] == 0)
                            {
                                alphaFix += weight;
                                continue;
                            }
                            bSum += sourcePixels[pp] * weight;
                            gSum += sourcePixels[pp + 1] * weight;
                            rSum += sourcePixels[pp + 2] * weight;
                            aSum += sourcePixels[pp + 3] * weight;
                        }
                    }

                    //                    C#、WPF、バイリニア法での画像の拡大縮小変換、半透明画像(32bit画像)対応版 - 午後わてんのブログ
                    //https://gogowaten.hatenablog.com/entry/2021/04/17/151803#32bit%E3%81%A824bit%E3%81%AF%E9%81%95%E3%81%A3%E3%81%9F
                    //完全透明ピクセルによるRGB値の修正
                    //参照範囲がすべて完全透明だった場合は0のままでいいので計算しない
                    if (alphaFix == 1) continue;
                    //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                    double rgbFix = 1 / (1 - alphaFix);
                    bSum *= rgbFix;
                    gSum *= rgbFix;
                    rSum *= rgbFix;

                    //0～255の範囲を超えることがあるので、修正
                    bSum = bSum < 0 ? 0 : bSum > 255 ? 255 : bSum;
                    gSum = gSum < 0 ? 0 : gSum > 255 ? 255 : gSum;
                    rSum = rSum < 0 ? 0 : rSum > 255 ? 255 : rSum;
                    aSum = aSum < 0 ? 0 : aSum > 255 ? 255 : aSum;

                    int ap = (y * stride) + (x * pByte);
                    pixels[ap] = (byte)(bSum + 0.5);
                    pixels[ap + 1] = (byte)(gSum + 0.5);
                    pixels[ap + 2] = (byte)(rSum + 0.5);
                    pixels[ap + 3] = (byte)(aSum + 0.5);
                }
            }

            //_ = Parallel.For(0, height, y =>
            //  {

            //  });

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, source.Format, null, pixels, stride);
            return bitmap;

            //修正した重み取得
            double[,] GetFixWeights(double rx, double ry, int actN, double inScale)
            {
                //全体の参照距離
                int nn = actN * 2;
                //基準になる距離計算
                double sx = rx - (int)rx;
                double sy = ry - (int)ry;
                double dx = (sx < 0.5) ? 0.5 - sx : 0.5 - sx + 1;
                double dy = (sy < 0.5) ? 0.5 - sy : 0.5 - sy + 1;

                //各ピクセルの重みと、重み合計を計算
                double[] xw = new double[nn];
                double[] yw = new double[nn];
                double xSum = 0, ySum = 0;
                for (int i = -actN; i < actN; i++)
                {
                    //距離に倍率を掛け算したのをLanczosで重み計算
                    double x = SincA(Math.Abs(dx + i) * inScale);
                    xSum += x;
                    xw[i + actN] = x;
                    double y = SincA(Math.Abs(dy + i) * inScale);
                    ySum += y;
                    yw[i + actN] = y;
                }

                //重み合計で割り算して修正、全体で100%(1.0)にする
                for (int i = 0; i < nn; i++)
                {
                    xw[i] /= xSum;
                    yw[i] /= ySum;
                }

                // x * y
                double[,] ws = new double[nn, nn];
                for (int y = 0; y < nn; y++)
                {
                    for (int x = 0; x < nn; x++)
                    {
                        ws[x, y] = xw[x] * yw[y];
                    }
                }
                return ws;
            }
        }



        /// <summary>
        /// 画像のリサイズ、窓関数で補完、PixelFormats.Bgra32専用)
        /// 高速化なし
        /// </summary>
        /// <param name="source">PixelFormats.Bgra32のBitmap</param>
        /// <param name="width">変換後の横ピクセル数を指定</param>
        /// <param name="height">変換後の縦ピクセル数を指定</param>
        /// <param name="n">最大参照距離、3か4がいい</param>
        /// <returns></returns>
        private BitmapSource SincTypeGBgra32(BitmapSource source, int width, int height, int n)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;
            int sourceStride = sourceWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[sourceHeight * sourceStride];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            //変換後の画像の画素値の配列用
            double widthScale = (double)sourceWidth / width;//横倍率(逆倍率)
            double heightScale = (double)sourceHeight / height;
            int stride = width * pByte;
            byte[] pixels = new byte[height * stride];

            //倍率
            double scale = width / (double)sourceWidth;
            //逆倍率
            double inScale = widthScale;
            //最大参照距離 = 逆倍率 * n
            double limitD = widthScale * n;
            //実際の参照距離、は指定距離*逆倍率の切り上げにしたけど、切り捨てでも見た目の変化なし            
            int actD = (int)Math.Ceiling(limitD);
            //int actD = (int)(limitD);


            //拡大時の調整(これがないと縮小専用)
            if (1.0 < scale)
            {
                scale = 1.0;//重み計算に使うようで、拡大時は1固定
                actD = n;//拡大時の実際の参照距離は指定距離と同じ
                inScale = 1.0;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //参照点
                    double rx = (x + 0.5) * widthScale;
                    double ry = (y + 0.5) * heightScale;
                    //参照点四捨五入で基準
                    int xKijun = (int)(rx + 0.5);
                    int yKijun = (int)(ry + 0.5);
                    //修正した重み取得
                    //double[,] ws = GetFixWeights(rx, ry, actN, scale*0.1, n);
                    double[,] ws = GetFixWeights(rx, ry, actD, n);

                    double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                    double alphaFix = 0;
                    //参照範囲は基準から上(xは左)へnn、下(xは右)へnn-1の範囲
                    for (int yy = -actD; yy < actD; yy++)
                    {
                        int yc = yKijun + yy;
                        //マイナス座標や画像サイズを超えていたら、収まるように修正
                        yc = yc < 0 ? 0 : yc > sourceHeight - 1 ? sourceHeight - 1 : yc;
                        for (int xx = -actD; xx < actD; xx++)
                        {
                            int xc = xKijun + xx;
                            xc = xc < 0 ? 0 : xc > sourceWidth - 1 ? sourceWidth - 1 : xc;
                            int pp = (yc * sourceStride) + (xc * pByte);
                            double weight = ws[xx + actD, yy + actD];
                            //完全透明ピクセル(a=0)だった場合はRGBは計算しないで
                            //重みだけ足し算して後で使う
                            if (sourcePixels[pp + 3] == 0)
                            {
                                alphaFix += weight;
                                continue;
                            }
                            bSum += sourcePixels[pp] * weight;
                            gSum += sourcePixels[pp + 1] * weight;
                            rSum += sourcePixels[pp + 2] * weight;
                            aSum += sourcePixels[pp + 3] * weight;
                        }
                    }

                    //                    C#、WPF、バイリニア法での画像の拡大縮小変換、半透明画像(32bit画像)対応版 - 午後わてんのブログ
                    //https://gogowaten.hatenablog.com/entry/2021/04/17/151803#32bit%E3%81%A824bit%E3%81%AF%E9%81%95%E3%81%A3%E3%81%9F
                    //完全透明ピクセルによるRGB値の修正
                    //参照範囲がすべて完全透明だった場合は0のままでいいので計算しない
                    if (alphaFix == 1) continue;
                    //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                    double rgbFix = 1 / (1 - alphaFix);
                    bSum *= rgbFix;
                    gSum *= rgbFix;
                    rSum *= rgbFix;

                    //0～255の範囲を超えることがあるので、修正
                    bSum = bSum < 0 ? 0 : bSum > 255 ? 255 : bSum;
                    gSum = gSum < 0 ? 0 : gSum > 255 ? 255 : gSum;
                    rSum = rSum < 0 ? 0 : rSum > 255 ? 255 : rSum;
                    aSum = aSum < 0 ? 0 : aSum > 255 ? 255 : aSum;

                    int ap = (y * stride) + (x * pByte);
                    pixels[ap] = (byte)(bSum + 0.5);
                    pixels[ap + 1] = (byte)(gSum + 0.5);
                    pixels[ap + 2] = (byte)(rSum + 0.5);
                    pixels[ap + 3] = (byte)(aSum + 0.5);
                }
            }

            //_ = Parallel.For(0, height, y =>
            //  {

            //  });

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, source.Format, null, pixels, stride);
            return bitmap;

            //修正した重み取得
            double[,] GetFixWeights(double rx, double ry, int actN, int n)
            {
                //全体の参照距離
                int nn = actN * 2;
                //基準になる距離計算
                double sx = rx - (int)rx;
                double sy = ry - (int)ry;
                double dx = (sx < 0.5) ? 0.5 - sx : 0.5 - sx + 1;
                double dy = (sy < 0.5) ? 0.5 - sy : 0.5 - sy + 1;

                //各ピクセルの重みと、重み合計を計算
                double[] xw = new double[nn];
                double[] yw = new double[nn];
                double xSum = 0, ySum = 0;
                for (int i = -actN; i < actN; i++)
                {
                    //距離に倍率を掛け算したのをLanczosで重み計算
                    //double x = SincA(Math.Abs(dx + i) / (limitD / 2.0));//n=2より大きいとボケる
                    //double x = SincA(Math.Abs(dx + i) / (inScale / 2.0));//ノイズ多め
                    double x = SincA(Math.Abs(dx + i) / (actN / 2.0));
                    //double x = SincA(Math.Abs(dx + i) / actN);//ボケる
                    //double x = SincA.Abs(dx + i) / (n / 2));//ノイズ多め
                    xSum += x;
                    xw[i + actN] = x;
                    //double y = SincA(Math.Abs(dy + i) / (limitD / 2.0));
                    //double y = SincA(Math.Abs(dy + i) / (inScale / 2.0));
                    double y = SincA(Math.Abs(dy + i) / (actN / 2.0));
                    //double y = SincA(Math.Abs(dy + i) / actN);
                    //double y = SincA(Math.Abs(dy + i) / (n / 2));
                    ySum += y;
                    yw[i + actN] = y;
                }

                //重み合計で割り算して修正、全体で100%(1.0)にする
                for (int i = 0; i < nn; i++)
                {
                    xw[i] /= xSum;
                    yw[i] /= ySum;
                }

                // x * y
                double[,] ws = new double[nn, nn];
                for (int y = 0; y < nn; y++)
                {
                    for (int x = 0; x < nn; x++)
                    {
                        ws[x, y] = xw[x] * yw[y];
                    }
                }
                return ws;
            }
        }


        private BitmapSource BitmapHeightX2(BitmapSource source)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int motoWidth = source.PixelWidth;
            int motoHeight = source.PixelHeight;
            int motoStride = motoWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[motoHeight * motoStride];
            source.CopyPixels(sourcePixels, motoStride, 0);

            //変換後の画像の画素値の配列用
            int height = motoHeight * 2;
            byte[] pixels = new byte[height * motoStride];

            for (int y = 0; y < motoHeight; y++)
            {
                for (int x = 0; x < motoWidth; x++)
                {
                    int pp = (y * motoStride) + (x * pByte);
                    int pp2 = pp + (motoStride * y);
                    pixels[pp2] = sourcePixels[pp];
                    pixels[pp2 + 1] = sourcePixels[pp + 1];
                    pixels[pp2 + 2] = sourcePixels[pp + 2];
                    pixels[pp2 + 3] = sourcePixels[pp + 3];

                    pp2 += motoStride;
                    pixels[pp2] = sourcePixels[pp];
                    pixels[pp2 + 1] = sourcePixels[pp + 1];
                    pixels[pp2 + 2] = sourcePixels[pp + 2];
                    pixels[pp2 + 3] = sourcePixels[pp + 3];
                }
            }

            BitmapSource bitmap = BitmapSource.Create(motoWidth, height, 96, 96, source.Format, null, pixels, motoStride);
            return bitmap;
        }


        private BitmapSource BitmapWidthX2(BitmapSource source)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int motoWidth = source.PixelWidth;
            int motoHeight = source.PixelHeight;
            int motoStride = motoWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[motoHeight * motoStride];
            source.CopyPixels(sourcePixels, motoStride, 0);

            //変換後の画像の画素値の配列用
            int sakiWidth = motoWidth * 2;
            int sakiStride = motoStride * 2;
            byte[] pixels = new byte[motoHeight * sakiStride];

            for (int y = 0; y < motoHeight; y++)
            {
                for (int x = 0; x < motoWidth; x++)
                {
                    int pp = (y * motoStride) + (x * pByte);
                    int pp2 = (y * sakiStride) + (x * 2 * pByte);
                    pixels[pp2] = sourcePixels[pp];
                    pixels[pp2 + 1] = sourcePixels[pp + 1];
                    pixels[pp2 + 2] = sourcePixels[pp + 2];
                    pixels[pp2 + 3] = sourcePixels[pp + 3];

                    pixels[pp2 + 4] = sourcePixels[pp];
                    pixels[pp2 + 5] = sourcePixels[pp + 1];
                    pixels[pp2 + 6] = sourcePixels[pp + 2];
                    pixels[pp2 + 7] = sourcePixels[pp + 3];
                }
            }
            BitmapSource bitmap = BitmapSource.Create(sakiWidth, motoHeight, 96, 96, source.Format, null, pixels, sakiStride);
            return bitmap;
        }


        private BitmapSource BitmapX2(BitmapSource source)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int motoWidth = source.PixelWidth;
            int motoHeight = source.PixelHeight;
            int motoStride = motoWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[motoHeight * motoStride];
            source.CopyPixels(sourcePixels, motoStride, 0);

            //変換後の画像の画素値の配列用
            int sakiWidth = motoWidth * 2;
            int sakiHeight = motoHeight * 2;
            int sakiStride = motoStride * 2;
            byte[] pixels = new byte[sakiHeight * sakiStride];

            for (int y = 0; y < motoHeight; y++)
            {
                for (int x = 0; x < motoWidth; x++)
                {
                    int pp = (y * motoStride) + (x * pByte);
                    byte p0 = sourcePixels[pp];
                    byte p1 = sourcePixels[pp + 1];
                    byte p2 = sourcePixels[pp + 2];
                    byte p3 = sourcePixels[pp + 3];

                    int pp2 = (y * 2 * sakiStride) + (x * 2 * pByte);
                    pixels[pp2] = p0;
                    pixels[pp2 + 1] = p1;
                    pixels[pp2 + 2] = p2;
                    pixels[pp2 + 3] = p3;
                    pixels[pp2 + 4] = p0;
                    pixels[pp2 + 5] = p1;
                    pixels[pp2 + 6] = p2;
                    pixels[pp2 + 7] = p3;

                    pp2 += sakiStride;
                    pixels[pp2] = p0;
                    pixels[pp2 + 1] = p1;
                    pixels[pp2 + 2] = p2;
                    pixels[pp2 + 3] = p3;
                    pixels[pp2 + 4] = p0;
                    pixels[pp2 + 5] = p1;
                    pixels[pp2 + 6] = p2;
                    pixels[pp2 + 7] = p3;

                }
            }
            BitmapSource bitmap = BitmapSource.Create(sakiWidth, sakiHeight, 96, 96, source.Format, null, pixels, sakiStride);
            return bitmap;
        }



        private BitmapSource BitmapCenter(BitmapSource source)
        {
            int w = source.PixelWidth;
            int h = source.PixelHeight;
            if (w == 640 && h == 480) return source;

            int x = (640 - w) / 2;
            int y = (480 - h) / 2;

            DrawingVisual dv = new();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(source, new Rect(x, y, w, h));
            }
            var render = new RenderTargetBitmap(640, 480, 96, 96, PixelFormats.Pbgra32);
            render.Render(dv);
            return render;
        }

        /// <summary>
        /// PS1のスクショ画像をドット保持して640x480にリサイズ
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        //private BitmapSource BitmapPS1(BitmapSource source)
        //{
        //    BitmapSource bmp = source;
        //    //240以下は縦2倍
        //    if (bmp.PixelHeight <= 240) { bmp = BitmapHeightX2(bmp); }
        //    //512未満は横2倍
        //    if (bmp.PixelWidth < 512) bmp = BitmapWidthX2(bmp);
        //    //中途半端な横サイズ画像はリサイズ
        //    if (bmp.PixelWidth > 640 || bmp.PixelWidth < 624)
        //    {
        //        bmp = SincBgra32(bmp, 640, 480, 4);
        //    }
        //    //中央配置(640x480未満の場合)
        //    bmp = BitmapCenter(bmp);
        //    return bmp;
        //}

        //jinで撮ったもの専用、左16ドットから320以外は左右を削る
        private BitmapSource BitmapPS1(BitmapSource source)
        {
            BitmapSource bmp = source;
            int w = bmp.PixelWidth;
            int h = bmp.PixelHeight;
            //368x480だけ特別
            if (w == 368 && h == 480) bmp = Croped368x480(bmp);

            if (bmp.PixelHeight <= 240)
            {
                bmp = BitmapHeightX2(bmp);
            }
            if (bmp.PixelWidth < 512) bmp = BitmapWidthX2(bmp);
            if (bmp.PixelWidth > 640 || bmp.PixelWidth < 624)
            {
                //横588か584どちらがいいのかわからん、クロノ・クロスは588のほうが円に近いけど、計算自体は584のほうが正確な気がする
                bmp = BicubicBgra32Ex(bmp, 588, bmp.PixelHeight, -0.5);
                //bmp = BitmapX2(bmp);
                //bmp = LanczosBgra32(bmp, 588, 480, 3);//ランチョス、これがいい
                //bmp = LanczosBgra32(bmp, 584, 480, 3);//ランチョス、これがいい
                //bmp = LanczosBgra32(bmp, 588, bmp.PixelHeight, 3);//ランチョス、これがいい
                //bmp = LanczosBgra32(bmp, 584, bmp.PixelHeight, 3);//ランチョス、これがいい
                //bmp = LanczosBgra32(bmp, 640, bmp.PixelHeight, 3);//ランチョス、これがいい
                //bmp = LanczosBgra32(bmp, 640, 480, 3);//ランチョス、これがいい
                //bmp = SincTypeGBgra32(bmp, 640, 480, 2);//シャープだけど輪郭、nは2より大きいとボケる
                //bmp = SincBgra32(bmp, 640, 480, 4);//過剰なシャープ派手画質、
            }
            bmp = BitmapCenter(bmp);
            return bmp;
        }
        private BitmapSource Croped368x480(BitmapSource source)
        {
            return new CroppedBitmap(source, new Int32Rect(16, 0, 320, 480));
        }




        #endregion 特殊保存PS1スクショ用


        #region 画像縮小処理
        //窓関数
        private double Sinc(double d)
        {
            return Math.Sin(Math.PI * d) / (Math.PI * d);
        }
        //かなりシャープ
        private double GetLanczosWeightG(double d, int n)
        {
            if (d == 0) return 1.0;
            else if (d > n) return 0.0;
            else
            {
                double nn = n / 2.0;
                return Sinc(d / nn);
            }
        }

        private double GetLanczosWeightX(double d, int n, double scale)
        {
            if (d == 0) return 1.0;
            else if (d > n) return 0.0;
            else
            {
                return Sinc(d / scale) * Sinc(d / (n * scale));
            }
        }

        /// <summary>
        /// 画像の縮小、窓関数法で補完、PixelFormats.Bgra32専用)
        /// 拡大には使えない
        /// </summary>
        /// <param name="source">PixelFormats.Bgra32のBitmap</param>
        /// <param name="width">変換後の横ピクセル数を指定</param>
        /// <param name="height">変換後の縦ピクセル数を指定</param>
        /// <param name="n">最大参照距離、逆倍率の2倍がいい(1/5倍なら5*2=10)</param>
        /// <returns></returns>
        private BitmapSource LanczosBgra32(BitmapSource source, int width, int height)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;
            int sourceStride = sourceWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[sourceHeight * sourceStride];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            //変換後の画像の画素値の配列用
            double widthScale = (double)sourceWidth / width;//横倍率
            double heightScale = (double)sourceHeight / height;
            int stride = width * pByte;
            byte[] pixels = new byte[height * stride];

            //逆倍率、横だけで見ているけど縦もあったほうがいいかも
            double reScale = widthScale;
            //参照距離、四捨五入？切り上げのほうがいい？
            int n = (int)(reScale * 2);
            _ = Parallel.For(0, height, y =>
              {
                  for (int x = 0; x < width; x++)
                  {
                      //参照点
                      double rx = (x + 0.5) * widthScale;
                      double ry = (y + 0.5) * heightScale;
                      //参照点四捨五入で基準
                      int xKijun = (int)(rx + 0.5);
                      int yKijun = (int)(ry + 0.5);
                      //修正した重み取得
                      var ws = GetFixWeights(rx, ry, n);

                      double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                      double alphaFix = 0;
                      //参照範囲は基準から上(xは左)へn、下(xは右)へn-1の範囲
                      for (int yy = -n; yy < n; yy++)
                      {
                          int yc = yKijun + yy;
                          //マイナス座標や画像サイズを超えていたら、収まるように修正
                          yc = yc < 0 ? 0 : yc > sourceHeight - 1 ? sourceHeight - 1 : yc;
                          for (int xx = -n; xx < n; xx++)
                          {
                              int xc = xKijun + xx;
                              xc = xc < 0 ? 0 : xc > sourceWidth - 1 ? sourceWidth - 1 : xc;
                              int pp = (yc * sourceStride) + (xc * pByte);
                              double weight = ws[xx + n, yy + n];
                              //完全透明ピクセル(a=0)だった場合はRGBは計算しないで
                              //重みだけ足し算して後で使う
                              if (sourcePixels[pp + 3] == 0)
                              {
                                  alphaFix += weight;
                                  continue;
                              }
                              bSum += sourcePixels[pp] * weight;
                              gSum += sourcePixels[pp + 1] * weight;
                              rSum += sourcePixels[pp + 2] * weight;
                              aSum += sourcePixels[pp + 3] * weight;
                          }
                      }

                      //                    C#、WPF、バイリニア法での画像の拡大縮小変換、半透明画像(32bit画像)対応版 - 午後わてんのブログ
                      //https://gogowaten.hatenablog.com/entry/2021/04/17/151803#32bit%E3%81%A824bit%E3%81%AF%E9%81%95%E3%81%A3%E3%81%9F
                      //完全透明ピクセルによるRGB値の修正
                      //参照範囲がすべて完全透明だった場合は0のままでいいので計算しない
                      if (alphaFix == 1) continue;
                      //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                      double rgbFix = 1 / (1 - alphaFix);
                      bSum *= rgbFix;
                      gSum *= rgbFix;
                      rSum *= rgbFix;

                      //0～255の範囲を超えることがあるので、修正
                      bSum = bSum < 0 ? 0 : bSum > 255 ? 255 : bSum;
                      gSum = gSum < 0 ? 0 : gSum > 255 ? 255 : gSum;
                      rSum = rSum < 0 ? 0 : rSum > 255 ? 255 : rSum;
                      aSum = aSum < 0 ? 0 : aSum > 255 ? 255 : aSum;

                      int ap = (y * stride) + (x * pByte);
                      pixels[ap] = (byte)(bSum + 0.5);
                      pixels[ap + 1] = (byte)(gSum + 0.5);
                      pixels[ap + 2] = (byte)(rSum + 0.5);
                      pixels[ap + 3] = (byte)(aSum + 0.5);
                  }
              });


            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, source.Format, null, pixels, stride);
            return bitmap;

            //修正した重み取得
            double[,] GetFixWeights(double rx, double ry, int n)
            {
                int nn = n * 2;//全体の参照距離
                //基準になる距離計算
                double sx = rx - (int)rx;
                double sy = ry - (int)ry;
                double dx = (sx < 0.5) ? 0.5 - sx : 0.5 - sx + 1;
                double dy = (sy < 0.5) ? 0.5 - sy : 0.5 - sy + 1;

                //各ピクセルの重みと、重み合計を計算
                double[] xw = new double[nn];
                double[] yw = new double[nn];
                double xSum = 0, ySum = 0;
                for (int i = -n; i < n; i++)
                {
                    //double x = myFunc(Math.Abs(dx + i), n);
                    double x = GetLanczosWeightG(Math.Abs(dx + i), n);
                    xSum += x;
                    xw[i + n] = x;
                    double y = GetLanczosWeightG(Math.Abs(dy + i), n);
                    //double y = myFunc(Math.Abs(dy + i), n);
                    ySum += y;
                    yw[i + n] = y;
                }

                //重み合計で割り算して修正、全体で100%(1.0)にする
                for (int i = 0; i < nn; i++)
                {
                    xw[i] /= xSum;
                    yw[i] /= ySum;
                }

                // x * y
                double[,] ws = new double[nn, nn];
                for (int y = 0; y < nn; y++)
                {
                    for (int x = 0; x < nn; x++)
                    {
                        ws[x, y] = xw[x] * yw[y];
                    }
                }
                return ws;
            }
            //かなりシャープ
            double GetLanczosWeightG(double d, int n)
            {
                if (d == 0) return 1.0;
                else if (d > n) return 0.0;
                else
                {
                    double nn = n / 2.0;
                    return Sinc(d / nn);
                }
            }

        }
        /// <summary>
        /// 画像の縮小、ランチョス法で補完、PixelFormats.Bgra32専用)
        /// 拡大には使えない
        /// </summary>
        /// <param name="source">PixelFormats.Bgra32のBitmap</param>
        /// <param name="width">変換後の横ピクセル数を指定</param>
        /// <param name="height">変換後の縦ピクセル数を指定</param>
        /// <param name="n">最大参照距離、逆倍率の2倍がいい(1/5倍なら5*2=10)</param>
        /// <returns></returns>
        private BitmapSource LanczosBgra32TypeX(BitmapSource source, int width, int height)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;
            int sourceStride = sourceWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[sourceHeight * sourceStride];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            //変換後の画像の画素値の配列用
            double widthScale = (double)sourceWidth / width;//横倍率
            double heightScale = (double)sourceHeight / height;
            int stride = width * pByte;
            byte[] pixels = new byte[height * stride];

            //逆倍率、横だけで見ているけど縦もあったほうがいいかも
            double reScale = widthScale;
            //参照距離、四捨五入？切り上げのほうがいい？
            int n = (int)(reScale * 2);
            _ = Parallel.For(0, height, y =>
              {
                  for (int x = 0; x < width; x++)
                  {
                      //参照点
                      double rx = (x + 0.5) * widthScale;
                      double ry = (y + 0.5) * heightScale;
                      //参照点四捨五入で基準
                      int xKijun = (int)(rx + 0.5);
                      int yKijun = (int)(ry + 0.5);
                      //修正した重み取得
                      var ws = GetFixWeights(rx, ry, n);

                      double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                      double alphaFix = 0;
                      //参照範囲は基準から上(xは左)へn、下(xは右)へn-1の範囲
                      for (int yy = -n; yy < n; yy++)
                      {
                          int yc = yKijun + yy;
                          //マイナス座標や画像サイズを超えていたら、収まるように修正
                          yc = yc < 0 ? 0 : yc > sourceHeight - 1 ? sourceHeight - 1 : yc;
                          for (int xx = -n; xx < n; xx++)
                          {
                              int xc = xKijun + xx;
                              xc = xc < 0 ? 0 : xc > sourceWidth - 1 ? sourceWidth - 1 : xc;
                              int pp = (yc * sourceStride) + (xc * pByte);
                              double weight = ws[xx + n, yy + n];
                              //完全透明ピクセル(a=0)だった場合はRGBは計算しないで
                              //重みだけ足し算して後で使う
                              if (sourcePixels[pp + 3] == 0)
                              {
                                  alphaFix += weight;
                                  continue;
                              }
                              bSum += sourcePixels[pp] * weight;
                              gSum += sourcePixels[pp + 1] * weight;
                              rSum += sourcePixels[pp + 2] * weight;
                              aSum += sourcePixels[pp + 3] * weight;
                          }
                      }

                      //                    C#、WPF、バイリニア法での画像の拡大縮小変換、半透明画像(32bit画像)対応版 - 午後わてんのブログ
                      //https://gogowaten.hatenablog.com/entry/2021/04/17/151803#32bit%E3%81%A824bit%E3%81%AF%E9%81%95%E3%81%A3%E3%81%9F
                      //完全透明ピクセルによるRGB値の修正
                      //参照範囲がすべて完全透明だった場合は0のままでいいので計算しない
                      if (alphaFix == 1) continue;
                      //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                      double rgbFix = 1 / (1 - alphaFix);
                      bSum *= rgbFix;
                      gSum *= rgbFix;
                      rSum *= rgbFix;

                      //0～255の範囲を超えることがあるので、修正
                      bSum = bSum < 0 ? 0 : bSum > 255 ? 255 : bSum;
                      gSum = gSum < 0 ? 0 : gSum > 255 ? 255 : gSum;
                      rSum = rSum < 0 ? 0 : rSum > 255 ? 255 : rSum;
                      aSum = aSum < 0 ? 0 : aSum > 255 ? 255 : aSum;

                      int ap = (y * stride) + (x * pByte);
                      pixels[ap] = (byte)(bSum + 0.5);
                      pixels[ap + 1] = (byte)(gSum + 0.5);
                      pixels[ap + 2] = (byte)(rSum + 0.5);
                      pixels[ap + 3] = (byte)(aSum + 0.5);
                  }
              });


            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, source.Format, null, pixels, stride);
            return bitmap;

            //修正した重み取得
            double[,] GetFixWeights(double rx, double ry, int n)
            {
                int nn = n * 2;//全体の参照距離
                //基準になる距離計算
                double sx = rx - (int)rx;
                double sy = ry - (int)ry;
                double dx = (sx < 0.5) ? 0.5 - sx : 0.5 - sx + 1;
                double dy = (sy < 0.5) ? 0.5 - sy : 0.5 - sy + 1;

                //各ピクセルの重みと、重み合計を計算
                double[] xw = new double[nn];
                double[] yw = new double[nn];
                double xSum = 0, ySum = 0;
                for (int i = -n; i < n; i++)
                {
                    double x = GetLanczosWeightX(Math.Abs(dx + i), n, reScale);
                    xSum += x;
                    xw[i + n] = x;
                    double y = GetLanczosWeightX(Math.Abs(dy + i), n, reScale);
                    ySum += y;
                    yw[i + n] = y;
                }

                //重み合計で割り算して修正、全体で100%(1.0)にする
                for (int i = 0; i < nn; i++)
                {
                    xw[i] /= xSum;
                    yw[i] /= ySum;
                }

                // x * y
                double[,] ws = new double[nn, nn];
                for (int y = 0; y < nn; y++)
                {
                    for (int x = 0; x < nn; x++)
                    {
                        ws[x, y] = xw[x] * yw[y];
                    }
                }
                return ws;
            }

            double GetLanczosWeightX(double d, int n, double scale)
            {
                if (d == 0) return 1.0;
                else if (d > n) return 0.0;
                else
                {
                    return Sinc(d / scale) * Sinc(d / (n * scale));
                }
            }

        }




        /// <summary>
        /// ランチョス補完法での重み計算
        /// </summary>
        /// <param name="d">距離</param>
        /// <param name="n">最大参照距離</param>
        /// <returns></returns>
        private double GetLanczosWeight(double d, int n)
        {
            if (d == 0) return 1.0;
            else if (d > n) return 0.0;
            else return Sinc(d) * Sinc(d / n);
        }

        /// <summary>
        /// ランチョス補完法での重み計算
        /// </summary>
        /// <param name="d">距離</param>
        /// <param name="n">最大参照距離</param>
        /// <returns></returns>
        private double GetLanczosWeight(double d, int n, double limitD)
        {
            if (d == 0) return 1.0;
            else if (d > limitD) return 0.0;
            else return Sinc(d) * Sinc(d / n);
        }

        /// <summary>
        /// 画像の拡大縮小、ランチョス法で補完、PixelFormats.Bgra32専用)
        /// 通常版をセパラブルとParallelで高速化
        /// </summary>
        /// <param name="source">PixelFormats.Bgra32のBitmap</param>
        /// <param name="width">変換後の横ピクセル数を指定</param>
        /// <param name="height">変換後の縦ピクセル数を指定</param>
        /// <param name="n">最大参照距離、3か4がいい</param>
        /// <returns></returns>
        private BitmapSource LanczosBgra32Ex(BitmapSource source, int width, int height, int n)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;
            int sourceStride = sourceWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[sourceHeight * sourceStride];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            //変換後の画像の画素値の配列用
            double widthScale = (double)sourceWidth / width;//横の逆倍率
            double heightScale = (double)sourceHeight / height;
            int stride = width * pByte;
            byte[] pixels = new byte[height * stride];

            //横処理用配列
            double[] xResult = new double[sourceHeight * stride];

            //実際に参照する最大ピクセル半径 = 逆倍率(1/倍率)*2の切り上げ
            double scale = (double)width / sourceWidth;
            //最大参照距離 = 逆倍率 * n
            double limitD = widthScale * n;
            //実際の参照距離、は指定距離*逆倍率の切り上げにしたけど、切り捨てでも見た目の変化なし            
            int actD = (int)Math.Ceiling(limitD);


            //横処理
            _ = Parallel.For(0, sourceHeight, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    //参照点
                    double rx = (x + 0.5) * widthScale;
                    //参照点四捨五入で基準
                    int xKijun = (int)(rx + 0.5);
                    //修正した重み取得
                    double[] ws = GetFixWeihgts(rx, actD, n, scale, limitD);

                    double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                    double alphaFix = 0;
                    int pp;
                    for (int xx = -actD; xx < actD; xx++)
                    {
                        int xc = xKijun + xx;
                        //マイナス座標や画像サイズを超えていたら、収まるように修正
                        xc = xc < 0 ? 0 : xc > sourceWidth - 1 ? sourceWidth - 1 : xc;
                        pp = (y * sourceStride) + (xc * pByte);
                        double weight = ws[xx + actD];
                        //完全透明ピクセル(a=0)だった場合はRGBは計算しないで
                        //重みだけ足し算して後で使う
                        if (sourcePixels[pp + 3] == 0)
                        {
                            alphaFix += weight;
                            continue;
                        }
                        bSum += sourcePixels[pp] * weight;
                        gSum += sourcePixels[pp + 1] * weight;
                        rSum += sourcePixels[pp + 2] * weight;
                        aSum += sourcePixels[pp + 3] * weight;
                    }
                    //                    C#、WPF、バイリニア法での画像の拡大縮小変換、半透明画像(32bit画像)対応版 - 午後わてんのブログ
                    //https://gogowaten.hatenablog.com/entry/2021/04/17/151803#32bit%E3%81%A824bit%E3%81%AF%E9%81%95%E3%81%A3%E3%81%9F

                    //完全透明ピクセルによるRGB値の修正
                    //参照範囲がすべて完全透明だった場合は0のままでいいので計算しない
                    if (alphaFix == 1) continue;
                    //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                    double rgbFix = 1 / (1 - alphaFix);
                    bSum *= rgbFix;
                    gSum *= rgbFix;
                    rSum *= rgbFix;

                    pp = y * stride + x * pByte;
                    xResult[pp] = bSum;
                    xResult[pp + 1] = gSum;
                    xResult[pp + 2] = rSum;
                    xResult[pp + 3] = aSum;
                }
            });

            //縦処理
            _ = Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double ry = (y + 0.5) * heightScale;
                    int yKijun = (int)(ry + 0.5);

                    double[] ws = GetFixWeihgts(ry, actD, n, scale, limitD);
                    double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                    double alphaFix = 0;
                    int pp;
                    for (int yy = -actD; yy < actD; yy++)
                    {
                        int yc = yKijun + yy;
                        yc = yc < 0 ? 0 : yc > sourceHeight - 1 ? sourceHeight - 1 : yc;
                        pp = (yc * stride) + (x * pByte);
                        double weight = ws[yy + actD];

                        if (xResult[pp + 3] == 0)
                        {
                            alphaFix += weight;
                            continue;
                        }
                        bSum += xResult[pp] * weight;
                        gSum += xResult[pp + 1] * weight;
                        rSum += xResult[pp + 2] * weight;
                        aSum += xResult[pp + 3] * weight;
                    }

                    if (alphaFix == 1) continue;
                    //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                    double rgbFix = 1 / (1 - alphaFix);
                    bSum *= rgbFix;
                    gSum *= rgbFix;
                    rSum *= rgbFix;

                    //0～255の範囲を超えることがあるので、修正
                    bSum = bSum < 0 ? 0 : bSum > 255 ? 255 : bSum;
                    gSum = gSum < 0 ? 0 : gSum > 255 ? 255 : gSum;
                    rSum = rSum < 0 ? 0 : rSum > 255 ? 255 : rSum;
                    aSum = aSum < 0 ? 0 : aSum > 255 ? 255 : aSum;
                    int ap = (y * stride) + (x * pByte);
                    pixels[ap] = (byte)(bSum + 0.5);
                    pixels[ap + 1] = (byte)(gSum + 0.5);
                    pixels[ap + 2] = (byte)(rSum + 0.5);
                    pixels[ap + 3] = (byte)(aSum + 0.5);
                }
            });


            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, source.Format, null, pixels, stride);
            return bitmap;

            //修正した重み取得
            double[] GetFixWeihgts(double r, int actD, int n, double scale, double limitD)
            {
                int nn = actD * 2;//全体の参照距離
                //基準距離
                double s = r - (int)r;
                double d = (s < 0.5) ? 0.5 - s : 0.5 - s + 1;

                //各重みと重み合計
                double[] ws = new double[nn];
                double sum = 0;
                for (int i = -actD; i < actD; i++)
                {
                    double w = GetLanczosWeight(Math.Abs(d + i) * scale, n, limitD);
                    sum += w;
                    ws[i + actD] = w;
                }

                //重み合計で割り算して修正、全体で100%(1.0)にする
                for (int i = 0; i < nn; i++)
                {
                    ws[i] /= sum;
                }
                return ws;
            }
        }



        /// <summary>
        /// バイキュービックで重み計算、縮小対応版
        /// </summary>
        /// <param name="d">距離</param>
        /// <param name="actN">参照最大距離、拡大時は2固定</param>
        /// <param name="a">定数、-1.0 ～ -0.5 が丁度いい</param>
        /// <returns></returns>
        private static double GetWeightCubic(double d, double a, double scale, double inScale)
        {
            double dd = d * scale;

            if (d == inScale * 2) return 0;
            else if (d <= inScale) return ((a + 2) * (dd * dd * dd)) - ((a + 3) * (dd * dd)) + 1;
            else if (d < inScale * 2) return (a * (dd * dd * dd)) - (5 * a * (dd * dd)) + (8 * a * dd) - (4 * a);
            else return 0;
        }



        /// <summary>
        /// 画像のリサイズ、バイキュービック法で補完、PixelFormats.Bgra32専用)
        /// セパラブルなので縦横別々処理とParallelで高速化したもの
        /// </summary>
        /// <param name="source">PixelFormats.Bgra32のBitmap</param>
        /// <param name="width">変換後の横ピクセル数を指定</param>
        /// <param name="height">変換後の縦ピクセル数を指定</param>
        /// <param name="a">係数、-1.0～-0.5がいい</param>
        /// <param name="soft">trueで縮小時に画質がソフトになる？</param>
        /// <returns></returns>
        private BitmapSource BicubicBgra32Ex(BitmapSource source, int width, int height, double a, bool soft = false)
        {
            //1ピクセルあたりのバイト数、Byte / Pixel
            int pByte = (source.Format.BitsPerPixel + 7) / 8;

            //元画像の画素値の配列作成
            int sourceWidth = source.PixelWidth;
            int sourceHeight = source.PixelHeight;
            int sourceStride = sourceWidth * pByte;//1行あたりのbyte数
            byte[] sourcePixels = new byte[sourceHeight * sourceStride];
            source.CopyPixels(sourcePixels, sourceStride, 0);

            //変換後の画像の画素値の配列用
            double widthScale = (double)sourceWidth / width;//横倍率(逆倍率)
            double heightScale = (double)sourceHeight / height;
            int stride = width * pByte;
            byte[] pixels = new byte[height * stride];

            //横処理用配列
            double[] xResult = new double[sourceHeight * stride];

            //倍率
            double scale = width / (double)sourceWidth;
            double inScale = 1.0 / scale;//逆倍率
            //実際の参照距離、縮小時だけに関係する、倍率*2の切り上げが正しいはず
            //切り捨てするとぼやけるソフト画質になる
            int actD = (int)Math.Ceiling(widthScale * 2);
            if (soft == true) actD = ((int)widthScale) * 2;//切り捨て

            //拡大時の調整(これがないと縮小専用)
            if (1.0 < scale)
            {
                scale = 1.0;//重み計算に使う、拡大時は1固定
                inScale = 1.0;//逆倍率、拡大時は1固定
                actD = 2;//拡大時のバイキュービックの参照距離は2で固定
            }

            //横処理
            _ = Parallel.For(0, sourceHeight, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    //参照点
                    double rx = (x + 0.5) * widthScale;
                    //参照点四捨五入で基準
                    int xKijun = (int)(rx + 0.5);
                    //修正した重み取得
                    double[] ws = GetFixWeights(rx, actD, a, scale, inScale);
                    double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                    double alphaFix = 0;

                    int pp;
                    for (int xx = -actD; xx < actD; xx++)
                    {
                        int xc = xKijun + xx;
                        //マイナス座標や画像サイズを超えていたら、収まるように修正
                        xc = xc < 0 ? 0 : xc > sourceWidth - 1 ? sourceWidth - 1 : xc;
                        pp = (y * sourceStride) + (xc * pByte);
                        double weight = ws[xx + actD];
                        //完全透明ピクセル(a=0)だった場合はRGBは計算しないで
                        //重みだけ足し算して後で使う
                        if (sourcePixels[pp + 3] == 0)
                        {
                            alphaFix += weight;
                            continue;
                        }
                        bSum += sourcePixels[pp] * weight;
                        gSum += sourcePixels[pp + 1] * weight;
                        rSum += sourcePixels[pp + 2] * weight;
                        aSum += sourcePixels[pp + 3] * weight;
                    }
                    //                    C#、WPF、バイリニア法での画像の拡大縮小変換、半透明画像(32bit画像)対応版 - 午後わてんのブログ
                    //https://gogowaten.hatenablog.com/entry/2021/04/17/151803#32bit%E3%81%A824bit%E3%81%AF%E9%81%95%E3%81%A3%E3%81%9F

                    //完全透明ピクセルによるRGB値の修正
                    //参照範囲がすべて完全透明だった場合は0のままでいいので計算しない
                    if (alphaFix == 1) continue;
                    //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                    double rgbFix = 1 / (1 - alphaFix);
                    bSum *= rgbFix;
                    gSum *= rgbFix;
                    rSum *= rgbFix;

                    pp = y * stride + x * pByte;
                    xResult[pp] = bSum;
                    xResult[pp + 1] = gSum;
                    xResult[pp + 2] = rSum;
                    xResult[pp + 3] = aSum;
                }
            });
            //縦処理
            _ = Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double ry = (y + 0.5) * heightScale;
                    int yKijun = (int)(ry + 0.5);
                    double[] ws = GetFixWeights(ry, actD, a, scale, inScale);

                    double bSum = 0, gSum = 0, rSum = 0, aSum = 0;
                    double alphaFix = 0;
                    int pp;
                    for (int yy = -actD; yy < actD; yy++)
                    {
                        int yc = yKijun + yy;
                        yc = yc < 0 ? 0 : yc > sourceHeight - 1 ? sourceHeight - 1 : yc;
                        pp = (yc * stride) + (x * pByte);
                        double weight = ws[yy + actD];

                        if (xResult[pp + 3] == 0)
                        {
                            alphaFix += weight;
                            continue;
                        }
                        bSum += xResult[pp] * weight;
                        gSum += xResult[pp + 1] * weight;
                        rSum += xResult[pp + 2] * weight;
                        aSum += xResult[pp + 3] * weight;
                    }
                    if (alphaFix == 1) continue;
                    //完全透明ピクセルが混じっていた場合は、その分を差し引いてRGB修正する
                    double rgbFix = 1 / (1 - alphaFix);
                    bSum *= rgbFix;
                    gSum *= rgbFix;
                    rSum *= rgbFix;

                    //0～255の範囲を超えることがあるので、修正
                    bSum = bSum < 0 ? 0 : bSum > 255 ? 255 : bSum;
                    gSum = gSum < 0 ? 0 : gSum > 255 ? 255 : gSum;
                    rSum = rSum < 0 ? 0 : rSum > 255 ? 255 : rSum;
                    aSum = aSum < 0 ? 0 : aSum > 255 ? 255 : aSum;
                    int ap = (y * stride) + (x * pByte);
                    pixels[ap] = (byte)(bSum + 0.5);
                    pixels[ap + 1] = (byte)(gSum + 0.5);
                    pixels[ap + 2] = (byte)(rSum + 0.5);
                    pixels[ap + 3] = (byte)(aSum + 0.5);
                }
            });

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, source.Format, null, pixels, stride);
            return bitmap;

            //修正した重み取得

            double[] GetFixWeights(double r, int actD, double a, double scale, double inScale)
            {
                //全体の参照距離
                int nn = actD * 2;
                //基準になる距離計算
                double s = r - (int)r;
                double d = (s < 0.5) ? 0.5 - s : 0.5 - s + 1;
                //各重みと重み合計
                double[] ws = new double[nn];
                double sum = 0;
                for (int i = -actD; i < actD; i++)
                {
                    double w = GetWeightCubic(Math.Abs(d + i), a, scale, inScale);
                    sum += w;
                    ws[i + actD] = w;
                }
                //重み合計で割り算して修正、全体で100%(1.0)にする
                for (int i = 0; i < nn; i++)
                {
                    ws[i] /= sum;
                }
                return ws;
            }
        }


        #endregion 画像縮小処理



        #region クリップボード

        //        クリップボードに複数の形式のデータをコピーする - .NET Tips(VB.NET, C#...)
        //https://dobon.net/vb/dotnet/system/clipboardmultidata.html

        /// <summary>
        /// BitmapSourceをPNG形式に変換したものと、そのままの形式の両方をクリップボードにコピーする
        /// </summary>
        /// <param name="source"></param>
        private void ClipboadSetImageWithPng(BitmapSource source)
        {
            //DataObjectに入れたいデータを入れて、それをクリップボードにセットする
            DataObject data = new();

            //BitmapSource形式そのままでセット
            data.SetData(typeof(BitmapSource), source);

            //PNG形式にエンコードしたものをMemoryStreamして、それをセット
            //画像をPNGにエンコード
            PngBitmapEncoder pngEnc = new();
            pngEnc.Frames.Add(BitmapFrame.Create(source));
            //エンコードした画像をMemoryStreamにSava
            using var ms = new System.IO.MemoryStream();
            pngEnc.Save(ms);
            data.SetData("PNG", ms);

            //クリップボードにセット
            Clipboard.SetDataObject(data, true);

        }

        //クリップボードにコピー
        private void ToClipboard()
        {
            if (MyThumbs.Count <= 0) return;
            //サイズが一定以上になる場合は確認
            Size size = GetSaveBitmapSize();
            if (size.Width * size.Height > 25_000_000)
            {
                //int B = (int)size.Width * (int)size.Height * 4;
                //int k = B / 1000;//1024?
                //int MB = k / 1000;
                if (MessageBox.Show($"画像が大きい{size}、コピーする？", "確認", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                {
                    return;
                }
            }
            BitmapSource bmp = MakeSaveBitmap();
            ClipboadSetImageWithPng(bmp);
            RenewStatusProcessed("クリップボードにコピーした");
        }
        //クリップボードから追加
        private void AddFromClipboardImage()
        {
            BitmapSource bmp = GetImageFromClipboardWithPNG();// Clipboard.GetImage();
            if (bmp != null)
            {
                AddImageThumb(MakeImage(bmp));
                ChangeLocate();
                RenewStatusProcessed("クリップボードから貼り付けた");
            }
            else
            {
                MessageBox.Show($"クリップボードに画像はなかった");
                return;
            }
        }

        /// <summary>
        /// クリップボードからBitmapSourceを取り出して返す、PNG(アルファ値保持)形式に対応
        /// </summary>
        /// <returns></returns>
        private BitmapSource GetImageFromClipboardWithPNG()
        {
            BitmapSource source = null;
            //クリップボードにPNG形式のデータがあったら、それを使ってBitmapFrame作成して返す
            //なければ普通にClipboardのGetImage、それでもなければnullを返す
            using var ms = (System.IO.MemoryStream)Clipboard.GetData("PNG");
            if (ms != null)
            {
                //source = BitmapFrame.Create(ms);//これだと取得できない
                source = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            else if (Clipboard.ContainsImage())
            {
                source = Clipboard.GetImage();
            }
            return source;
        }

        #endregion クリップボード


        #region 削除

        //削除時
        //管理用リストから削除
        //MyCanvasから削除
        //座標リストの最後の要素を削除、再配置、MyCanvasサイズ更新
        //MyActiveThumbを変更
        private void RemoveThumb(ImageThumb t)
        {
            if (MyThumbs.Count == 0) return;

            int i = MyThumbs.IndexOf(t);

            MyThumbs.Remove(t);
            MyCanvas.Children.Remove(t);
            MyLocate.RemoveAt(MyLocate.Count - 1);
            SetLocate();//再配置
            SetMyCanvasSize();

            //ActiveThumbの調整
            if (MyThumbs.Count == 0)
            {
                MyActiveThumb = null;
            }
            else if (i >= MyThumbs.Count)
            {
                MyActiveThumb = MyThumbs[i - 1];
            }
            else
            {
                MyActiveThumb = MyThumbs[i];
            }
            //ステータスバーの表示更新
            ChangedSaveImageSize();
        }

        //全削除
        private void RemoveAllThumbs()
        {
            if (MyThumbs.Count == 0) return;
            foreach (var item in MyThumbs)
            {
                MyCanvas.Children.Remove(item);
            }
            MyThumbs.Clear();
            MyLocate.Clear();
            SetMyCanvasSize();
            MyActiveThumb = null;
            //ステータスバーの表示更新
            ChangedSaveImageSize();
        }

        //連結範囲内画像削除
        private void RemoveAreaThumb()
        {
            List<ImageThumb> list = new();
            int row = MyData.Row;
            int col = MyData.Col;
            //連結範囲外に画像がある場合
            if (MyThumbs.Count > row * col)
            {
                for (int i = 0; i < row * col; i++)
                {
                    list.Add(MyThumbs[i]);
                }
                foreach (var item in list)
                {
                    RemoveThumb(item);
                }
            }
            //全画像が連結範囲内の場合は全部削除
            else
            {
                RemoveAllThumbs();
            }
        }
        #endregion 削除

        #region ショートカットキー

        //
        /// <summary>
        /// ActiveThumbの変更と入れ替え移動
        /// </summary>
        /// <param name="key">押されたキー</param>
        /// <param name="isSelect">trueでActiveThumbの変更、falseで入れ替え移動</param>
        private void MoveActiveThumb(Key key, bool isSelect)
        {
            //if (MyActiveThumb.IsFocused == false) return;
            if (MyThumbs.Count == 0) return;
            int i = MyThumbs.IndexOf(MyActiveThumb);
            int mod = i % MyData.Col;
            //左右制限
            //左端判定
            if (mod == 0)
            {
                //左方向のキーなら何もしない
                if (key is Key.NumPad1 or Key.NumPad4 or Key.NumPad7)
                    return;
            }
            //右端判定
            else if (mod == MyData.Col - 1)
            {
                //右方向のキーなら何もしない
                if (key is Key.NumPad3 or Key.NumPad6 or Key.NumPad9)
                    return;
            }

            //移動先のIndex決定
            int ii;
            if (key == Key.NumPad1) ii = i + MyData.Col - 1;

            else if (key is Key.NumPad2) ii = i + MyData.Col;
            else if (key is Key.NumPad4) ii = i - 1;
            else if (key is Key.NumPad6) ii = i + 1;
            else if (key is Key.NumPad8) ii = i - MyData.Col;

            else if (key == Key.NumPad3) ii = i + MyData.Col + 1;
            else if (key == Key.NumPad5) ii = i + MyData.Col;
            else if (key == Key.NumPad7) ii = i - MyData.Col - 1;
            else if (key == Key.NumPad9) ii = i - MyData.Col + 1;
            else return;

            //上下制限
            if (ii < 0 || ii > MyThumbs.Count - 1) return;

            //入れ替え移動
            if (isSelect)
            {
                //入れ替え
                MoveThumb(i, ii, MyActiveThumb);
                //MyThumbs[ii].Focus();
                //indexに従って表示位置変更
                SetLocate();

            }
            //ActiveThumbの変更
            else
            {
                MyThumbs[ii].Focus();
            }



        }



        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.None:
                    //MoveActiveThumb(e.Key);
                    switch (e.Key)
                    {
                        case Key.Delete:
                            RemoveThumb(MyActiveThumb);
                            break;
                        case Key.PageDown:
                            MyUpDownSize.MyValue -= MyUpDownSize.MySmallChange;
                            break;
                        case Key.PageUp:
                            MyUpDownSize.MyValue += MyUpDownSize.MySmallChange;
                            break;
                        default:
                            break;
                    }
                    break;
                case ModifierKeys.Alt:
                    break;
                case ModifierKeys.Control:
                    switch (e.Key)
                    {
                        case Key.Up:

                            break;
                        case Key.Down:

                            break;
                        case Key.S:
                            if (MyThumbs.Count <= 0) return;
                            SaveImage();
                            break;
                        case Key.C:
                            ToClipboard();
                            break;
                        case Key.V:
                            AddFromClipboardImage();
                            break;
                        default:
                            break;
                    }

                    break;
                case ModifierKeys.Shift:
                    switch (e.Key)
                    {
                        case Key.Right:
                            MyUpDownCol.MyValue += MyUpDownCol.MySmallChange;
                            break;
                        case Key.Left:
                            MyUpDownCol.MyValue -= MyUpDownCol.MySmallChange;
                            break;
                        case Key.Up:
                            MyUpDownRow.MyValue -= MyUpDownRow.MySmallChange;
                            break;
                        case Key.Down:
                            MyUpDownRow.MyValue += MyUpDownRow.MySmallChange;
                            break;
                        default:
                            break;
                    }
                    break;
                case ModifierKeys.Windows:
                    break;
                default:
                    break;
            }
        }


        #endregion ショートカットキー

        #region ステータスバーの表示更新

        //保存サイズの再計算
        private Size GetSaveBitmapSize()
        {
            Size bmpSize = new();
            if (MyData == null) return bmpSize;
            if (MyThumbs == null) return bmpSize;
            if (MyThumbs.Count == 0) return bmpSize;

            var (imageCount, areaRows, areaCols) = GetMakeRectParam();
            int w = 0, h = 0;
            switch (MyData.SaveScaleSizeType)
            {
                //一つの横幅指定
                case SaveScaleSizeType.OneWidth:
                    w = MyData.SaveOneWidth * areaCols;
                    if (MyData.IsMargin)
                    {
                        w += (areaCols + 1) * MyData.Margin;
                    }
                    break;
                //全体指定
                case SaveScaleSizeType.All:
                    w = MyData.SaveWidth;
                    h = MyData.SaveHeight;
                    break;
                //左上画像の横幅
                case SaveScaleSizeType.TopLeft:
                    w = MyThumbs[0].MyBitmapSource.PixelWidth * areaCols;
                    h = MyThumbs[0].MyBitmapSource.PixelHeight * areaRows;
                    if (MyData.IsMargin)
                    {
                        w += (areaCols + 1) * MyData.Margin;
                        h += (areaRows + 1) * MyData.Margin;
                    }
                    break;
                case SaveScaleSizeType.Special:
                    w = 640 * areaCols;
                    h = 480 * areaRows;
                    break;
                default:
                    break;
            }
            //余白分を追加
            if (MyData.IsMargin)
            {
                int margin = MyData.Margin;
                w += margin + (areaCols * margin);
                h += margin + (areaRows * margin);
            }
            return new Size(w, h);
            //MyStatusItemSaveImageSize.Content = $"保存サイズ(横{w}, 縦{strH})";
        }
        //保存サイズの再計算
        private void ChangedSaveImageSize()
        {
            Size size = GetSaveBitmapSize();
            string strH = size.Height == 0 ? "可変" : size.Height.ToString();

            MyStatusItemSaveImageSize.Content = $"保存サイズ(横{size.Width}, 縦{strH})";
        }


        #endregion ステータスバーの表示更新

        #region アプリの設定保存と読み込み
        private bool SaveData(string path, object data)
        {
            DataContractSerializer serializer = new(typeof(Data));
            XmlWriterSettings settings = new();
            settings.Encoding = new UTF8Encoding();
            try
            {
                using (XmlWriter xw = XmlWriter.Create(path, settings))
                {
                    serializer.WriteObject(xw, data);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"アプリの設定保存できなかった\n{ex.Message}",
                    $"{System.Reflection.Assembly.GetExecutingAssembly()}");
                return false;
            }
        }

        private Data LoadData(string path)
        {
            DataContractSerializer serializer = new(typeof(Data));
            try
            {
                using (XmlReader xr = XmlReader.Create(path))
                {
                    return (Data)serializer.ReadObject(xr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"読み込みできなかった\n{ex.Message}",
                    $"{System.Reflection.Assembly.GetExecutingAssembly().GetName()}");
                return null;
            }
        }

        #endregion アプリの設定保存と読み込み




        private void MyButtonTest_Click(object sender, RoutedEventArgs e)
        {
            var data = MyData;
            var size = MyData.Size;
            var row = MyData.Row;
            //MyThumbs[0].MyStrokeRectangle.Visibility = Visibility.Visible;

        }


        #region クリックとかのイベント関連

        private void MyButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (MyThumbs.Count <= 0) return;
            SaveImage();
        }

        private void MyButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveThumb(MyActiveThumb);
        }

        private void MyButtonRemoveArea_Click(object sender, RoutedEventArgs e)
        {
            RemoveAreaThumb();
        }

        private void MyButtonClear_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllThumbs();
        }
        private void MyUpDownCol_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangeLocate();
            ChangedSaveImageSize();
        }

        private void MyUpDownRow_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangeLocate();
            ChangedSaveImageSize();
        }

        private void MyUpDownSize_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangeLocate();
            MyStatusItem1.Content = MyUpDownSize.MyValue.ToString();
            ChangedSaveImageSize();
        }

        //クリップボードへコピー
        private void MyButtonToClipboard_Click(object sender, RoutedEventArgs e)
        {
            ToClipboard();
        }


        //クリップボードから追加
        private void MyButtonFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            AddFromClipboardImage();
        }

        private void RadioButtonSaveSize_Click(object sender, RoutedEventArgs e)
        {
            ChangedSaveImageSize();
        }
        private void MyCheckBoxMargin_Click(object sender, RoutedEventArgs e)
        {
            ChangedSaveImageSize();
        }
        private void MyUpDownMargin_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangedSaveImageSize();
        }

        //プレビューウィンドウ開く
        private void MyButtonPreview_Click(object sender, RoutedEventArgs e)
        {
            if (MyPreviewWindow == null)
            {
                MyPreviewWindow = new Preview(this, MyData);
                MyPreviewWindow.Owner = this;
                MyPreviewWindow.SetBitmap(MakeSaveBitmap());
                MyPreviewWindow.ShowDialog();

            }
        }

        //テンキーでThumbの移動、ActiveThumbの変更
        private void Thumb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            MoveActiveThumb(e.Key, Keyboard.Modifiers == ModifierKeys.Control);

            //MyCanvas.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(MyCanvas), 0, Key.Left) { RoutedEvent = Keyboard.KeyDownEvent });





            ////var key = Key.Insert;                    // Key to send
            //var target = Keyboard.FocusedElement;    // Target element
            //var routedEvent = Keyboard.KeyDownEvent; // Event to send
            //var ttt = sender as ImageThumb;

            //target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(ttt), 0, e.Key)

            //{ RoutedEvent = routedEvent });



        }



        //設定を名前を付けて保存
        private void MyButtonSaveData_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new();
            dialog.Filter = "(xml)|*.xml";
            if (dialog.ShowDialog() == true)
            {
                SaveData(dialog.FileName, MyData);
            }

        }

        //設定ファイルを選択して読み込み
        private void MyButtonLoadData_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.Filter = "(xml)|*.xml";
            if (dialog.ShowDialog() == true)
            {
                var data = LoadData(dialog.FileName);
                if (data == null) return;
                MyData = data;
                this.DataContext = MyData;
            }
        }

        //アプリ終了時、設定保存
        private void Window_Closed(object sender, EventArgs e)
        {
            _ = SaveData(AppDir + "\\" + APP_DATA_FILE_NAME, MyData);
        }



        #endregion クリックとかのイベント関連

    }



    //MainWindowのDataContextにBindingするデータ用クラス    
    [DataContract]//using System.Runtime.Serialization;
    public class Data// : System.ComponentModel.INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;
        //private void RaisePropertyChanged([CallerMemberName] string pName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pName));
        //}
        [DataMember] public int Row { get; set; } = 2;
        [DataMember] public int Col { get; set; } = 2;
        [DataMember] public int Size { get; set; } = 120;
        [DataMember] public int SaveWidth { get; set; } = 240;
        [DataMember] public int SaveHeight { get; set; } = 160;
        [DataMember] public int SaveOneWidth { get; set; } = 120;

        //ドラッグ移動での入れ替えモード、trueで入れ替え、falseは挿入
        [DataMember] public bool IsSwap { get; set; } = true;


        [DataMember] public bool IsSavedBitmapRemove { get; set; }

        //保存サイズの指定方法
        [DataMember] public SaveScaleSizeType SaveScaleSizeType { get; set; } = SaveScaleSizeType.OneWidth;
        //隙間を白で塗る
        [DataMember] public bool IsSaveBackgroundWhite { get; set; }
        //余白サイズ
        [DataMember] public int Margin { get; set; } = 10;
        //余白フラグ
        [DataMember] public bool IsMargin { get; set; } = false;

        //jpeg品質
        [DataMember] public int JpegQuality { get; set; } = 90;

        //Expanderの開閉状態
        [DataMember] public bool IsExpanded { get; set; } = true;

        //ウィンドウ位置、サイズ
        [DataMember] public double Top { get; set; } = 0;
        [DataMember] public double Left { get; set; } = 0;
        [DataMember] public double Width { get; set; } = 654;
        [DataMember] public double Height { get; set; } = 520;

        //プレビューウィンドウ位置、サイズ
        [DataMember] public double PreWindowTop { get; set; } = 100;
        [DataMember] public double PreWindowLeft { get; set; } = 100;
        [DataMember] public double PreWindowWidth { get; set; } = 614;
        [DataMember] public double PreWindowHeight { get; set; } = 520;



        //これはMainWindowの方で管理したほうが良かったかも
        public ObservableCollection<ImageThumb> MyThumbs { get; set; } = new();

        [OnDeserialized]
        private void OnDeserialized(System.Runtime.Serialization.StreamingContext c)
        {
            if (MyThumbs == null) MyThumbs = new();
        }

    }



    #region コンバーター
    //保存範囲を示す枠(MyRectangle)のサイズ用
    public class MyConverterRectangleSize : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            decimal hen = (decimal)values[0];
            decimal size = (decimal)values[1];
            return (double)(hen * size);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    //bool値反転コンバータ
    //ドラッグ移動での入れ替えモード、trueで入れ替え、falseは挿入
    public class MyConverterBoolReverce : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            return !b;
        }
    }

    //    C#のWPFでRadioButtonのIsCheckedに列挙型をバインドする - Ararami Studio
    //https://araramistudio.jimdo.com/2016/12/27/wpf%E3%81%A7radiobutton%E3%81%AEischecked%E3%81%AB%E5%88%97%E6%8C%99%E5%9E%8B%E3%82%92%E3%83%90%E3%82%A4%E3%83%B3%E3%83%89%E3%81%99%E3%82%8B/

    public class MyConverterSaveScaleType : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = parameter as string;
            var p = Enum.Parse(typeof(SaveScaleSizeType), str);
            var v = (SaveScaleSizeType)value;
            bool b = p.Equals(v);
            return b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = parameter as string;
            if (true.Equals(value))
                return Enum.Parse(targetType, str);
            else
                return DependencyProperty.UnsetValue;
        }
    }

    public class MyConverterBoolVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility v = (Visibility)value;
            return v == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion コンバーター


    #region 列挙型

    //保存時の縮尺指定
    public enum SaveScaleSizeType
    {
        //変更時はXAMLのほうでも変更する必要がある、radioButtonのところのConverterParameter

        OneWidth,//ひとつあたりの幅
        All,//全体
        TopLeft,//先頭画像横幅にあわせる
        Special,//特殊640x480
    }

    #endregion 列挙型









}
