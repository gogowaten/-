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


        public MainWindow()
        {
            InitializeComponent();
            MyInitialize();

        }
        private void MyInitialize()
        {
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
        /// <param name="t">ドラッグ移動中のThumb</param>
        /// <param name="x">Canvas上でのX座標</param>
        /// <param name="y">Canvas上でのY座標</param>
        private void Idou移動中処理(ImageThumb t, double x, double y)
        {
            int imaIndex = MyThumbs.IndexOf(t);//ドラッグ移動中ThumbのIndex            

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

            //入れ替え発生時
            //最短距離のIndexと移動中のThumbのIndexが違うなら入れ替え処理
            if (moyoriIndex != imaIndex)
            {
                //Thumbリストのindexを入れ替え
                MyThumbs.RemoveAt(imaIndex);
                MyThumbs.Insert(moyoriIndex, t);

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
                //サイズ
                BitmapSource bmp = MyThumbs[i].MyImage.Source as BitmapSource;
                double width = bmp.PixelWidth;
                double ratio = MyData.Size / width;
                if (ratio > 1) ratio = 1;
                width *= ratio;

                //X座標、中央揃え
                double x = (i % MasuYoko) * MyData.Size;
                x = x + (MyData.Size - width) / 2;

                //Y座標は後で計算
                drawRects.Add(new(x, 0, width, bmp.PixelHeight * ratio));
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
            List<Rect> drawRects = MakeRects();

            DrawingVisual dv = new();
            using (DrawingContext dc = dv.RenderOpen())
            {
                for (int i = 0; i < drawRects.Count; i++)
                {
                    BitmapSource source = MyThumbs[i].MyImage.Source as BitmapSource;
                    dc.DrawImage(source, drawRects[i]);
                }
            }
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



        #region クリックとかのイベント関連

        private void MyButtonTest_Click(object sender, RoutedEventArgs e)
        {
            var data = MyData;
            var size = MyData.Size;
            var row = MyData.Row;
            MyThumbs[0].MyStrokeRectangle.Visibility = Visibility.Visible;

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
        public int Col { get; set; } = 3;
        public int Size { get; set; } = 40;


        //これはMainWindowの方で管理したほうが良かったかも
        public ObservableCollection<ImageThumb> MyThumbs { get; set; } = new();


    }




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


}
