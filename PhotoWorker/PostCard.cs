//using DevExpress.XtraTreeList.Nodes;
using Photoshop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PhotoWorker
{
    public class PostCard:PostCardType 
    {
        public static List<PostCard> AllPostCards = new List<PostCard>();

        public delegate void ProcessHandler(PostCard sender);

        public static ProcessHandler BeginPreprocess=DoNothing;
        public static ProcessHandler BegineProcess = DoNothing;
        public static ProcessHandler EndPreprocess = DoNothing;
        public static ProcessHandler EndProcess = DoNothing;
        public static ProcessHandler BeginRecut = DoNothing;


        private static void DoNothing(PostCard sender)
        {
            //throw new NotImplementedException();
        }


        private int _angle;
        private static Application _myPs;
        public PostCard(FileInfo sourceFileInfo,PostCardAlbum parentPostCardAlbum)
        {
            AllPostCards .Add (this);
            _state = "未处理";
            SourceFile = sourceFileInfo;
            if (".db.ini.doc.docx.xls.dwg.pdf.xlsx.ppt.exe.rar.wow.dll.ocx.xlsx.ico.lnk.html.xml.".Contains(sourceFileInfo.Extension.ToLower()))
            {
                return ;
            }
            if (!sourceFileInfo.Name.StartsWith("PostCard_"))
            {
                var tmpFile = new FileInfo(sourceFileInfo.Directory + "\\PostCard_" +Guid.NewGuid().ToString()+ "_____" + ChangeNotChineseReg(_sourceFile.Name.Substring(0, _sourceFile.Name.Length - _sourceFile.Extension.Length)) + _sourceFile.Extension);
                _sourceFile.MoveTo(tmpFile.FullName );
                SourceFile = tmpFile;
            }
            ParentPostCardAlbum = parentPostCardAlbum;
            ParentPostCardAlbum.PostCards.Add(this);
            Copys = ParentPostCardAlbum .Copys !=0 ? 1 : ParentPostCardAlbum.Copys;
            _productArea.Width = 20 * ParentPostCardAlbum.ProductSize.Width;
            _productArea.Height = 20 * ParentPostCardAlbum.ProductSize.Height;
            _pictureArea = _productArea;

            SetMyNode();
            PostCardAlbum.PreQueue.Enqueue(this);
            if (PostCardAlbum.ProcessThread == null || PostCardAlbum.ProcessThread.IsAlive == false)
            {
                PostCardAlbum.ProcessThread = new System.Threading.Thread(PostCardAlbum.ThreadMethod);
                PostCardAlbum.ProcessThread.Start();
            }
        }
        public static int Count=0;
        private Rectangle _pictureArea = new Rectangle(0, 0, 0, 0), _canvasArea = new Rectangle(0, 0, 0, 0), _productArea = new Rectangle(0, 0, 0, 0);
        private bool _inQueue;
        //private int copys=1;

        public new char OppoppositeType
        {
            get => base.OppoppositeType.ToString ().ToUpper()[0];
            set => base.OppoppositeType = value;
        }
       
        //public int Copys
        //{
        //    get { return copys; }
        //    set { copys = value; }
        //}

        public bool ReadOny => (FinishedFile.Exists || _inQueue);
        private bool _canResize = true ;
        private FileInfo _sourceFile, _thumbnailFile, _finishedFile;

        public FileInfo FinishedFile
        {
            get 
            {
                _finishedFile.Refresh();
                IsProcessed = _finishedFile.Exists;
                return _finishedFile; 
            }
        }

        private int _type = -1;//默认为B版
        //private Size sourcePictureSize = Size.Empty;
        private Size _pictureSize = Size.Empty;
        //private int angle=0;
        private string _state;
        //private bool canMove = false;
        private Image _thumbnailImage;

        //new public PostCardAlbum ParentPostCardAlbum
        //{
        //    get { return parentPostCardAlbum; }
        //}
        private Boolean _isPreProcessed;

        public static int CountPreProcessed { get; private set; }

        public static int CountProcessed { get; private set; }

        public bool IsPreProcessed
        {
            get => _isPreProcessed;
            set 
            {
                if (value != _isPreProcessed)
                {
                    _isPreProcessed = value;
                    if (value)
                        CountPreProcessed += 1;
                    else
                        CountPreProcessed -= 1;
                }
            }
        }
        private bool _isProcessed;

        public Boolean IsProcessed
        {
            get => _isProcessed;
            set 
            {
                if (value != _isProcessed)
                {
                    _isProcessed = value;
                    if (value)
                        CountProcessed += 1;
                    else
                        CountProcessed -= 1;
                }
            }
        }
        public Image ThumbnailImage
        {
            get => _thumbnailImage;
            set 
            {
                Angle = 0;
                _thumbnailImage = value;
                if (value== null)
                    _state = "错误";
                else
                {
                    IsPreProcessed = true;
                    var ratio = value.Height / (double)value.Width;
                    if (ratio < 1)
                        ratio = 1 / ratio;
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (ParentPostCardAlbum.Type)
                    {
                        case 0:
                        case 1:
                        case 2:
                            Type = ParentPostCardAlbum.Type;
                            break;
                        case 3:
                            Type = 0;
                            if (ratio < (1 + (_pictureSize.Width / (double)(_pictureSize.Height ))) / 2)
                                Type = 2;
                            break;
                        case 4:
                            Type = 1;
                            if (ratio < (1 + (_pictureSize.Width / (double)(_pictureSize.Height ))) / 2)
                                Type = 2;
                            break;
                    }
                    InitPictureCanvasArea();

                }
            }
        }

        public int Angle
        {
            get => _angle;
            set 
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch ((value+360-_angle) % 360)
                {
                    case 90:
                        _thumbnailImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 180:
                        _thumbnailImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 270:
                        _thumbnailImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
                _angle = value % 360;
                InitPictureCanvasArea();
            }
        }
        public string State
        {
            get => _state;
            set => _state = value;
        }
        public new int Type
        {
            get => _type;
            set 
            {
                //if (type.Equals(value))
                //    return;
                _type = value;
                _pictureArea = _canvasArea = Rectangle.Empty;
                Angle = 0;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (_type)
                {
                    case 0://A版
                        _pictureSize.Width = ParentPostCardAlbum.ProductSize.Width - 10;
                        _pictureSize.Height = ParentPostCardAlbum.ProductSize.Height - 10;
                        if (_thumbnailFile != null & _thumbnailImage.Width < _thumbnailImage.Height)
                            Angle = 270;
                        break;
                    case 1://B版
                        _pictureSize = ParentPostCardAlbum.ProductSize;
                        if (_thumbnailImage != null &&_thumbnailImage.Width < _thumbnailImage.Height)
                            Angle = 270;
                        break;
                    case 2://C版
                        _pictureSize.Width = _pictureSize.Height = ParentPostCardAlbum.ProductSize.Height - 10;
                        Angle = 270;
                        break;
                    case 3:
                        if (_thumbnailImage  != null)
                        {
                            var ratio = _thumbnailImage.Width / (double)_thumbnailImage.Height;
                            if (ratio < 1)
                                ratio = 1 / ratio;
                            Type = 0;
                            if (ratio < (1 + (_pictureSize.Width / (double)(_pictureSize.Height))) / 2)
                                Type = 2;
                        }
                        break;
                    case 4:
                        if (_thumbnailImage != null)
                        {
                            var ratio = _thumbnailImage.Width / (double)_thumbnailImage.Height;
                            if (ratio < 1)
                                ratio = 1 / ratio;
                            Type = 1;
                            if (ratio < (1 + (_pictureSize.Width / (double)(_pictureSize.Height))) / 2)
                                Type = 2;
                        }
                        break;
                }
                InitPictureCanvasArea();
            }
        }

        public FileInfo SourceFile
        {
            get => _sourceFile;
            set 
            { 
                _sourceFile = value;
                _finishedFile  = new FileInfo(_sourceFile.DirectoryName + "\\INK_Orange\\" + _sourceFile.Name.Substring(0, _sourceFile.Name.Length - _sourceFile.Extension.Length) + _sourceFile.Extension.Replace('.', '_') + ".jpg");
                _thumbnailFile = new FileInfo(_sourceFile.DirectoryName + "\\Thumbnail\\" + _sourceFile.Name.Substring(0, _sourceFile.Name.Length - _sourceFile.Extension.Length) + _sourceFile.Extension.Replace('.', '_') + ".jpg");
                if (FinishedFile.Exists)
                    IsProcessed = true;
                if (_finishedFile.Directory != null && !_finishedFile.Directory.Exists)
                    _finishedFile.Directory.Create();
                if (_thumbnailFile.Directory != null && !_thumbnailFile.Directory.Exists)
                    _thumbnailFile.Directory.Create();
            }
        }
        public int ProcessPostCard()
        {
            BegineProcess(this);
            if (!_inQueue)
                return 0;
            _myPs = new Application();
            Document myDoc;
            try//尝试打开，如果打不开进行标记
            {
                myDoc = _myPs.Open(_sourceFile.FullName);
            }
            catch
            {
                _inQueue = false;
                return 1;
            }
            //如果色彩模式不是CMYK、RGB、灰度的话，转化为CMYK模式
            if ((myDoc.Mode != PsDocumentMode.psCMYK) && (myDoc.Mode != PsDocumentMode.psRGB) && (myDoc.Mode != PsDocumentMode.psGrayscale))
            {
                myDoc.ChangeMode(PsChangeMode.psConvertToCMYK);
            }
            if (myDoc.Resolution < 50)
            {
                ChangeMySolution();
            }
            //切换到点
            _myPs.ActiveDocument = myDoc;
            var rulerOld = _myPs.Preferences.RulerUnits;
            _myPs.Preferences.RulerUnits = PsUnits.psPoints;
            //MyPS.DoJavaScriptFile(System.IO.Directory.GetCurrentDirectory() + "\\创建快照.js");
            //获取图像和画布尺寸
            //尝试
            if (Angle != 0)
                myDoc.RotateCanvas(Angle);

            var cutLeft = myDoc.Width * (_pictureArea.Left - _canvasArea.Left) / _canvasArea.Width;
            var cutRight = myDoc.Width * (_pictureArea.Right - _canvasArea.Left) / _canvasArea.Width;
            var cutTop = myDoc.Height * (_pictureArea.Top - _canvasArea.Top) / _canvasArea.Height;
            var cutBottom = myDoc.Height * (_pictureArea.Bottom - _canvasArea.Top) / _canvasArea.Height;

            //切换回毫米
            _myPs.Preferences.RulerUnits = PsUnits.psMM;
            ResetColor();


            //double pictureWidth =this.pictureSize.Width;
            //double pictureHeight = this.pictureSize.Height ;


            Cut(cutLeft, cutTop, cutRight, cutBottom, _pictureSize.Width, _pictureSize.Height, 300);
            switch (_type)
            {
                case 0://A版
                    myDoc.ResizeCanvas(ParentPostCardAlbum.ProductSize.Width, ParentPostCardAlbum.ProductSize.Height);
                    break;
                case 1://B版
                    break;
                case 2://C版
                    //Cut(cutLeft, cutTop, cutRight, cutBottom, pictureSize.Width, pictureSize.Height, 300);
                    myDoc.ResizeCanvas(Math.Min(ParentPostCardAlbum.ProductSize.Height, ParentPostCardAlbum.ProductSize.Width), Math.Min(ParentPostCardAlbum.ProductSize.Height, ParentPostCardAlbum.ProductSize.Width));
                    myDoc.ResizeCanvas(Math.Max(ParentPostCardAlbum.ProductSize.Height, ParentPostCardAlbum.ProductSize.Width), Math.Min(ParentPostCardAlbum.ProductSize.Height, ParentPostCardAlbum.ProductSize.Width), PsAnchorPosition.psMiddleLeft );
                    //MyDoc.RotateCanvas(270);
                    break;
            }

            var myJpegSaveOption = new JPEGSaveOptions
            {
                Quality = 12,
                Matte = PsMatteType.psNoMatte,
                FormatOptions = PsFormatOptionsType.psStandardBaseline
            };

            try
            {
                _myPs.DoJavaScript("executeAction(charIDToTypeID( \"FltI\" ), undefined, DialogModes.NO );");
            }
            catch
            {
                // ignored
            }

          
            foreach (Channel orange in myDoc.Channels)
            {
                if (orange.Visible == false)
                {
                    orange.Delete();
                }
            }
            //MyDoc.SaveAs(sourceFile.DirectoryName + "\\INK_Orange\\" + sourceFile.Name.Substring(0, sourceFile.Name.Length - sourceFile.Extension.Length) + sourceFile.Extension.Replace(".", "_") + ".jpg", MyJpegSaveOption);
            myDoc.SaveAs(_finishedFile.FullName, myJpegSaveOption);
            //closeMyFile();//关闭文件
            myDoc.Close();
            _myPs.Preferences.RulerUnits = rulerOld;
            FinishedFile.Refresh();
            _inQueue = false;

            EndProcess(this);
            return 0;
            //myFileNode.SelectImageIndex = myFileNode.ImageIndex = 5;
            //MyPS.ActiveDocument.
            //MyDoc.Crop ()
            //MyDoc .





            //MessageBox.Show(MyDoc.PixelAspectRatio.ToString());
            //MyDoc.Save();
            //MyDoc.ResizeCanvas(20, 20);


            //MyPS .Documents[0].Info.


        }
        /// <summary>
        /// 裁切文件
        /// </summary>
        /// <param name="left">裁切区域左侧 单位：点</param>
        /// <param name="top">裁切区域上端 单位：点</param>
        /// <param name="right">裁切区域左侧 单位：点</param>
        /// <param name="bottom">裁切区域下端 单位：点</param>
        /// <param name="width">裁切出来的宽度 单位：毫米</param>
        /// <param name="height">裁切出来的高度 单位：毫米</param>
        /// <param name="resolution">裁切出来的分辨率</param>
        private static void Cut(double left, double top, double right, double bottom, double width, double height, double resolution)
        {
            var desc4 = new ActionDescriptor();
            var desc5 = new ActionDescriptor();
            desc5.PutUnitDouble(_myPs.CharIDToTypeID("Top "), _myPs.CharIDToTypeID("#Rlt"), top);
            desc5.PutUnitDouble(_myPs.CharIDToTypeID("Left"), _myPs.CharIDToTypeID("#Rlt"), left);
            desc5.PutUnitDouble(_myPs.CharIDToTypeID("Btom"), _myPs.CharIDToTypeID("#Rlt"), bottom);
            desc5.PutUnitDouble(_myPs.CharIDToTypeID("Rght"), _myPs.CharIDToTypeID("#Rlt"), right);
            desc4.PutObject(_myPs.CharIDToTypeID("T   "), _myPs.CharIDToTypeID("Rctn"), desc5);
            desc4.PutUnitDouble(_myPs.CharIDToTypeID("Angl"), _myPs.CharIDToTypeID("#Ang"), 0.000000);
            desc4.PutUnitDouble(_myPs.CharIDToTypeID("Wdth"), _myPs.CharIDToTypeID("#Pxl"), 11.811 * width);
            desc4.PutUnitDouble(_myPs.CharIDToTypeID("Hght"), _myPs.CharIDToTypeID("#Pxl"), 11.811 * height);
            desc4.PutUnitDouble(_myPs.CharIDToTypeID("Rslt"), _myPs.CharIDToTypeID("#Rsl"), resolution);
            _myPs.ExecuteAction(_myPs.CharIDToTypeID("Crop"), desc4, 3);

        }
        private void ResetColor()
        {
            var desc49 = new ActionDescriptor();
            var ref22 = new ActionReference();
            ref22.PutProperty(_myPs.CharIDToTypeID("Clr "), _myPs.CharIDToTypeID("Clrs"));
            desc49.PutReference(_myPs.CharIDToTypeID("null"), ref22);
            _myPs.ExecuteAction(_myPs.CharIDToTypeID("Rset"), desc49, 3);
        }
        /// <summary>
        /// 改变分辨率
        /// </summary>
        private void ChangeMySolution()
        {
            var desc3 = new ActionDescriptor();
            desc3.PutUnitDouble(_myPs.CharIDToTypeID("Rslt"), _myPs.CharIDToTypeID("#Rsl"), 300.000000);
            _myPs.ExecuteAction(_myPs.CharIDToTypeID("ImgS"), desc3, 3);
        }

        /// <summary>
        /// 创建缩略图
        /// </summary>
        /// <returns>0:需要删除  1:A版  2：B版  3：C版  11：已经存在的A版  12：已经存在的B版  3：已经存在的C版</returns>
        public void PreProcessPostCard(int resultWidth)
        {
            BeginPreprocess(this);
            if (_thumbnailFile.Exists)//缩略图文件存在，直接跳转到判断版式上。
            {
                ThumbnailImage = GetImage(_thumbnailFile.FullName);
                goto preprocessEnd;
            }
            //创建新的PS应用程序
            _myPs = new Application();
            Document myDoc;
            //尝试打开文件
            try
            {
                myDoc = _myPs.Open(_sourceFile.FullName);
            }
            catch//打不开文件的时候
            {
                //转换后的文件信息（转换的好的文件）
                var convertToFile = new FileInfo(_sourceFile.DirectoryName + "\\" + _sourceFile.Name.Substring(0, _sourceFile.Name.Length - _sourceFile.Extension.Length) + _sourceFile.Extension.Replace('.', '_') + ".jpg");
                //如果已经存在转换后的文件，不再进行转化，执行完成IF语句之后，直接跳出。
                if (convertToFile.Exists)
                {
                    if (!new DirectoryInfo(_sourceFile.DirectoryName + "\\QuestionFile").Exists) new DirectoryInfo(_sourceFile.DirectoryName + "\\QuestionFile").Create();                    //如果没有Question文件夹，创建新的文件夹
                    var questionOldFile = new FileInfo(_sourceFile.DirectoryName + "\\QuestionFile\\" + _sourceFile.Name);                                                               //移动后的文件位置（问题文件）
                    if (questionOldFile.Exists) questionOldFile.Delete();                                                                                                                   //如果移动后的文件已经存在，则删除
                    _sourceFile.MoveTo(questionOldFile.FullName);
                    //直接移动到问题文件夹里面
                    State = "文件重复";
                    goto preprocessEnd;
                }
                #region 转化文件

                var myProcess = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        Arguments = "\"-> JPG\" \"Original Size\" \"" + _sourceFile.FullName + "\" \"" + convertToFile.FullName + "\" /hide",
                        FileName = System.Windows.Forms.Application.StartupPath + "\\ConfigsFile\\FormatFactory.exe",
                        CreateNoWindow = true
                    }
                };
                myProcess.Start();
                myProcess.WaitForExit();
                convertToFile.Refresh();
                #endregion
                //刷新后，如果文件存在，说明转化成功
                if (convertToFile.Exists)
                {
                    if (!new DirectoryInfo(_sourceFile.DirectoryName + "\\QuestionFile").Exists)
                        new DirectoryInfo(_sourceFile.DirectoryName + "\\QuestionFile").Create();
                    var questionOldFile = new FileInfo(_sourceFile.DirectoryName + "\\QuestionFile\\" + _sourceFile.Name);
                    if (questionOldFile.Exists)
                        questionOldFile.Delete();
                    _sourceFile.MoveTo(questionOldFile.FullName);
                    //myFileRoot.Delete();
                    _sourceFile = convertToFile;
                    myDoc = _myPs.Open(_sourceFile.FullName);
                }//转化失败，返回错误标记。
                else
                {
                    _state = "文件无法打开";
                    goto preprocessEnd;
                }
            }
            #region 判断色彩模式
            if ((myDoc.Mode != PsDocumentMode.psCMYK) && (myDoc.Mode != PsDocumentMode.psRGB) && (myDoc.Mode != PsDocumentMode.psGrayscale))
            {
                myDoc.ChangeMode(PsChangeMode.psConvertToCMYK);
            }
            #endregion
            #region 分辨率过低的话，调整分辨率
            if (myDoc.Resolution < 50)
            {
                _myPs.ActiveDocument = myDoc;
                ChangeMySolution();
            }
            #endregion
            if (_thumbnailFile.Exists)
                _thumbnailFile.Delete();
            //重置色彩
            ResetColor();
            //保存选项
            var myJpeg = new JPEGSaveOptions
            {
                Quality = 12,
                FormatOptions = PsFormatOptionsType.psStandardBaseline,
                Matte = PsMatteType.psWhiteMatte
            };
            //MyJPEG .Scans =

            var rulerOld = _myPs.Preferences.RulerUnits;
            // 修改标尺为点
            _myPs.Preferences.RulerUnits = PsUnits.psPoints;
            //缩小像素
            if (myDoc.Height < myDoc.Width)
                myDoc.ResizeImage(resultWidth, resultWidth * myDoc.Height / myDoc.Width, 72);
            else
                myDoc.ResizeImage(resultWidth * myDoc.Width / myDoc.Height, resultWidth, 72);
            //合并图层
            _myPs.ActiveDocument = myDoc;
            if (_myPs.ActiveDocument != null)
                _myPs.DoJavaScript("executeAction(charIDToTypeID( \"FltI\" ), undefined, DialogModes.NO );");
            //===================================================================================================保存缩略图
            #region 去除不可见通道
            foreach (Channel orange in myDoc .Channels )
            {
                if (orange.Visible ==false)
                {
                    orange.Delete();
                }
            }
            #endregion
            if (_thumbnailFile .Directory != null && !_thumbnailFile .Directory.Exists)
            {
                _thumbnailFile.Directory.Create();
            }
            myDoc.SaveAs(_thumbnailFile.FullName, myJpeg);
            //切换成毫米标尺
            _myPs.Preferences.RulerUnits = rulerOld;
            //长宽比
            _myPs.ActiveDocument = myDoc;
            myDoc.Close();
            
            //MyPS.DoJavaScriptFile(System.Windows.Forms.Application.StartupPath + "\\ConfigsFile\\CloseMyFile.js");
            //MyDoc.Close();
            _finishedFile.Refresh();
            State = _finishedFile.Exists ? "已处理" : "已经预处理";
            ThumbnailImage = GetImage(_thumbnailFile.FullName );
            
            preprocessEnd:
            EndProcess(this);
        }
       
        private static string ChangeNotChineseReg(string text)
        {
            var c = text.ToArray();
            for (var i = 0; i < c.Length; i++)
            {
                if ((c[i] != '%') && ((c[i] >= 0x4e00 && c[i] <= 0x9fbb) || (c[i] > 32 && c[i] < 127) || ("；：，。‘“’”".Contains(c[i]))))
                {

                }
                else
                {
                    c[i] = '_';
                }
            }
            return new string(c);
        }
        private Image GetImage(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);

                var memoryStream = new MemoryStream(bytes);
                return Image.FromStream(memoryStream);
            }
        }
        public void CanvasMove(int deltaX, int deltaY)
        {
            if (ReadOny)
                return;
            if (Math.Abs(_scale) > 0)
            {
                deltaX = (int)(deltaX / _scale);
                deltaY = (int)(deltaY / _scale);
            }
            if (_canvasArea.Width > _pictureArea.Width)
            {
                _canvasArea.X += deltaX;
                if (_canvasArea.Left > _pictureArea.Left)
                    _canvasArea.X = _pictureArea.Left;
                if (_canvasArea.Right < _pictureArea.Right)
                    _canvasArea.X = _pictureArea.Left + _pictureArea.Width - _canvasArea.Width;
            }
            if (_canvasArea.Height > _pictureArea.Height)
            {
                _canvasArea.Y += deltaY;
                if (_canvasArea.Top > _pictureArea.Top)
                    _canvasArea.Y = _pictureArea.Top;
                if (_canvasArea.Bottom < _pictureArea.Bottom)
                    _canvasArea.Y = _pictureArea.Top + _pictureArea.Height - _canvasArea.Height;
            }
        }
        public void CanvasResize(Point mousePoint, int wheelSpeed, int delta, bool allowOut, bool isSlow)
        {
            if (ReadOny )
                return;
            if (isSlow)
                wheelSpeed = wheelSpeed / 20;
            if (Math.Abs(_scale) > 0)
            {
                mousePoint.Offset(-_offsetPoint.X, -_offsetPoint.Y);
                mousePoint.X = (int)(mousePoint.X / _scale);
                mousePoint.Y = (int)(mousePoint.Y / _scale);
                wheelSpeed =(int)(wheelSpeed/_scale) ;
            }
            if (!_canvasArea.IsEmpty && _thumbnailImage!=null && _canResize)
            {
                var oldCanvasArea = _canvasArea;
                //如果图像偏向正方形，则拉伸高度
                if (_thumbnailImage.Size.Height * _pictureSize.Width > _pictureSize.Height * _thumbnailImage.Size.Width)
                {
                    _canvasArea.Height += Math.Abs(delta) / delta * wheelSpeed;
                    _canvasArea.Width = _canvasArea.Height * _thumbnailImage.Size.Width / _thumbnailImage.Size.Height;
                }
                else//偏向于长方形
                {
                    _canvasArea.Width += Math.Abs(delta) / delta * wheelSpeed;
                    _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                }
                if ((_canvasArea.Width > _pictureArea.Width) && (_canvasArea.Height > _pictureArea.Height) && !allowOut )
                {
                    if (_canvasArea.Width * _pictureArea.Height > _canvasArea.Height * _pictureArea.Width)
                    {
                        _canvasArea.Height = _pictureArea.Height;
                        _canvasArea.Width = _canvasArea.Height *_thumbnailImage.Width / _thumbnailImage.Height;
                    }
                    else
                    {
                        _canvasArea.Width = _pictureArea.Width;
                        _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Height / _thumbnailImage.Width;
                    }
                }
                var myscale = (double)_canvasArea.Width / oldCanvasArea.Width;
                //缩放到合适的坐标
                _canvasArea.X = mousePoint.X - (int)((mousePoint.X - oldCanvasArea.X) * myscale);
                _canvasArea.Y = mousePoint.Y - (int)((mousePoint.Y - oldCanvasArea.Y) * myscale);

                //画布尺寸小于图像尺寸,整体＜尺寸
                if (_pictureArea .Width>=_canvasArea .Width && _pictureArea .Height >=_canvasArea .Height)
                {
                    if (_thumbnailImage.Size.Width * _pictureArea.Height > _thumbnailImage.Size.Height * _pictureArea.Width)
                    {//图像偏向于长方形
                        _canvasArea.Width = _pictureArea.Width;
                        _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                    }
                    else
                    {//图像偏向于正方形
                        _canvasArea.Height = _pictureArea.Height;
                        _canvasArea.Width = _canvasArea.Height * _thumbnailImage.Size.Width / _thumbnailImage.Size.Height;
                    }
                    //重置坐标
                    _canvasArea.X = _pictureArea.X + (_pictureArea.Width - _canvasArea.Width) / 2;
                    _canvasArea.Y = _pictureArea.Y + (_pictureArea.Height - _canvasArea.Height) / 2;
                }
                else
                {
                    if (_canvasArea.Width < _pictureArea.Width)//画布区域宽度比图片区域宽度小，偏向于正方形
                    {
                        //此代码块根据白边，调整宽度
                        var whiteWidth = _pictureSize.Width * (_pictureArea.Width - _canvasArea.Width) / _pictureArea.Width;
                        if (whiteWidth <= 5)//白边≤5
                            _canvasArea.Width = _pictureArea.Width;
                        else if (whiteWidth < 10)//白边＜10
                            _canvasArea.Width = (_pictureSize.Width - 10) * _pictureArea.Width / _pictureSize.Width;

                        _canvasArea.X = _pictureArea.Left + (_pictureArea.Width - _canvasArea.Width) / 2;
                        if (_canvasArea.Top > _pictureArea.Top)//坐标位于图片区域下侧
                            _canvasArea.Y = _pictureArea.Top;
                        if (_canvasArea.Bottom < _pictureArea.Bottom)//下侧位于图片区域上侧
                            _canvasArea.Y = _pictureArea.Top - _canvasArea.Height + _pictureArea.Height;
                    }
                    else if (_canvasArea.Height < _pictureArea.Height)//画布区域高度比图片区域高度小，长方形
                    {
                        {//此代码块根据白边，调整高度
                            var whiteHeihgt = _pictureSize.Height * (_pictureArea.Height - _canvasArea.Height) / _pictureArea.Height;
                            if (whiteHeihgt <= 5)//白边≤5
                                _canvasArea.Height = _pictureArea.Height;
                            else if (whiteHeihgt < 10)//白边＜10
                                _canvasArea.Height = (_pictureSize.Height - 10) * _pictureArea.Height / _pictureSize.Height;
                        }
                        _canvasArea.Y = _pictureArea.Top + (_pictureArea.Height - _canvasArea.Height) / 2;
                        if (_canvasArea.Left > _pictureArea.Left)
                            _canvasArea.X = _pictureArea.Left;
                        if (_canvasArea.Right < _pictureArea.Right)
                            _canvasArea.X = -_canvasArea.Width + _pictureArea.Width + _pictureArea.Left;
                    }
                    else
                    {
                        if (!allowOut )
                        {

                        }
                        if (_canvasArea.Left > _pictureArea.Left)
                            _canvasArea.X = _pictureArea.Left;
                        if (_canvasArea.Right < _pictureArea.Right)
                            _canvasArea.X = -_canvasArea.Width + _pictureArea.Width + _pictureArea.Left;
                        if (_canvasArea.Top > _pictureArea.Top)
                            _canvasArea.Y = _pictureArea.Top;
                        if (_canvasArea.Bottom < _pictureArea.Bottom)
                            _canvasArea.Y = _pictureArea.Top - _canvasArea.Height + _pictureArea.Height;
                    }
                }
            }
        }
        private void InitPictureCanvasArea()
        {
            //如果成品框像素为空
            if (_productArea == Rectangle.Empty)
                return;

            _pictureArea.Width = _productArea.Width * _pictureSize.Width / ParentPostCardAlbum.ProductSize.Width;//图像框的像素=成品框像素×（图像框尺寸/成品框尺寸）
            _pictureArea.Height = _productArea.Height * _pictureSize.Height / ParentPostCardAlbum.ProductSize.Height;//图像框的像素=成品框像素×（图像框尺寸/成品框尺寸）
            _pictureArea.Location = new Point((_productArea.Height - _pictureArea.Height) / 2, (_productArea.Height - _pictureArea.Height) / 2);//左侧为宽度差的一半，顶端为高度差的一半

            if (_thumbnailImage !=null)
            {
                var whiteHeihgt = _pictureSize.Height - _pictureSize.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                if (whiteHeihgt > 0 && whiteHeihgt <= 5)
                {//间隙＜5
                    _canvasArea = _pictureArea ;
                }
                else
                {
                    if (_type == 2)
                    {
                        if (_thumbnailImage.Size.Width * _pictureArea.Height <= _pictureArea.Width * _thumbnailImage.Size.Height)
                        {//高度相同（高度比较大）
                            _canvasArea.Height = _pictureArea.Height;
                            _canvasArea.Width = _canvasArea.Height * _thumbnailImage.Size.Width / _thumbnailImage.Size.Height;
                            _canvasArea.Y = _pictureArea.Y;
                            _canvasArea.X = _pictureArea.X + (_pictureArea.Width - _canvasArea.Width) / 2;
                        }
                        else
                        {//宽度相同（宽度比较大）
                            _canvasArea.Width = _pictureArea.Width;
                            _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                            _canvasArea.X = _pictureArea.X;
                            _canvasArea.Y = _pictureArea.Y + (_pictureArea.Height  - _canvasArea.Height ) / 2;

                        }
                    }
                    else
                    {
                        if (_thumbnailImage.Size.Width * _pictureArea.Height >= _pictureArea.Width * _thumbnailImage.Size.Height)
                        {//高度相同（宽度比较大）
                            _canvasArea.Height = _pictureArea.Height;
                            _canvasArea.Width = _canvasArea.Height * _thumbnailImage.Size.Width / _thumbnailImage.Size.Height;
                        }
                        else
                        {//宽度相同（高度比较大）
                            _canvasArea.Width = _pictureArea.Width;
                            _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                        }
                        //设置坐标相同
                        _canvasArea.Location = _pictureArea.Location;
                    }
                }
            }
        }
        public void SetProductArea(Size maxSize, double scale)
        {
            if (maxSize.Width <= scale || maxSize.Height <= scale)
            {
                return ;
            }
            var tmpArea = new Rectangle();

            tmpArea.Width =(int) (maxSize.Width * scale);
            tmpArea.Height = (int)(tmpArea.Width * ParentPostCardAlbum.ProductSize.Height / (double)ParentPostCardAlbum.ProductSize.Width);
            if (tmpArea.Height > (int)(maxSize.Height * scale))
            {
                tmpArea.Height =(int) (maxSize.Height*scale);
                tmpArea.Width = (int)(tmpArea.Height * ParentPostCardAlbum.ProductSize.Width / (double)ParentPostCardAlbum.ProductSize.Height);
            }
            tmpArea.X = (maxSize.Width - tmpArea.Width) / 2;
            tmpArea.Y = (maxSize.Height - tmpArea.Height) / 2;
            //设置缩放比例
            if (!_productArea.IsEmpty)
                _scale = tmpArea.Width / (double)_productArea.Width;
            _offsetPoint = tmpArea.Location;
            //return tmpArea; 
        }
        private double _scale = 1;
        private Point _offsetPoint = Point.Empty;
        public void Fit(int option)
        {
            if (ReadOny)
                return;
            if (_thumbnailImage ==null)
                return;
            switch (option)
            {
                case 0://强制
                    _canvasArea = _pictureArea;
                    break;
                case 1://完全适合
                    if (_pictureSize.Width * _thumbnailImage.Size.Height < _thumbnailImage.Size.Width * _pictureSize.Height)
                    {
                        _canvasArea.Width = _pictureArea.Width;
                        _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                        _canvasArea.X = _pictureArea.X;
                        _canvasArea.Y = _pictureArea.Y + (_pictureArea.Height - _canvasArea.Height) / 2;
                    }
                    else
                    {
                        _canvasArea.Height = _pictureArea.Height;
                        _canvasArea.Width = _canvasArea.Height * _thumbnailImage.Size.Width / _thumbnailImage.Size.Height;
                        _canvasArea.X = _pictureArea.X + (_pictureArea.Width - _canvasArea.Width) / 2;
                        _canvasArea.Y = _pictureArea.Y;
                    }
                    break;
                case 2://适合宽度
                    if (_pictureArea.Width * _thumbnailImage.Size.Height > _thumbnailImage.Size.Width * _pictureArea.Height)
                    {
                        _canvasArea.Width = _pictureArea.Width;
                        _canvasArea.Height = _canvasArea.Width * _thumbnailImage.Size.Height / _thumbnailImage.Size.Width;
                        _canvasArea.Location = _pictureArea.Location;
                    }
                    else
                    {
                        _canvasArea.Height = _pictureArea.Height;
                        _canvasArea.Width = _canvasArea.Height * _thumbnailImage.Size.Width / _thumbnailImage.Size.Height;
                        _canvasArea.Location = _pictureArea.Location;
                    }
                    break;
            }

        }
        public void ReSet()
        {
            _state = "";
            Type = ParentPostCardAlbum.Type;
            InitPictureCanvasArea();
        }
        new public void Remove()
        {
            IsPreProcessed = false;
            IsProcessed = false;

            base.Remove();
            if (ParentPostCardAlbum != null)
            {
                ParentPostCardAlbum.PostCards.Remove(this);
                ParentPostCardAlbum.Remove();
            }
        }
        public void SaveCanvasArea(Rectangle canvasArea,Rectangle pictureArea)
        {
        }

        public Rectangle ProductArea => new Rectangle(_offsetPoint, new Size((int)(_productArea.Width * _scale), (int)(_productArea.Height * _scale)));

        public Rectangle PictureArea => new Rectangle ((int)(_pictureArea .X*_scale+_offsetPoint .X),(int)(_pictureArea .Y*_scale+_offsetPoint .Y),(int)(_pictureArea .Width *_scale),(int)(_pictureArea .Height *_scale));

        public Rectangle CanvasArea => new Rectangle((int)(_canvasArea.X * _scale + _offsetPoint.X), (int)(_canvasArea.Y * _scale + _offsetPoint.Y), (int)(_canvasArea.Width * _scale), (int)(_canvasArea.Height * _scale));

        public void EnterQueue()
        {
            _inQueue = true;
            Node.SetValue(1, "已提交");
            PostCardAlbum.ProcessQueue.Enqueue(this);
            //if (PostCardAlbum .processThread.ThreadState==System.Threading .ThreadState .Unstarted
            //    || PostCardAlbum.processThread.ThreadState == System.Threading.ThreadState.Stopped)
            if (PostCardAlbum.ProcessThread == null || PostCardAlbum.ProcessThread.IsAlive == false)
            {
                PostCardAlbum.ProcessThread = new System.Threading.Thread(PostCardAlbum.ThreadMethod);
                PostCardAlbum.ProcessThread.Start();
            }
        }
        public void ReCut()
        {
            if (FinishedFile.Exists)
                _finishedFile.Delete();
            FinishedFile.Refresh ();
            _inQueue = false;
            BeginRecut(this);
        }
        public static void  RemoveAll()
        {
            PostCardAlbum.ProcessThread.Abort();
            PostCardAlbum.ProcessQueue.Clear();
            PostCardAlbum.PreQueue.Clear();
            foreach (var tmpPostCard in AllPostCards )
            {
                if (tmpPostCard ._finishedFile .Exists )
                {tmpPostCard._finishedFile.Delete();
                }
                if (tmpPostCard ._thumbnailFile .Exists )
                {
                    tmpPostCard._thumbnailFile.Delete();
                }
                tmpPostCard.Remove ();
            }
            AllPostCards.Clear();
        }

    }
    
}
