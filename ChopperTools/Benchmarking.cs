using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using ChopperTools.Models;
using AutoHotkey.Interop;
using AutoIt;
using System.Threading;
using System.Windows.Controls;

namespace ChopperTools
{
    public class Benchmarking
    {
        private static readonly string xperfargs = "*Microsoft-Windows-Kernel-Processor-Power+*Microsoft-Windows-Shell-Core+" +
            "*Microsoft-Windows-Kernel-Power+*Microsoft-Windows-DxgKrnl+*Microsoft-Windows-Win32k+*Microsoft-Windows-Dwm-Cor" +
            "e+*Microsoft-Windows-DXGI+*Microsoft-Windows-Direct3D11+*SysConfig+0d663b46-c4bf-4c22-9cb5-2f2c79bacbbb+2013dbb" +
            "2-2f76-4b2c-950a-0c9dfac62398+*Image+2f900642-1a32-408f-b286-e6a07a1750a5+*Process+*Thread+*PageFault+*DiskIo+4" +
            "1916587-debb-4c34-b7fa-909301f128cd+41dbcb73-5a7f-476b-b844-61a49d724e3a+4eab923a-d424-4f25-9e2c-f5bc49985b78+5" +
            "602aa38-c16d-461f-86b0-d78d7cd80a24+63327924-0dcc-418c-a5f0-5faf329f14ab+*EventTrace+70930759-eda2-4551-a254-41" +
            "c7cfc9e4b3+7768062c-700d-4479-8ddd-7e7f60ccb4b4+810143b0-8898-433d-b00d-fba5d05edf6d+8ebf1282-0500-45c6-a8af-3e" +
            "ee75678a96+*FileIo+*SysConfigEx+ae78763d-362e-49dc-8d07-af87b3573d89+b3369e69-074d-4e14-a7bd-845bc1b47ee9+*Imag" +
            "eId+*EventMetadata+*Perfinfo+*Power+*WinSATAssessment";    // Holy cow that's a lot
                                                                        // Don't change this please
                                                                        // xperf args povided by Daniel Niedermeyer and Rudolph Balaz
        private static readonly string cwd = Directory.GetCurrentDirectory();
        private static readonly string path_3dm = "C:\\Program Files\\UL\\3DMark\\3DMarkCmd.exe";
        private static readonly string path_cinebench = cwd + "\\tools\\CinebenchR20\\Cinebench.exe";

        private static readonly string tat = "C:\\Program Files\\Intel Corporation\\Intel(R)TAT6\\Host\\ThermalAnalysisToolCmd.exe";
        private static Process pTat;
        private static readonly string xperf = "C:\\Program Files (x86)\\Windows Kits\\10\\Windows Performance Toolkit\\xperf.exe";
        private static Process pXperf;

        private static readonly string results = cwd + "\\results\\";
        private static string currentResults;

        static bool isRunning;

        static List<BackgroundWorker> bwList = new List<BackgroundWorker>();

        public static int progress;

        public static void RunPlaylist(List<BenchTestObj> tests, ProgressBar progBar)
        {
            if (!Directory.Exists(results))
            {
                Directory.CreateDirectory(results);
            }
            string dt = DateTime.Now.ToString("yyyy_MM_dd-HH-mm-ss");
            currentResults = results + dt + "\\";
            
            Directory.CreateDirectory(currentResults);            

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, a) => { BW_RunBench(tests, bw, a); };
            bw.RunWorkerCompleted += (o, a) => { MessageBox.Show("All tests finished"); };
            bw.ProgressChanged += (o, a) => { progBar.Value = a.ProgressPercentage; };
            bw.WorkerReportsProgress = true;
            bwList.Add(bw);
            bw.RunWorkerAsync();
        }

        public static void RunAu3Script(string path, int iters)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, a) => { BW_AU3(path, iters); };
            bwList.Add(bw);
            bw.RunWorkerAsync();
        }

        public static void RunAhkScript(string path, int iters)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, a) => { BW_AHK(path, iters); };
            bwList.Add(bw);
            bw.RunWorkerAsync();
        }

        public static void RunCinebench(bool isMulticore, int iters)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (o, a) => { BW_CB(isMulticore, iters); };
            bwList.Add(bw);
            bw.RunWorkerAsync();
        }

        public static void Run3DMarkBench(BenchName bench, int iterations)
        {
            if (!HelperFunctions.Installers.Check3DM())
            {
                MessageBox.Show("3DMark was not found on your system. Installing now.");
                HelperFunctions.Installers.Install_3DMark();
            }
            else
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (o, a) => { BW_3DM(bench, iterations); };
                bwList.Add(bw);
                bw.RunWorkerAsync();
            }
        }

        static void BW_RunBench(List<BenchTestObj> tests, BackgroundWorker bw, DoWorkEventArgs a)
        {
            int num = tests.Count;
            int i = 0;

            bw.ReportProgress(0);
            
            foreach (BenchTestObj test in tests)
            {
                if (bw.CancellationPending)
                {
                    a.Cancel = true;
                    return;
                }
                switch (test.testType)
                {
                    case TestType.AUTOHOTKEY:
                        WaitForBench();
                        RunAhkScript(test.path, test.iters);
                        break;
                    case TestType.AUTOITV3:
                        WaitForBench();
                        RunAu3Script(test.path, test.iters);
                        break;
                    case TestType.CINEBENCH:
                        WaitForBench();
                        RunCinebench(test.cinebenchMC, test.iters);
                        break;
                    case TestType.MARK:
                        WaitForBench();
                        Run3DMarkBench(test.markName ?? BenchName.FireStrike, test.iters);
                        break;
                }
                float p = (float)i / (float)num;
                int progress = (int)(p * 100);
                bw.ReportProgress(progress);
                i++;
            }
            WaitForBench();             // One final wait, to ensure that the BackgroundWorker.RunWorkerCompleted event fires after all benches are complete
            bw.ReportProgress(100);     // instead of firing as soon as it's done starting the final benchmark
            Thread.Sleep(1000);
            bw.ReportProgress(0);
        }                       

        static void BW_AU3(string path, int iters)
        {
            if (!File.Exists("C:\\Program Files (x86)\\AutoIt3\\AutoIt3.exe")) // We need to install AutoIt because, unfortunately, the AutoItX3Lib doesn't 
            {                                                                  // provide the ability to run whole scripts, only individual commands.
                AutoItX.RunWait("autoit-v3-setup.exe /S", cwd);                // I used the AutoIt to install the AutoIt...
            }                                                                  // https://i.kym-cdn.com/photos/images/newsfeed/001/534/991/18e.jpg

            for (int i = 0; i < iters; i++)
            {
                try { AutoItX.RunWait("\"C:\\Program Files (x86)\\AutoIt3\\AutoIt3.exe\" /AutoIt3ExecuteScript \"" + path + "\"", cwd); }
                catch { MessageBox.Show("au3 failed!"); }   // au3 has its own error message, so this will probably (hopefully) never run
            }
            isRunning = false;
        }
        static void BW_AHK(string path, int iters)
        {
            for (int i = 0; i < iters; i++)
            {
                var ahk = AutoHotkeyEngine.Instance;
                try { ahk.LoadFile(path); }
                catch { MessageBox.Show("ahk failed!"); }
            }
            isRunning = false;
        }

        static void BW_CB(bool isMulticore, int iters)
        {
            for (int i = 0; i < iters; i++)
            {
                Process p = Process.Start(path_cinebench, "g_acceptDisclaimer=true " + (isMulticore ? "g_CinebenchCPUXTest=true" : "g_CinebenchCPU1Test=true"));
                p.WaitForExit();
            }
            isRunning = false;
        }

        static void BW_3DM(BenchName name, int iters)
        {
            string args = "";
            switch (name)
            {
                case BenchName.FireStrike:
                    args = "--definition=firestrike.3dmdef";
                    break;
                case BenchName.SkyDiver:
                    args = "--definition=skydiver.3dmdef";
                    break;
                case BenchName.TimeSpy:
                    args = "--definition=timespy.3dmdef";
                    break;
            }
            for (int i = 0; i < iters; i++)
            {
                Process p = Process.Start(path_3dm, args);
                p.WaitForExit();
            }
            isRunning = false;
        }

        static void WaitForBench()
        {
            while(isRunning) { Thread.Sleep(200); }
            isRunning = true;
        }

        static void RunTAT(string tatFileName)
        {
            pTat = Process.Start(tat, "-AL -AU=N -m=\"" + currentResults + tatFileName + ".csv\"");
        }
        static void StopTAT()
        {
            Process.Start(tat, "-stop");
            pTat = null;
        }

        static void RunXperf()
        {
            pXperf = Process.Start(xperf, "-start chtools -on " + xperfargs);
        }
        static void StopXperf(string etlFileName)
        {
            Process.Start(xperf, "-stop chtools -d \"" + currentResults + etlFileName + ".etl\"");
            pXperf = null;
        }

    }
}
