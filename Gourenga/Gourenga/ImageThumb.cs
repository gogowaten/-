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
namespace Gourenga
{    
  public  class ImageThumb:Thumb
    {
        public BitmapSource MyBitmapSource;
        public Image MyImage;
        private Canvas MyPanel;
        //枠表示
        private Visibility myVisibleStroke;
        private Rectangle MyStrokeRectangle = new();
        public Visibility MyVisibleStroke
        {
            get => myVisibleStroke; set
            {
                //MyStrokeRectangle.Visibility = value;
                myVisibleStroke = value;
            }
        }
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

            //waku
            MyStrokeRectangle.Stroke = Brushes.Black;
            MyStrokeRectangle.StrokeThickness = 1.0;
            MyPanel.Children.Add(MyStrokeRectangle);
            BindingExpressionBase neko = MyStrokeRectangle.SetBinding(WidthProperty, MakeBinding(img, WidthProperty, BindingMode.OneWay));
            neko = MyStrokeRectangle.SetBinding(HeightProperty, MakeBinding(img, HeightProperty, BindingMode.OneWay));

            Binding b = new();
            b.Source = this;
            b.Path = new PropertyPath(nameof(MyVisibleStroke));
            BindingOperations.SetBinding(MyStrokeRectangle, VisibilityProperty, b);
        }
        private Binding MakeBinding(DependencyObject o,DependencyProperty prop,BindingMode mode)
        {
            Binding b = new();
            b.Source = o;
            b.Path = new PropertyPath(prop);
            b.Mode = mode;
            return b;
        }
    }
    class FlatThumb : Thumb
    {
        private Rectangle MyStrokeRectangle;

    }
}
