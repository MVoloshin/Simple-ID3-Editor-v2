using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace id3g_v2
{
    // Token: 0x02000008 RID: 8
    public partial class Form1 : Form
    {
        // Token: 0x0600002B RID: 43 RVA: 0x000059C0 File Offset: 0x00003BC0
        public void initControls()
        {
            int x = 10;
            Dictionary<Label, string> tags = new Dictionary<Label, string>();
            for (int i = 0; i < 6; i++)
            {
                Form1.buttons[i] = new Button();
                Form1.buttons[i].Location = new Point(x, 10);
                Form1.buttons[i].Size = new Size(60, 20);
                Form1.buttons[i].Text = Program.bt[i];
                if (i > 0 && i < 4)
                {
                    Form1.buttons[i].Enabled = false;
                }
                base.Controls.Add(Form1.buttons[i]);
                x += 64;
            }
            for (int i = 0; i < 21; i++)
            {
                //Program.imageRemove(i);
            }
            x = 290;
            for (int i = 0; i < 3; i++)
            {
                Form1.imageOptions[i] = new Button();
                Form1.imageOptions[i].Location = new Point(x, 210);
                Form1.imageOptions[i].Size = new Size(30, 30);
                Form1.imageOptions[i].Text = Program.imgOptLbl[i];
                base.Controls.Add(Form1.imageOptions[i]);
                x += 35;
            }
            Form1.imageOptions[0].Click += this.openImage_Click;
            Form1.imageOptions[1].Click += this.deleteImage_Click;
            Form1.imageOptions[2].Click += this.saveImage_Click;
            Form1.descImage.Size = new Size(100, 20);
            Form1.descImage.Location = new Point(290, 180);
            Form1.imageType.Size = new Size(100, 20);
            Form1.imageType.Location = new Point(290, 150);
            Form1.imageType.DropDownStyle = ComboBoxStyle.DropDownList;
            Form1.imageType.SelectedIndexChanged += this.imagetype_SelectedIndexChanged;
            for (int i = 0; i < Program.imageTypes.Length; i++)
            {
                Form1.imageType.Items.Add(Program.imageTypes[i]);
            }
            Form1.imageType.SelectedIndex = 3;
            base.Controls.Add(Form1.imageType);
            base.Controls.Add(Form1.descImage);
            Form1.buttons[0].Click += this.open_Click;
            Form1.buttons[1].Click += this.save_Click;
            Form1.buttons[2].Click += this.more_Click;
            Form1.buttons[4].Click += this.options_Click;
            Form1.descImage.TextChanged += this.descImage_TextChanged;
            for (int i = 0; i < Program.kol; i++)
            {
                Form1.labels[i] = new Label();
                Form1.labels[i].Name = Program.tags[i];
                Form1.values[i] = new TextBox();
                Form1.labels[i].Size = new Size(80, 20);
                if (i != 5)
                {
                    Form1.values[i].Size = new Size(180, 20);
                }
                else
                {
                    Form1.values[i].Size = new Size(0, 0);
                }
                Form1.labels[i].Text = Program.typeDescription(Program.tags[i]);
                Form1.values[i].Text = "";
                Form1.values[i].Enabled = false;
                base.Controls.Add(Form1.labels[i]);
                base.Controls.Add(Form1.values[i]);
            }
            Form1.genreBox.Size = new Size(180, 20);
            Form1.genreBox.Enabled = false;
            //Program.switchImageControls(false, 0);
            base.Controls.Add(Form1.genreBox);
            this.showMore(0, 8);
        }

        // Token: 0x0600002C RID: 44 RVA: 0x00005DDF File Offset: 0x00003FDF
        public Form1()
        {
            this.InitializeComponent();
            this.initControls();
            Program.save.Filter = "MPEG-1 Audio Layer 3(*.MP3)|*.mp3";
            Program.open.Filter = "MPEG-1 Audio Layer 3(*.MP3)|*.mp3";
        }

        // Token: 0x0600002D RID: 45 RVA: 0x00005E20 File Offset: 0x00004020
        public void showMore(int a, int b)
        {
            int shift = 40;
            for (int i = 0; i < Program.kol; i++)
            {
                Form1.labels[i].Hide();
                Form1.values[i].Hide();
                Form1.genreBox.Location = new Point(90, 190);
            }
            for (int i = a; i < b; i++)
            {
                Form1.labels[i].Location = new Point(10, shift + 2);
                Form1.values[i].Location = new Point(90, shift);
                Form1.labels[i].Show();
                Form1.values[i].Show();
                shift += 30;
            }
        }

        // Token: 0x0600002E RID: 46 RVA: 0x00005ED8 File Offset: 0x000040D8
        private void open_Click(object sender, EventArgs e)
        {
            if (Program.open.ShowDialog() == DialogResult.OK)
            {
                file = new myFile();
                if (file.openFile(Program.open.FileName, FileMode.Open))
                {
                    Form2 f = new Form2(file.detectID3());
                }
            }
        }

        // Token: 0x0600002F RID: 47 RVA: 0x00005F24 File Offset: 0x00004124
        private void descImage_TextChanged(object sender, EventArgs e)
        {
            Program.pics[Form1.imageType.SelectedIndex].text = Form1.descImage.Text;
        }

        // Token: 0x06000030 RID: 48 RVA: 0x00005F4C File Offset: 0x0000414C
        private void options_Click(object sender, EventArgs e)
        {
            Form f = new Options();
            f.ShowDialog();
        }

        // Token: 0x06000031 RID: 49 RVA: 0x00005F67 File Offset: 0x00004167
        private void imagetype_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Program.imageSelect();
        }

        // Token: 0x06000032 RID: 50 RVA: 0x00005F70 File Offset: 0x00004170
        private void save_Click(object sender, EventArgs e)
        {
            if (Program.save.ShowDialog() == DialogResult.OK)
            {
                    //if (Form1.values[6].Enabled)
                    //{
                    //    Program.writeID3v2();
                    //}
                    //else
                    file = new myFile();
                     if (file.openFile(Program.save.FileName, FileMode.Create))
                    {
                        myID3v1tag.writeID3v1();
                        file.writeAllBytes(myID3v1tag.getOutput());
                     }
                        
                }
        }

        // Token: 0x06000033 RID: 51 RVA: 0x00005FD8 File Offset: 0x000041D8
        private void more_Click(object sender, EventArgs e)
        {
            if (Form1.buttons[2].Text == ">>")
            {
                Form1.buttons[2].Text = "<<";
                Form1.genreBox.Hide();
                this.showMore(8, 16);
            }
            else
            {
                Form1.buttons[2].Text = ">>";
                Form1.genreBox.Show();
                this.showMore(0, 8);
            }
        }

        // Token: 0x06000034 RID: 52 RVA: 0x00006058 File Offset: 0x00004258
        private void openImage_Click(object sender, EventArgs e)
        {
            if (Form1.imageType.SelectedIndex == 1)
            {
                Program.openimg.Filter = "Images (*.PNG)|*.png";
            }
            else
            {
                Program.openimg.Filter = "Images (*.JPG;*.JPEG;*.PNG)|*.jpg;*.jpeg;*.png";
            }
            if (Program.openimg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (Image.FromFile(Program.openimg.FileName).Width != Image.FromFile(Program.openimg.FileName).Height)
                    {
                        MessageBox.Show("Proportions will be lost", "Invalid size", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                    string ext = Program.openimg.FileName.ToLower();
                    Program.pics[Form1.imageType.SelectedIndex].img = Image.FromFile(Program.openimg.FileName);
                    if (ext.Replace("e", "").EndsWith(".jpg"))
                    {
                        Program.pics[Form1.imageType.SelectedIndex].mime = "image/jpeg";
                    }
                    else
                    {
                        Program.pics[Form1.imageType.SelectedIndex].mime = "image/png";
                    }
                    //Program.imageSelect();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось открыть изображение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }

        // Token: 0x06000035 RID: 53 RVA: 0x000061C8 File Offset: 0x000043C8
        private void deleteImage_Click(object sender, EventArgs e)
        {
           // Program.imageRemove(Form1.imageType.SelectedIndex);
           // Program.imageSelect();
        }

        // Token: 0x06000036 RID: 54 RVA: 0x000061E4 File Offset: 0x000043E4
        private void saveImage_Click(object sender, EventArgs e)
        {
            int fmt;
            if (Program.pics[Form1.imageType.SelectedIndex].mime.EndsWith("jpeg"))
            {
                Program.saveimg.Filter = "Images (*.JPG;*.JPEG)|*.jpg;*.jpeg";
                fmt = 0;
            }
            else
            {
                Program.saveimg.Filter = "Images (*.PNG)|*.png";
                fmt = 1;
            }
            if (Program.saveimg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (fmt == 0)
                    {
                        Program.pics[Form1.imageType.SelectedIndex].img.Save(Program.saveimg.FileName, ImageFormat.Jpeg);
                    }
                    else
                    {
                        Program.pics[Form1.imageType.SelectedIndex].img.Save(Program.saveimg.FileName, ImageFormat.Png);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось сохранить изображение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }

        // Token: 0x06000037 RID: 55 RVA: 0x000062F8 File Offset: 0x000044F8
        private void Form1_Paint(object sender, PaintEventArgs pe)
        {
            Graphics g = base.CreateGraphics();
            g.DrawRectangle(new Pen(Brushes.Black, 1f), 289, 39, 101, 101);
            if (Program.pics[Form1.imageType.SelectedIndex].img == null)
            {
                g.FillRectangle(Brushes.Gray, new Rectangle(290, 40, 100, 100));
                g.DrawString("No Image", new Font("Arial", 10f), new SolidBrush(Color.White), 310f, 80f);
            }
            else
            {
                g.DrawImage(Program.pics[Form1.imageType.SelectedIndex].img, 290, 40, 100, 100);
            }
        }

        public void setTextBoxValue(int index, string val)
        {
            values[index].Text = val;
        }

        public string getTextBoxValue(int index, string val)
        {
            return values[index].Text;
        }

        // Token: 0x06000038 RID: 56 RVA: 0x000063D0 File Offset: 0x000045D0
       /* protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }*/

        // Token: 0x06000039 RID: 57 RVA: 0x00006408 File Offset: 0x00004608
        private void InitializeComponent()
        {
            
            Form1.labels = new Label[Program.kol];
            Form1.values = new TextBox[Program.kol];
            Form1.buttons = new Button[6];
            Form1.genreBox = new ComboBox();
            Form1.imageType = new ComboBox();
            Form1.descImage = new TextBox();
            Form1.imageOptions = new Button[3];
            Form1.genreBox.Items.Clear();
            for (int i = 0; i < 195; i++)
            {
                Form1.genreBox.Items.Add(Program.genres[i]);
            }
            base.SuspendLayout();
            this.DoubleBuffered = true;
            base.AutoScaleDimensions = new SizeF(6f, 13f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.ClientSize = new Size(410, 280);
            base.Name = "Form1";
            this.Text = "Simple ID3 Editor";
            base.FormBorderStyle = FormBorderStyle.FixedSingle;
            base.ResumeLayout(false);
            base.MaximizeBox = false;
            base.Paint += this.Form1_Paint;
        }

        // Token: 0x0400002C RID: 44
        //private IContainer components = null;

        // Token: 0x0400002D RID: 45
        public static Label[] labels;

        // Token: 0x0400002E RID: 46
        public static TextBox[] values;

        // Token: 0x0400002F RID: 47
        public static Button[] buttons;

        // Token: 0x04000030 RID: 48
        public static TextBox descImage;

        // Token: 0x04000031 RID: 49
        public static ComboBox genreBox;

        // Token: 0x04000032 RID: 50
        public static ComboBox imageType;

        // Token: 0x04000033 RID: 51
        public static Button[] imageOptions;

        public static myFile file;

        public static ID3v1tag myID3v1tag;
        public static ID3v2tag myID3v2tag;
    }
}