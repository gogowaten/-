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
      

        public ImageThumb(Image img)
        {
            ControlTemplate template = new(typeof(Thumb));
            template.VisualTree = new FrameworkElementFactory(typeof(Canvas), "panel");
            this.Template = template;
            this.ApplyTemplate();
            MyPanel = (Canvas)template.FindName("panel", this);
            MyPanel.Background = Brushes.Transparent;

            MyPanel.Children.Add(img);
            MyImage = img;
            MyBitmapSource = img.Source as BitmapSource;

            //waku
            MyStrokeRectangle.Visibility = Visibility.Collapsed;
            MyStrokeRectangle.Stroke = Brushes.Tomato;
            MyStrokeRectangle.StrokeThickness = 1.0;
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
