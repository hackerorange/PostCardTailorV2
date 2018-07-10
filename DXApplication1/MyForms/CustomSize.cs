using System;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace PostCardTailor.MyForms
{
    public partial class CustomSize : XtraForm
    {
        private class CustomSizeModel
        {
            public int Height { get; set; }
            public int Width { get; set; }
        }
        public CustomSize()
        {
            InitializeComponent();
            ProductSize = new CustomSizeModel()
            {
                Height = 100,
                Width = 148
            };
            customHeight.DataBindings.Add("EditValue", ProductSize, "Height");
            customWidth.DataBindings.Add("EditValue", ProductSize, "Width");
        }

        private CustomSizeModel ProductSize { get; }

        public int CustomerSizeWidth => ProductSize.Width;
        public int CustomerSizeHeight => ProductSize.Height;
        private void SimpleButton1_Click(object sender, EventArgs e)
        {
            if (Math.Min((int)customWidth.Value, (int)customHeight.Value)<=10)
                DialogResult = DialogResult.Cancel;

            ProductSize.Width = Math.Max ((int)customWidth.Value,(int)customHeight.Value);
            ProductSize.Height = Math.Min((int)customWidth.Value, (int)customHeight.Value);
            DialogResult = DialogResult.OK;
        }

        private void SimpleButton2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel ;
        }
    }
}