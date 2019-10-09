using System;
using System.Collections;

namespace ChopperTools.Models
{
    public class BenchTestObj
    {
        public BenchTestObj()
        {

        }
        public BenchTestObj(TestType type, string scriptPath, int iterations)
        {
            testType = type;
            path = scriptPath;
            iters = iterations;
        }
        public BenchTestObj(TestType type, BenchName bName, int iterations)
        {
            testType = type;
            markName = bName;
            iters = iterations;
        }
        public BenchTestObj(TestType type, bool isMulticore, int iterations)
        {
            testType = type;
            cinebenchMC = isMulticore;
            iters = iterations;
        }

        public TestType testType;
        public string path = "";
        public int iters;
        public BenchName? markName;

        public bool cinebenchMC = false;

    }
    public class BenchTestObjs : ICollection
    {
        public ArrayList objArray = new ArrayList();

        public BenchTestObj this[int index]
        {
            get { return (BenchTestObj)objArray[index]; }
        }

        public void CopyTo(Array a, int index)
        {
            objArray.CopyTo(a, index);
        }
        public int Count
        {
            get { return objArray.Count; }
        }
        public object SyncRoot
        {
            get { return this; }
        }
        public bool IsSynchronized
        {
            get { return false; }
        }
        public IEnumerator GetEnumerator()
        {
            return objArray.GetEnumerator();
        }

        public void Add(BenchTestObj newBench)
        {
            objArray.Add(newBench);
        }
    }
    public enum TestType
    {
        AUTOITV3,
        AUTOHOTKEY,
        /*3D*/MARK,
        CINEBENCH,
    }

    // 3DMark benchmark Name
    public enum BenchName
    {
        FireStrike,
        TimeSpy,
        SkyDiver,
        NightRaid,
        PortRoyal,
        CloudGate,
        IceStorm,
    }
}
