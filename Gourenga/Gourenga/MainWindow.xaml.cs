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

namespace Gourenga
{
    public partial class MainWindow : Window
    {
        //アプリ情報表示用、名前、バージョン
        private const string APP_NAME = "絶対画連合連画";
        private string AppVersion;

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
                }
            }
        }


        private List<Point> MyLocate = new();//座標リスト
        private Data MyData;//DataContextに指定する
        private string LastDirectory;//ドロップしたファイルのフォルダパス
        private string LastFileName;//ドロップしたファイル名
        private int LastFileExtensionIndex;//ドロップしたファイルの拡張子判別用インデックス
        private int OriginalIndex;//移動前のIndex

        public MainWindow()
        {
            InitializeComponent();
            MyInitialize();

        }
        private void MyInitialize()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = i; j < 12; j += 3)
                {
                    var neko = j;
                }
            }
#if DEBUG
            this.Left = 0;
            this.Top = 0;
            MyButtonTest.Visibility = Visibility.Visible;
#endif

            MyData = new();
            this.DataContext = MyData;
            MyThumbs = MyData.MyThumbs;

            //アプリ情報
            string[] coms = Environment.GetCommandLineArgs();
            AppVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(coms[0]).FileVersion;
            this.Title = APP_NAME + AppVersion;
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


                //indexに従って表示位置変更
                SetLocate(moyoriIndex);
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
                    double y = iRow * hOne;
                    y += (hOne - h) / 2;
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
                    double yy = margin + (y * (margin + hOne));
                    yy += (hOne - h) / 2;
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
                case SaveScaleSizeType.MatchTopLeftImage:
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
                    w *= ratio;
                    h *= ratio;
                    if (h > maxH) maxH = h;
                    double x = iCol * pieceW;
                    x += (pieceW - w) / 2;

                    tempRect.Add(new Rect(x, 0, w, h));
                }
                for (int i = 0; i < tempRect.Count; i++)
                {
                    Rect r = tempRect[i];
                    double y = yKijun + (maxH - r.Height) / 2;
                    drawRects.Add(new Rect(r.X, y, r.Width, r.Height));
                }
                yKijun += maxH;
            }

            return drawRects;
        }

        //保存サイズは1画像の横幅を基準にする場合
        private List<Rect> MakeRectsForWidthTypeWithMargin()
        {
            //保存対象画像数と実際の縦横個数を取得
            (int imageCount, int areaRows, int areaCols) = GetMakeRectParam();

            List<Rect> drawRects = new();

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            double pieceW = MyData.SaveOneWidth;
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.MatchTopLeftImage)
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

                    tempRect.Add(new Rect(xx, 0, w, h));
                }
                for (int i = 0; i < tempRect.Count; i++)
                {
                    Rect r = tempRect[i];
                    double yy = margin + yKijun + (maxH - r.Height) / 2;
                    drawRects.Add(new Rect(r.X, yy, r.Width, r.Height));
                }
                yKijun += maxH + margin;
            }

            return drawRects;
        }



        //日時をstringで取得
        private string MakeDateString()
        {
            DateTime time = DateTime.Now;
            string str = time.ToString("yyyyMMdd_HHmmssfff");
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
                case (SaveScaleSizeType.Overall, true):
                    drawRects = MakeRectsForOverallTypeWithMargin();
                    break;

                case (SaveScaleSizeType.Overall, false):
                    drawRects = MakeRectsForOverallType();
                    break;

                case (SaveScaleSizeType.OneWidth, true):
                    drawRects = MakeRectsForWidthTypeWithMargin();
                    break;

                case (SaveScaleSizeType.OneWidth, false):
                    drawRects = MakeRectsForWidthType();
                    break;

                case (SaveScaleSizeType.MatchTopLeftImage, true):
                    drawRects = MakeRectsForWidthTypeWithMargin();
                    break;

                case (SaveScaleSizeType.MatchTopLeftImage, false):
                    drawRects = MakeRectsForWidthType();
                    break;

                default:
                    drawRects = MakeRectsForOverallType();
                    break;
            }
            //最終的な全体画像サイズ取得
            Size renderBimpSize = GetRenderSize(drawRects);
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.Overall)
            {
                renderBimpSize.Width = MyData.SaveWidth;
                renderBimpSize.Height = MyData.SaveHeight;
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
                        (int)(renderBimpSize.Width + 0.5),
                        (int)(renderBimpSize.Height + 0.5)));
                }
                //各画描画
                for (int i = 0; i < drawRects.Count; i++)
                {
                    BitmapSource source = MyThumbs[i].MyBitmapSource;
                    dc.DrawImage(source, drawRects[i]);
                }
            }
            //var neko = dv.ContentBounds;

            //Bitmap作成
            RenderTargetBitmap renderBitmap;
            if (MyData.SaveScaleSizeType == SaveScaleSizeType.OneWidth ||
                MyData.SaveScaleSizeType == SaveScaleSizeType.MatchTopLeftImage)
            {
                //サイズは四捨五入
                int width = (int)(renderBimpSize.Width + 0.5);
                int height = (int)(renderBimpSize.Height + 0.5);
                renderBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(dv);
            }
            else if (MyData.SaveScaleSizeType == SaveScaleSizeType.Overall)
            {
                //決め打ちサイズ
                renderBitmap = new(MyData.SaveWidth, MyData.SaveHeight, 96, 96, PixelFormats.Pbgra32);
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
            else if (MyData.SaveScaleSizeType == SaveScaleSizeType.MatchTopLeftImage &&
                MyData.IsMargin)
            {
                dRect.Width += margin;
                dRect.Height += margin;
            }
            return dRect.Size;
        }
        private void SaveImage()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "*.png|*.png|*.jpg|*.jpg|*.bmp|*.bmp|*.gif|*.gif|*.tiff|*.tiff|*.wdp|*.wdp;*jxr";
            saveFileDialog.AddExtension = true;//ファイル名に拡張子追加

            //初期フォルダ指定、開いている画像と同じフォルダ
            saveFileDialog.InitialDirectory = LastDirectory;
            saveFileDialog.FileName = LastFileName + "_";
            //saveFileDialog.FileName = MakeDateString();

            saveFileDialog.FilterIndex = LastFileExtensionIndex;

            if (saveFileDialog.ShowDialog() == true)
            {
                BitmapEncoder encoder = MakeBitmapEncoder(saveFileDialog.FilterIndex);
                encoder.Frames.Add(BitmapFrame.Create(MakeSaveBitmap(), null, MakeMetadata(encoder), null));
                using (var fs = new System.IO.FileStream(
                    saveFileDialog.FileName,
                    System.IO.FileMode.Create,
                    System.IO.FileAccess.Write))
                {
                    //保存
                    encoder.Save(fs);
                    //保存フォルダのパスと拡張子を記録
                    LastDirectory = System.IO.Path.GetDirectoryName(saveFileDialog.FileName);
                    LastFileExtensionIndex = saveFileDialog.FilterIndex;
                    if (MyData.IsSavedBitmapRemove)
                    {
                        RemoveAreaThumb();
                    }
                }

            }
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
                    encoder = new JpegBitmapEncoder();
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
                    var wmp = new WmpBitmapEncoder();
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
            BitmapSource bmp = MakeSaveBitmap();
            ClipboadSetImageWithPng(bmp);
        }
        //クリップボードから追加
        private void AddFromClipboardImage()
        {
            BitmapSource bmp = GetImageFromClipboardWithPNG();// Clipboard.GetImage();
            if (bmp != null)
            {
                AddImageThumb(MakeImage(bmp));
                ChangeLocate();
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.None:
                    switch (e.Key)
                    {
                        case Key.Delete:
                            RemoveThumb(MyActiveThumb);
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
                            MyUpDownSize.MyValue += MyUpDownSize.MySmallChange;
                            break;
                        case Key.Down:
                            MyUpDownSize.MyValue -= MyUpDownSize.MySmallChange;
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
        private void ChangedSaveImageSize()
        {
            if (MyData == null) return;
            if (MyThumbs.Count == 0) return;
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
                case SaveScaleSizeType.Overall:
                    w = MyData.SaveWidth;
                    h = MyData.SaveHeight;
                    break;
                //左上画像の横幅
                case SaveScaleSizeType.MatchTopLeftImage:
                    w = MyThumbs[0].MyBitmapSource.PixelWidth * areaCols;
                    h = MyThumbs[0].MyBitmapSource.PixelHeight * areaRows;
                    if (MyData.IsMargin)
                    {
                        w += (areaCols + 1) * MyData.Margin;
                        h += (areaRows + 1) * MyData.Margin;
                    }
                    break;
                default:
                    break;
            }
            string strH = h == 0 ? "可変" : h.ToString();

            MyStatusItemSaveImageSize.Content = $"保存サイズ(横{w}, 縦{strH})";
        }

        #endregion ステータスバーの表示更新

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



        #endregion クリックとかのイベント関連

    }

    //MainWindowのDataContextにBindingするデータ用クラス
    public class Data// : System.ComponentModel.INotifyPropertyChanged
    {
        //public event PropertyChangedEventHandler PropertyChanged;
        //private void RaisePropertyChanged([CallerMemberName] string pName = null)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pName));
        //}

        public int Row { get; set; } = 2;
        public int Col { get; set; } = 2;
        public int Size { get; set; } = 120;
        public int SaveWidth { get; set; } = 240;
        public int SaveHeight { get; set; } = 160;
        public int SaveOneWidth { get; set; } = 120;

        //ドラッグ移動での入れ替えモード、trueで入れ替え、falseは挿入
        public bool IsSwap { get; set; } = true;


        public bool IsSavedBitmapRemove { get; set; }

        //保存サイズの指定方法
        public SaveScaleSizeType SaveScaleSizeType { get; set; } = SaveScaleSizeType.OneWidth;
        //隙間を白で塗る
        public bool IsSaveBackgroundWhite { get; set; }
        //余白サイズ
        public int Margin { get; set; }
        //余白フラグ
        public bool IsMargin { get; set; } = false;


        //これはMainWindowの方で管理したほうが良かったかも
        public ObservableCollection<ImageThumb> MyThumbs { get; set; } = new();


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
    #endregion コンバーター


    #region 列挙型

    //保存時の縮尺指定
    public enum SaveScaleSizeType
    {
        OneWidth,//ひとつあたりの幅
        Overall,//全体
        MatchTopLeftImage,//先頭画像横幅にあわせる
    }

    #endregion 列挙型









}
