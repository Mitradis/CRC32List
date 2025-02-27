using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace CRC32List
{
    public partial class FormMain : Form
    {
        List<string> dropList1 = new List<string>();
        List<string> dropList2 = new List<string>();
        string lastPath1 = null;
        string lastPath2 = null;
        bool md5 = false;

        public FormMain()
        {
            InitializeComponent();
        }

        void button1_Click(object sender, EventArgs e)
        {
            addFiles(true);
        }

        void button2_Click(object sender, EventArgs e)
        {
            addFiles(false);
        }

        void button3_Click(object sender, EventArgs e)
        {
            md5 = !md5;
            if (!backgroundWorker1.IsBusy && !backgroundWorker2.IsBusy)
            {
                Text = md5 ? "MD5 List" : "CRC32 List";
            }
            button3.ForeColor = md5 ? Color.Firebrick : Color.Gray;
        }

        void addFiles(bool button)
        {
            if ((button ? lastPath1 : lastPath2) != null)
            {
                (button ? openFileDialog1 : openFileDialog2).InitialDirectory = button ? lastPath1 : lastPath2;
            }
            DialogResult result = button ? openFileDialog1.ShowDialog() : openFileDialog2.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (button)
                {
                    lastPath1 = Path.GetDirectoryName(openFileDialog1.FileNames[0]);
                }
                else
                {
                    lastPath2 = Path.GetDirectoryName(openFileDialog2.FileNames[0]);
                }
                (button ? dropList1 : dropList2).AddRange(button ? openFileDialog1.FileNames : openFileDialog2.FileNames);
                prepareState(button);
            }
        }

        void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) && !backgroundWorker1.IsBusy)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        void listView2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) && !backgroundWorker2.IsBusy)
            {
                e.Effect = DragDropEffects.All;
            }
        }

        void listView1_DragDrop(object sender, DragEventArgs e)
        {
            dropList1.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            prepareState(true);
        }

        void listView2_DragDrop(object sender, DragEventArgs e)
        {
            dropList2.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            prepareState(false);
        }

        void prepareState(bool list)
        {
            (list ? button1 : button2).Enabled = false;
            (list ? CRC1 : CRC2).Text = md5 ? "MD5" : "CRC32";
            (list ? dropList1 : dropList2).Sort();
            (list ? dropList1 : dropList2).Add(list ? "listView1" : "listView2");
            (list ? listView1 : listView2).Items.Clear();
            foreach (ListViewItem item in (list ? listView2 : listView1).Items)
            {
                item.BackColor = SystemColors.Window;
            }
            (list ? backgroundWorker1 : backgroundWorker2).RunWorkerAsync(list ? dropList1 : dropList2);
        }

        void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = getItems((List<string>)e.Argument, worker);
        }

        void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            e.Result = getItems((List<string>)e.Argument, worker);
        }

        List<ListViewItem> getItems(List<string> list, BackgroundWorker worker)
        {
            List<ListViewItem> outList = new List<ListViewItem>();
            int count = list.Count - 1;
            outList.Add(new ListViewItem(list[count]));
            for (int i = 0; i < count; i++)
            {
                if (File.Exists(list[i]))
                {
                    worker.ReportProgress(i);
                    ListViewItem item = new ListViewItem(new string[] { md5 ? getMD5(list[i]) : getCRC(list[i]), Path.GetFileName(list[i]) });
                    outList.Add(item);
                }
            }
            return outList;
        }

        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressState(true, e.ProgressPercentage);
        }

        void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressState(false, e.ProgressPercentage);
        }

        void progressState(bool list, int index)
        {
            Text = (md5 ? "MD5 List: " : "CRC32 List: ") + Path.GetFileName((list ? dropList1 : dropList2)[index]);
        }

        void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            completedState((List<ListViewItem>)e.Result);
        }

        void backgroundWorker2_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            completedState((List<ListViewItem>)e.Result);
        }

        void completedState(List<ListViewItem> result)
        {
            bool list = result[0].Text == "listView1";
            result.RemoveAt(0);
            (list ? listView1 : listView2).Items.AddRange(result.ToArray());
            if (listView1.Items.Count > 0 && listView2.Items.Count > 0)
            {
                compareLists();
            }
            (list ? filesName1 : filesName2).Text = (list ? listView1 : listView2).Items.Count > 0 ? "Файлы" + " (" + (list ? listView1 : listView2).Items.Count + "):" : "Файлы";
            if (list ? !backgroundWorker2.IsBusy : !backgroundWorker1.IsBusy)
            {
                Text = md5 ? "MD5 List" : "CRC32 List";
            }
            (list ? dropList1 : dropList2).Clear();
            (list ? button1 : button2).Enabled = true;
        }

        void compareLists()
        {
            int count1 = listView1.Items.Count;
            for (int i = 0; i < count1; i++)
            {
                int count2 = listView2.Items.Count;
                for (int j = 0; j < count2; j++)
                {
                    if (String.Equals(listView1.Items[i].SubItems[1].Text, listView2.Items[j].SubItems[1].Text, StringComparison.OrdinalIgnoreCase))
                    {
                        if (String.Equals(listView1.Items[i].Text, listView2.Items[j].Text, StringComparison.OrdinalIgnoreCase))
                        {
                            listView1.Items[i].BackColor = Color.PaleGreen;
                            listView2.Items[j].BackColor = Color.PaleGreen;
                        }
                        else
                        {
                            listView1.Items[i].BackColor = Color.MistyRose;
                            listView2.Items[j].BackColor = Color.MistyRose;
                        }
                    }
                    else if (String.Equals(listView1.Items[i].Text, listView2.Items[j].Text, StringComparison.OrdinalIgnoreCase))
                    {
                        if (listView1.Items[i].BackColor != Color.PaleGreen)
                        {
                            listView1.Items[i].BackColor = Color.LemonChiffon;
                        }
                        if (listView2.Items[j].BackColor != Color.PaleGreen)
                        {
                            listView2.Items[j].BackColor = Color.LemonChiffon;
                        }
                    }
                }
            }
        }

        void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (listView1.Focused && listView1.SelectedItems.Count > 0)
                {
                    toMemory(listView1);
                }
                else if (listView2.Focused && listView2.SelectedItems.Count > 0)
                {
                    toMemory(listView2);
                }
            }
        }

        void toMemory(ListView list)
        {
            List<string> result = new List<string>();
            int count = list.SelectedItems.Count;
            for (int i = 0; i < count; i++)
            {
                result.Add(list.SelectedItems[i].Text + "\t" + list.SelectedItems[i].SubItems[1].Text);
            }
            Clipboard.SetText(String.Join(Environment.NewLine, result));
            result.Clear();
        }

        string getMD5(string file)
        {
            MD5 md5 = MD5.Create();
            Stream st = File.OpenRead(file);
            byte[] hash = md5.ComputeHash(st);
            md5.Clear();
            st.Close();
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();

        }

        string getCRC(string file)
        {
            string line = "";
            try
            {
                FileStream streamFile = File.OpenRead(file);
                line = String.Format("{0:X}", calculateCRC(streamFile));
                streamFile.Close();
            }
            catch
            {
                MessageBox.Show("Нет доступа к: " + file);
            }
            while (line.Length != 8)
            {
                line = "0" + line;
            }
            return line;
        }

        uint calculateCRC(Stream stream)
        {
            const int buffer_size = 1024;
            const uint POLYNOMIAL = 0xEDB88320;
            uint result = 0xFFFFFFFF;
            uint Crc32;
            byte[] buffer = new byte[buffer_size];
            uint[] table = new uint[256];
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
                    table[i] = Crc32;
                }
                int count = stream.Read(buffer, 0, buffer_size);
                while (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        result = ((result) >> 8) ^ table[(buffer[i]) ^ ((result) & 0x000000FF)];
                    }
                    count = stream.Read(buffer, 0, buffer_size);
                }
            }
            buffer = null;
            table = null;
            stream.Close();
            return ~result;
        }
    }
}
