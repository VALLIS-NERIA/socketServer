using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
using System.Data.SqlClient;

namespace socketServer {
    unsafe partial class Program {

        //delegate void Receive();
        //cmd.CommandText = "select * from dev_history";
        //SqlDataReader sdr = cmd.ExecuteReader();

        static void Pull(string dev_ip) {
            byte[] b = new byte[] { 0xfc };
            UdpClient sender = new UdpClient(dev_ip, remote_port_dev);
            sender.Send(b,1);
        }

        static void Work_Dev(Status s) {
            string name = Translate(s.id, 10);
            DevInfo info = new DevInfo(name, DateTime.Now, s);
            if (dict.ContainsKey(name)) {
                DevInfo info0 = dict[name];
                info.last_update = info0.last_update;
                if (info0.latest_status != info.latest_status || (DateTime.Now - info.last_update) > update_span) {
                    //如果状态有变，或者超过多久时间没写过，就写入数据库
                    WriteDB(info);
                }
                else {
                }
                dict.Remove(name);
                dict.Add(name, info);
            }
            else {
                dict.Add(name, info);
                WriteDB(info);
            }

        }

        static void WriteDB(DevInfo s) {
            conn.Open();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.CommandText = string.Format(@"insert into dev_history (time,deviceid,z1,z2) values ('{0}','{1}',{2},{3})", s.last_receive, s.dev_id, s.latest_status.z1, s.latest_status.z2);
            cmd.ExecuteNonQuery();
            s.last_update = DateTime.Now;
            conn.Close();
        }

        static void Callback_Dev(IAsyncResult ar) {
            IPEndPoint e = new IPEndPoint(IPAddress.Any, 0);
            byte[] receiveBytes = listener_dev.EndReceive(ar, ref e);
            Status s = Parse(receiveBytes);
            Console.WriteLine(string.Format("Report form device {0}: z1 = {1}, z2= {2}", Translate(s.id, 10), s.z1, s.z2));
            Work_Dev(s);
            listener_dev.BeginReceive(new AsyncCallback(Callback_Dev), new object());

        }

        static void Callback_Ctl(IAsyncResult ar) {
            byte[] receiveBytes = listener_ctl.EndReceive(ar, ref ep_ctl_remote);//ep会被更新
            //if(receiveBytes!={''})
            byte[] sendBytes = new byte[] { };
            foreach (var item in dict) {
                sendBytes.Concat(Serial(item.Value.latest_status));
            }
            sender_ctl = new UdpClient();
            sender_ctl.Connect(ep_ctl_remote);
            sender_ctl.Send(sendBytes, sendBytes.Length);
            listener_ctl.BeginReceive(new AsyncCallback(Callback_Ctl), new object());
        }


        static void Main(string[] args) {
            

            
            listener_dev.BeginReceive(new AsyncCallback(Callback_Dev), new object());
            listener_ctl.BeginReceive(new AsyncCallback(Callback_Ctl), new object());


            //Socket socket_dev_s = new Socket(SocketType.Stream, ProtocolType.IP);
            //socket_dev_s.Bind(ep_dev);
            //socket_dev_s.Listen(10);
            //byte[] buf = new byte[50];
            //socket_dev_s.Accept();
            //socket_dev_s.Receive(buf);
            //Console.WriteLine(Encoding.ASCII.GetString(buf));


            while (true) {
                Pull(Console.ReadLine());
            };
        }
    }
}
