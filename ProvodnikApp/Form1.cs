using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProvodnikApp
{
    public partial class Form1 : Form
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 0x0000000C;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private string _folderPath = null;
        private string _copiedPath = null;

        public Form1()
        {
            InitializeComponent();

            this.listViewFiles.Columns.Add("Имя", 250, HorizontalAlignment.Left);
            this.listViewFiles.Columns.Add("Тип", 100, HorizontalAlignment.Left);
            this.listViewFiles.Columns.Add("Размер", 100, HorizontalAlignment.Right);
            this.listViewFiles.Columns.Add("Дата изменения", 150, HorizontalAlignment.Left);

            ImageList imageList = new ImageList();
            imageList.Images.Add("folder", Properties.Resources.FolderIcon);
            imageList.Images.Add("file", Properties.Resources.FileIcon);
            listViewFiles.LargeImageList = imageList;
            listViewFiles.SmallImageList = imageList;

            LoadFolderContent();

        }

        private void LoadFolderContent()
        {
            this.Text = $"Содержимое папки: {_folderPath}";

            listViewFiles.Items.Clear();

            try
            {
                if (_folderPath == null)
                    return;
                textBoxDerictory.Text = _folderPath;

                var directories = Directory.GetDirectories(_folderPath);
                var files = Directory.GetFiles(_folderPath);

                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var item = new ListViewItem(dirInfo.Name)
                    {
                        ImageIndex = 0, 
                        Tag = dir,       
                    };

                    item.SubItems.Add("Папка"); // Тип
                    item.SubItems.Add("");      // Размер для папки оставить пустым
                    item.SubItems.Add(dirInfo.LastWriteTime.ToString()); // Дата изменения

                    listViewFiles.Items.Add(item);
                }

                // Добавляем файлы в ListView
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var item = new ListViewItem(fileInfo.Name)
                    {
                        ImageIndex = 1,
                        Tag = file       
                    };
                    item.SubItems.Add(fileInfo.Extension + " файл"); // Тип файла
                    item.SubItems.Add(fileInfo.Length.ToString() + " байт"); // Размер
                    item.SubItems.Add(fileInfo.LastWriteTime.ToString()); // Дата изменения
                    listViewFiles.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке содержимого папки: {ex.Message}");
                var parentDirectory = Directory.GetParent(_folderPath);
                if (parentDirectory != null)
                {
                    _folderPath = parentDirectory.FullName;
                    LoadFolderContent();
                }
            }
        }

        // Двойное нажатие на папку\каталог
        private void listViewFiles_DoubleClick(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var path = selectedItem.Tag.ToString();

                if (Directory.Exists(path))
                {
                    _folderPath = path;
                    LoadFolderContent();
                }
                else if (File.Exists(path))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось открыть файл !\n{ex.Message}");
                    }
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            var parentDirectory = Directory.GetParent(_folderPath);
            if (parentDirectory != null)
            {
                _folderPath = parentDirectory.FullName;
                LoadFolderContent();
            }
            else
            {
                MessageBox.Show("Вы уже находитесь в корневой директории.");
            }
        }

        // Открыть
        private void OpenItem_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var path = selectedItem.Tag as string;

                if (File.Exists(path) || Directory.Exists(path))
                {
                    Process.Start("explorer", path);
                }
                else
                {
                    MessageBox.Show("Файл или папка не найдены.");
                }
            }
        }

        // Обновить
        private void RefreshItem_Click(object sender, EventArgs e)
        {
            LoadFolderContent();
        }

        // Удалить
        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var path = selectedItem.Tag as string;

                var result = MessageBox.Show("Вы уверены, что хотите удалить этот элемент?", "Удаление", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                        }
                        LoadFolderContent();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                    }
                }
            }
        }

        // Переименовать
        private void RenameItem_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var oldPath = selectedItem.Tag as string;

                if (oldPath != null)
                {
                    string newName = Microsoft.VisualBasic.Interaction.InputBox("Введите новое имя:", "Переименовать", selectedItem.Text);
                    if (!string.IsNullOrEmpty(newName))
                    {
                        string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);

                        try
                        {
                            if (File.Exists(oldPath))
                            {
                                File.Move(oldPath, newPath);
                            }
                            else if (Directory.Exists(oldPath))
                            {
                                Directory.Move(oldPath, newPath);
                            }
                            LoadFolderContent();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при переименовании: {ex.Message}");
                        }
                    }
                }
            }
        }

        // Копировать
        private void CopyItem_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                _copiedPath = selectedItem.Tag as string;
                MessageBox.Show("Элемент скопирован.");
            }
        }

        // Вставить
        private void PasteItem_Click(object sender, EventArgs e)
        {
            if (_copiedPath == null)
            {
                MessageBox.Show("Буфер обмена пуст.");
                return;
            }

            var destinationPath = Path.Combine(_folderPath, Path.GetFileName(_copiedPath));

            try
            {
                if (Directory.Exists(_copiedPath))
                {
                    CopyDirectory(_copiedPath, destinationPath);
                }
                else if (File.Exists(_copiedPath))
                {
                    File.Copy(_copiedPath, destinationPath, overwrite: true);
                }
                LoadFolderContent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при вставке: {ex.Message}");
            }
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string destDir = Path.Combine(destinationDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDir);
            }
        }

        // Свойства
        private void PropertiesItem_Click(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewFiles.SelectedItems[0];
                var path = selectedItem.Tag as string;

                if (File.Exists(path) || Directory.Exists(path))
                {
                    ShowFileProperties(path);
                }
            }
        }

        private void ShowFileProperties(string path)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = path;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;

            if (!ShellExecuteEx(ref info))
            {
                MessageBox.Show("Не удалось открыть окно свойств.");
            }
        }

        

        private void textBoxDerictory_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _folderPath = textBoxDerictory.Text;
                LoadFolderContent();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            _folderPath = @"C:\Users";
            LoadFolderContent();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _folderPath = @"C:\Program Files";
            LoadFolderContent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _folderPath = @"C:\Windows";
            LoadFolderContent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            _folderPath = @"C:\Windows\System";
            LoadFolderContent();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _folderPath = @"C:\Program Files (x86)";
            LoadFolderContent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", $"/select, \"{_folderPath}\"");
        }
    }
}
