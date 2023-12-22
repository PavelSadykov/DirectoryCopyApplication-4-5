using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace DirectoryCopyApplication
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource cancellationTokenSource;
        private TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
        private ManualResetEventSlim pauseEvent = new ManualResetEventSlim(true);
        public Form1()
        {
            InitializeComponent();
        }

        private async void btnStart_Click_1(object sender, EventArgs e)
        {
            string sourceDirectory = txtSourcePath.Text;
            string destinationDirectory = txtDestinationPath.Text;

            if (string.IsNullOrEmpty(sourceDirectory) || string.IsNullOrEmpty(destinationDirectory))
            {
                MessageBox.Show("Пожалуйста, выберите директории для копирования.");
                return;
            }

            if (!Directory.Exists(sourceDirectory))
            {
                MessageBox.Show("Исходная директория не существует.");
                return;
            }

            progressBar.Minimum = 0;
            progressBar.Value = 0;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            await CopyDirectoryAsync(sourceDirectory, destinationDirectory, token);

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
            }

            MessageBox.Show("Копирование завершено!");
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken token)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string[] files = Directory.GetFiles(sourceDir);
            int totalFiles = files.Length;

            foreach (string file in files)
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(destStream, 4096, token);
                    }
                }

                // обновление прогрессБара
                UpdateProgressBar();

                if (token.IsCancellationRequested)
                {
                    return; // Остановка копирования, если запросили отмену
                }
            }

            string[] subDirectories = Directory.GetDirectories(sourceDir);
            foreach (string subDir in subDirectories)
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                await CopyDirectoryAsync(subDir, destSubDir, token);
            }
        }

        private void UpdateProgressBar()
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBar.Value = (int)(((double)progressBar.Value + 1) / progressBar.Maximum * 100);
            });
        }

        private void btnBrowseSource_Click_1(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtSourcePath.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void btnBrowseDestination_Click_1(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtDestinationPath.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }
       

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }


        private void buttonStop_Click(object sender, EventArgs e)
        {
            taskCompletionSource.TrySetResult(true); // Завершение операции копирования
            progressBar.Value = 0; // Очистка ProgressBar и текстБоксов
            txtSourcePath.Clear();
            txtDestinationPath.Clear();
            txtNumThreads.Clear();
        }

        private void button_Click(object sender, EventArgs e)
        {
            pauseEvent.Reset();
        }

        private void btnResume_Click(object sender, EventArgs e)
        {
            pauseEvent.Set();
        }
    }
}