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
        //private ObservableCollection<ImageThumb> MyThumbs = new();
        private ObservableCollection<ImageThumb> MyThumbs;
        private ImageThumb MyActiveThumb
        {
            get => myActiveThumb; set
            {
                var neko = MyThumbs.IndexOf(MyActiveThumb);
                var inu = MyThumbs.IndexOf(myActiveThumb);
                var uma = MyThumbs.IndexOf(value);
                if (myActiveThumb != null)
                {
                    MyActiveThumb.MyStrokeRectangle.Visibility = Visibility.Collapsed;

                }
                myActiveThumb = value;
                value.MyStrokeRectangle.Visibility = Visibility.Visible;
            }
        }
        private List<Point> MyLocate = new();
        private Data MyData;
        private ImageThumb myActiveThumb;

        public MainWindow()
        {
            InitializeComponent();
            MyInitialize();

        }
        private void MyInitialize()
        {
            this.Left = 0;
            this.Top = 0;
            MyData = new();
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

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) == false) return;
            var datas = (string[])e.Data.GetData(DataFormats.FileDrop);
            var paths = datas.ToList();
            paths.Sort();

            for (int i = 0; i < paths.Count; i++)
            {
                AddImage(MakeBitmapSourceBgra32FromFile(paths[i]));
            }
            Panel.SetZIndex(MyRectangle, MyThumbs.Count + 1);
            //SetMyCanvasSize();

            if (MyActiveThumb == null)
            {
                MyActiveThumb = MyThumbs[MyThumbs.Count - 1];
            }
        }

        private void AddImage(BitmapSource source)
        {
            if (source == null) return;

            Image img = new() { Source = source, StretchDirection = StretchDirection.DownOnly };
            img.Width = MyData.Size;
            img.Height = MyData.Size;

            int i = MyThumbs.Count;
            int x = i % MyData.Col * MyData.Size;
            int y = (int)((double)i / MyData.Col) * MyData.Size;
            MyLocate.Add(new Point(x, y));
            ImageThumb thumb = new(img);// new(img, 0, 0, x, y);
            
            Canvas.SetLeft(thumb, x);
            Canvas.SetTop(thumb, y);
            SetThumbSizeBinding(thumb);

            MyCanvas.Children.Add(thumb);
            Panel.SetZIndex(thumb, i);
            MyThumbs.Add(thumb);
            thumb.Width = MyData.Size;
            thumb.Height = MyData.Size;

            //マウスドラッグ移動
            //開始時

            thumb.DragStarted += (s, e) =>
            {
                thumb.Opacity = 0.5;
                MyActiveThumb.MyStrokeRectangle.Visibility = Visibility.Collapsed;

                //最上面表示、インデックス取得
                Panel.SetZIndex(thumb, MyThumbs.Count);

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
                //SetLocate(index);
                MyActiveThumb = thumb;

            };
            SetMyCanvasSize();
        }

        private void SetThumbSizeBinding(ImageThumb t)
        {
            Binding b = new();
            b.Source = MyUpDownSize;
            b.Path = new PropertyPath(ControlLibraryCore20200620.NumericUpDown.MyValueProperty);
            t.MyImage.SetBinding(WidthProperty, b);
            t.MyImage.SetBinding(HeightProperty, b);

        }

        //座標リスト更新
        private void ChangeLocate()
        {
            if (MyThumbs == null) return;
            for (int i = 0; i < MyThumbs.Count; i++)
            {
                int x = i % MyData.Col * MyData.Size;
                int y = (int)((double)i / MyData.Col) * MyData.Size;
                MyLocate[i] = new Point(x, y);
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
            t.Opacity = 0.5;

            Idou移動中処理(t, x, y);
        }

        //2点間距離
        private double GetDistance(Point a, Point b)
        {
            return Math.Sqrt((a - b) * (a - b));
        }
        #endregion ドラッグ移動系

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

        }

        private void MyButtonRemove_Click(object sender, RoutedEventArgs e)
        {

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

    public class Data : System.ComponentModel.INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string pName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pName));
        }

        public int Row { get; set; } = 2;
        public int Col { get; set; } = 3;
        public int Size { get; set; } = 40;

    

        public ObservableCollection<ImageThumb> MyThumbs { get; set; } = new();


    }





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
