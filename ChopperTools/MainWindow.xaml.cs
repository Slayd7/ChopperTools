using System.Diagnostics;
using System.Windows;
using System.IO;
using ChopperTools.Models;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using Microsoft.Win32;

namespace ChopperTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<BenchTestObj> benchList;
        BenchTestObj heldObj;

        private static readonly string cwd = Directory.GetCurrentDirectory();

        Process pOverlay;

        List<string> BenchNames
        {
            get
            {
                List<string> ret = new List<string>();
                foreach(BenchTestObj obj in benchList)
                {
                    string[] path = obj.path.Split('/', '\\');
                    string p = "";
                    if(path.Length > 0) p = path[path.Length - 1];
                    string s;
                    switch (obj.testType)
                    {
                        case TestType.AUTOHOTKEY:
                            s = "AHK:  " + p + " (" + obj.iters + ")";
                            ret.Add(s);
                            break;
                        case TestType.AUTOITV3:
                            s = "AU3:  " + p + " (" + obj.iters + ")";
                            ret.Add(s);
                            break;
                        case TestType.MARK:
                            if(obj.markName != null) p = obj.markName.Value.ToString();
                            s = "3DM: " + p + " (" + obj.iters + ")";
                            ret.Add(s);
                            break;
                        case TestType.CINEBENCH:
                            s = "CB:  " + (obj.cinebenchMC ? "Multicore" : "Singlecore") + " (" + obj.iters + ")";
                            ret.Add(s);
                            break;
                    }
                }
                return ret;
            }
        }
        public MainWindow()
        {
            benchList = new List<BenchTestObj>();
            InitializeComponent();
            benchListBox.ItemsSource = BenchNames;
            box_3dmark.ItemsSource = Enum.GetValues(typeof(BenchName));
        }
        private void MarkButton_Click(object sender, RoutedEventArgs e)
        {
            Benchmarking.Run3DMarkBench((BenchName)box_3dmark.SelectedItem, int.Parse(baseBenchIters.Text));            
        }
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            string n = ((Button)sender).Name;
            switch (n)
            {
                case "markAdd":
                    benchList.Add(new BenchTestObj(TestType.MARK, (BenchName)box_3dmark.SelectedItem, int.Parse(baseBenchIters.Text)));
                    break;
                case "ahkAdd":
                    if (!File.Exists(ahkPath.GetLineText(0)))
                    {
                        MessageBox.Show("Invalid path: " + ahkPath.GetLineText(0));
                        return;
                    }
                    benchList.Add(new BenchTestObj(TestType.AUTOHOTKEY, ahkPath.GetLineText(0), int.Parse(ahkBenchIters.Text)));
                    break;
                case "au3Add":
                    if (!File.Exists(au3Path.GetLineText(0)))
                    {
                        MessageBox.Show("Invalid path: " + au3Path.GetLineText(0));
                        return;
                    }
                    benchList.Add(new BenchTestObj(TestType.AUTOITV3, au3Path.GetLineText(0), int.Parse(au3BenchIters.Text)));
                    break;
                case "cbAdd":
                    bool mc = cbmc_checkbox.IsChecked ?? false;
                    benchList.Add(new BenchTestObj(TestType.CINEBENCH, mc, int.Parse(cbIters.Text)));
                    break;
            }
            benchListBox.ItemsSource = BenchNames;
        }        
        private void PowerOverlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (pOverlay == null)
            {
                pOverlay = Process.Start(cwd + "\\tools\\PowerOverlay\\PowerOverlay.exe");
            }
            else
            {
                pOverlay.Kill();
                pOverlay = null;
            }
        }
        private void CBButton_Click(object sender, RoutedEventArgs e)
        {
            bool mc = cbmc_checkbox.IsChecked ?? false;
            int iters = int.Parse(cbIters.Text);
            Benchmarking.RunCinebench(mc, iters);
        }
        private void AHK_Button_Click(object sender, RoutedEventArgs e)
        {
            Benchmarking.RunAhkScript(ahkPath.GetLineText(0), int.Parse(ahkBenchIters.Text));
        }
        private void AHKBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".ahk",
                Filter = "AutoHotkey Script (.ahk)|*.ahk"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                ahkPath.Text = filename;
            }
        }
        private void NumericPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Console.WriteLine("text: [ " + e.Text + " ]");
            e.Handled = !IsTextAllowed(e.Text);
        }
        private static readonly Regex _regex = new Regex("([^0-9]|\\s)"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void AU3_Button_Click(object sender, RoutedEventArgs e)
        {
            Benchmarking.RunAu3Script(au3Path.GetLineText(0), int.Parse(au3BenchIters.Text));
        }
        private void AU3BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".au3",
                Filter = "AutoIt v3 Script (.au3)|*.au3"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                au3Path.Text = filename;
            }
        }
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            object obj = GetDataFromListBox(lb, e.GetPosition(lb));

            if (obj != null)
            {
                heldObj = benchList[lb.Items.IndexOf(obj)];
                lb.SelectedItem = heldObj;
                lb.SelectedIndex = lb.Items.IndexOf(obj);
                DragDrop.DoDragDrop(lb, obj, DragDropEffects.Move);
            }
        }
        private static object GetDataFromListBox(ListBox source, Point point)
        {
            if (source.InputHitTest(point) is UIElement element)
            {
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    data = source.ItemContainerGenerator.ItemFromContainer(element);

                    if (data == DependencyProperty.UnsetValue)
                    {
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    }

                    if (element == source)
                    {
                        return null;
                    }
                }

                if (data != DependencyProperty.UnsetValue)
                {
                    return data;
                }
            }

            return null;
        }
        private void ListBox_Drop(object sender, DragEventArgs e)
        {            
            ListBox lb = (ListBox)sender;
            object obj = GetDataFromListBox(lb, e.GetPosition(lb));
            BenchTestObj b = benchList[lb.Items.IndexOf(obj)];
            if (heldObj == null || b == null || heldObj == b) return;
            int pos = benchList.IndexOf(b);
            benchList.Remove(heldObj);
            benchList.Insert(pos, heldObj);

            benchListBox.ItemsSource = BenchNames;
            heldObj = null;
        }
        private void Iters_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            int idx = t.CaretIndex;
            t.Text = Regex.Replace(t.Text, "\\s", "");
            t.CaretIndex = idx;
            if (t.Text.Length == 0) t.Text = "1";
        }
        private void RunPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (benchList.Count == 0)
            {
                MessageBox.Show("Cannot run an empty playlist!");
                return;
            }
            Benchmarking.RunPlaylist(benchList, playlistProgressBar);
        }
        private void RemoveTest_Click(object sender, RoutedEventArgs e)
        {
            if (benchListBox.SelectedItem != null)
            {
                int pos = benchListBox.SelectedIndex;
                benchList.Remove(benchList[pos]);
                benchListBox.ItemsSource = BenchNames;
            }
        }
        private void SavePlaylist_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.FileName = "Playlist";
            saveDlg.DefaultExt = ".xml";
            saveDlg.Filter = "XML File (.xml)|*.xml";
            if (saveDlg.ShowDialog() == true) Playlist.SavePlaylist(benchList, saveDlg.FileName);

        }
        private void LoadPlaylist_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = "XML File (.xml)|*.xml"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                Playlist.LoadPlaylist(filename, out benchList);
                benchListBox.ItemsSource = BenchNames;
            }
        }        
        private void InstallPrereqs_Click(object sender, RoutedEventArgs e)
        {
            HelperFunctions.Installers.InstallPrereqs();
        }
    }
}
