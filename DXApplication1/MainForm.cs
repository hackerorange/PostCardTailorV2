using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraLayout.Utils;
using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using PhotoWorker;
using PostCardTailor.MyForms;
using CellValueChangedEventArgs = DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs;

namespace PostCardTailor
{
    public partial class MainForm : RibbonForm
    {
        private Image _currentFinishedImage;
        private PostCard _currentPostCard;
        private PostCardAlbum _currentPostCardAlbum;
        public string[] Args;
        private bool _ctrlKeyDown;
        private bool _mouseLeftDown;
        private Process _myProcess = new Process();
        private Point _oldPoint;
        private bool _shiftKeyDown;

        private string _strNotPicture = "";
        private Image _transportationImage;
        private readonly int _wheelSpeed = 100;

        public MainForm()
        {
            InitializeComponent();
            //设置明信片初始化事件
            PostCardType.InitPostCardAlbum += PostCardTypeNodeInit;
            PostCardType.DeleteTreeListNode += PostCardAlbumRemoving;
            PostCardAlbum.RequirPoarCardAlbumInformation += SetPostCardAlbumInformation;
            PostCardAlbum.PdfProcessProgress += PdfProgress;
            PostCard.BeginPreprocess += PostCard_BeginPreprocess;
            PostCard.EndPreprocess += postCard_EndPreprocess;
            PostCard.BegineProcess += PostCard_BeginProcess;
            PostCard.EndProcess += PostCard_EndProcess;
            PostCard.BeginRecut += PostCard_BeginRecut;
        }

   

        private void ThreadMethod()
        {
            while (IsDisposed == false || PostCardAlbum.ProcessQueue.Count != 0 || PostCardAlbum.PreQueue.Count != 0)
            {
                while (IsDisposed == false && PostCardAlbum.ProcessQueue.Count == 0 && PostCardAlbum.PreQueue.Count == 0) Thread.Sleep(500);

                //系统退出
                if (PostCardAlbum.ProcessQueue.Count == 0 && PostCardAlbum.PreQueue.Count == 0) continue;
                //优先预处理
                if (PostCardAlbum.PreQueue.Count != 0)
                {
                    var tmpWorker = PostCardAlbum.PreQueue.Dequeue();
                    //if (!IsDisposed)
                    //    barStaticItem1.Caption = "正在预处理：" + tmpWorker.SourceFile.Name;
                    tmpWorker.PreProcessPostCard(500);
                    //BeginInvoke(new delegatereFreshPostCard(reFreshPostCard), tmpWorker);
                    //if (!IsDisposed)
                    //    barStaticItem1.Caption = "";
                }

                //有裁切作业
                if (PostCardAlbum.ProcessQueue.Count != 0)
                {
                    var tmpWorker = PostCardAlbum.ProcessQueue.Dequeue();
                    //if (!IsDisposed)
                    //    barStaticItem1.Caption = "正在处理：" + tmpWorker.SourceFile.Name;
                    //BeginInvoke(new delegateSetImage(setMyImage), tmpWorker.Node, 2);
                    tmpWorker.ProcessPostCard();
                    //BeginInvoke(new delegatereFreshPostCard(reFreshPostCard), tmpWorker);

                    //if (!IsDisposed)
                    //    barStaticItem1.Caption = "";
                    //continue;
                }
            }
        }

        private void SetMyImage(TreeListNode treeListNode, int a)
        {
            treeListNode.SelectImageIndex = treeListNode.ImageIndex = a;
            if (treeListNode == treeList1.FocusedNode)
                ReFreshMyNode();
        }

        private void PostCard_BeginRecut(PostCard sender)
        {
            sender.Node.SelectImageIndex = sender.Node.ImageIndex = 1;
            sender.Node.SetValue(state, "");
            BeginInvoke(new DelegatereFreshPostCard(ReFreshPostCard), sender);
        }

        private void ReFreshPostCard(PostCard postCard)
        {
            progressValue.Caption = PostCard.CountProcessed.ToString();
            progressMax.Caption = PostCard.CountPreProcessed.ToString();
            pathProcessProgress.EditValue = PostCard.CountProcessed;
            repositoryItemProgressBar1.Maximum = PostCard.CountPreProcessed;
            progressValue.Refresh();
            progressMax.Refresh();
            pathProcessProgress.Refresh();
            if (postCard.ThumbnailImage != null && postCard.Node != null)
            {
                postCard.Node.SetValue(state, "");
                postCard.Node.ImageIndex = postCard.Node.SelectImageIndex = 1;
            }

            if (postCard.State == "文件无法打开") postCard.Remove();
            if (postCard.FinishedFile.Exists && postCard.Node != null)
            {
                postCard.Node.SetValue(state, "已处理");
                postCard.Node.ImageIndex = postCard.Node.SelectImageIndex = 5;
            }

            if (postCard.Node != null && postCard.Node == treeList1.FocusedNode) ReFreshMyNode();
            Application.DoEvents();
        }


        private void PostCard_EndProcess(PostCard sender)
        {
            barStaticItem1.Caption = "";
            BeginInvoke(new DelegatereFreshPostCard(ReFreshPostCard), sender);
            //Application.DoEvents();
        }

        private void PostCard_BeginProcess(PostCard sender)
        {
            barStaticItem1.Caption = "正在处理" + sender.SourceFile.Name;
            BeginInvoke(new DelegateSetImage(SetMyImage), sender.Node, 2);
        }

        private void postCard_EndPreprocess(PostCard sender)
        {
            barStaticItem1.Caption = "";
            //BeginInvoke(new delegateSetImage(setMyImage),sender.Node, 2);
            //reFreshPostCard(sender);
            BeginInvoke(new DelegatereFreshPostCard(ReFreshPostCard), sender);
        }

        private void PostCard_BeginPreprocess(PostCard sender)
        {
            barStaticItem1.Caption = "正在预处理" + sender.SourceFile.Name;
            BeginInvoke(new DelegateSetImage(SetMyImage), sender.Node, 2);
        }


        /// <summary>
        ///     删除影集时触发的操作
        /// </summary>
        /// <param name="sender"></param>
        private void PostCardAlbumRemoving(PostCardType sender)
        {
            treeList1.DeleteNode(sender.Node);
        }

        private void PdfProgress(int value, int maxValue)
        {
            progressBarControl1.Properties.Maximum = maxValue;
            progressBarControl1.EditValue = value;
            Application.DoEvents();
            if (value == 0)
            {
                //MessageBox.Show("导出完成");
            }
        }

        private void SetPostCardAlbumInformation(PostCardAlbum sender)
        {
            sender.MyIni.Refresh();
            if (sender.MyIni.Exists)
            {
                var iFileStream = new FileStream(sender.MyIni.FullName, FileMode.Open);
                var myStreamReader = new StreamReader(iFileStream);
                var tmpProductSize = new Size();
                while (!myStreamReader.EndOfStream)
                {
                    var tmpString = myStreamReader.ReadLine();
                    if (tmpString.StartsWith("[版式]:")) sender.Type = Convert.ToInt32(tmpString.Substring(5));
                    if (tmpString.StartsWith("[宽度]:")) tmpProductSize.Width = Convert.ToInt32(tmpString.Substring(5));
                    if (tmpString.StartsWith("[高度]:")) tmpProductSize.Height = Convert.ToInt32(tmpString.Substring(5));
                    if (tmpString.StartsWith("[反面]:")) sender.OppoppositeType = Convert.ToChar(tmpString.Substring(5, 1));
                }

                myStreamReader.Close();
                iFileStream.Close();
                //宽度必须大于高度,宽度和高度不得小于20
                if (tmpProductSize.Width < tmpProductSize.Height) tmpProductSize = new Size(tmpProductSize.Height, tmpProductSize.Width);
                if (tmpProductSize.Width < 20)
                    tmpProductSize.Width = 20;
                if (tmpProductSize.Height < 20)
                    tmpProductSize.Height = 20;
                sender.ProductSize = tmpProductSize;
            }
            else
            {
                new FolderInfo(sender).ShowDialog();
                //new FolderInfo(sender).ShowDialog();
            }
        }

        /// <summary>
        ///     对↑↓←→按键进项响应
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (treeList1.FocusedNode != null && treeList1.FocusedNode.Tag != null && treeList1.FocusedNode.Tag.GetType().Name == "PostCard")
                if (sourcePicture.Focused)
                {
                    switch (keyData.ToString().ToLower())
                    {
                        case "left, shift":
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(50, 0);
                            sourcePicture.Refresh();
                            return true;
                        case "right, shift":
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(-50, 0);
                            sourcePicture.Refresh();
                            return true;
                        case "up, shift":
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(0, 50);
                            sourcePicture.Refresh();
                            return true;
                        case "down, shift":
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(0, -50);
                            sourcePicture.Refresh();
                            return true;
                    }

                    switch (keyData)
                    {
                        case Keys.Up:
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(0, 2);
                            sourcePicture.Refresh();
                            return true;
                        case Keys.Down:
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(0, -2);
                            sourcePicture.Refresh();
                            return true;
                        case Keys.Left:
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(2, 0);
                            sourcePicture.Refresh();
                            return true;
                        case Keys.Right:
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(-2, 0);
                            sourcePicture.Refresh();
                            return true;
                        case Keys.Home:
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(1000, 1000);
                            sourcePicture.Refresh();
                            return true;
                        case Keys.End:
                            ((PostCard) treeList1.FocusedNode.Tag).CanvasMove(-1000, -1000);
                            sourcePicture.Refresh();
                            return true;
                        case Keys.A:
                            if (_currentPostCard != null && !_currentPostCard.ReadOny)
                            {
                                _currentPostCard.Type = 0;
                                ReFreshMyNode();
                            }

                            return true;
                        case Keys.B:
                            if (_currentPostCard != null && !_currentPostCard.ReadOny)
                            {
                                _currentPostCard.Type = 1;
                                ReFreshMyNode();
                            }

                            return true;
                        case Keys.C:
                            if (_currentPostCard != null && !_currentPostCard.ReadOny)
                            {
                                _currentPostCard.Type = 2;
                                ReFreshMyNode();
                            }

                            return true;
                    }
                }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //打开常规设置文件
            var inifstream = new FileStream(Application.StartupPath + "\\ConfigsFile\\myIni.ini", FileMode.OpenOrCreate);
            var streamReader = new StreamReader(inifstream);
            while (!streamReader.EndOfStream)
            {
                var tmpString = streamReader.ReadLine();
                if (tmpString != null && tmpString.StartsWith("[Skin]"))
                    defaultLookAndFeel1.LookAndFeel.SkinName = tmpString.Substring(6);
            }

            streamReader.Close();
            streamReader.Dispose();
            inifstream.Close();
            inifstream.Dispose();
            //打开非图片配置文件
            inifstream = new FileStream(Application.StartupPath + "\\ConfigsFile\\NotPicture.ini", FileMode.Open);
            streamReader = new StreamReader(inifstream);
            if (!streamReader.EndOfStream)
                _strNotPicture = streamReader.ReadLine();
            streamReader.Close();
            streamReader.Dispose();
            inifstream.Close();
            inifstream.Dispose();

            sourcePicture.MouseWheel += CanvasMouseWheel;
            CheckForIllegalCrossThreadCalls = false;
            //Thread myThread=new Thread(ThreadMethod);
            //myThread.Start();
            //myThread.
        }

        /// <summary>
        ///     初始化明信片影集时触发的操作
        /// </summary>
        /// <param name="sender">需要初始化的明信片</param>
        private void PostCardTypeNodeInit(PostCardType sender)
        {
            if (sender.ParentPostCardAlbum != null)
                sender.Node = sender.ParentPostCardAlbum.Node.Nodes.Add();
            else
                sender.Node = treeList1.Nodes.Add();

            if (sender.GetType().Name == "PostCard")
            {
                sender.Node.SetValue(treeFileName, ((PostCard) sender).SourceFile.Name);
                sender.Node.SetValue(state, ((PostCard) sender).State);
                sender.Node.ImageIndex = sender.Node.SelectImageIndex = 1;
            }

            if (sender.GetType().Name == "PostCardAlbum")
            {
                sender.Node.SetValue(state, "");
                sender.Node.SetValue(treeFileName, ((PostCardAlbum) sender).SourceFolderInfo.Name);
            }

            if (sender.Node.ParentNode != null && sender.Node.ParentNode.Expanded == false)
                sender.Node.ParentNode.ExpandAll();
            if (_currentPostCard == null && _currentPostCardAlbum == null) sender.Node.Selected = true;
            Application.DoEvents();
        }

        /// <summary>
        ///     处理鼠标滑轮滚动时候，pictureEdit中图片的缩放;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanvasMouseWheel(object sender, MouseEventArgs e)
        {
            if (_currentPostCard != null)
            {
                _currentPostCard.CanvasResize(e.Location, _wheelSpeed, e.Delta, _shiftKeyDown, _ctrlKeyDown);
                sourcePicture.Refresh();
            }

            //base.OnMouseWheel(e);
        }

        /// <summary>
        ///     重新绘画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureEdit1_Paint(object sender, PaintEventArgs e)
        {
            if (_currentPostCard?.ThumbnailImage != null)
            {
                var productArea = _currentPostCard.ProductArea;
                var pictureArea = _currentPostCard.PictureArea;
                var canvasArea = _currentPostCard.CanvasArea;
                Brush myBlackBrush = new SolidBrush(Color.FromArgb(150, Color.Black));
                Brush myWhiteBrush = new SolidBrush(Color.White);
                var myPen = new Pen(Color.Green, 2);
                var g = e.Graphics;
                if (_currentFinishedImage != null)
                {
                    productArea.Offset(10, 10);
                    g.FillRectangle(myBlackBrush, productArea);
                    productArea.Offset(-10, -10);
                    g.DrawImage(_currentFinishedImage, _currentPostCard.ProductArea);
                }
                else
                {
                    var mm = _currentPostCard.ThumbnailImage;
                    if (mm == null)
                        return;
                    productArea.Offset(10, 10);
                    g.FillRectangle(myBlackBrush, productArea);
                    productArea.Offset(-10, -10);
                    g.FillRectangle(myWhiteBrush, productArea);

                    if (_transportationImage != null && (_mouseLeftDown || !_currentPostCard.ReadOny))
                        g.DrawImage(_transportationImage, canvasArea);

                    var pictureAreaImage = new Rectangle();
                    if (canvasArea.Width == 0 || canvasArea.Height == 0)
                        return;
                    pictureAreaImage.X = (pictureArea.X - canvasArea.X) * mm.Width / canvasArea.Width;
                    pictureAreaImage.Y = (pictureArea.Y - canvasArea.Y) * mm.Width / canvasArea.Width;
                    pictureAreaImage.Width = pictureArea.Width * mm.Width / canvasArea.Width;
                    pictureAreaImage.Height = pictureArea.Height * mm.Height / canvasArea.Height;
                    g.DrawImage(mm, pictureArea, pictureAreaImage, GraphicsUnit.Pixel);

                    if (_currentPostCard.ReadOny)
                        myPen.Color = Color.Red;
                    g.DrawRectangle(myPen, pictureArea);
                }
            }

            pictureEdit1.Refresh();
        }

        public Bitmap PTransparentAdjust(Bitmap src, int num)
        {
            try
            {
                var w = src.Width;
                var h = src.Height;
                var dstBitmap = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                var srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var dstData = dstBitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                unsafe
                {
                    var pIn = (byte*) srcData.Scan0.ToPointer();
                    var pOut = (byte*) dstData.Scan0.ToPointer();
                    for (var y = 0; y < h; y++)
                    {
                        for (var x = 0; x < w; x++)
                        {
                            int b = pIn[0];
                            int g = pIn[1];
                            int r = pIn[2];
                            pOut[1] = (byte) g;
                            pOut[2] = (byte) r;
                            pOut[3] = (byte) num;
                            pOut[0] = (byte) b;
                            pIn += 4;
                            pOut += 4;
                        }

                        pIn += srcData.Stride - w * 4;
                        pOut += srcData.Stride - w * 4;
                    }

                    src.UnlockBits(srcData);
                    dstBitmap.UnlockBits(dstData);
                    return dstBitmap;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return null;
            }
        }

        private void FormatTypeChange(object sender, ItemClickEventArgs e)
        {
            if (_currentPostCard != null && !_currentPostCard.ReadOny)
            {
                _currentPostCard.Type = (int) e.Item.Tag;
                ReFreshMyNode();
            }
        }

        //TreeListNode currentNode;
        private void treeList1_FocusedNodeChanged(object sender, FocusedNodeChangedEventArgs e)
        {
            cutMyPicture.Enabled = false;
            recutMyPicture.Enabled = false;
            _currentPostCard = null;
            _currentPostCardAlbum = null;
            if (e.Node != null && e.Node.Tag != null)
                if (e.Node.Tag.GetType().Name == "PostCard")
                {
                    _currentPostCard = (PostCard) e.Node.Tag;
                    groupAlbumPrint.Visibility = LayoutVisibility.Never;
                    groupPostCard.Visibility = LayoutVisibility.Always;
                    layoutControlItem8.Visibility = LayoutVisibility.Always;
                    groupPostCard.Selected = true;
                }
                else if (e.Node.Tag.GetType().Name == "PostCardAlbum")
                {
                    foreach (var tmpPostCard in ((PostCardAlbum) e.Node.Tag).PostCards)
                        if (tmpPostCard.FinishedFile.Exists == false)
                        {
                            tmpPostCard.Node.Selected = true;
                            return;
                        }

                    _currentPostCardAlbum = (PostCardAlbum) e.Node.Tag;
                    groupAlbumPrint.Visibility = LayoutVisibility.Always;
                    groupPostCard.Visibility = LayoutVisibility.Never;
                    layoutControlItem8.Visibility = LayoutVisibility.Never;
                    groupAlbumPrint.Selected = true;
                }

            ReFreshMyNode();
        }

        private void ReFreshMyNode()
        {
            sourcePicture.Cursor = Cursors.Default;
            if (_currentFinishedImage != null)
            {
                _currentFinishedImage.Dispose();
                _currentFinishedImage = null;
            }

            _currentFinishedImage = null;
            //progressValue.Caption = PostCard.Count_Processed.ToString();
            //progressMax.Caption = PostCard.Count_PreProcessed.ToString();
            //pathProcessProgress.EditValue =PostCard.Count_Processed;
            //repositoryItemProgressBar1.Maximum = PostCard.Count_PreProcessed;
            if (_currentPostCard != null && _currentPostCard.ThumbnailImage != null)
            {
                //sizeName.Text = "成品尺寸：" + ((PostCardAlbum)currentPostCard.ParentPostCardAlbum).ProductSize.Width.ToString() + "×" + ((PostCardAlbum )currentPostCard.ParentPostCardAlbum).ProductSize.Height.ToString();
                cutMyPicture.Enabled = false;
                recutMyPicture.Enabled = false;
                if (!_currentPostCard.ReadOny)
                {
                    sourcePicture.Cursor = Cursors.Hand;
                    cutMyPicture.Enabled = true;
                    _currentPostCard.Node.SetValue(1, "");
                    _currentPostCard.Node.ImageIndex = _currentPostCard.Node.SelectImageIndex = 1;
                    sourcePicture.Focus();
                }
                else
                {
                    recutMyPicture.Enabled = true;
                    if (_currentPostCard.FinishedFile.Exists)
                    {
                        _currentPostCard.Node.SetValue(1, "已处理");
                        _currentPostCard.Node.ImageIndex = _currentPostCard.Node.SelectImageIndex = 5;
                        _currentFinishedImage = GetImage(_currentPostCard.FinishedFile.FullName);
                    }
                }

                _currentPostCard.SetProductArea(sourcePicture.Size, 0.7);
                _transportationImage = PTransparentAdjust((Bitmap) _currentPostCard.ThumbnailImage, 200);
            }

            sourcePicture.Refresh();
            if (_currentPostCardAlbum != null)
            {
                textEdit1.Text = _currentPostCardAlbum.SourceFolderInfo.FullName;
                if (_currentPostCardAlbum.SourceFolderInfo.FullName.Length > 42)
                    customerRequire.Text = _currentPostCardAlbum.SourceFolderInfo.FullName.Substring(_currentPostCardAlbum.SourceFolderInfo.FullName.Length - 42);
                else
                    customerRequire.Text = _currentPostCardAlbum.SourceFolderInfo.FullName;

                myDataSet.Tables[0].Clear();
                foreach (var tmpPostCard in _currentPostCardAlbum.PostCards)
                {
                    var tmpNewRow = myDataSet.Tables[0].NewRow();
                    tmpNewRow[0] = tmpPostCard.SourceFile.Name;
                    tmpNewRow[1] = tmpPostCard.Copys;
                    tmpNewRow[2] = tmpPostCard.OppoppositeType;
                    tmpNewRow[3] = tmpPostCard.Type + 65;
                    myDataSet.Tables[0].Rows.Add(tmpNewRow);
                }

                posCardCount.EditValue = _currentPostCardAlbum.PostCards.Count;
                postCardPrintCount.EditValue = _currentPostCardAlbum.PostCardCount;
                paperSelect_SelectedIndexChanged(null, null);
            }
        }

        private static Image GetImage(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, bytes.Length);

                    var memoryStream = new MemoryStream(bytes);
                    return Image.FromStream(memoryStream);
                }
            }
            catch
            {
                return null;
            }
            
        }

        private void OpenMyRootSource(object sender, ItemClickEventArgs e)
        {
            var myFolder = new FolderBrowserDialog();

            if (myFolder.ShowDialog() == DialogResult.OK)
            {
                var postCardAlbum = new PostCardAlbum(new DirectoryInfo(myFolder.SelectedPath));
            }
        }

        private void PictureEdit1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_currentPostCard != null) _oldPoint = e.Location;
            _mouseLeftDown = true;
            //sourcePicture.Refresh();
        }

        private void SourcePicture_MouseMove(object sender, MouseEventArgs e)
        {
            //if (currentNode != null && currentNode.Tag) != "已经预处理")
            //    return;
            if (_currentPostCard != null && _currentPostCard.ThumbnailImage != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    _mouseLeftDown = true;
                    _currentPostCard.CanvasMove(e.X - _oldPoint.X, e.Y - _oldPoint.Y);
                    _oldPoint = e.Location;
                }
                else
                {
                    _mouseLeftDown = false;
                }

                sourcePicture.Refresh();
            }
        }

        private void PictureEdit1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey) _shiftKeyDown = true;
            if (e.KeyCode == Keys.ControlKey) _ctrlKeyDown = true;
        }

        private void PictureEdit1_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ShiftKey:
                    _shiftKeyDown = false;
                    break;
                case Keys.ControlKey:
                    _ctrlKeyDown = false;
                    break;
            }
        }

        /// <summary>
        ///     图像和裁切框适合选项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FitOption(object sender, ItemClickEventArgs e)
        {
            _currentPostCard?.Fit((int) e.Item.Tag);
            sourcePicture.Refresh();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //if (this.Visible==true)
            //{
            //    this.Hide();
            //    e.Cancel = true;
            //    return;
            //}
            if (progressMax.Caption != progressValue.Caption)
            {
                if (MessageBox.Show("还有未完成的作业，是否真的停止？", "提示", MessageBoxButtons.OKCancel) != DialogResult.OK)
                    e.Cancel = true;
                return;
            }

            var oFstream = new FileStream(Application.StartupPath + "\\ConfigsFile\\myIni.ini", FileMode.Create);
            var streamWriter = new StreamWriter(oFstream);
            streamWriter.WriteLine("[Skin]" + defaultLookAndFeel1.LookAndFeel.SkinName);
            streamWriter.Close();
            oFstream.Close();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
        }

        private void SubmitMyPicture(object sender, EventArgs e)
        {
            if (_currentPostCard != null)
            {
                _currentPostCard.EnterQueue();
                PostCard tmpPostCard = null;
                tmpPostCard = _currentPostCard.ParentPostCardAlbum.GetNotCat();
                if (tmpPostCard == null)
                    foreach (TreeListNode tmpNode in treeList1.Nodes)
                        if (tmpNode.Tag != null && tmpNode.Tag.GetType().Name == "PostCardAlbum")
                        {
                            tmpPostCard = ((PostCardAlbum) tmpNode.Tag).GetNotCat();
                            if (tmpPostCard != null)
                                break;
                        }

                if (tmpPostCard == null)
                {
                    splashScreenManager2.ShowWaitForm();
                    Thread.Sleep(1000);
                    splashScreenManager2.CloseWaitForm();
                }
                else
                {
                    tmpPostCard.Node.Selected = true;
                }

                ReFreshMyNode();
            }
        }

        private void rotationCounterClockWise_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_currentPostCard != null && !_currentPostCard.ReadOny)
            {
                _currentPostCard.Angle += (int) e.Item.Tag;
                //sourcePicture.Refresh();
                ReFreshMyNode();
            }
        }

        private void sourcePicture_DoubleClick(object sender, EventArgs e)
        {
            if (cutMyPicture.Enabled)
            {
                SubmitMyPicture(null, null);
            }
            else
            {
                if (treeList1.Nodes.Count == 0) OpenMyRootSource(null, null);
            }
        }

        private void recutMyPicture_Click(object sender, EventArgs e)
        {
            _currentPostCard.ReCut();
            ReFreshMyNode();
        }

        private void locatedMyPath_Click(object sender, EventArgs e)
        {
            if (_currentPostCardAlbum != null)
            {
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.DefaultExt = "pdf";
                saveFileDialog.FileName = "result";
                saveFileDialog.Filter = "PDF文件|*.pdf";
                saveFileDialog.InitialDirectory = _currentPostCardAlbum.SourceFolderInfo.FullName;
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _currentPostCardAlbum.ResultFileName = saveFileDialog.FileName;
                    _currentPostCardAlbum.ConvertToPdf(openAfterCombine.Checked);
                }
            }
        }

        private void 工单详情_DragDrop(object sender, DragEventArgs e)
        {
            foreach (var dragFile in (Array) e.Data.GetData(DataFormats.FileDrop))
            {
                var str = dragFile.ToString();
                bool flag;
                if (Directory.Exists(str))
                {
                    flag = false;
                    foreach (TreeListNode tmpNode in treeList1.Nodes)
                        //if TmpNode.get
                        if (((PostCardAlbum) tmpNode.Tag).SourceFolderInfo.FullName.ToLower() == str.ToLower())
                            flag = true;
                    if (!flag)
                    {
                        var currentAlbum = new PostCardAlbum(new DirectoryInfo(str));
                    }
                }
            }

            //ThreadPool.QueueUserWorkItem(CopyMyFile,pm);
        }

        private void 工单详情_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void 显示主程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
        }

        private void 隐藏主程序ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void toolTipController1_GetActiveObjectInfo(object sender, ToolTipControllerGetActiveObjectInfoEventArgs e)
        {
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            ReFreshMyNode();
        }

        private void pictureEdit1_Paint_1(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var myPen = new Pen(Color.Red, 2);

            if (_currentPostCard != null)
                if (_currentPostCard.ThumbnailImage != null)
                {
                    var canvasArea = _currentPostCard.CanvasArea;
                    var pictureArea = _currentPostCard.PictureArea;

                    var thumthumbnailCanvasArea = new Rectangle();
                    //获取缩略图的尺寸
                    thumthumbnailCanvasArea.Width = (int) (pictureEdit1.Width * 0.7);
                    thumthumbnailCanvasArea.Height = thumthumbnailCanvasArea.Width * canvasArea.Height / canvasArea.Width;
                    if (thumthumbnailCanvasArea.Height > pictureEdit1.Height * 0.7)
                    {
                        thumthumbnailCanvasArea.Height = (int) (pictureEdit1.Height * 0.7);
                        thumthumbnailCanvasArea.Width = thumthumbnailCanvasArea.Height * canvasArea.Width / canvasArea.Height;
                    }

                    thumthumbnailCanvasArea.X = (pictureEdit1.Width - thumthumbnailCanvasArea.Width) / 2;
                    thumthumbnailCanvasArea.Y = (pictureEdit1.Height - thumthumbnailCanvasArea.Height) / 2;

                    var thumthumbnailPictureArea = new Rectangle();
                    thumthumbnailPictureArea.X = thumthumbnailCanvasArea.X + (pictureArea.X - canvasArea.X) * thumthumbnailCanvasArea.Width / canvasArea.Width;
                    thumthumbnailPictureArea.Y = thumthumbnailCanvasArea.Y + (pictureArea.Y - canvasArea.Y) * thumthumbnailCanvasArea.Height / canvasArea.Height;
                    thumthumbnailPictureArea.Width = pictureArea.Width * thumthumbnailCanvasArea.Width / canvasArea.Width;
                    thumthumbnailPictureArea.Height = pictureArea.Height * thumthumbnailCanvasArea.Height / canvasArea.Height;

                    if (_currentPostCard.ReadOny)
                        myPen.Color = Color.Red;
                    else
                        myPen.Color = Color.Green;
                    g.DrawImage(_currentPostCard.ThumbnailImage, thumthumbnailCanvasArea);
                    g.DrawRectangle(myPen, thumthumbnailPictureArea);
                }
        }

        private void resetMyPostCardAlbum_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_currentPostCardAlbum != null)
                if (MessageBox.Show("确定要重新设置该影集的所有明细片吗？", "确认", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    _currentPostCardAlbum.Reset();
                    if (_currentPostCardAlbum.PostCards.Count != 0) _currentPostCardAlbum.PostCards[0].Node.Selected = true;
                }
        }

        private void paperSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (paperSelect.Text == "SRA3横向") _currentPostCardAlbum.PaperSize = new Size(450, 320);
            if (paperSelect.Text == "SRA3纵向") _currentPostCardAlbum.PaperSize = new Size(320, 450);
            textEdit2.EditValue = _currentPostCardAlbum.PaperRowColumn.Width * _currentPostCardAlbum.PaperRowColumn.Height;
            //paperPrintCount.EditValue ;
            paperPrintCount.EditValue = _currentPostCardAlbum.PaperCount;
        }

        private void gridView1_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (e.Column.ColumnHandle == 2)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(e.Value.ToString()[0]))
                {
                }
                else
                {
                    gridView1.SetFocusedValue('A');
                }

                _currentPostCardAlbum.PostCards[e.RowHandle].OppoppositeType = e.Value.ToString()[0];
            }

            if (e.Column.ColumnHandle == 1)
            {
                _currentPostCardAlbum.PostCards[e.RowHandle].Copys = (int) e.Value;
                postCardPrintCount.EditValue = _currentPostCardAlbum.PostCardCount;
                paperSelect_SelectedIndexChanged(null, null);
            }
        }

        private void 退出软件ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonEdit1_EditValueChanged(object sender, EventArgs e)
        {
        }

        private void buttonEdit1_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button.Index == 0)
            {
                var myFolderSave = new FolderBrowserDialog();
                if (myFolderSave.ShowDialog() == DialogResult.OK)
                {
                }
            }
        }

        private void customerRequire_TextChanged(object sender, EventArgs e)
        {
            if (_currentPostCardAlbum != null) _currentPostCardAlbum.WaterMark = customerRequire.Text;
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            if (_currentPostCard != null && _currentPostCard.ParentPostCardAlbum != null) _currentPostCard.ParentPostCardAlbum.Node.Selected = true;
        }

        private void sourcePicture_EditValueChanged(object sender, EventArgs e)
        {
        }

        private void gridView1_CellValueChanging(object sender, CellValueChangedEventArgs e)
        {
            if (e.Column.ColumnHandle == 2)
            {
                if ("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(e.Value.ToString()[0]))
                    gridView1.SetFocusedValue(e.Value.ToString()[0]);
                else
                    gridView1.SetFocusedValue('A');
                _currentPostCardAlbum.PostCards[e.RowHandle].OppoppositeType = e.Value.ToString()[0];
            }

            //if (e.Column.ColumnHandle == 1)
            //{
            //    currentPostCardAlbum.postCards[e.RowHandle].Copys = (int)e.Value;
            //    postCardPrintCount.EditValue = currentPostCardAlbum.PostCardCount;
            //    paperSelect_SelectedIndexChanged(null, null);
            //}
        }

        private void checkEdit1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkEdit1.Checked)
            {
                colpostCardCopys.OptionsColumn.AllowEdit = false;
                colpostCardCopys.OptionsColumn.ReadOnly = true;
                colpostCardCopys.OptionsColumn.AllowFocus = false;
                textEdit3.Enabled = true;
                spinEdit1.Enabled = true;
                textEdit3.EditValue = 1;
                //spinEdit1.Properties.MaxValue = currentPostCardAlbum.postCards.Count;
                //foreach ()
            }
            else
            {
                colpostCardCopys.OptionsColumn.AllowEdit = true;
                colpostCardCopys.OptionsColumn.ReadOnly = false;
                colpostCardCopys.OptionsColumn.AllowFocus = true;
                textEdit3.Enabled = false;
                spinEdit1.Enabled = false;
            }
        }

        private void textEdit3_EditValueChanged(object sender, EventArgs e)
        {
            spinEdit1.Value = gridView1.RowCount * textEdit3.Value;
            //for (int i = 0; i < gridView1.RowCount; i++)
            //{
            //    gridView1.SetRowCellValue(i, colpostCardCopys, textEdit3.Value  );
            //}
        }

        private void spinEdit1_EditValueChanged(object sender, EventArgs e)
        {
            var tmp001 = (int) spinEdit1.Value / gridView1.RowCount;
            var tmp002 = gridView1.RowCount - (int) spinEdit1.Value % gridView1.RowCount;
            for (var i = 0; i < gridView1.RowCount; i++)
                if (i < tmp002)
                    gridView1.SetRowCellValue(i, colpostCardCopys, tmp001);
                else
                    gridView1.SetRowCellValue(i, colpostCardCopys, tmp001 + 1);
        }

        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            PostCard.RemoveAll();
            groupAlbumPrint.Visibility = LayoutVisibility.Never;
            groupPostCard.Visibility = LayoutVisibility.Always;
            layoutControlItem8.Visibility = LayoutVisibility.Always;
            groupPostCard.Selected = true;
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            var String = _currentPostCardAlbum.SourceFolderInfo.FullName;
            var myProcess = new Process();
            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.Arguments = String;
            myProcess.StartInfo.FileName = "Explorer";
            myProcess.StartInfo.CreateNoWindow = false;
            myProcess.Start();
        }

        private void gridControl1_Click(object sender, EventArgs e)
        {
        }

        //private Queue<PostCard> preQueue = new Queue<PostCard>();
        //private Queue<PostCard> processQueue = new Queue<PostCard>();
        private delegate void DelegateSetImage(TreeListNode treeListNode, int a);

        private delegate void DelegatereFreshPostCard(PostCard postCard);
    }
}