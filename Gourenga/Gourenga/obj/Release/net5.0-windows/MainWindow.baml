   M S B A M L   `   `   `  ���� U  QControlLibraryCore20200620, Version=1.2.2.0, Culture=neutral, PublicKeyToken=nullkLclr-namespace:ControlLibraryCore20200620;assembly=ControlLibraryCore20200620ControlLibraryCore20200620  C ?Gourenga, Version=1.8.0.0, Culture=neutral, PublicKeyToken=null#clr-namespace:GourengaGourenga    Gourenga.MainWindow   \ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     \nupLclr-namespace:ControlLibraryCore20200620;assembly=ControlLibraryCore20200620     (localclr-namespace:Gourenga     -     � Title$  
MainWindow�� � 
ResizeMode$ CanResizeWithGrip=� J� 	AllowDrop$
 True������  �� Path$ Height� �� Mode$ TwoWay=����� $ Width�$ TwoWay=�
 � Top �� $	 Top�$ TwoWay=� � Left �� $
 Left�$ TwoWay=� � DataContext   Gourenga.Data  .���  � UseLayoutRounding$
 True��.+�]� 	 � 	Resources	 %  Gourenga.MyConverterBoolReverce   bool��%   &	          .+��� $��Bottom=�.@��� -   
 ��Name$
 MyStatusItem1��$��item1���� �� -   $
 MyStatusItemSaveImageSize��$��save image size���� �� -   $
 MyStatusItemPrecessed��$��Right=�$��	processed��$��0,0,10,0q�]� $	��Top=�	  �� 
TargetType �� Property �� Value��%F   '��       �� " ��.���  � ��$	 5,4q���  � ��$
 10,2q�.+��� $��Right=� �� Orientation$ 
Horizontal=�.+��� -    ��Name$ MyButtonRemove��$��削除���� .+��� -   $ MyButtonRemoveArea��$��範囲削除���� -   $ MyButtonClear��$��	全削除���� $ 
Horizontal=�
�������.+��� -   $ MyButtonTest��$
��test��$y�	Collapsed=��� -	   $ MyButtonSave��$
��Left=�$��連結保存���� .+��� -
   $ MyButtonSaveData��$��設定保存���� -   $ MyButtonLoadData��$��Bottom=�$��設定読込���� -   $ MyButtonPreview��$��Bottom=�$��プレビュー���� .+��� -   $ MyButtonToClipboard��$��	コピー���� -   $ MyButtonFromClipboard��$��貼り付け��9� $	��Top=�$��設定�� 9� 
IsExpanded$
 True�� 9� ExpandDirection$
 Down=�.���� $	��Top=�$ 
Horizontal=�.+��� $��
10,10,5,10q�.+��� $��連結範囲��.���� $ 
Horizontal=�.+�.   (ControlLibraryCore20200620.NumericUpDown  -   $��MyUpDownCol��   
MyMinValue$ 1~�   
MyMaxValue$ 10~�   MyStringFormat$ 	横数 00��$��60��   MyValue �� $	 Col�  -   $��MyUpDownRow��$ 1~�$ 10~�$ 	縦数 00��$��60��$��10,0,0,0q� �� $	 Row�  -   $��MyUpDownSize��$ 40~�$	 960~�$ 1表示サイズ 000��   MySmallChange$ 40~�   MyLargeChange$ 40~�$	��100��   MyTextAlignment$ Center=�$��5,4,5,0q� �� $
 Size��� $��移動モード��$��0,5,0,0q�.���� $ 
Horizontal=�.+�	� $��交換��$	��0,5q� d� 	IsChecked �� *IsSwap+	� $��挿入��$��15,5,0,0q� �� *IsSwap+ �� 	Converter# [  �� $��	5,10,5,10q�	 ��%3   '        �� "  .���  � ��$ 10,0,0,0q�.+��� $��保存サイズ指定��.���� $ Vertical=�	 '  !Gourenga.MyConverterSaveScaleType  ConvSaveType��%   &	         .+��� $��1q�
����ح�._��� $ 
Horizontal=�$��10,6,0,0q�.+�	� -   $��
1画像幅��$��Center=�$��80�� �� $ SaveScaleSizeType�# [  �� ConverterParameter$ OneWidth��  -   $��MyUpDownOneWidth��$ 1~�$
 1200~�$ 	横 00000��$ 1~�$ 40~�$��70�� �� $ SaveOneWidth��� $��1q�
����ح�._��� $ 
Horizontal=�$��10,0,0,0q�.+�	� -   $��全体サイズ��$��Center=�$��80�� �� $ SaveScaleSizeType�# [ $	 All���� .+�  -   $��MyUpDownWidth��$ 1~�$ 10240~�$ 	横 00000��$ 1~�$ 40~�$��70�� �� $ 	SaveWidth�  -   $��MyUpDownHeight��$ 1~�$ 10240~�$ 	縦 00000��$ 1~�$ 40~�$��70�� �� $ 
SaveHeight��� $��1q�
����ح�._��� $ 
Horizontal=�$��10,0,0,0q�.+�	� -   $-��'左上画像の横幅を基準にする�� �� $ SaveScaleSizeType�# [ $ TopLeft���� $��	5,10,5,10q�.+��� $��保存時の動作��.���� .+��� $-��'保存した画像は非表示にする��$
��10,4q� �� $ IsSavedBitmapRemove�$ TwoWay=��� $*��$画像同士の隙間は白にする��$
��10,4q� �� $ IsSaveBackgroundWhite�$ TwoWay=��� $ 
Horizontal=�.+��� -    ��Name$ MyCheckBoxMargin��$��余白を付ける��$��Center=�$
��10,4q� �� *
IsMargin+$ TwoWay=�  -   $��MyUpDownMargin��$ 0~�$	 100~�$	 000�� �� *Margin+{��� *
IsMargin+�� $ 
Horizontal=�$��	10,4,10,0q�.+��� $��
Jpeg品質��$��Center=�  -   $��MyUpDownJpeg��$ 80~�$	 100~�$	 000��$ 1~�$ 1~� �� *JpegQuality+�� $
��Auto=�$
��Auto=�$|�False��.���� -    ��Name$ MyCanvas��$��10q�$
��Left=�$	��Top=�.+��� -    ��Name$ MyRectangle��
����� �$��5�� J� Opacity$	 0.7Q�	 '  !Gourenga.MyConverterRectangleSize  Size��%   &	         ��`�   `� 	Converter#  [ .1��� ! �� ElementName$! MyUpDownCol��$ MyValue��� $! MyUpDownSize��$ MyValue���`� #  [ .1��� $! MyUpDownRow��$ MyValue��� $! MyUpDownSize��$ MyValue�