using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.Windows.Shapes;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

using System.Globalization;

//Thumbを継承
//ControlTemplateを変更してCanvasパネルにImageと枠表示用のRectangleを追加したThumb
//枠サイズはImageサイズとBinding
namespace Gourenga
{
    public class ImageThumb : Thumb
    {
        public BitmapSource MyBitmapSource;
        public Image MyImage;
        private Canvas MyPanel;
        public Rectangle MyStrokeRectangle = new();
        public Rectangle MyStrokeRectangle2 = new();
      

        public ImageThumb(Image img)
        {
            ControlTemplate template = new(typeof(Thumb));
            template.VisualTree = new FrameworkElementFactory(typeof(Canvas), "panel");
            this.Template = template;
            this.ApplyTemplate();
            MyPanel = (Canvas)template.FindName("panel", this);
            MyPanel.Background = Brushes.Transparent;
            this.Focusable = true;//フォーカスできる

            this.SetBinding(WidthProperty, new Binding("Size"));
            this.SetBinding(HeightProperty, new Binding("Size"));

            MyPanel.Children.Add(img);
            MyImage = img;
            MyBitmapSource = img.Source as BitmapSource;

            //waku
            MyStrokeRectangle2.Visibility = Visibility.Collapsed;
            MyStrokeRectangle2.Stroke = Brushes.White;
            MyStrokeRectangle2.StrokeThickness = 2.0;
            //MyStrokeRectangle.StrokeDashArray = new DoubleCollection() { 1 };
            MyPanel.Children.Add(MyStrokeRectangle2);
            _ = MyStrokeRectangle2.SetBinding(WidthProperty, MakeBinding(img, WidthProperty, BindingMode.OneWay));
            _ = MyStrokeRectangle2.SetBinding(HeightProperty, MakeBinding(img, HeightProperty, BindingMode.OneWay));
            MyStrokeRectangle2.SetBinding(VisibilityProperty, MakeBinding(MyStrokeRectangle, VisibilityProperty, BindingMode.OneWay));
            
            MyStrokeRectangle.Visibility = Visibility.Collapsed;
            MyStrokeRectangle.Stroke = Brushes.Magenta;
            MyStrokeRectangle.StrokeThickness = 2.0;
            MyStrokeRectangle.StrokeDashArray = new DoubleCollection() { 2,2 };
            MyPanel.Children.Add(MyStrokeRectangle);
            _ = MyStrokeRectangle.SetBinding(WidthProperty, MakeBinding(img, WidthProperty, BindingMode.OneWay));
            _ = MyStrokeRectangle.SetBinding(HeightProperty, MakeBinding(img, HeightProperty, BindingMode.OneWay));

            
        }
        private Binding MakeBinding(DependencyObject o, DependencyProperty prop, BindingMode mode)
        {
            Binding b = new();
            b.Source = o;
            b.Path = new PropertyPath(prop);
            b.Mode = mode;
            return b;
        }
    }
   
}
