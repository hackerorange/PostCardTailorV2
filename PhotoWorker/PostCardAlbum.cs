using DevExpress.XtraTreeList.Nodes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text;
using System.Threading;
//using WebSupergoo.ABCpdf7;

namespace PhotoWorker
{
    public class PostCardAlbum:PostCardType
    {
        #region 定义事件
        public delegate void ProgressHandler(int value, int maxValue);

        public static event InitPostCardAlbumHandler RequirPoarCardAlbumInformation;
        public static event ProgressHandler PdfProcessProgress;
        private int _unifiedQuantity = 0;
        private bool _isUnifiedQuantity = false;
        private bool _isUnifiedCopy = false;

        #endregion

        public bool IsUnifiedQuantity
        {
            get => _isUnifiedQuantity;
            set 
            {
                _isUnifiedQuantity = value;
                //如果统一总数，则取消统一份数
                if (_isUnifiedQuantity )
                {
                    _isUnifiedCopy = false;
                }
                //如果统一总数后，总数不为0，并且已经初始化完成（所有子集和明信片都已经加载完成）
                if (_isUnifiedQuantity && _unifiedQuantity!=0 && _hadInitFinished )
                {
                    _isUnifiedCopy = false;
                    var tmp001 = _unifiedQuantity / PostCards.Count();
                    var tmp002 = PostCards.Count() - _unifiedQuantity % PostCards.Count();
                    for (var i = 0; i < PostCards.Count; i++)
                    {
                        if (i < tmp002)
                        {
                            PostCards[i].Copys = tmp001;
                        }
                        else
                        {
                            PostCards[i].Copys = tmp001 + 1;
                        }
                    }
                }
            }
        }
        public new int Copys
        {
            get => base.Copys;
            set 
            {
                base.Copys = value;
                IsUnifiedCopy = true;
                _isUnifiedQuantity = false;
            }
        }

        public bool IsUnifiedCopy
        {
            get => _isUnifiedCopy;
            set 
            { 
                _isUnifiedCopy = value;
                if (_isUnifiedCopy && _hadInitFinished)
                {
                    _isUnifiedQuantity = false;
                    if (Copys == 0)
                    {
                        Copys = 1;
                    }
                    foreach(PostCard tmpPostCard in PostCards )
                    {
                        tmpPostCard.Copys = Copys;
                    }
                }
            }
        }


        
        public PostCardAlbum(DirectoryInfo sourceFolderInfo, TreeListNode treeListNode,PostCardAlbum postCardAlbum=null)
        {
            SourceFolderInfo = sourceFolderInfo;
            //this.Node = treeListNode;
            ParentPostCardAlbum = postCardAlbum;
            SetMyNode();
        }
        public PostCardAlbum(DirectoryInfo sourceFolderInfo,PostCardAlbum parentPostCardAlbum=null)
        {
            if (sourceFolderInfo.Attributes.ToString().Contains("System") || sourceFolderInfo.Name == "Thumbnail" || sourceFolderInfo.Name == "INK_Orange" || sourceFolderInfo.Name == "QuestionFile")
                return;
            try
            {
                sourceFolderInfo.GetFileSystemInfos();
            }
            catch
            {
                return;//当前子文件夹无法打开，跳过
            }
            SourceFolderInfo = sourceFolderInfo;
            ParentPostCardAlbum = parentPostCardAlbum;
            SetMyNode();//添加节点
            #region 添加子集
            foreach (DirectoryInfo tmpDirectory in sourceFolderInfo .GetDirectories ())
            {
                new PostCardAlbum(tmpDirectory, this);
            }
            #endregion
            #region 添加明信片
            foreach (var tmpFileInfo in sourceFolderInfo .GetFiles ())
            {
                new PostCard(tmpFileInfo, this);
            }
            #endregion
            HadInitFinished = true;
        }

        private Size _paperSize = Size.Empty;
        private Size _paperRowColumn = Size.Empty;
        private int _paperCount = 0;
        private string _resultFileName = "Result";
        private string _waterMark = "";

        public string WaterMark
        {
            get => _waterMark;
            set { _waterMark = value; }
        }

        public string ResultFileName
        {
            get => _resultFileName;
            set { _resultFileName = value; }
        }
        public int PaperCount => _paperCount;

        public int PostCardCount
        {
            get 
            {
                int count = 0;
                foreach (PostCard tmpPostCard in PostCards )
                {
                    count += tmpPostCard.Copys;
                }
                return count;

            }
        }

        public Size PaperRowColumn => _paperRowColumn;

        public Size PaperSize
        {
            get => _paperSize;
            set
            {
                _paperSize = value;
                if (!_productSize.IsEmpty)
                {
                    _paperRowColumn.Width = _paperSize.Width / _productSize.Width;
                    _paperRowColumn.Height = _paperSize.Height / _productSize.Height;
                    _paperCount = (int)Math.Ceiling(((double)PostCardCount) / (_paperRowColumn.Width * _paperRowColumn.Height));
                }
            }
        }

        public new PostCardAlbum ParentPostCardAlbum
        {
            get => base.ParentPostCardAlbum;
            set 
            {
                base.ParentPostCardAlbum = value;
                value?.ChildPostCardAlbum.Add(this);
            }
        }
        public List<PostCard> PostCards = new List<PostCard>();
        public List<PostCardAlbum> ChildPostCardAlbum = new List<PostCardAlbum>();
        public static Queue<PostCard> PreQueue = new Queue<PostCard>(), ProcessQueue = new Queue<PostCard>();
        public static Thread ProcessThread = new Thread(ThreadMethod);
        
        private Size _productSize=Size.Empty ;
        private bool _hadInitFinished = false;

        public bool HadInitFinished
        {
            get => _hadInitFinished;
            set 
            { 
                _hadInitFinished = value;
                if (value)//遍历完毕,尝试删除,如果有对象的话,不会删除
                    Remove();
            }
        }

        public FileInfo MyIni => new FileInfo(SourceFolderInfo.FullName + "//INK.ini");

        public DirectoryInfo SourceFolderInfo { get; set; }

        public Size ProductSize
        {
            get 
            {
                if (_productSize == Size.Empty)
                {
                    RequirPoarCardAlbumInformation?.Invoke(this);
                }
                return _productSize; 
            }
            set => _productSize = value;
        }
        /// <summary>
        /// 重设尺寸
        /// </summary>
        public void Reset()
        {
            MyIni.Refresh();
            MyIni.Delete();
            if(RequirPoarCardAlbumInformation !=null)
                RequirPoarCardAlbumInformation(this);//获取尺寸信息
            foreach (PostCard tmpPostCard in PostCards )
            {
                tmpPostCard.ReSet();
                tmpPostCard.ReCut();
            }
        }

        public  static  void ThreadMethod()
        {
            while (ProcessQueue.Count != 0|| PreQueue.Count != 0)
            {
                //优先预处理
                if (PreQueue.Count != 0)
                {
                    PostCard tmpWorker = PreQueue.Dequeue();
                    tmpWorker.PreProcessPostCard(500);
                    continue;
                }
                //有裁切作业
                if (ProcessQueue.Count != 0)
                {
                    PostCard tmpWorker = ProcessQueue.Dequeue();
                    tmpWorker.ProcessPostCard();
                }
            }
        }

        new public void Remove()
        {
            if (_hadInitFinished && PostCards.Count == 0 && ChildPostCardAlbum.Count == 0)
            {
                base.Remove();
                if (ParentPostCardAlbum != null)
                {
                    ParentPostCardAlbum.ChildPostCardAlbum.Remove(this);
                    ParentPostCardAlbum.Remove();
                }
            }
        }
        //public void RemoveAll()
        //{
        //    preQueue.Clear();
        //    processQueue.Clear();
        //    PostCard.Count_All = PostCard.Count_PreProcessed = PostCard.Count_Processed = 0;

        //    foreach (PostCardAlbum tmpPostCardAlbum in this.childPostCardAlbum)
        //    {
        //        tmpPostCardAlbum.RemoveAll();
        //    }
        //    foreach (PostCard tmpPostCard in this.postCards )
        //    {
        //        ((PostCardType)tmpPostCard).Remove();
        //    }
        //    postCards.Clear();
        //    childPostCardAlbum.Clear();
        //    this.Remove();
        //}
        public PostCard GetNotCat()
        {
            PostCard myReturn=null ;
            foreach  (PostCardAlbum tmpPostCardAlbum in ChildPostCardAlbum)
            {
                myReturn = tmpPostCardAlbum.GetNotCat();
                if (myReturn != null)
                    return myReturn;
            }
            foreach (PostCard tmpPostCard in PostCards)
            {
                if (!tmpPostCard.ReadOny)
                {
                    return tmpPostCard;
                }
            }
            //this.node.Expanded = false;
            return null;
        }
        public void ConvertToPdf(Boolean isOpen=false)
        {
            if (_hadInitFinished == false||PreQueue.Count !=0)
                return;
            Document oddProductDocument = new Document();
            //Document evenProductDocument = new Document();
            //设置PDF的左右上下的白边
            float leftWhite = (_paperSize.Width - PaperRowColumn.Width * _productSize.Width)/2;
            float topWhite = (_paperSize.Height - PaperRowColumn.Height * _productSize.Height)/2;
            //设置PDF的尺寸和边距
            oddProductDocument.SetPageSize(new iTextSharp.text.Rectangle(MyMath.MMtoPix(450), MyMath.MMtoPix(320)));
            //evenProductDocument.SetPageSize(new iTextSharp.text.Rectangle(MyMath.MMtoPix(450), MyMath.MMtoPix(320)));
            oddProductDocument.SetMargins(MyMath.MMtoPix(leftWhite), MyMath.MMtoPix(450 - 150 - leftWhite), 20, MyMath.MMtoPix(10));
                        //设置字体
            BaseFont bf001=BaseFont .CreateFont ();
            BaseFont bf002 = BaseFont.CreateFont();

            iTextSharp .text .Font font001=new iTextSharp.text.Font (bf001 ,10);
            iTextSharp .text .Font font002=new iTextSharp.text.Font (bf002 ,10);
            try 
            {
                PdfWriter.GetInstance(oddProductDocument, new FileStream(SourceFolderInfo.FullName + "\\oddProduct.pdf", FileMode.Create));
            }catch 
            {
                System.Windows.Forms.MessageBox.Show ("文件无法创建，请解除占用");
                return ;
            }
            
            oddProductDocument.Open();
            int currentRow = 0;
            int currentColumn = -1;
            int pdfProcessMax= 0;
            foreach (PostCard tmpPostCard in PostCards )
            {
                if (tmpPostCard.FinishedFile.Exists)
                    pdfProcessMax += tmpPostCard.Copys;
            }

            int pdfProcessValue = 0;
            iTextSharp.text.Image myImage=null;

            foreach (PostCard tmpPostCard in PostCards)
            {
                if (tmpPostCard.FinishedFile.Exists)
                {

                    for (int i = 1; i <= tmpPostCard.Copys; i++)
                    {
                        pdfProcessValue++;
                        PdfProcessProgress?.Invoke(pdfProcessValue, pdfProcessMax);
                        currentRow += ((++currentColumn) / PaperRowColumn.Width);
                        currentColumn %= PaperRowColumn.Width;
                        if (currentRow >= PaperRowColumn.Height)
                        {
                            oddProductDocument.Add(new Chunk(_waterMark, font002));
                            oddProductDocument.NewPage();
                            currentRow = 0;
                        }
                        myImage = iTextSharp.text.Image.GetInstance(tmpPostCard.FinishedFile.FullName);
                        myImage.ScaleAbsolute(MyMath.MMtoPix(_productSize.Width), MyMath.MMtoPix(_productSize.Height));
                        myImage.SetAbsolutePosition(MyMath.MMtoPix(leftWhite) + currentColumn * MyMath.MMtoPix(_productSize.Width), MyMath.MMtoPix(topWhite) + (PaperRowColumn.Height - currentRow - 1) * MyMath.MMtoPix(_productSize.Height));
                        oddProductDocument.Add(myImage);
                    }
                }
            }
            currentRow += ((++currentColumn) / PaperRowColumn.Width);
            currentColumn %= PaperRowColumn.Width;
            if (currentRow != 0 || currentColumn != 0)
            {
                int myi = 0;
                while (myImage != null && (currentRow < _paperRowColumn.Height))
                {
                    myImage = iTextSharp.text.Image.GetInstance(PostCards[myi].FinishedFile.FullName);
                    myImage.ScaleAbsolute(MyMath.MMtoPix(_productSize.Width), MyMath.MMtoPix(_productSize.Height));
                    myImage.SetAbsolutePosition(MyMath.MMtoPix(leftWhite) + currentColumn * MyMath.MMtoPix(_productSize.Width), MyMath.MMtoPix(topWhite) + (PaperRowColumn.Height - currentRow - 1) * MyMath.MMtoPix(_productSize.Height));
                    oddProductDocument.Add(myImage);
                    myi = (myi + 1) % PostCards.Count;
                    currentRow += ((++currentColumn) / PaperRowColumn.Width);
                    currentColumn %= PaperRowColumn.Width;

                }
            }
            oddProductDocument.Add(new Chunk(_waterMark, font002));
            oddProductDocument.Close();

            try
            {
                PdfReader front = new PdfReader(SourceFolderInfo.FullName + "\\oddProduct.pdf");
                Document mysecond = new Document();
                PdfCopy myCopy = new PdfCopy(mysecond, new FileStream(_resultFileName, FileMode.Create));
                mysecond.Open();
                for (int i = 1; i <= front.NumberOfPages; i++)
                {
                    PdfProcessProgress?.Invoke(i, front.NumberOfPages);
                    myCopy.AddPage(myCopy.GetImportedPage(front, i));
                }
                myCopy.Close();
                front.Close();
                PdfProcessProgress?.Invoke(0, 1);
                File.Delete(SourceFolderInfo.FullName + "\\oddProduct.pdf");
                File.Delete(SourceFolderInfo.FullName + "\\evenProduct.pdf");
            }
            catch
            {
                return;
            }
            if (isOpen)
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                myProcess.StartInfo.UseShellExecute = true;
                myProcess.StartInfo.Arguments = ResultFileName ;
                myProcess.StartInfo.FileName = "Explorer";
                myProcess.StartInfo.CreateNoWindow = false;
                myProcess.Start();

            }
        }
    }
}
