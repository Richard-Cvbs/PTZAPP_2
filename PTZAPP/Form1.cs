using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NetSDKCS;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Data.SqlClient;
using System.Threading;

namespace PTZAPP
{
    public partial class Form1 : Form
    {
        private const int m_WaitTime = 5000;
        private const int SyncFileSize = 5 * 1024 * 1204;
        private static fDisConnectCallBack m_DisConnectCallBack;
        private static fHaveReConnectCallBack m_ReConnectCallBack;
        private static fRealDataCallBackEx2 m_RealDataCallBackEx2;
        private static fSnapRevCallBack m_SnapRevCallBack;
        public IntPtr[] logins = new IntPtr[500];
        public DataTable dtIps;
        public DataTable dtPort;
       
        private IntPtr m_LoginID = IntPtr.Zero;
        private NET_DEVICEINFO_Ex m_DeviceInfo;
        private IntPtr m_RealPlayID = IntPtr.Zero;
        private uint m_SnapSerialNum = 1;
        private bool m_IsInSave = false;
        private int SpeedValue = 4;
        private const int MaxSpeed = 8;
        private const int MinSpeed = 1;
        public int status;
        EM_RealPlayType type;
        Camera cam;
        AutoCompleteStringCollection datos = new AutoCompleteStringCollection();


        //---------------------------------------------------------------------------------------
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hwnd, int revert);
        [DllImport("user32.dll", EntryPoint = "GetMenuItemCount")]
        private static extern int GetMenuItemCount(IntPtr hmenu);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        private static extern int RemoveMenu(IntPtr hmenu, int npos, int wflags);
        [DllImport("user32.dll", EntryPoint = "DrawMenuBar")]
        private static extern int DrawMenuBar(IntPtr hwnd);
        private const int MF_BYPOSITION = 0x0400;
        private const int MF_DISABLED = 0x0002;
        //----------------------------------------------------------------------------------------


        public Form1()
        {
            InitializeComponent(); this.Load += new EventHandler(Form1_Load);
            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //-----------------------------------------------------------------------------------
            IntPtr hmenu = GetSystemMenu(this.Handle, 0);
            int cnt = GetMenuItemCount(hmenu);
            RemoveMenu(hmenu, cnt - 1, MF_DISABLED | MF_BYPOSITION);
            RemoveMenu(hmenu, cnt - 2, MF_DISABLED | MF_BYPOSITION);
            //-----------------------------------------------------------------------------------
            
            btn_play.Enabled = false;
            btn_stop.Enabled = false;

            m_DisConnectCallBack = new fDisConnectCallBack(DisConnectCallBack);
            m_ReConnectCallBack = new fHaveReConnectCallBack(ReConnectCallBack);
            m_RealDataCallBackEx2 = new fRealDataCallBackEx2(RealDataCallBackEx);
            m_SnapRevCallBack = new fSnapRevCallBack(SnapRevCallBack);
            try
            {
                NETClient.Init(m_DisConnectCallBack, IntPtr.Zero, null);
                NETClient.SetAutoReconnect(m_ReConnectCallBack, IntPtr.Zero);
                NETClient.SetSnapRevCallBack(m_SnapRevCallBack, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Process.GetCurrentProcess().Kill();
            }

            dataGridView1.DataSource = GetIps();
            StatusChange();

        }

        private void DisConnectCallBack(IntPtr lLoginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {
            this.BeginInvoke((Action)UpdateDisConnectUI);
        }

        private void UpdateDisConnectUI()
        {
            this.Text = "PTZAPP is Offline";
        }

        private void ReConnectCallBack(IntPtr lLoginID, IntPtr pchDVRIP, int nDVRPort, IntPtr dwUser)
        {
            this.BeginInvoke((Action)UpdateReConnectUI);
        }
        private void UpdateReConnectUI()
        {
            this.Text = "PTZAPP Is Online";
        }

        private void RealDataCallBackEx(IntPtr lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr param, IntPtr dwUser)
        {
            //do something such as save data,send data,change to YUV. 比如保存数据，发送数据，转成YUV等.
        }

        private void SnapRevCallBack(IntPtr lLoginID, IntPtr pBuf, uint RevLen, uint EncodeType, uint CmdSerial, IntPtr dwUser)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "capture";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (EncodeType == 10) //.jpg
            {
                DateTime now = DateTime.Now;
                string fileName = "async" + CmdSerial.ToString() + ".jpg";
                string filePath = path + "\\" + fileName;
                byte[] data = new byte[RevLen];
                Marshal.Copy(pBuf, data, 0, (int)RevLen);
                using (FileStream stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    stream.Write(data, 0, (int)RevLen);
                    stream.Flush();
                    stream.Dispose();
                }
            }
        }

        private void txt_port_ketpress(Object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 8 && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void btn_conectar_Click(object sender, EventArgs e)
        {
            cam = new Camera(txt_ip.Text, txt_port.Text, txt_user.Text, txt_pass.Text, txt_parameter1.Text, txt_parameter2.Text, txt_parameter3.Text);
            int statuscam = cam.isActive();
            int id_login = Getid();
            m_LoginID = IntPtr.Zero;
            if(statuscam != -1)
            if (IntPtr.Zero == m_LoginID)
            {
                ushort port = 0;
                try {
                    port = Convert.ToUInt16(txt_port.Text.Trim());
                }
                catch {
                    MessageBox.Show("Error");
                    return;
                }
                m_DeviceInfo = new NET_DEVICEINFO_Ex();
                m_LoginID = NETClient.Login(txt_ip.Text.Trim(), port, txt_user.Text.Trim(), txt_pass.Text.Trim(), EM_LOGIN_SPAC_CAP_TYPE.TCP, IntPtr.Zero, ref m_DeviceInfo);
                logins[id_login] = m_LoginID;
                    if (m_LoginID != IntPtr.Zero  )
                    {
                        if (statuscam != 1)
                        {
                            ptb_status.Image = Properties.Resources.online;
                                btn_play.Image = Properties.Resources.play;
                                btn_play.Cursor = Cursors.Hand;
                                btn_play.Enabled = true;
                    }
                    else
                    {
                        ptb_status.Image = Properties.Resources.online;
                        btn_play.Image = Properties.Resources.playdis;
                        btn_play.Cursor = Cursors.Default;
                        btn_play.Enabled = false;
                        btn_stop.Image = Properties.Resources.stop2;
                        btn_stop.Cursor = Cursors.Hand;
                        btn_stop.Enabled = true;
                    }

                }
                type = EM_RealPlayType.Realplay_1;
                m_RealPlayID = NETClient.RealPlay(m_LoginID, 0, ptb_principal.Handle, type);
                if (IntPtr.Zero == m_RealPlayID)
                {
                    MessageBox.Show(this, NETClient.GetLastError());
                    return;
                }
                NETClient.SetRealDataCallBack(m_RealPlayID, m_RealDataCallBackEx2, IntPtr.Zero, EM_REALDATA_FLAG.DATA_WITH_FRAME_INFO | EM_REALDATA_FLAG.PCM_AUDIO_DATA | EM_REALDATA_FLAG.RAW_DATA | EM_REALDATA_FLAG.YUV_DATA);
                

                btn_conectar.Image = Properties.Resources.Conectar_dis;
                btn_conectar.Enabled = false;
                btn_conectar.Cursor = Cursors.Default;
            }
            else
            {
                bool result = NETClient.Logout(m_LoginID);
                if (!result)
                {
                    MessageBox.Show(this, NETClient.GetLastError());
                    return;
                }
                m_LoginID = IntPtr.Zero;
            }
            

        }

        public void StatusChange()
        {
            using (SqlConnection connection = new Database().connection)
            {
                connection.Open();
                string query = "update ptz_settings set status = 0;";
                SqlCommand cmd = new SqlCommand(query, connection);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                }

                connection.Close();
                connection.Dispose();
            }
        }


        int p1 = 0;
        int p2 = 0;
        int p3 = 0;
        int cam_id = 0;
        String cam_ip = "";
        String cam_port = "";




        public void updatezerostatus()
        {
            int id = Getid();
            using (SqlConnection connection = new Database().connection)
            {
                connection.Open();
                string query = "update ptz_settings set status = 0, updated_at = GETDATE() where id = "+id+";";
                SqlCommand cmd = new SqlCommand(query, connection);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                }

                connection.Close();
                connection.Dispose();
            }
        }

        public void updateonestatus()
        {
            int id = Getid();
            using (SqlConnection connection = new Database().connection)
            {
                connection.Open();
                string query = "update ptz_settings set status = 1, updated_at = GETDATE() where id = " + id + ";";
                SqlCommand cmd = new SqlCommand(query, connection);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Close();
                    reader.Dispose();
                }

                connection.Close();
                connection.Dispose();
            }
        }
        private void btn_play_Click(object sender, EventArgs e)
        {
            updateonestatus();
            using (SqlConnection connection = new Database().connection)
            {
                connection.Open();
                string query;
                query = " select cast(ptz_settings.status as int) as status ,cameras.port,sim_cards.IP,ptz_settings.param1,ptz_settings.param2,ptz_settings.param3,ptz_settings.id from ptz_settings" +
                        " join cameras on cameras.id = ptz_settings.camera_id" +
                        " join units on units.id = cameras.unit_id" +
                        " join sim_cards on sim_cards.unit_id = units.id" +
                        " where sim_cards.IP = '" + txt_ip.Text + "' and cameras.port = '" + txt_port.Text + "'";
                SqlCommand cmd = new SqlCommand(query, connection);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        p1 = (int)(reader["param1"]);
                        p2 = (int)(reader["param2"]);
                        p3 = (int)(reader["param3"]);
                        cam_ip = (String)(reader["IP"]);
                        cam_port = (reader["port"].ToString());
                        status = (int)(reader["status"]);
                        cam_id = (int)(reader["id"]);
                    }
                    reader.Close();
                    reader.Dispose();
                }
                connection.Close();
                connection.Dispose();
                
                if (p1 == 0 && p2 == 0 && p3 == 0)
                {
                    MessageBox.Show("Sin valores en la base de datos");
                }
                else
                {
                    if(status == 1)
                    {
                        btn_play.Image = Properties.Resources.playdis;
                        btn_play.Enabled = false;
                        btn_mainstream.Image = Properties.Resources.MAINSTRAM_DIS;
                        btn_mainstream.Enabled = false;
                        pictureBox1.Image = Properties.Resources.SUBSTREAM_DIS;
                        pictureBox1.Enabled = false;
                        btn_stop.Cursor = Cursors.Hand;
                        btn_stop.Image = Properties.Resources.stop2;
                        btn_stop.Enabled = true;
                        hilos(cam_id);
                    }
                    else
                    {
                        //btn_mainstream.Image = Properties.Resources.MAINSTRAM_DIS;
                        //btn_mainstream.Enabled = true;
                        //btn_mainstream.Cursor = Cursors.Hand;
                        //pictureBox1.Image = Properties.Resources.SUBSTREAM_DIS;
                        //pictureBox1.Enabled = true;
                        //pictureBox1.Cursor = Cursors.Hand;

                    }
                }
            }
        }

        Thread[] th = new Thread[500];
        public int hilos(int cam_id)
        {
           int id_hilo = cam_id-1;
            
            Thread.Sleep(10);
            
               if (cam_ip.Equals(txt_ip.Text) && cam_port.Equals(txt_port.Text)) {
                    Thread.Sleep(10);
                    th[id_hilo] = new Thread(() => PTZGOTO(m_LoginID, p1, p2, p3));
                    Thread.Sleep(10);
                    th[id_hilo].Start();
                    Thread.Sleep(10);
               }
            return cam_id;
        }
        public static void PTZGOTO(IntPtr m, int p, int t, int z)
        {
            do
            {
                bool ret = NETClient.PTZControl(m, 0, EM_EXTPTZ_ControlType.FASTGOTO, p, t, 0, false, IntPtr.Zero);
                Thread.Sleep(10000);

                bool ret2 = NETClient.PTZControl(m, 0, EM_EXTPTZ_ControlType.FASTGOTO, -1 * (p), t, 0, false, IntPtr.Zero);
                Thread.Sleep(10000);

                bool ret3 = NETClient.PTZControl(m, 0, EM_EXTPTZ_ControlType.FASTGOTO, -1 * (p), t, 0, false, IntPtr.Zero);
                Thread.Sleep(10000);

                bool ret4 = NETClient.PTZControl(m, 0, EM_EXTPTZ_ControlType.FASTGOTO, p, t, 0, false, IntPtr.Zero);
                Thread.Sleep(10000);

                bool ret5 = NETClient.PTZControl(m, 0, EM_EXTPTZ_ControlType.FASTGOTO, 0, z, 0, false, IntPtr.Zero);
                Thread.Sleep(10000);

                bool ret6 = NETClient.PTZControl(m, 0, EM_EXTPTZ_ControlType.FASTGOTO, 0, -1 * (z), 0, false, IntPtr.Zero);
                Thread.Sleep(10000);
            } while (true);
         }


        private void btn_desconectar_Click_1(object sender, EventArgs e)
        {
            bool ret = NETClient.StopRealPlay(m_RealPlayID);
            m_RealPlayID = IntPtr.Zero;
            m_LoginID = IntPtr.Zero;
            ptb_principal.Refresh();
            ptb_status.Image = Properties.Resources.ofline;
            btn_play.Enabled = false;
            btn_stop.Enabled = false;
            btn_play.Image = Properties.Resources.playdis;
            btn_stop.Image = Properties.Resources.stop2dis;
            btn_play.Cursor = Cursors.Default;
            btn_stop.Cursor = Cursors.Default;
            btn_conectar.Image = Properties.Resources.Conectar;
            btn_conectar.Enabled = true;
            btn_conectar.Cursor = Cursors.Hand;
            btn_mainstream.Image = Properties.Resources.MAINSTRAM;
            btn_mainstream.Enabled = true;
            btn_mainstream.Cursor = Cursors.Hand;
            pictureBox1.Image = Properties.Resources.SUBSTREAM;
            pictureBox1.Enabled = true;
            pictureBox1.Cursor = Cursors.Hand;
            txt_ip.Text = "";
            txt_port.Text = "";
            txt_user.Text = "";
            txt_pass.Text = "";
            txt_parameter1.Text = "";
            txt_parameter2.Text = "";
            txt_parameter3.Text = "";
            label8.Visible = false;

        }

        private void PTZControl(EM_EXTPTZ_ControlType type, int param1, int param2, bool isStop)
        {
            bool ret = NETClient.PTZControl(m_LoginID, 0, type, param1, param2, 0, isStop, IntPtr.Zero);
            if (!ret)
            {
                MessageBox.Show(this, NETClient.GetLastError());
            }
        }

        private void top_button_MouseDown(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.UP_CONTROL, 0, SpeedValue, false);
        }

        private void top_button_MouseUp(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.UP_CONTROL, 0, SpeedValue, true);
        }

        private void left_button_MouseDown(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.LEFT_CONTROL, 0, SpeedValue, false);
        }

        private void left_button_MouseUp(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.LEFT_CONTROL, 0, SpeedValue, true);
        }

        private void right_button_MouseDown(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.RIGHT_CONTROL, 0, SpeedValue, false);
        }

        private void right_button_MouseUp(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.RIGHT_CONTROL, 0, SpeedValue, true);
        }

        private void bottom_button_MouseDown(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.DOWN_CONTROL, 0, SpeedValue, false);
        }

        private void bottom_button_MouseUp(object sender, MouseEventArgs e)
        {
            PTZControl(EM_EXTPTZ_ControlType.DOWN_CONTROL, 0, SpeedValue, true);
        }

        private void btn_salve_Click(object sender, EventArgs e)
        {
            cam = new Camera(txt_ip.Text, txt_port.Text, txt_user.Text, txt_pass.Text, txt_parameter1.Text, txt_parameter2.Text, txt_parameter3.Text);
            cam.add();
            dataGridView1.DataSource = GetIps();
        }
        
    
        private void txt_search_TextChanged(object sender, EventArgs e)
        {
            DataView dvIps = dtIps.DefaultView;
            dvIps.RowFilter = String.Format("serial_number LIKE '%{0}%'",txt_search.Text);
            dataGridView1.DataSource = dvIps;
        }
        
        public int Getid()
        {
            int id = 0;
            using (SqlConnection connection = new Database().connection)
            {
            
                connection.Open();
                string query;
                query = "select ptz_settings.id from ptz_settings" +
                        " join cameras on cameras.id = ptz_settings.camera_id" +
                        " join units on units.id = cameras.unit_id" +
                        " join sim_cards on sim_cards.unit_id = units.id" +
                        " where sim_cards.IP = '" + txt_ip.Text + "' and cameras.port = '" + txt_port.Text + "'";
                SqlCommand cmd = new SqlCommand(query, connection);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        id = (int)(reader["id"]);
                    }
                    reader.Close();
                    reader.Dispose();
                }
                connection.Close();
                connection.Dispose();
            }
            return id;
        }

        private void btn_stop_Click(object sender, EventArgs e, Thread[] t)
        {
            for (int i = 0; i < t.Length; i++)
            {
                if (cam_ip.Equals(txt_ip.Text) && cam_port.Equals(txt_port.Text))
                {
                    t[i].Abort();
                }
            }
        }

        private void btn_mainstream_Click(object sender, EventArgs e)
        {
            if (type == EM_RealPlayType.Realplay_1) {
                bool ret = NETClient.StopRealPlay(m_RealPlayID);
                type = EM_RealPlayType.Realplay;
                m_RealPlayID = NETClient.RealPlay(m_LoginID, 0, ptb_principal.Handle, type);

            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (type == EM_RealPlayType.Realplay)
            {
                bool ret = NETClient.StopRealPlay(m_RealPlayID);
                type = EM_RealPlayType.Realplay_1;
                m_RealPlayID = NETClient.RealPlay(m_LoginID, 0, ptb_principal.Handle, type);

            }
        }

        private void btn_delete_Click(object sender, EventArgs e)
        {
            int id = 0;
            if (txt_ip.Text != "" || txt_port.Text != "")
            {
                id = Getid();
                using (SqlConnection connection = new Database().connection)
                {
                    connection.Open();
                    string query;
                    query = "delete from ptz_settings where id = " + id + ";";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.ExecuteNonQuery();
                    connection.Close();
                    connection.Dispose();
                }
                MessageBox.Show("Camera deleted");
            }
            else
            {
                MessageBox.Show("Fill all the fields");
            }
            dataGridView1.DataSource = GetIps();
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            if(status == 1)
            {
                int id_hilo = Getid();
                if(th[id_hilo-1] != null)
                th[id_hilo-1].Abort();
                btn_play.Image = Properties.Resources.play;
                btn_play.Enabled = true;
                btn_play.Cursor = Cursors.Hand;
                btn_mainstream.Image = Properties.Resources.MAINSTRAM;
                btn_mainstream.Enabled = true;
                pictureBox1.Image = Properties.Resources.SUBSTREAM;
                pictureBox1.Enabled = true;
                btn_stop.Cursor = Cursors.Default;
                btn_stop.Image = Properties.Resources.stop2dis;
                btn_stop.Enabled = false;
                MessageBox.Show("stop");
            }
            else
            {
                btn_play.Image = Properties.Resources.play;
                btn_play.Enabled = true;
                btn_mainstream.Image = Properties.Resources.MAINSTRAM;
                btn_mainstream.Enabled = true;
                pictureBox1.Image = Properties.Resources.SUBSTREAM;
                pictureBox1.Enabled = true;
                btn_stop.Cursor = Cursors.Default;
                btn_stop.Image = Properties.Resources.stop2dis;
                btn_stop.Enabled = false;
                updatezerostatus();
            }        
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
        }

        public DataTable GetIps()
        {
            dtIps = new DataTable();
            //dtPort = new DataTable();
            dtIps.Columns.Add("serial_number", typeof(String));
            dtIps.Columns.Add("port", typeof(String));
            if (checkActive.Checked)
            {
                using (SqlConnection connection = new Database().connection)
                {
                    connection.Open();
                    string query;
                    query = "select units.serial_number, cameras.port from ptz_settings" +
                              " join cameras on cameras.id = ptz_settings.camera_id" +
                              " join units on cameras.unit_id = units.id" +
                              " join sites on units.site_id = sites.id" +
                              " join sim_cards on sim_cards.unit_id = units.id" +
                              " where ptz_settings.status = 1;";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    SqlDataAdapter MyAdapter = new SqlDataAdapter(query, connection);
                    MyAdapter.Fill(dtIps);
                    dataGridView1.DataSource = dtIps;

                    connection.Close();
                    connection.Dispose();
                }
                return dtIps;

            } else if (checkInactive.Checked)
            {
                using (SqlConnection connection = new Database().connection)
                {
                    connection.Open();
                    string query;
                    query = "select units.serial_number, cameras.port from ptz_settings" +
                              " join cameras on cameras.id = ptz_settings.camera_id" +
                              " join units on cameras.unit_id = units.id" +
                              " join sites on units.site_id = sites.id" +
                              " join sim_cards on sim_cards.unit_id = units.id" +
                              " where ptz_settings.status = 0;";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    SqlDataAdapter MyAdapter = new SqlDataAdapter(query, connection);
                    MyAdapter.Fill(dtIps);
                    dataGridView1.DataSource = dtIps;

                    connection.Close();
                    connection.Dispose();
                }

                return dtIps;

            }
            else
            using (SqlConnection connection = new Database().connection)
            {
                connection.Open();
                string query;
                query = "select units.serial_number, cameras.port from ptz_settings" +
                          " join cameras on cameras.id = ptz_settings.camera_id" +
                          " join units on cameras.unit_id = units.id" +
                          " join sites on units.site_id = sites.id" +
                          " join sim_cards on sim_cards.unit_id = units.id;";
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataAdapter MyAdapter = new SqlDataAdapter(query, connection);
                MyAdapter.Fill(dtIps);
                dataGridView1.DataSource = dtIps;
               
                connection.Close();
                connection.Dispose();
            }
            return dtIps;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //int row = dtIps.Select;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            String site;
            int port;
            if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
            {
                dataGridView1.CurrentRow.Selected = true;
                site = dataGridView1.Rows[e.RowIndex].Cells["serial_number"].FormattedValue.ToString();
                label8.Text = site;
                label8.Visible = true;
                port = int.Parse(dataGridView1.Rows[e.RowIndex].Cells["port"].FormattedValue.ToString());
                
                using (SqlConnection connection = new Database().connection)
                {
                    connection.Open();
                    string query;
                    query = "select sim_cards.IP, cameras.port, cameras.username, cameras.password," +
                            " ptz_settings.param1,ptz_settings.param2,ptz_settings.param3 from ptz_settings" +
                            " join cameras on cameras.id = ptz_settings.camera_id" +
                            " join units on cameras.unit_id = units.id" +
                            " join sites on units.site_id = sites.id" +
                            " join sim_cards on sim_cards.unit_id = units.id " +
                            " where units.serial_number = '"+site+"' and cameras.port = '"+port+"'; ";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if(reader.Read())
                        {
                            txt_ip.Text = (reader["IP"].ToString());
                            txt_port.Text = (reader["port"].ToString());
                            txt_user.Text = (reader["username"].ToString());
                            txt_pass.Text = (reader["password"].ToString());
                            txt_parameter1.Text = (reader["param1"].ToString());
                            txt_parameter2.Text = (reader["param2"].ToString());
                            txt_parameter3.Text = (reader["param3"].ToString());
                        }
                        reader.Close();
                        reader.Dispose();
                    }
                    connection.Close();
                    connection.Dispose();
                }

            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void checkInactive_CheckedChanged(object sender, EventArgs e)
        {
            if (checkInactive.Checked)
            {
                GetIps();
                checkActive.Enabled = false;
            }
            else
            {
                checkActive.Enabled = true;
                GetIps();
            }
        }

        private void checkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (checkActive.Checked)
            {
                GetIps();
                checkInactive.Enabled = false;
            }
            else
            {
                checkInactive.Enabled = true;
                GetIps();
            }
        }

        private void ptb_principal_Click(object sender, EventArgs e)
        {

        }

        private void carCounter_GridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void label10_Click(object sender, EventArgs e)
        {
            
            label10.Text = "some text";
        }

        private void startCounterButton_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;

        }

        //private static int initalCars = (Directory.GetFiles("C:/Temp/Camera/camera/6H0BBD3PAZC5550/2022-04-18/001/jpg", "*", SearchOption.AllDirectories).Length) / 3;
        //int currentCars = initalCars;
        //int prevFiles = Directory.GetFiles("C:/Temp/Camera/camera/6H0BBD3PAZC5550/2022-04-18/001/jpg", "*", SearchOption.AllDirectories).Length;
        private void timer1_Tick(object sender, System.EventArgs e)
        {
           /*
            int currentFiles = Directory.GetFiles("C:/Temp/Camera/camera/6H0BBD3PAZC5550/2022-04-18/001/jpg", "*", SearchOption.AllDirectories).Length;
            if (currentFiles != prevFiles) { currentCars++; }
            prevFiles = currentFiles;
            label10.Text = currentCars.ToString();
           */
        }
    }
}
