namespace RXDDemo
{
    partial class rxdDemoMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.btnOpen = new System.Windows.Forms.Button();
            this.dlgOpen = new System.Windows.Forms.OpenFileDialog();
            this.button2 = new System.Windows.Forms.Button();
            this.dlgSaveXML = new System.Windows.Forms.SaveFileDialog();
            this.btnOpenXML = new System.Windows.Forms.Button();
            this.dlgOpenXml = new System.Windows.Forms.OpenFileDialog();
            this.dlgSaveRXC = new System.Windows.Forms.SaveFileDialog();
            this.btnConvert = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBox1
            // 
            this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox1.Location = new System.Drawing.Point(191, 44);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(597, 394);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // listBox1
            // 
            this.listBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.listBox1.FormattingEnabled = true;
            this.listBox1.IntegralHeight = false;
            this.listBox1.Location = new System.Drawing.Point(12, 44);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(173, 394);
            this.listBox1.TabIndex = 1;
            this.listBox1.SelectedValueChanged += new System.EventHandler(this.listBox1_SelectedValueChanged);
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(12, 12);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 2;
            this.btnOpen.Text = "Open RX*";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.button1_Click);
            // 
            // dlgOpen
            // 
            this.dlgOpen.DefaultExt = "ReX data files (*.rxd)|*.rxd";
            this.dlgOpen.Filter = "ReX structure files (*.rxc)|*.rxc|ReX data files (*.rxd)|*.rxd";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(272, 12);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 3;
            this.button2.Text = "Save XML";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // dlgSaveXML
            // 
            this.dlgSaveXML.DefaultExt = "Binary XML (*.xml)|*.xml";
            this.dlgSaveXML.Filter = "Binary XML (*.xml)|*.xml";
            // 
            // btnOpenXML
            // 
            this.btnOpenXML.Location = new System.Drawing.Point(191, 12);
            this.btnOpenXML.Name = "btnOpenXML";
            this.btnOpenXML.Size = new System.Drawing.Size(75, 23);
            this.btnOpenXML.TabIndex = 4;
            this.btnOpenXML.Text = "Open XML";
            this.btnOpenXML.UseVisualStyleBackColor = true;
            this.btnOpenXML.Click += new System.EventHandler(this.btnOpenXML_Click);
            // 
            // dlgOpenXml
            // 
            this.dlgOpenXml.DefaultExt = "ReX configuration files (*.xml)|*.xml";
            this.dlgOpenXml.Filter = "ReX configuration files (*.xml)|*.xml";
            // 
            // dlgSaveRXC
            // 
            this.dlgSaveRXC.Filter = "*.rxc|*.rxc";
            // 
            // btnConvert
            // 
            this.btnConvert.Location = new System.Drawing.Point(713, 12);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(75, 23);
            this.btnConvert.TabIndex = 5;
            this.btnConvert.Text = "Convert";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // rxdDemoMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnConvert);
            this.Controls.Add(this.btnOpenXML);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.richTextBox1);
            this.Name = "rxdDemoMain";
            this.Text = "RXD demo app";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.OpenFileDialog dlgOpen;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.SaveFileDialog dlgSaveXML;
        private System.Windows.Forms.Button btnOpenXML;
        private System.Windows.Forms.OpenFileDialog dlgOpenXml;
        private System.Windows.Forms.SaveFileDialog dlgSaveRXC;
        private System.Windows.Forms.Button btnConvert;
    }
}

