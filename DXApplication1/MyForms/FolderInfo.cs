using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using DevExpress.XtraEditors;
using PhotoWorker;
using PostCardTailor.Annotations;
using PostCardTailor.model;
using Spring.Http.Converters.Json;
using Spring.Rest.Client;

namespace PostCardTailor.MyForms
{
    public partial class FolderInfo : XtraForm
    {

        private readonly List<PostCardProductSize> _productSizeList;
        private readonly MySize _postCardProductSize;
        private readonly PostCardAlbum _postCardAlbum;

        private sealed class MySize:INotifyPropertyChanged
        {
            private string _sizeName;
            private int _width;

            public string SizeName
            {
                get => _sizeName;
                set
                {
                    if (value == _sizeName) return;
                    _sizeName = value;
                    OnPropertyChanged(nameof(SizeName));
                }
            }

            public int Width
            {
                get => _width;
                set
                {
                    if (value == _width) return;
                    _width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }

            public int Height { get; set; }


            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public FolderInfo(PostCardAlbum postCardAlbum)
        {
            InitializeComponent();
            var restTemplate = new RestTemplate("http://localhost:8087");
            restTemplate.MessageConverters.Add(new NJsonHttpMessageConverter());
            try
            {
                var bodyResponse = restTemplate.GetForObject<BodyResponse<PageInfo<PostCardProductSize>>>("/size/postCardProductSize");
                if (bodyResponse.Code > 0)
                {
                    _productSizeList = bodyResponse.Data.Page;
                    sizeSelector.Properties.DataSource = _productSizeList;
                }
                else
                {
                    throw new Exception("返回码小于0，使用默认尺寸");
                }
            }
            catch
            {
                _productSizeList = new List<PostCardProductSize>
                {
                    new PostCardProductSize
                    {
                        Height = 100,
                        Width = 148,
                        SizeName = "默认尺寸"
                    }
                };
                sizeSelector.Properties.DataSource = _productSizeList;
            }
            _postCardAlbum = postCardAlbum;
            _postCardProductSize = new MySize
            {
                Width = 148,
                Height = 100
            };
            paperWidth.DataBindings.Add(new Binding("EditValue", _postCardProductSize, "Width"));
            paperHeight.DataBindings.Add(new Binding("EditValue", _postCardProductSize, "Height"));
        }
        private void SimpleButton1_Click(object sender, EventArgs e)
        {
            SaveMySelect();
            _postCardAlbum.ProductSize = new Size(_postCardProductSize.Width, _postCardProductSize.Height);
            _postCardAlbum.Type = radioGroup1.SelectedIndex;
            _postCardAlbum.OppoppositeType = comboBoxEdit1.Text[0];
            DialogResult = DialogResult.OK;
        }
        private void SaveMySelect()
        {
            if (paperHeight .Value >paperWidth .Value )
            {
                var tmp = paperWidth.Value ;
                paperWidth.Value  = paperHeight.Value ;
                paperHeight.Value  = tmp;
            }
            if (_postCardAlbum.MyIni .Exists)
                File.SetAttributes(_postCardAlbum.MyIni.FullName, FileAttributes.Normal);
            var ofile = new FileStream(_postCardAlbum.MyIni.FullName , FileMode.Create);
            var myWriter = new StreamWriter(ofile);
            myWriter.WriteLine("[版式]:"+radioGroup1.SelectedIndex);
            myWriter.WriteLine("[尺寸]:" + sizeSelector.EditValue);
            myWriter.WriteLine("[宽度]:" + paperWidth.Value );
            myWriter.WriteLine("[高度]:" + paperHeight.Value );
            myWriter.WriteLine("[反面]:" + comboBoxEdit1.Text[0]);
            myWriter.Flush();
            myWriter.Close();
            ofile.Close ();
            File.SetAttributes(_postCardAlbum.MyIni.FullName, FileAttributes.Hidden);
        }

        private void FolderInfo_Shown(object sender, EventArgs e)
        {
            radioGroup1.SelectedIndex = 1;
            textEdit1.Text = _postCardAlbum .SourceFolderInfo .FullName  ;
            sizeSelector.EditValue=new PostCardProductSize
            {
                SizeName = "明信片",
                Width = 148,
                Height = 100
            };
        }

        private void ComboBoxEdit1_EditValueChanged(object sender, EventArgs e)
        {
            if (sizeSelector.GetSelectedDataRow() is PostCardProductSize postCardProductSize)
            {
                _postCardProductSize.Height = postCardProductSize.Height;
                _postCardProductSize.Width = postCardProductSize.Width;
            }
        }

        //private readonly CustomSize myCustomSize=new CustomSize ();
        private void SizeSelector_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            //添加按钮
            if (e.Button .Index ==1)
            {
                var myCustomSize=new CustomSize();
               
                if (myCustomSize.ShowDialog() == DialogResult.OK)
                {
                    var size = new PostCardProductSize
                    {
                        Width = myCustomSize.CustomerSizeWidth,
                        Height = myCustomSize.CustomerSizeHeight,
                        SizeName = myCustomSize.CustomerSizeWidth.ToString("000") + "×" + myCustomSize.CustomerSizeHeight.ToString("000")
                    };
                    var count = _productSizeList.Count(post => post.Width == size.Width && post.Height == size.Height);
                    if (count == 0)
                    {
                        _productSizeList.Add(size);
                        //TODO:服务器端没有此记录，插入数据库
                        //TODO:插入记录成功后，将此尺寸插入到尺寸列表中
                    }
                    sizeSelector.Properties.DataSource = _productSizeList;
                    sizeSelector.EditValue = size;
                }
            }
        }
    }
}