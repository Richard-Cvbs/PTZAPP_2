using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetSDKCS;

namespace PTZAPP
{
    class Camera : Form1
    {
        public String ip;
        public String port;
        public String user;
        public String pass;
        public String paramOne;
        public String paramTwo;
        public String paramThree;
        public String id;

        public List<String> serialnumber_list = new List<String>();
        public List<String> name_list = new List<String>();
        public List<String> ip_list = new List<String>();



        public Camera(String ip,String port,String user, String pass, String paramOne, String paramTwo, String paramThree) {
            this.ip = ip;
            this.port = port;
            this.user = user;
            this.pass = pass;
            this.paramOne = paramOne;
            this.paramTwo = paramTwo;
            this.paramThree = paramThree;
        }

        public Camera()
        {

        }
        
        
        public void add()
        {
            if (port != "" && ip != "" && user != "" && pass != "") {
                
                using (SqlConnection connection = new Database().connection)
                {
                    connection.Open();
                    String queryId;
                    queryId = "select cameras.id, cameras.port from cameras" +
                              " join units on cameras.unit_id = units.id" +
                              " join sim_cards on units.id = sim_cards.unit_id" +
                              " where sim_cards.IP = '"+ip+"' and cameras.port = "+port+";";
                    SqlCommand cmd = new SqlCommand( queryId, connection);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            id = reader["id"].ToString();
                        }
                    }
                    connection.Close();
                }


                String date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                int id_insert = 0;
                if (paramOne != "" || paramTwo != "" || paramThree != "")
                {
                    using (SqlConnection connection = new Database().connection)
                    {
                        connection.Open();
                        String queryInsert;
                        queryInsert = "if exists(select * from ptz_settings where camera_id = " + id + ")" +
                        " BEGIN" +
                        " update ptz_settings set param1 = " + paramOne + ", param2 = " + paramTwo + ", param3 = " + paramThree + ", updated_at = GETDATE() where camera_id = " + id + ";" +
                        " END" +
                        " ELSE" +
                        " BEGIN" +
                        " insert into ptz_settings(camera_id, param1, param2, param3, created_at, status) values(" + id + ", " + paramOne + ", " + paramTwo + ", " + paramThree + ", GETDATE(), 0); " +
                        " SELECT CAST(scope_identity() AS int);" +
                        " END";

                        SqlCommand cmdStations = new SqlCommand(queryInsert, connection);

                        try
                        {
                            id_insert = int.Parse(cmdStations.ExecuteScalar().ToString());

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }


                        connection.Close();
                    }
                    MessageBox.Show("Saved!");
                }
                else
                {
                    MessageBox.Show("Fill all the fields");
                }
            }
            else {
                MessageBox.Show("Fill all the fields");
            }
        }


        public int isActive()
        {
            if (ip != "" || port != "")
            {
                int statuscam = 0;
                using (SqlConnection connection = new Database().connection)
                {
                    connection.Open();
                    String queryId;
                    queryId = " select cast(ptz_settings.status as int) as status from ptz_settings " +
                              " join cameras on cameras.id = ptz_settings.camera_id" +
                              " join units on units.id = cameras.unit_id" +
                              " join sim_cards on sim_cards.unit_id = units.id" +
                              " where sim_cards.IP = '" + ip + "' and cameras.port = " + port + ";";
                    SqlCommand cmd = new SqlCommand(queryId, connection);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            statuscam = int.Parse(reader["status"].ToString());
                        }
                        else
                        {
                            statuscam = 0;
                        }
                    }
                    connection.Close();

                }
                return statuscam;
            }
            MessageBox.Show("You can't connect whitout ip and port ;)");
            return -1;
        }



        public void search()
        {
            using (SqlConnection connection = new Database().connection)
            {
                connection.Open();
                String queryId;
                queryId = "select ptz_settings.camera_id,units.serial_number, sites.name, sim_cards.IP from ptz_settings" + 
                          " join cameras on cameras.id = ptz_settings.camera_id" +
                          " join units on cameras.unit_id = units.id"+
                          " join sites on units.site_id = sites.id" +
                          " join sim_cards on sim_cards.unit_id = units.id" +
                          " where ptz_settings.camera_id = 2731 OR units.serial_number like '%r2d3%' OR sim_cards.IP = '107.85.112.24';";
                SqlCommand cmd = new SqlCommand(queryId, connection);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        serialnumber_list.Add(reader["serial_number"].ToString());
                        name_list.Add(reader["name"].ToString());
                        ip_list.Add(reader["IP"].ToString());
                      
                    }
                }
                connection.Close();
            }

        }

    }
}
