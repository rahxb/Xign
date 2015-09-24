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

        #region Class, Enum, Const

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


        public enum GuidModuleKind
        {
            None = 0x00,    // None
            Unused  = 0x55, // U 
            File    = 0x46, // F
            Folder  = 0x4f, // O

            // ... UFO!?
        }

        public class GuidModuleItem
        {
            public string guid { get; set; } = "";
            public GuidModuleKind kind { get; set; } = GuidModuleKind.None;
            public UInt64 location { get; set; } = 0;
            public UInt64 length { get; set; } = 0;
            public string hash { get; set; } = "";
        }


        public class DataModuleItem
        {
            public string guid { get; set; } = "";
            public string parent_guid { get; set; } = "";
            public string f_name { get; set; } = "";
            public string content { get; set; } = "";
        }

        const string GUID_FILE = "guid.csv";
        const string DATA_FILE = "data.dat";

        #endregion

        List<GuidModuleItem> _guidList = new List<GuidModuleItem>();




        #region GUI

        public frmFileManager()
        {
            InitializeComponent();
        }

        private void frmFileManager_Load(object sender, EventArgs e)
        {
            trFolder.LabelEdit = true;
            trFolder.Nodes.Add("root", "root");
            trFolder.AfterLabelEdit += TrFolder_AfterLabelEdit;

            LoadGuid();

            foreach (GuidModuleItem gmi in _guidList)
            {
                DataModuleItem dmi = LLoadData(gmi.guid);
                if (dmi.parent_guid.Equals(""))
                    trFolder.Nodes["root"].Nodes.Add(dmi.guid, dmi.f_name);
            }
            trFolder.ExpandAll();
        }

        /// <summary>
        /// ツリーのラベルの編集が完了したときのイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrFolder_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            LAddFolder(e.Node.Text, "");
        }

        private void mnuFRBNewFolder_Click(object sender, EventArgs e)
        {
            string strKey = Guid.NewGuid().ToString();
            string strDummyName = "新規フォルダ" + Environment.TickCount;

            //GuidModuleItem gmi = new GuidModuleItem();
            //gmi.guid = Guid.NewGuid().ToString();
            //gmi.kind = GuidModuleKind.Folder;

            //_guidList.Add(gmi);

            TreeNode tn = trFolder.SelectedNode.Nodes.Add(strKey, strDummyName);
            trFolder.SelectedNode.ExpandAll();
            trFolder.SelectedNode = tn;
            tn.BeginEdit();
        }

        #endregion







        #region Functions

        /// <summary>
        /// ツリーにフォルダを追加する
        /// </summary>
        /// <param name="strFolderName"></param>
        void LAddFolder(string strFolderName, string parent_guid)
        {
            // データ追加
            GuidModuleItem gmi;
            using (FileStream fs = new FileStream(DATA_FILE, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                gmi = AddData(fs, Guid.NewGuid().ToString(), parent_guid, strFolderName, new byte[] { });
            }

            // ヘッダ追加
            _guidList.Add(gmi);

            // guidをファイルに保存する
            SaveGuid();
        }

        void LoadGuid()
        {
            _guidList.Clear();

            bool bFirst = true;
            if (System.IO.File.Exists(GUID_FILE) == false)
                return;

            string[] arLines = System.IO.File.ReadAllLines(GUID_FILE);
            string[] arHeader = null;
            foreach(string strLine in arLines)
            {
                string[] arFields = strLine.Split('\t');
                if (bFirst)
                {
                    bFirst = false;
                    arHeader = arFields;
                }
                else
                {
                    GuidModuleItem gmi = new GuidModuleItem();
                    for (int i = 0; i < arHeader.Length; i++)
                    {
                        string name = arHeader[i];
                        string value = arFields[i];
                        switch(name.ToUpper())
                        {
                            case "GUID":
                                gmi.guid = value;
                                break;
                            case "KIND":
                                gmi.kind = ToModuleKind(value.ToUpper());
                                break;
                            case "LOCATION":
                                gmi.location = UInt64.Parse(value);
                                break;
                            case "LENGTH":
                                gmi.length = UInt64.Parse(value);
                                break;
                            case "HASH":
                                gmi.hash = value;
                                break;
                        }
                    }
                    _guidList.Add(gmi);
                }
            }
        }

        GuidModuleKind ToModuleKind(string str)
        {
            GuidModuleKind mk = GuidModuleKind.None;
            switch (str.ToUpper())
            {
                case "U":
                    mk = GuidModuleKind.Unused;
                    break;
                case "F":
                    mk = GuidModuleKind.File;
                    break;
                case "O":
                    mk = GuidModuleKind.Folder;
                    break;
                default:
                    throw new NotImplementedException();
            }
            return mk;
        }

        void SaveGuid()
        {
            /*
            [guid.csv]
            guid	kind	location	length  hash
            00-110.	(U/F/O)	0x0001		0x0200  (sha256)
            */

            List<string> arContent = new List<string>();

            arContent.Add(string.Join("\t", new string[] { "guid", "kind", "location", "length", "hash" }));
            foreach(GuidModuleItem gmi in _guidList)
            {
                arContent.Add(string.Join("\t", new string[] { gmi.guid, char.ConvertFromUtf32((int)gmi.kind), gmi.location.ToString(), gmi.length.ToString(), gmi.hash }));
            }
            System.IO.File.WriteAllText(GUID_FILE, string.Join("\r\n", arContent.ToArray()));
        }

        void LAddFile(string strFilePath, string parent_guid)
        {
            string name = System.IO.Path.GetFileName(strFilePath);
            byte[] binary = System.IO.File.ReadAllBytes(strFilePath);
            using (FileStream fs = new FileStream(DATA_FILE, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                AddData(fs, "", parent_guid, name, binary);
            }
        }

        DataModuleItem LLoadData(string guid)
        {
            GuidModuleItem gmiFound = null;
            foreach (GuidModuleItem gmi in _guidList)
            {
                if (gmi.guid.Equals(guid))
                {
                    gmiFound = gmi;
                    break;
                }
            }
            if (gmiFound == null)
                return null;

            byte[] binData = null;
            using (FileStream fs = new FileStream(DATA_FILE, FileMode.Open, FileAccess.Read))
            {
                fs.Position = (long)gmiFound.location;
                binData = new byte[gmiFound.length];
                fs.Read(binData, 0, binData.Length);
            }
            string strData = System.Text.Encoding.UTF8.GetString(binData);
            string[] arUL = strData.Split(new string[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
            string strUpper = arUL[0];

            string strLower = "";
            if (2 <= arUL.Length)
                strLower = arUL[1];

            string[] arLines = strUpper.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            DataModuleItem dmi = new DataModuleItem();
            foreach(string strLine in arLines)
            {
                string[] arFields = strLine.Split(new char[] { ':' }, 2);
                if (2 <= arFields.Length)
                {
                    string name = arFields[0];
                    string value = arFields[1];
                    switch (name.ToUpper())
                    {
                        case "GUID":
                            dmi.guid = value;
                            break;
                        case "PARENT-GUID":
                            dmi.parent_guid = value;
                            break;
                        case "F-NAME":
                            dmi.f_name = Base64DecodeText(value);
                            break;
                    }
                }
            }
            dmi.content = strLower;
            return dmi;
        }

        string Base64DecodeText(string strBase64EncodeAndUTF8)
        {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(strBase64EncodeAndUTF8));
        }

        string Base64EncodeText(string strPlain)
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(strPlain));
        }


        public GuidModuleItem AddData(Stream srm, string guid, string parent_guid, string f_name, byte[] file_data)
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

            string b64f_name = Base64EncodeText(f_name);
            GuidModuleKind mkKind = GuidModuleKind.File;

            // 追記する内容を準備する
            StringBuilder sbText = new StringBuilder();
            sbText.Append(string.Format("{0}:{1}\r\n", "guid", guid));
            sbText.Append(string.Format("{0}:{1}\r\n", "parent-guid", parent_guid));
            sbText.Append(string.Format("{0}:{1}\r\n", "f-name", b64f_name));
            sbText.Append("" + "\r\n");
            if (file_data.Length == 0)
            {
                mkKind = GuidModuleKind.Folder;
                sbText.Append("" + "\r\n");
            }
            else
            {
                mkKind = GuidModuleKind.File;
                Convert.ToBase64String(file_data, Base64FormattingOptions.InsertLineBreaks);
            }
            byte[] binary = System.Text.Encoding.UTF8.GetBytes(sbText.ToString());

            // ストリームに追記する
            srm.Write(binary, 0, binary.Length);
            srm.WriteByte(0x00);

            // ヘッダを準備
            GuidModuleItem gmiResult = new GuidModuleItem();
            gmiResult.guid = guid;
            gmiResult.hash = "";
            gmiResult.kind = mkKind;
            gmiResult.length = (UInt64)binary.Length;
            gmiResult.location = (UInt64)lngLocation;

            return gmiResult;
        }

        #endregion



    }
}
