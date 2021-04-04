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

        //ImageThumb作成
        private void AddImageThumb(Image img)
        {
            if (img == null) return;

            //作成、Zオーダー、サイズBinding、Canvasに追加、管理リストに追加、マウス移動イベント
            ImageThumb thumb = new(img);
            Panel.SetZIndex(thumb, MyThumbs.Count);

            SetThumbSizeBinding(thumb);

            MyCanvas.Children.Add(thumb);
            MyThumbs.Add(thumb);

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
        private List<Rect> MakeRects()
        {
            List<Rect> drawRects = new();
            //横に並べる個数
            int MasuYoko = MyData.Col;

            int saveImageCount = MyData.Row * MyData.Col;
            if (saveImageCount > MyThumbs.Count) saveImageCount = MyThumbs.Count;

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            for (int i = 0; i < saveImageCount; i++)
            {
                //縮小サイズ決定、指定サイズの正方形に収まるようにアスペクト比保持で縮小
                //縦横それぞれの縮小率計算して小さい方に合わせる
                //拡大はしないので縮小率が1以上なら1に抑える
                BitmapSource bmp = MyThumbs[i].MyImage.Source as BitmapSource;
                double w = bmp.PixelWidth;
                double h = bmp.PixelHeight;
                double widthRatio = MyData.Size / w;
                double heightRatio = MyData.Size / h;
                double ratio = Math.Min(widthRatio, heightRatio);
                if (ratio > 1) ratio = 1;
                w *= ratio;
                h *= ratio;

                //X座標、中央揃え
                double x = (i % MasuYoko) * MyData.Size;
                x = x + (MyData.Size - w) / 2;

                //Y座標は後で計算
                drawRects.Add(new(x, 0, w, h));
            }

            //Y座標計算
            //Y座標はその行にある画像の中で最大の高さを求めて、中央揃えのY座標を計算
            //行ごとに計算する必要がある

            //今の行の基準Y座標、次の行へは今の行の高さを加算していく
            double kijun = 0;
            int count = 0;
            while (count < saveImageCount)
            {
                int end = count + MasuYoko;
                if (end > saveImageCount) end = saveImageCount;
                //Y座標計算
                kijun += SubFunc(count, end, kijun);
                //横に並べる個数が3なら0 3 6…
                count += MasuYoko;
            }

            //Y座標計算
            //開始と終了Index指定、基準値
            double SubFunc(int begin, int end, double kijun)
            {
                //行の高さを求める(最大の画像が収まる)
                double max = 0;
                for (int i = begin; i < end; i++)
                {
                    if (drawRects[i].Height > max) max = drawRects[i].Height;
                }
                //Y座標 = 基準値 + (行の高さ - 画像の高さ) / 2
                for (int i = begin; i < end; i++)
                {
                    Rect temp = drawRects[i];
                    temp.Y = kijun + (max - drawRects[i].Height) / 2;
                    drawRects[i] = temp;
                }
                return max;
            }
            return drawRects;
        }

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
        private List<Rect> MakeRectsTate()
        {
            //保存対象画像数と横に並べる個数
            int saveImageCount = MyData.Row * MyData.Col;
            int saveCols = MyData.Col;
            if (saveImageCount > MyThumbs.Count)
            {
                saveImageCount = MyThumbs.Count;
                saveCols = (int)Math.Ceiling((double)MyThumbs.Count / MyData.Row);
            }

            List<Rect> drawRects = new();

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            double yKijun = 0;
            double maxHeight = 0;

            for (int i = 0; i < MyData.Row; i++)
            {
                List<Rect> tempRect = new();
                for (int j = 0; j < saveCols; j++)
                {
                    int index = i + j * MyData.Row;
                    if (index > saveImageCount - 1) break;

                    //縮小後のサイズを計算
                    double w = MyThumbs[index].MyBitmapSource.PixelWidth;
                    double h = MyThumbs[index].MyBitmapSource.PixelHeight;
                    double ratio = GetScaleRatio(w, h, MyData.Size);
                    w *= ratio;
                    h *= ratio;
                    //この行の高さの最大値記録
                    if (h > maxHeight) maxHeight = h;
                    //X座標、中央揃え
                    double xx = (j % MyData.Col) * MyData.Size;
                    xx += (MyData.Size - w) / 2;
                    //Rect作成、Y座標は後で計算するので0にしておく
                    tempRect.Add(new(xx, 0, w, h));
                }
                //Y座標
                for (int j = 0; j < tempRect.Count; j++)
                {
                    Rect r = tempRect[j];
                    double y = yKijun + (maxHeight - r.Height) / 2;
                    drawRects.Add(new(r.X, y, r.Width, r.Height));
                }
                yKijun += maxHeight;
            }


            return drawRects;
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
        private List<Rect> MakeRects2()
        {
            //保存対象画像数と横に並べる個数
            int saveImageCount = MyData.Row * MyData.Col;
            int saveRows = MyData.Row;
            int saveCols = MyData.Col;
            //縦*横よりThumb数が少なければ枠を縮める必要がある
            if (MyData.Row * MyData.Col > MyThumbs.Count)
            {
                saveImageCount = MyThumbs.Count;
                saveCols = (int)Math.Ceiling((double)MyThumbs.Count / MyData.Row);
                saveRows = (int)Math.Ceiling((double)MyThumbs.Count / MyData.Col);
            }

            List<Rect> drawRects = new();

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            double pieceW = MyData.SaveWidth / saveCols;
            double pieceH = MyData.SaveHeight / saveRows;

            for (int iRow = 0; iRow < MyData.Row; iRow++)
            {
                for (int iCol = 0; iCol < saveCols; iCol++)
                {
                    int index = iRow * MyData.Col + iCol;
                    if (saveImageCount <= index) break;

                    double w = MyThumbs[index].MyBitmapSource.PixelWidth;
                    double h = MyThumbs[index].MyBitmapSource.PixelHeight;
                    double ratio = GetScaleRatio(pieceW, pieceH, w, h);
                    w *= ratio;
                    h *= ratio;
                    double x = iCol * pieceW;
                    x += (pieceW - w) / 2;
                    double y = iRow * pieceH;
                    y += (pieceH - h) / 2;
                    drawRects.Add(new Rect(x, y, w, h));
                }
            }

            return drawRects;
        }

        //保存サイズは1画像の横幅を基準にする場合
        private List<Rect> MakeRects3()
        {
            //保存対象画像数と横に並べる個数
            int saveImageCount = MyData.Row * MyData.Col;
            //int saveRows = MyData.Row;
            int saveCols = MyData.Col;
            //縦*横よりThumb数が少なければ枠を縮める必要がある
            if (MyData.Row * MyData.Col > MyThumbs.Count)
            {
                saveImageCount = MyThumbs.Count;
                saveCols = (int)Math.Ceiling((double)MyThumbs.Count / MyData.Row);
                //saveRows = (int)Math.Ceiling((double)MyThumbs.Count / MyData.Col);
            }

            List<Rect> drawRects = new();

            //サイズとX座標
            //指定横幅に縮小、アスペクト比は保持
            double pieceW = MyData.SaveOneWidth;

            double yKijun = 0;
            List<Rect> tempRect = new();
            for (int iRow = 0; iRow < MyData.Row; iRow++)
            {
                double maxH = 0;
                tempRect.Clear();
                for (int iCol = 0; iCol < saveCols; iCol++)
                {
                    int index = iRow * MyData.Col + iCol;
                    if (saveImageCount <= index) break;

                    double w = MyThumbs[index].MyBitmapSource.PixelWidth;
                    double h = MyThumbs[index].MyBitmapSource.PixelHeight;
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
            //描画する座標とサイズを取得
            //List<Rect> drawRects = MakeRects2();
            //List<Rect> drawRects = MakeRectsTate();
            //List<Rect> drawRects = MakeRects();
            List<Rect> drawRects = new();
            if (MyData.IsSaveSizeOneWidth)
            {
                drawRects = MakeRects3();
            }
            else
            {
                drawRects = MakeRects2();
            }

            DrawingVisual dv = new();


            using (DrawingContext dc = dv.RenderOpen())
            {
                for (int i = 0; i < drawRects.Count; i++)
                {
                    BitmapSource source = MyThumbs[i].MyBitmapSource;
                    dc.DrawImage(source, drawRects[i]);
                }
            }



            if (MyData.IsSaveSizeOneWidth)
            {
                //最終的な全体画像サイズ計算、RectのUnionを使う
                Rect dRect = new();
                for (int i = 0; i < drawRects.Count; i++)
                {
                    dRect = Rect.Union(dRect, drawRects[i]);
                }
                int width = (int)dRect.Width;
                int height = (int)dRect.Height;
                RenderTargetBitmap render = new(width, height, 96, 96, PixelFormats.Pbgra32);
                render.Render(dv);
                return render;
            }
            else
            {
                //決め打ちサイズ
                RenderTargetBitmap render = new(MyData.SaveWidth, MyData.SaveHeight, 96, 96, PixelFormats.Pbgra32);
                render.Render(dv);
                return render;

            }

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
                BitmapEncoder encoder = null;
                switch (saveFileDialog.FilterIndex)
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
                }

            }
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
        }


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


        #region クリックとかのイベント関連

        private void MyButtonTest_Click(object sender, RoutedEventArgs e)
        {
            var data = MyData;
            var size = MyData.Size;
            var row = MyData.Row;
            //MyThumbs[0].MyStrokeRectangle.Visibility = Visibility.Visible;

        }

        private void MyButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (MyThumbs.Count <= 0) return;
            SaveImage();
        }

        private void MyButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveThumb(MyActiveThumb);
        }

        private void MyUpDownCol_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangeLocate();
        }

        private void MyUpDownRow_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangeLocate();
        }

        private void MyUpDownSize_MyValueChanged(object sender, ControlLibraryCore20200620.MyValuechangedEventArgs e)
        {
            ChangeLocate();
            MyStatusItem1.Content = MyUpDownSize.MyValue.ToString();
        }

        private void RadioButtonSort_Click(object sender, RoutedEventArgs e)
        {
            ChangeLocate();
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
        //画像の並び順、trueで横、falseは縦
        //public bool IsHorizontalSort { get; set; } = true;
        //保存サイズの指定方法、trueで1画像の幅基準、falseは保存画像縦横指定
        public bool IsSaveSizeOneWidth { get; set; } = true;

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

    #endregion コンバーター
}
