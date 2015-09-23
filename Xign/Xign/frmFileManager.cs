using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Xign
{
    public partial class frmFileManager : Form
    {
        /*

        [guid.csv]
        guid	kind	location	length  hash
        00-110.	(U/F/O)	0x0001		0x0200  (sha256)

            guid 
            kind F=File  O=Folder  U=Unused
            location  data.dat file-inner-address
            length    header&data-length(not-included-EOD)
            hash      check-crash-header&data

        [data.dat]
        guid:(ascii) (crlf)
        parent-guid:(ascii) (crlf)
        f-name:(base64&utf8) (crlf)
        f-attributes:unimplementation (crlf)
        f-write-datetime:unimplementation (crlf)
        f-create-datetime:unimplementation (crlf)
        (crlf)
        (base64-76chars-each-crlf)
        (EOD-\0)
        guid:(ascii) (crlf)
        parent-guid:(ascii) (crlf)
        f-name:(base64&utf8) (crlf)
        f-attributes:unimplementation (crlf)
        f-write-datetime:unimplementation (crlf)
        f-create-datetime:unimplementation (crlf)
        (crlf)
        (base64-76chars-each-crlf)
        (EOD-\0)
        ...
        */

        const string DATA_FILE = "data.dat";

        public enum ModuleKind
        {
            Unused=0x55,   // U 
            File =0x46,    // F
            Folder = 0x4f, // O

            // ... UFO!?
        }

        public class ModuleItem
        {
            public string GUID { get; set; }
            public ModuleKind Kind { get; set; }
            public UInt64 Location { get; set; }
            public UInt64 Length { get; set; }
            public string Hash { get; set; }
        }
        List<ModuleItem> _guidList = new List<ModuleItem>();

        public frmFileManager()
        {
            InitializeComponent();
        }

        private void frmFileManager_Load(object sender, EventArgs e)
        {
            trFolder.LabelEdit = true;
            trFolder.Nodes.Add("root", "root");
        }

        void LAddFolder(string strFolderName)
        {
            using (FileStream fs = new FileStream(DATA_FILE, FileMode.Create, FileAccess.ReadWrite))
            {
                AddData(fs, Guid.NewGuid().ToString(), "", strFolderName, new byte[] { });
            }
        }

        void LAddFile(string strFilePath, string parent_guid)
        {
            string name = System.IO.Path.GetFileName(strFilePath);
            byte[] binary = System.IO.File.ReadAllBytes(strFilePath);
            using (FileStream fs = new FileStream(DATA_FILE, FileMode.Create, FileAccess.ReadWrite))
            {
                AddData(fs, "", parent_guid, name, binary);
            }
        }

        public ModuleItem AddData(Stream srm, string guid, string parent_guid, string f_name, byte[] file_data)
        {
            /*
            guid:(ascii) (crlf)
            parent-guid:(ascii) (crlf)
            f-name:(base64&utf8) (crlf)
            f-attributes:unimplementation (crlf)
            f-write-datetime:unimplementation (crlf)
            f-create-datetime:unimplementation (crlf)
            (crlf)
            (base64-76chars-each-crlf)
            (EOD-\0)
            */


            // 現在は無条件でストリームの最後に追記する
            srm.Position = srm.Length;
            long lngLocation = srm.Position;

            string b64f_name = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(f_name));
            ModuleKind mkKind = ModuleKind.File;

            // 追記する内容を準備する
            StringBuilder sbText = new StringBuilder();
            sbText.Append(string.Format("{0}:{1}\r\n", "guid", guid));
            sbText.Append(string.Format("{0}:{1}\r\n", "parent-guid", parent_guid));
            sbText.Append(string.Format("{0}:{1}\r\n", "f-name", b64f_name));
            sbText.Append("" + "\r\n");
            if (file_data.Length == 0)
            {
                mkKind = ModuleKind.Folder;
                sbText.Append("" + "\r\n");
            }
            else
            {
                mkKind = ModuleKind.File;
                Convert.ToBase64String(file_data, Base64FormattingOptions.InsertLineBreaks);
            }
            byte[] binary = System.Text.Encoding.UTF8.GetBytes(sbText.ToString());

            // ストリームに追記する
            srm.Write(binary, 0, binary.Length);
            srm.WriteByte(0x00);

            // ヘッダを準備
            ModuleItem miResult = new ModuleItem();
            miResult.GUID = guid;
            miResult.Hash = "";
            miResult.Kind = mkKind;
            miResult.Length = (UInt64)binary.Length;
            miResult.Location = (UInt64)lngLocation;

            return miResult;
        }

        private void mnuFRBNewFolder_Click(object sender, EventArgs e)
        {
            string strKey = Guid.NewGuid().ToString();
            string strDummyName = "新規フォルダ" + Environment.TickCount;

            ModuleItem mi = new ModuleItem();
            mi.GUID = Guid.NewGuid().ToString();
            mi.Kind = ModuleKind.Folder;
            
            _guidList.Add(mi);

            TreeNode tn = trFolder.SelectedNode.Nodes.Add(strKey, strDummyName);
            trFolder.SelectedNode.ExpandAll();
            trFolder.SelectedNode = tn;
            tn.BeginEdit();
        }
    }
}
