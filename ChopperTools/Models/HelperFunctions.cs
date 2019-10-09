using System.Diagnostics;
using System.Windows;
using System.IO;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;

namespace ChopperTools.Models
{
    static class HelperFunctions
    {
        public class Installers
        {
            private static readonly string cwd = Directory.GetCurrentDirectory();
            public static bool Check3DM() { return File.Exists("C:\\Program Files\\UL\\3DMark\\3DMarkCmd.exe"); }
            public static bool CheckTAT() { return File.Exists("C:\\Program Files\\Intel Corporation\\Intel(R)TAT6\\Host\\ThermalAnalysisToolCmd.exe"); }
            public static bool CheckXperf() { return File.Exists("C:\\Program Files (x86)\\Windows Kits\\10\\Windows Performance Toolkit\\xperf.exe"); }
            public static void Install_3DMark()
            {
                Process[] pname = Process.GetProcessesByName("3dmark-setup.exe");
                if (pname.Length == 0)
                {
                    Process p = Process.Start(cwd + "\\installers\\3dmark-setup.exe", "/quiet /force");
                    p.EnableRaisingEvents = true;
                    p.Exited += (o, a) => {
                        Process.Start("C:\\Program Files\\UL\\3DMark\\3DMarkCmd.exe", "---register=3DM-DEV-2JLJQ-2SJHH-WSU73-XVRXU");                        
                    };
                    p.WaitForExit();
                }                
            }
            public static void Install_3DMark_DLC()
            {
                for (int i = 0; i < 3; i++)
                {
                    string dlcpath; 
                    string inspath; 
                    switch ((BenchName)i)
                    {
                        case BenchName.FireStrike:
                            dlcpath = "C:\\ProgramData\\UL\\3DMark\\chops\\dlc\\fire-strike-test\\";
                            inspath = cwd + "\\installers\\benchmarks\\3dmark-v2-9-0-fire-strike-test-v1-1-44.dlc";
                            break;
                        case BenchName.TimeSpy:
                            dlcpath = "C:\\ProgramData\\UL\\3DMark\\chops\\dlc\\time-spy-test\\";
                            inspath = cwd + "\\installers\\benchmarks\\3dmark-v2-9-0-time-spy-test-v1-1-625.dlc";
                            break;
                        case BenchName.SkyDiver:
                            dlcpath = "C:\\ProgramData\\UL\\3DMark\\chops\\dlc\\sky-diver-test\\";
                            inspath = cwd + "\\installers\\benchmarks\\3dmark-v2-9-0-sky-diver-test-v1-0-26.dlc";
                            break;
                        default:
                            MessageBox.Show("Only Fire Strike, Time Spy, and Sky Diver are currently available!");
                            return;
                    }
                    if (!Directory.Exists(dlcpath))
                    {
                        Process p = Process.Start("C:\\Program Files\\UL\\3DMark\\3DMarkCmd.exe", "--install=" + inspath);
                        p.EnableRaisingEvents = true;
                        p.WaitForExit();
                    }
                }
            }
            public static void Install_TAT()
            {
                Process[] pname = Process.GetProcessesByName("Intel(R)ThermalAnalysisToolInstallerWin.exe");
                if (pname.Length == 0)
                {
                    Process p = Process.Start(cwd + "\\installers\\Intel(R)ThermalAnalysisToolInstallerWin.exe", "-s");
                    p.WaitForExit();
                }
            }
            public static void Install_Xperf()
            {
                Process[] pname = Process.GetProcessesByName("adksetup.exe");
                if (pname.Length == 0)
                {
                    Process p = Process.Start(cwd + "\\installers\\ADK\\adksetup.exe", "/q /features OptionId.WindowsPerformanceToolkit");
                    p.WaitForExit();
                }                
            }

            internal static void InstallPrereqs()
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (o, a) => { BW_Prereqs(); };
                bw.WorkerReportsProgress = true;
                bw.RunWorkerAsync();
            }

            private static void BW_Prereqs()
            {
                bool mark = Check3DM();
                bool tat = CheckTAT();
                bool xp = CheckXperf();
                if (!mark) Install_3DMark();
                if (!tat) Install_TAT();
                if (!xp) Install_Xperf();
                                
                string res = "";

                if (!mark || !tat || !xp)
                {
                    res += "Prerequisites installed:\n\n";
                    if (!mark) res += "UL 3DMark\n";
                    if (!tat) res += "Intel(R) Thermal Analysis Tool\n";
                    if (!xp) res += "Windows Performance Toolkit - Xperf\n";
                    res += "\n";
                }

                if (mark || tat || xp)
                {
                    res += "Prerequisites found (No action taken):\n\n";
                    if (mark) res += "UL 3DMark\n";
                    if (tat) res += "Intel(R) Thermal Analysis Tool\n";
                    if (xp) res += "Windows Performance Toolkit - Xperf\n";
                }

                MessageBox.Show(res);
            }
        }
    }
}
