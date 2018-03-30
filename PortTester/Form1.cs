using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PortTester
{
    public partial class Form1 : Form
    {
        WebClient client;
        string IP = "";
        string localIP = "";
        IPEndPoint computerBeingTested;
        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "" + 8080;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
        }

        //hostButton
        int button1State = 0;
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1State == 0)
            {
                button1.Text = "Start listening to UDP/TCP on port: "+lastValidPort;
                button2.Enabled = false;
                button2.Visible = false;
                textBox1.ReadOnly = true;
                client = new WebClient();
                byte[] response = client.DownloadData("http://checkip.dyndns.org");
                textBox1.Text = IP = new UTF8Encoding().GetString(response).Split(':')[1].Split('<')[0].Trim(' ');
                textBox1.Text += " (local: " + localIP + ")";
                this.Height = 280;
                toolTip1.SetToolTip(button1, "Click me once you have the desired port selected.");
                button1State++;
            }
            else
            {
                textBox2.ReadOnly = true;
                button1.Text = "Started listening to UDP/TCP on port: " + lastValidPort;
                button1.Enabled = false;
                this.Height = 400;
                txt("Starting TCP Test for " + localIP + ":" +lastValidPort+" (30s Timeout)\n");

                var startListen = new Thread(() => {
                    try
                    {
                        try
                        {
                            TcpListener listener = new TcpListener(IPAddress.Parse(localIP), lastValidPort);
                            listener.Start();
                            if (!listener.AcceptTcpClientAsync().Wait(30000))
                            {
                                txt("Connection attempt not received. Port is either closed or no connection attempt was made.");
                            }
                            else
                            {
                                txt("TCP Connection Established. TCP "+lastValidPort+" is OPEN");
                            }
                            listener.Stop();
                        }
                        catch (Exception ex)
                        {
                            txt("Error: " + ex.ToString());
                        }

                        txt("\nStarting UDP Test for " + IP + ":" + lastValidPort + " (30s Timeout)\n");

                        try
                        {
                            UdpClient listener = new UdpClient(lastValidPort);

                            if(!listener.ReceiveAsync().Wait(30000))
                            {
                                txt("UDP packet wasn't received. Port is either closed or no connection attempt was made.");
                            }
                            else
                            {
                                txt("UDP Connection Established. UDP " + lastValidPort + " is OPEN");
                            }
                            listener.Close();
                        }
                        catch (Exception ex)
                        {
                            txt("Error: " + ex.ToString());
                        }


                    }
                    catch (Exception ex)
                    {
                        txt("Error: " + ex.ToString());
                    }
                });
                startListen.IsBackground = true;
                startListen.Start();
            }
        }

        //client Button
        int button2State = 0;
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2State == 0)
            {
                button2.Text = "Start sending UDP/TCP packet to "+ IP + ":" + lastValidPort;
                button1.Enabled = false;
                button1.Visible = false;
                this.Height = 280;
                toolTip1.SetToolTip(button2, "Click me once you have the desired IP and Port selected.");
                button2State++;
            }
            else
            {
                textBox1.ReadOnly = true;
                textBox2.ReadOnly = true;
                button2.Text = "Started sending to UDP/TCP on port: " + lastValidPort;
                button2.Enabled = false;
                this.Height = 400;
                txt("Starting TCP Test for " + IP + ":" + lastValidPort + " (30s Timeout)\n");

                var startSend = new Thread(() => {
                    try
                    {
                        try
                        {
                            TcpClient client = new TcpClient(IP, lastValidPort);
                            if (!client.ConnectAsync(IPAddress.Parse(localIP), lastValidPort).Wait(30000))
                            {
                                txt("TCP Connection couldn't be established. Port is either closed or there is no receiver.");
                            }
                            else
                            {
                                txt("TCP Connection Established.");
                            }
                            client.Close();
                        }
                        catch (Exception ex)
                        {
                            txt("Error: " + ex.ToString());
                        }

                        txt("\nStarting UDP Test for " + IP + ":" + lastValidPort + " (30s Timeout)\n");

                        try
                        {
                            computerBeingTested = new IPEndPoint(IPAddress.Parse(IP), lastValidPort);
                            UdpClient client = new UdpClient(lastValidPort);
                            byte[] data = Encoding.UTF8.GetBytes(".");
                            if (!client.SendAsync(data, data.Length, computerBeingTested).Wait(30000))
                            {
                                txt("UDP packet couldn't be sent.");
                            }
                            else
                            {
                                txt("UDP packet was sent, but not necessarily received.");
                            }
                            client.Close();
                        }
                        catch (Exception ex)
                        {
                            txt("Error: " + ex.ToString());
                        }

                    }
                    catch (Exception ex)
                    {
                        txt("Error: " + ex.ToString());
                    }
                });

                startSend.IsBackground = true;
                startSend.Start();

            }


            //computerBeingTested = new IPEndPoint(IPAddress.Parse(textBox2.Text), 29005);
        }


        int lastValidPort = -1;
        int currentAttemptedPort = -1;
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (textBox2.Text.Trim(' ') != "")
            {
                if (int.TryParse(textBox2.Text, out currentAttemptedPort))
                {
                    if (currentAttemptedPort < 1 || currentAttemptedPort > 65535)
                    {
                        textBox2.Text = "" + lastValidPort;
                    }
                    else
                    {
                        lastValidPort = currentAttemptedPort;
                        if (button1State != 0)
                        {
                            button1.Text = "Start listening to UDP/TCP on port: " + lastValidPort;
                        }
                        if(button2State != 0)
                        {
                            button2.Text = "Start sending UDP/TCP packet to " + IP + ":" + lastValidPort;
                        }
                    }
                }
                else
                {
                    textBox2.Text = "" + lastValidPort;
                }
            }
        }

        private void txt(string s)
        {
            try
            {
                richTextBox1.Text += s + "\n";
            }
            catch (Exception ex)
            {
                this.richTextBox1.Invoke((MethodInvoker)delegate {
                    // Running on the UI thread
                    richTextBox1.Text += s+"\n";
                });
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if(textBox1.Text.Contains(' '))
            {
                textBox1.Text = textBox1.Text.Trim(' ');
            }
            IP = textBox1.Text;
            if (button2State == 1)
            {
                button2.Text = "Start sending UDP/TCP packet to " + IP + ":" + lastValidPort;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
