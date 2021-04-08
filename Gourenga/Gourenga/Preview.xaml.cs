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
using System.Windows.Shapes;

namespace Gourenga
{
    /// <summary>
    /// Preview.xaml の相互作用ロジック
    /// </summary>
    public partial class Preview : Window
    {
        private MainWindow MyMainWindow;
        public Preview(MainWindow window)
        {
            InitializeComponent();
            MyMainWindow = window;

            //背景を市松模様にする
            this.Background = MakeTileBrush(MakeCheckeredPattern(10, Colors.WhiteSmoke, Colors.LightGray));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //ウィンドウを閉じた直後
            MyMainWindow.MyPreviewWindow = null;
        }

      
        /// <summary>
        /// 市松模様の元になる画像作成、2色を2マスずつ合計4マス交互に並べた画像、
        /// □■
        /// ■□
        /// </summary>
        /// <param name="cellSize">1マスの1辺の長さ、作成される画像はこれの2倍の1辺になる</param>
        /// <param name="c1">色1</param>
        /// <param name="c2">色2</param>
        /// <returns>画像のピクセルフォーマットはBgra32</returns>
        private WriteableBitmap MakeCheckeredPattern(int cellSize, Color c1, Color c2)
        {
            int width = cellSize * 2;
            int height = cellSize * 2;
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            int stride = 4 * width;// wb.Format.BitsPerPixel / 8 * width;
            byte[] pixels = new byte[stride * height];
            //すべてを1色目で塗る
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = c1.B;
                pixels[i + 1] = c1.G;
                pixels[i + 2] = c1.R;
                pixels[i + 3] = c1.A;
            }

            //2色目で市松模様にする
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //左上と右下に塗る
                    if ((y < cellSize & x < cellSize) | (y >= cellSize & x >= cellSize))
                    {
                        int p = y * stride + x * 4;
                        pixels[p] = c2.B;
                        pixels[p + 1] = c2.G;
                        pixels[p + 2] = c2.R;
                        pixels[p + 3] = c2.A;
                    }
                }
            }
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return wb;
        }

        /// <summary>
        /// BitmapからImageBrush作成
        /// 引き伸ばし無しでタイル状に敷き詰め
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private ImageBrush MakeTileBrush(BitmapSource bitmap)
        {
            var imgBrush = new ImageBrush(bitmap);
            imgBrush.Stretch = Stretch.None;//これは必要ないかも
            //タイルモード、タイル
            imgBrush.TileMode = TileMode.Tile;
            //タイルサイズは元画像のサイズ
            imgBrush.Viewport = new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            //タイルサイズ指定方法は絶対値、これで引き伸ばされない
            imgBrush.ViewportUnits = BrushMappingMode.Absolute;
            return imgBrush;
        }






        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //起動直後
            //表示位置をメインウィンドウの少し下にする
            var main = MyMainWindow.PointToScreen(new Point());
            this.Top = main.Y + 130;
            this.Left = main.X;

            ////            WPFで、スクリーンの正確な解像度を取得する方法 | // もちぶろ
            ////https://slash-mochi.net/?p=3370

            ////ウィンドウサイズはデスクトップ解像度の半分
            //Matrix displayScale = PresentationSource.FromVisual(MyMainWindow).CompositionTarget.TransformToDevice;
            ////var vw = SystemParameters.VirtualScreenWidth;//マルチ画面のときはこれ
            ////var psw = SystemParameters.PrimaryScreenWidth;//プリマリの解像度？
            //this.Height = SystemParameters.PrimaryScreenHeight * displayScale.M11 / 2;
            //this.Width = SystemParameters.PrimaryScreenWidth * displayScale.M22 / 2;


            this.VisualBitmapScalingMode = BitmapScalingMode.Fant;
        }
        public void SetBitmap(BitmapSource source)
        {
            MyPreviewImage.Source = source;
        }
    }
}
