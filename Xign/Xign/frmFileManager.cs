using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xign
{
    public partial class frmFileManager : Form
    {
        class KeyAndDisplayName
        {
            public string Key { get; set; }
            public UInt64 Location { get; set; }
        }
        List<string> _KeyList = new List<string>();

        public frmFileManager()
        {
            InitializeComponent();
        }

        private void frmFileManager_Load(object sender, EventArgs e)
        {
            trFolder.LabelEdit = true;
            trFolder.Nodes.Add("root", "root");
        }

        private void mnuFRBNewFolder_Click(object sender, EventArgs e)
        {
            string strKey = Guid.NewGuid().ToString();
            string strDisplayName = "新規フォルダ" + Environment.TickCount;

            _KeyList.Add(strKey);

            TreeNode tn = trFolder.SelectedNode.Nodes.Add(strKey, strDisplayName);
            trFolder.SelectedNode.ExpandAll();
            trFolder.SelectedNode = tn;
            tn.BeginEdit();
        }
    }
}
