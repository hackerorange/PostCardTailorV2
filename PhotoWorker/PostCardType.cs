using DevExpress.XtraTreeList.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoWorker
{
    public class PostCardType
    {
        public delegate void DeletePostCardAlbumNodeHandler(PostCardType sender);
        public delegate void InitPostCardAlbumHandler(PostCardAlbum sender);
        public delegate void InitNode(PostCardType sender);

        public static event InitNode InitPostCardAlbum;
        public static event DeletePostCardAlbumNodeHandler DeleteTreeListNode;

        private System.Drawing.Size _pictureSize = System.Drawing.Size.Empty;
        private TreeListNode _node;
        private PostCardAlbum _parentPostCardAlbum;
        private int _type;
        private char _oppoppositeType = 'A';
        private int _copys = 0;

        public int Copys
        {
            get { return _copys; }
            set { _copys = value; }
        }


        public void SetMyNode()
        {
            InitPostCardAlbum(this);
        }

        public char OppoppositeType
        {
            get { return _oppoppositeType; }
            set { _oppoppositeType = value; }
        }

        public int Type
        {
            get { return _type; }
            set { _type = value; }
        }
        public TreeListNode Node
        {
            get
            {
                return _node;
            }
            set
            {
                if (_node != null)
                    _node.Tag = null;
                value.Tag = this;
                _node = value;
                //this.node.SetValue(0, sourceFile.Name);
                //this.node.SetValue(1, this.state);
                //this.node.ImageIndex = this.node.SelectImageIndex = 1;
                //if (value.ParentNode != null && value.ParentNode.Expanded == false)
                //    value.ParentNode.ExpandAll();
            }
        }
        public void Remove()
        {
            if (DeleteTreeListNode != null && _node != null)
            {
                DeleteTreeListNode(this);
            }
        }

        public PostCardAlbum ParentPostCardAlbum
        {
            get { return _parentPostCardAlbum; }
            set { _parentPostCardAlbum = value; }
        }


    }
}
