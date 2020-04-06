using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArkhamCityDisplay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker updateWorker;
        private const int ROW_HEIGHT = 40;

        public MainWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
        }

        private void uncompressSaveFile(string saveFileId, string outputPrefix)
        {
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.Arguments = "-d \"" + saveFileId + "\" " + outputPrefix;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.FileName = "batmancompressor/batman.exe";
            pProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
            pProcess.Start();
            string output = pProcess.StandardOutput.ReadToEnd(); //The output result
            //Debug.Text = output;
            pProcess.WaitForExit();
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
            updateWorker.CancelAsync();
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            updateWorker = new BackgroundWorker();
            updateWorker.WorkerSupportsCancellation = true;
            updateWorker.WorkerReportsProgress = true;
            updateWorker.DoWork += backgroundWorkerOnDoWork;
            updateWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;

            updateWorker.RunWorkerAsync();

            StopButton.IsEnabled = true;
            StartButton.IsEnabled = false;
        }

        private void backgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                worker.ReportProgress(0, "Dummy");
                Thread.Sleep(1000);
            }
        }

        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                string routepath = "Arkham City 100% Route - Route.tsv";
                string prisonerpath = "Arkham City 100% Route - Political Prisoners.tsv";
                string saveFile = SavePathBox.Text;
                if (string.IsNullOrEmpty(saveFile))
                {
                    throw new Exception("Save file path is not valid");
                }
                string decompressedSaveFilePrefix = "batmancompressor/decompressed.sgd";
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                string[] files = System.IO.Directory.GetFiles("batmancompressor");
                foreach (string file in files) 
                {
                    if (file.Contains("decompressed.sgd"))
                    {
                        System.IO.File.Delete(file);
                    }
                }
                uncompressSaveFile(saveFile, decompressedSaveFilePrefix);
                updateWindow(System.IO.File.ReadAllLines(routepath), System.IO.File.ReadAllLines(prisonerpath), decompressedSaveFilePrefix);
                stopWatch.Stop();
                long time = stopWatch.ElapsedMilliseconds;
                Debug.Text = time.ToString();
            }
            catch (Exception ex)
            {
                StopButton.IsEnabled = false;
                StartButton.IsEnabled = true;
                updateWorker.CancelAsync();
                System.Windows.MessageBox.Show("Error: " + ex.Message);
            }
                
        }

        private void updateWindow(string[] routeLines, string[] prisonerLines, string saveFile)
        {
            updateRouteWindow(routeLines, saveFile);
            updatePrisonerWindow(prisonerLines, saveFile);
        }

        private void updateRouteWindow(string[] routeLines, string saveFile)
        {
            DisplayGrid.Children.Clear();
            DisplayGrid.RowDefinitions.Clear();
            DisplayGrid.ColumnDefinitions.Clear();

            DisplayGrid.ShowGridLines = true;
            DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(210) });
            DisplayGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40) });

            //Collectibles are in the 2nd block;
            string collectiblesBlock = System.IO.File.ReadAllText(saveFile + "2");
            int saveFileSuffix = 3;

            List<String> storyBlocks = new List<String>();
            string filename = saveFile + saveFileSuffix;
            while (File.Exists(filename))
            {
                storyBlocks.Add(System.IO.File.ReadAllText(filename));
                saveFileSuffix++;
                filename = saveFile + saveFileSuffix;
            }

            int lineCount = 1;
            int firstNotDone = -1;
            for (int index = 0; index < routeLines.Length; index++)
            {
                if (index == 0)
                {
                    continue;
                }
                string line = routeLines[index];

                string[] lineComponents = line.Split('\t');
                DisplayGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(ROW_HEIGHT) });
                TextBlock txt0 = new TextBlock();
                txt0.Text = lineComponents[0];
                txt0.TextWrapping = TextWrapping.Wrap;
                Grid.SetColumn(txt0, 0);
                Grid.SetRow(txt0, lineCount - 1);
                DisplayGrid.Children.Add(txt0);

                string saveFileKey = lineComponents[2].Trim();
                string alternateSaveFileKey = lineComponents[3].Trim();
                bool markedDone = false;
                if (!String.IsNullOrEmpty(saveFileKey) && hasMatch(saveFileKey, alternateSaveFileKey, collectiblesBlock))
                {
                    TextBlock txt1 = new TextBlock();
                    txt1.Text = "Done";
                    Grid.SetColumn(txt1, 1);
                    Grid.SetRow(txt1, lineCount - 1);
                    DisplayGrid.Children.Add(txt1);
                    markedDone = true;
                }
                else
                {
                    foreach (string storyBlock in storyBlocks)
                    {
                        if (!String.IsNullOrEmpty(saveFileKey) && hasMatch(saveFileKey, alternateSaveFileKey, storyBlock))
                        {
                            TextBlock txt1 = new TextBlock();
                            txt1.Text = "Done";
                            Grid.SetColumn(txt1, 1);
                            Grid.SetRow(txt1, lineCount - 1);
                            DisplayGrid.Children.Add(txt1);
                            markedDone = true;
                            break;
                        }
                    }
                }

                if (firstNotDone == -1 && !markedDone)
                {
                    firstNotDone = lineCount;
                }
                lineCount++;
            }
            if (firstNotDone > -1)
            {
                double numInViewport = GridScroll.Height / ROW_HEIGHT;
                int scrollHeight = (firstNotDone - 4) * (ROW_HEIGHT);
                GridScroll.ScrollToVerticalOffset(scrollHeight);
            }
        }

        private void updatePrisonerWindow(string[] prisonerLines, string saveFile)
        {
            PrisonerGrid.Children.Clear();
            PrisonerGrid.RowDefinitions.Clear();
            PrisonerGrid.ColumnDefinitions.Clear();

            PrisonerGrid.ShowGridLines = true;
            PrisonerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(250) });
            int saveFileSuffix = 3;

            List<String> storyBlocks = new List<String>();
            string filename = saveFile + saveFileSuffix;
            while (File.Exists(filename))
            {
                storyBlocks.Add(System.IO.File.ReadAllText(filename));
                saveFileSuffix++;
                filename = saveFile + saveFileSuffix;
            }

            int lineCount = 1;
            for (int index = 0; index < prisonerLines.Length; index++)
            {
                if (index == 0)
                {
                    continue;
                }
                string line = prisonerLines[index];
                string[] lineComponents = line.Split('\t');
                string activeId = lineComponents[2].Trim();
                string savedId = lineComponents[3].Trim();

                foreach (string storyBlock in storyBlocks)
                {
                    if (hasMatch(activeId, null, storyBlock) && !hasMatch(savedId, null, storyBlock))
                    {
                        PrisonerGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(ROW_HEIGHT/2) });
                        TextBlock txt0 = new TextBlock();
                        txt0.Text = lineComponents[0];
                        txt0.TextWrapping = TextWrapping.Wrap;
                        Grid.SetColumn(txt0, 0);
                        Grid.SetRow(txt0, lineCount - 1);
                        PrisonerGrid.Children.Add(txt0);
                        lineCount++;
                    }
                }
            }
        }

        private Boolean hasMatch(string saveFileKey, string alternateSaveFileKey, string block)
        {
            Regex rx = new Regex(@"\b" + saveFileKey + @"\b");
            MatchCollection collectibleMatches = rx.Matches(block);
            bool firstMatch = collectibleMatches.Count > 0;
            if (collectibleMatches.Count <= 0 && !String.IsNullOrEmpty(alternateSaveFileKey))
            {
                Regex rx2 = new Regex(@"\b" + alternateSaveFileKey + @"\b");
                MatchCollection alternateCollectibleMatches = rx2.Matches(block);
                return alternateCollectibleMatches.Count > 0;
            }
            return firstMatch;
        }
    }
}
