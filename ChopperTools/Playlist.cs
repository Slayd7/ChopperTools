using ChopperTools.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace ChopperTools
{
    public class Playlist
    {
        public static void SavePlaylist(List<BenchTestObj> list, string name)
        {
            BenchTestObjs objs = new BenchTestObjs();
            foreach (BenchTestObj o in list)
            {
                objs.Add(o);
            }
            XmlSerializer ser = new XmlSerializer(typeof(BenchTestObjs));
            TextWriter writer = new StreamWriter(name);
            ser.Serialize(writer, objs);
            writer.Close();
        }

        public static void LoadPlaylist(string path, out List<BenchTestObj> list)
        {
            XmlSerializer ser = new XmlSerializer(typeof(BenchTestObjs));
            FileStream fs = new FileStream(path, FileMode.Open);
            BenchTestObjs objs;
            objs = (BenchTestObjs)ser.Deserialize(fs);
            list = new List<BenchTestObj>();
            foreach(BenchTestObj obj in objs.objArray)
            {
                list.Add(obj);
            }
        }
    }
}
