using RxLibrary;
using System;
using System.Windows.Forms;

namespace RxLibDemo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;
            RxLib.XmlToRxc(openFileDialog1.FileName, saveFileDialog1.FileName);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() != DialogResult.OK)
                return;
            if (saveFileDialog2.ShowDialog() != DialogResult.OK)
                return;

            string encryption = edtEncryption.Text.Trim();

            RxLib.ConvertData(openFileDialog2.FileName, saveFileDialog2.FileName, EncryptionKeyFile: encryption);
            MessageBox.Show(RxLib.LastConvertStatus());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (openFileDialog3.ShowDialog() == DialogResult.OK)
                edtEncryption.Text = openFileDialog3.FileName;

        }
    }
}
