using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CRC32List
{
    public partial class FormMain : Form
    {
        List<string> cacheFile = new List<string>();
        string lastFolder = null;
        int mouseWindowX = 0;
        int mouseWindowY = 0;

        public FormMain()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (lastFolder != null)
            {
                if (Directory.Exists(lastFolder))
                {
                    openFileDialog1.InitialDirectory = lastFolder;
                }
            }
            else
            {
                openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                lastFolder = Path.GetDirectoryName(openFileDialog1.FileName);
                listView2.Items.Clear();
                cacheFile.Clear();
                cacheFile.AddRange(openFileDialog1.FileNames);
                funcGet();
            }
        }

        private void funcGet()
        {
            foreach (string line in cacheFile)
            {
                if (File.Exists(line))
                {
                    string[] row = { Path.GetFileName(line), getCRC(line) };
                    var listViewItem = new ListViewItem(row);
                    listView2.Items.Add(listViewItem);
                }
            }
            filesName.Text = "Файлы (" + listView2.Items.Count.ToString() + "):";
        }

        private string getCRC(string file)
        {
            FileStream streamFile = File.OpenRead(file);
            string line = string.Format("{0:X}", calculateCRC(streamFile));
            streamFile.Close();
            while (line.Length != 8)
            {
                line = "0" + line;
            }
            return line;
        }

        private uint calculateCRC(Stream stream)
        {
            const int buffer_size = 1024;
            const uint POLYNOMIAL = 0xEDB88320;
            uint result = 0xFFFFFFFF;
            uint Crc32;
            byte[] buffer = new byte[buffer_size];
            uint[] table_CRC32 = new uint[256];
            unchecked
            {
                for (int i = 0; i < 256; i++)
                {
                    Crc32 = (uint)i;
                    for (int j = 8; j > 0; j--)
                    {
                        if ((Crc32 & 1) == 1)
                        {
                            Crc32 = (Crc32 >> 1) ^ POLYNOMIAL;
                        }
                        else
                        {
                            Crc32 >>= 1;
                        }
                    }
                    table_CRC32[i] = Crc32;
                }
                int count = stream.Read(buffer, 0, buffer_size);
                while (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        result = ((result) >> 8) ^ table_CRC32[(buffer[i]) ^ ((result) & 0x000000FF)];
                    }
                    count = stream.Read(buffer, 0, buffer_size);
                }
            }
            stream.Close();
            return ~result;
        }

        private void listView2_DragDrop(object sender, DragEventArgs e)
        {
            listView2.Items.Clear();
            cacheFile.Clear();
            cacheFile.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            funcGet();
        }

        private void listView2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mouseWindowX = e.X + 147;
            mouseWindowY = e.Y + 344;
            pictureBox1.MouseMove += pictureBox1_MouseMove;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Location = new Point(Cursor.Position.X - mouseWindowX, Cursor.Position.Y - mouseWindowY);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            pictureBox1.MouseMove -= pictureBox1_MouseMove;
        }
    }
}
