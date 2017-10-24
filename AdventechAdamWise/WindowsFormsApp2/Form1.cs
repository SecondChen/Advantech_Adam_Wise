using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace WindowsFormsApp2
{


    public partial class Form1 : Form
    {

        PublicWebService.WebService pb_ws = new PublicWebService.WebService();
        static String sEnvironment = GetEnvironment();

        String username =ConfigurationManager.AppSettings["Account"].ToString();
        String password = ConfigurationManager.AppSettings["Password"].ToString();
        String MyIP = ConfigurationManager.AppSettings["IPAddress"].ToString();
        int iDOInput = 0;

        public class DI
        {
            public string IP { get; set; }
            public string Ch { get; set; }
            public string Md { get; set; }
            public string Val { get; set; }
            public string Stat { get; set; }
            public string Cnting { get; set; }
            public string OvLch { get; set; }
        }
        public class DO
        {
            public string IP { get; set; }
            public string Ch { get; set; }
            public string Md { get; set; }
            public string Val { get; set; }
            public string Stat { get; set; }
            public string PsCtn { get; set; }
            public string PsStop { get; set; }
            public string PsIV { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string sJSON = GetJsonStringFromWISE("di_value", "slot_0" , "ch_1" );
            textBox1.Text = sJSON;
        }

        public string GetJsonStringFromWISE(string sIOType , string sSlot ,string sChannel )
        {
            string sResult = string.Empty;
            try
            {
                string sURL = string.Empty;
                string sIPAddress = "http://10.248.72.201/";

                sURL = sIPAddress + sIOType + "/" + sSlot + "/" + sChannel;

                // Create a request for the URL. 
                //WebRequest request = WebRequest.Create("http://10.248.72.201/do_value/slot_0/Ch_1");
                WebRequest request = WebRequest.Create(sURL);

                //Set Account and Password for Adventech ADAM WISE
                request.Proxy = null;
                //String username = "root";
                //String password = "00000000";
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                request.Headers.Add("Authorization", "Basic " + encoded);
                request.PreAuthenticate = true;

                // If required by the server, set the credentials.
                request.Credentials = CredentialCache.DefaultCredentials;
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                sResult = responseFromServer;
                //Console.WriteLine(responseFromServer);
                // Clean up the streams and the response.
                reader.Close();
                response.Close();


                DataSet ds = null;
                StringBuilder strSql = new StringBuilder();
                if (sIOType == "di_value")
                {
                    //----Use Json.net to analyze the Json string
                    //string json = @"{""Ch"":0,""Md"":0,""Val"":1}";
                    DI myData = JsonConvert.DeserializeObject<DI>(sResult);

                    myData.IP = MyIP;
                    //----Get data from ADAM WISE and insert into MS-SQL DB
                    string sRes = InsertDataToDIDB(myData);
                    strSql.Append(" select * from ");
                    strSql.Append(" FMM_ADAMWISE_DI ");
                    strSql.Append(" order by Create_Date desc ");
                }
                else
                {
                    //----Use Json.net to analyze the Json string
                    //string json = @"{""Ch"":0,""Md"":0,""Val"":1}";
                    DO myData = JsonConvert.DeserializeObject<DO>(sResult);
                    myData.IP = MyIP;
                    //----Get data from ADAM WISE and insert into MS-SQL DB
                    string sRes = InsertDataToDODB(myData);
                    strSql.Append(" select * from ");
                    strSql.Append(" FMM_ADAMWISE_DO ");
                    strSql.Append(" order by Create_Date desc ");

                }

                ds = pb_ws.ExcuteSQL_Query("CIMDB_" + sEnvironment, strSql.ToString(), "ADAMWISE_JOB");
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    //sResult = "SUCCESS";
                    dataGridView1.DataSource = ds.Tables[0];
                }


            }

            catch (Exception ex) {

                throw ex;
            }

            return sResult;
        }


        public string SendJsonStringToWISE(int iDOInput , string sSlot , string Channel)
        {
            string sResult=string.Empty;
            try
            {

                // Create a request using a URL that can receive a post. 
                string sURL = string.Empty;
                string sIPAddress = "http://10.248.72.201/";

                sURL = sIPAddress + "do_value/" + sSlot + "/ch_" + Channel; 

                //WebRequest request = WebRequest.Create("http://10.248.72.201/do_value/slot_0/ch_1");
                WebRequest request = WebRequest.Create(sURL);
                // Set the Method property of the request to POST.
                request.Method = "PUT";
                // Create POST data and convert it to a byte array.
                //string postData = @"{""Ch"":1,""Md"":3,""Stat"":1,""Val"":0,""PsCtn"":0,""PsStop"":0,""PsIV"": 0}";
                string postData = @"{""Ch"": " + Channel + @" ,""Md"":3,""Stat"":1,""Val"":" + iDOInput +  @",""PsCtn"":0,""PsStop"":0,""PsIV"": 0}";
                
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/json";
                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;

                //Set Account and Password for Adventech ADAM WISE
                request.Proxy = null;
                //String username = "root";
                //String password = "00000000";
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
                request.Headers.Add("Authorization", "Basic " + encoded);
                request.PreAuthenticate = true;


                // Get the request stream.
                Stream dataStream = request.GetRequestStream();
                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);
                // Close the Stream object.
                dataStream.Close();
                // Get the response.
                WebResponse response = request.GetResponse();
                // Display the status.
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                // Get the stream containing content returned by the server.
                dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
                // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return sResult;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (iDOInput == 0)
            {
                iDOInput = 1;
            }
            else
            {
                iDOInput = 0;
            }
            string sResult = SendJsonStringToWISE(iDOInput , "slot_0" , "1" );
            //textBox1.Text = sResult;

            string sJSON = GetJsonStringFromWISE("do_value", "slot_0" , "ch_1");
            textBox1.Text = sJSON;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string sJSON = GetJsonStringFromWISE("di_value", "slot_0" , "ch_0");
            textBox1.Text = sJSON;


        }

        public static string GetEnvironment()
        {
            try
            {
                String sEnvironment = System.Configuration.ConfigurationManager.AppSettings.Get("Environment").ToString(); //從有@darwinprecisions.com 改到無@後字串
                return sEnvironment;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (iDOInput == 0)
            {
                iDOInput = 1;
            }
            else
            {
                iDOInput = 0;
            }
            string sResult = SendJsonStringToWISE(iDOInput, "slot_0", "0");
            //textBox1.Text = sResult;

            string sJSON = GetJsonStringFromWISE("do_value", "slot_0" , "ch_0");
            textBox1.Text = sJSON;

        }

        public string InsertDataToDIDB(DI di)
        {
            DataSet ds = null;
            string sResult = string.Empty;
            try
            {
                StringBuilder strSql = new StringBuilder();
                strSql.Append(" Insert Into  " + "FMM_ADAMWISE_DI (");
                strSql.Append(" IP , ");
                strSql.Append(" Ch , ");
                strSql.Append(" Md , ");
                strSql.Append(" Val , ");
                strSql.Append(" Stat , ");
                strSql.Append(" Cnting , ");
                strSql.Append(" OvLch "); //最後一個參數不加入分隔符號
                strSql.Append(" ) ");
                strSql.Append(" Values ( ");

                strSql.Append("'" + di.IP + "' , ");
                strSql.Append("'" + di.Ch + "' , ");
                strSql.Append("'" + di.Md + "' , ");
                strSql.Append("'" + di.Val + "' , ");
                strSql.Append("N'" + di.Stat + "' , ");
                strSql.Append("N'" + di.Cnting + "' , ");
                strSql.Append("'" + di.OvLch + "'"); //最後一個參數不加入分隔符號
                strSql.Append(" ) ");

                ds = pb_ws.ExcuteSQL_Query("CIMDB_" + sEnvironment, strSql.ToString(), "ADAMWISE_JOB");
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    sResult = "SUCCESS";
                }

           
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

        public string InsertDataToDODB(DO MyDo)
        {
            DataSet ds = null;
            string sResult = string.Empty;
            try
            {
                StringBuilder strSql = new StringBuilder();
                strSql.Append(" Insert Into  " + "FMM_ADAMWISE_DO (");
                strSql.Append(" IP , ");
                strSql.Append(" Ch , ");
                strSql.Append(" Md , ");
                strSql.Append(" Val , ");
                strSql.Append(" Stat , ");
                strSql.Append(" PsCtn , ");
                strSql.Append(" PsStop , ");
                strSql.Append(" PsIV "); //最後一個參數不加入分隔符號
                strSql.Append(" ) ");
                strSql.Append(" Values ( ");

                strSql.Append("'" + MyDo.IP + "' , ");
                strSql.Append("'" + MyDo.Ch + "' , ");
                strSql.Append("'" + MyDo.Md + "' , ");
                strSql.Append("'" + MyDo.Val + "' , ");
                strSql.Append("N'" + MyDo.Stat + "' , ");
                strSql.Append("N'" + MyDo.PsCtn + "' , ");
                strSql.Append("N'" + MyDo.PsStop + "' , ");
                strSql.Append("'" + MyDo.PsIV + "'"); //最後一個參數不加入分隔符號
                strSql.Append(" ) ");

                ds = pb_ws.ExcuteSQL_Query("CIMDB_" + sEnvironment, strSql.ToString(), "ADAMWISE_JOB");
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    sResult = "SUCCESS";
                }


            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "";
        }

    }
}
