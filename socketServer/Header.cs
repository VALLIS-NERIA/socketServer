using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace socketServer {
    unsafe partial class Program {
        const int Z1_ERROR = 1;
        const int Z1_OK = 0;
        const int Z2_ERROR = 1;
        const int Z2_OK = 0;

        

        const int listen_port_ctl = 6666;
        const int listen_port_dev = 20002;
        const int remote_port_dev = 8888;
        const int remote_port_ctl = 9999;

        static TimeSpan update_span = new TimeSpan(0, 5, 0); //5 minutes


        static IPAddress web_server_ip = IPAddress.Parse("127.0.0.1");
        static IPEndPoint ep_ctl_remote = new IPEndPoint(web_server_ip, listen_port_ctl);
        static IPEndPoint ep_dev = new IPEndPoint(IPAddress.Any, listen_port_dev);
        static IPEndPoint ep_ctl = new IPEndPoint(IPAddress.Any, listen_port_ctl);

        

        static UdpClient listener_dev = new UdpClient(ep_dev);
        static UdpClient listener_ctl = new UdpClient(ep_ctl);
        static UdpClient sender_ctl ;

        static SqlConnection conn = new SqlConnection(@"Server=(localdb)\ExpDB;Integrated Security=True;");

        static Dictionary<string, DevInfo> dict = new Dictionary<string, DevInfo>();

        byte[] test = { 0x23, 0x6d, 0x6a,
                              0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                              0x2c, 0x01, 0x2c, 0x02, 0x2c,
                              0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa,
                              0x2c, 0x55 };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct Status {
            public byte dum1;
            public byte dum2;
            public byte dum3;
            public fixed byte id[10];
            public byte dum4;
            public byte z1;
            public byte dum5;
            public byte z2;
            public byte dum6;
            public fixed byte y[6];
            public byte dum7;
            public byte dum8;
            public static bool operator ==(Status lhs, Status rhs) {
                if (Translate(lhs.id, 10) == Translate(rhs.id, 10) && lhs.z1 == rhs.z1 && lhs.z2 == rhs.z2) return true;
                else return false;
            }
            public static bool operator !=(Status lhs, Status rhs) {
                return !(lhs == rhs);
            }
        }

        struct DevInfo {
            public string dev_id;
            public DateTime last_receive;
            public Status latest_status;
            public DateTime last_update;
            public DevInfo(string a, DateTime b, Status c, DateTime d) {
                dev_id = a;
                last_receive = b;
                latest_status = c;
                last_update = d;
            }
            public DevInfo(string a, DateTime b, Status c) {
                dev_id = a;
                last_receive = b;
                latest_status = c;
                last_update = new DateTime(1970, 1, 1, 0, 0, 0);
            }
        }

        static string Translate(byte* p, int length) {
            string s = null;
            for (int i = 0; i < length; i++) {
                s += Convert.ToChar(*p++);
            }
            return s;
        }

        static Status Parse(byte[] msg) {
            Status* s;
            fixed (byte* p = &msg[0]) {
                s = (Status*)p;
            }
            Status s2 = *s;
            return s2;
        }

        static byte[] Serial(Status s) {
            byte[] b = new byte[26];
            byte* p = (byte*)&s;
            for (int i = 0; i < 26; i++) {
                b[i] = *p++;
            }
            return b;
        }

        static long IpToInt(string ip) {
            char[] separator = new char[] { '.' };
            string[] items = ip.Split(separator);
            return long.Parse(items[0]) << 24
                    | long.Parse(items[1]) << 16
                    | long.Parse(items[2]) << 8
                    | long.Parse(items[3]);
        }
    }
}