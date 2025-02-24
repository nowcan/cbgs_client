using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace XqEngine
{
    public class XqEngineAdapter
    {
        public string m_name;
        public string m_path;
        public string m_type;

        private Process ps = null;
        private Queue<string> queue_Engine_UI = new Queue<string>();
        private Thread read_output;
        private Mutex mtx = new Mutex();
        
        void write_response_Engine_UI(string resp)
        {
            mtx.WaitOne();
            queue_Engine_UI.Enqueue(resp);
            mtx.ReleaseMutex();
        }

        string read_response_Engine_UI()
        {
            string str;
            mtx.WaitOne();
            if (queue_Engine_UI.Count > 0)
            {
                str = queue_Engine_UI.Dequeue();
            }
            else
            {
                str = null;
            }

            mtx.ReleaseMutex();
            return str;
        }

        public XqEngineAdapter()
        {
            ps = new Process();
        }

        ~XqEngineAdapter()
        {
            if (ps != null)
            {
                try
                {
                    if (ps.HasExited == false)
                    {
                        unload();
                    }
                }
                catch (Exception e)
                {
                }
            }
        }

        private void read_output_loop()
        {
            string line = null;
            try
            {
                while (!ps.HasExited)
                {
                    line = ps.StandardOutput.ReadLine();
                    if (line != null)
                    {
                        write_response_Engine_UI(line);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public void load(string path, string protocol)
        {
            Int32 t;
            string wdir = null;
            int idx;

            idx = path.LastIndexOf('\\');
            wdir = path.Substring(0, idx + 1);
            m_type = "UNKNOWN";
            m_path = path;
            ps = new Process();
            ps.StartInfo.FileName = path;
            ps.StartInfo.StandardOutputEncoding = Encoding.ASCII;
            ps.StartInfo.RedirectStandardInput = true;
            ps.StartInfo.RedirectStandardOutput = true;
            ps.StartInfo.RedirectStandardError = true;
            ps.StartInfo.UseShellExecute = false;
            ps.StartInfo.CreateNoWindow = true;
            ps.StartInfo.WorkingDirectory = wdir;
            try
            {
                ps.PriorityClass = ProcessPriorityClass.Idle;
            }
            catch
            {
            }

            ps.Start();
            read_output = new Thread(new ThreadStart(read_output_loop));
            read_output.Start();
            queue_Engine_UI.Clear();
            Thread.Sleep(1000);
            if (protocol.ToLower() == "ucci" || protocol.ToLower() == "auto")
            {
                if (m_type == "UNKNOWN")
                {
                    send_cmd("ucci");
                    t = 0;
                    while (t < 1000)
                    {
                        t++;
                        string line = recv_rsp();
                        if (line != null)
                        {
                            Debug.WriteLine(line);
                            if (line.Contains("ucciok"))
                            {
                                m_type = "UCCI";
                                break;
                            }

                            if (line.Contains("id name "))
                            {
                                m_name = line.Substring(8);
                            }
                        }

                        Thread.Sleep(10);
                    }
                }
            }

            if (protocol.ToLower() == "uci" || protocol.ToLower() == "auto")
            {
                if (m_type == "UNKNOWN")
                {
                    send_cmd("uci");
                    t = 0;
                    while (t < 1000)
                    {
                        t++;
                        string line = recv_rsp();
                        if (line != null)
                        {
                            Debug.WriteLine(line);
                            if (line.Contains("uciok"))
                            {
                                m_type = "UCI";
                                break;
                            }

                            if (line.Contains("id name "))
                            {
                                m_name = line.Substring(8);
                            }
                        }

                        Thread.Sleep(10);
                    }
                }
            }
        }

        public void unload()
        {
            send_cmd("stop");
            Thread.Sleep(10);
            send_cmd("quit");
            ps.WaitForExit(3000);
            try
            {
                read_output.Abort();
            }
            catch
            {
            }

            if (ps.HasExited == false)
            {
                ps.Kill();
            }

            ps.Close();
            ps = null;
        }

        public void send_cmd(string cmd)
        {
            ps.StandardInput.WriteLine(cmd);
        }

        public string recv_rsp()
        {
            return read_response_Engine_UI();
        }
    }
}
