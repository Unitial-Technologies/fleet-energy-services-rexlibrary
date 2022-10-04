using MDF4xx.Blocks;
using MDF4xx.IO;
using RXD.Base;
using RXD.Blocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MDF_Demo
{
    public partial class Form1 : Form
    {
        MDF mdf;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CCBlock cc = new CCBlock();

            cc.cc_val_length = 2;
            cc.cc_val[0].AsInt64 = 5;
            cc.cc_val[1].AsInt64 = 15;
            using FileStream stream = new FileStream("D:\\file.mf4", FileMode.Create);
            using BinaryWriter bw = new BinaryWriter(stream);
            bw.Write(cc.ToBytes());

        }

        private void button2_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Open MDF4 file",
                DefaultExt = "mf4",
                Filter = "MDF4 files (*.mf4)|*.mf4"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                mdf = MDF.Open(dlg.FileName);
                //mdf.Finalize(Path.ChangeExtension(mdf.FileName, "_fin.mf4"));
                log.AppendText
                    (
                    "Loaded file: " + dlg.FileName + Environment.NewLine +
                    "MDF version: " + mdf.Version + Environment.NewLine +
                    "MDF loaded: " + (!mdf.Empty).ToString() + Environment.NewLine +
                    "MDF finalized: " + mdf.Finalized.ToString() + Environment.NewLine +
                    "MDF sorted: " + mdf.Sorted.ToString() + Environment.NewLine
                    );
                lstBlocks.Items.Clear();
                foreach (KeyValuePair<Int64, BaseBlock> vp in mdf)
                {
                    lstBlocks.Items.Add(vp.Value);
                }
            }
        }

        private void lstBlocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            lstData.Clear();
            BaseBlock bb = (BaseBlock)lstBlocks.SelectedItem;

            List<FieldInfo> fields;
            List<PropertyInfo> props;
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            lstData.AppendText("Header" + Environment.NewLine);
            fields = bb.header.GetType().GetFields(flags).ToList();
            foreach (FieldInfo field in fields)
            {
                lstData.AppendText(field.Name.ToString() + " = " + field.GetValue(bb.header) + Environment.NewLine);
            }

            lstData.AppendText(Environment.NewLine + "Links" + Environment.NewLine);
            for (int i = 0; i < bb.links.Count; i++)
            {
                lstData.AppendText(i.ToString() + ": " + bb.links[i].ToString() + Environment.NewLine);
            }

            lstData.AppendText(Environment.NewLine + "Data" + Environment.NewLine);
            if (bb.dataObj != null)
            {
                fields = bb.dataObj.GetType().GetFields(flags).ToList();
                foreach (FieldInfo field in fields)
                {
                    lstData.AppendText(field.Name.ToString() + " = " + field.GetValue(bb.dataObj) + Environment.NewLine);
                }
            }

            lstData.AppendText(Environment.NewLine + "VarData" + Environment.NewLine);
            props = bb.GetType().GetProperties(flags).ToList();
            foreach (PropertyInfo prop in props)
            {
                lstData.AppendText(prop.Name.ToString() + " = " + prop.GetValue(bb) + Environment.NewLine);
            }
        }

        string GetFileName(bool open, string fileext, string title = "Select file")
        {
            Type DialogType = (open) ? typeof(OpenFileDialog) : typeof(SaveFileDialog);
            using (dynamic dlg = (FileDialog)Activator.CreateInstance(DialogType))
            {
                dlg.Title = title;
                dlg.DefaultExt = fileext;
                dlg.Filter = fileext;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }
            return null;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string inputfn = GetFileName(true, "Logger files(*.bin) | *.bin", "Open Logger file");
            string outputfn = GetFileName(false, "MDF4 files(*.mf4) | *.mf4", "Save MDF4 file");

            BinRXD rxd = new BinRXD(inputfn, DateTime.Now);
            rxd.ToMF4(outputfn);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //BinFormula bin = new BinFormula();
            //bin.data.A = 1.2;
            //byte[] b = bin.ToBytes();
        }
    }
}
