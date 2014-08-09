using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Threading;
using System.Net;

namespace CheckBannedId
{

    public struct TAccount
    {
        public int id;
        public String host;
        public String code;
        public String refer;
        public int thread;
    }

    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            textBox1.SelectionStart = 0;
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog Open1 = new OpenFileDialog();
            Open1.Filter = "数据库文件|*.mdb";
            if (Open1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = Open1.FileName;
                textBox1.ForeColor = Color.Black;
                button1.Enabled = true;
            }
        }

        private string _mdbPath;
        private OleDbConnection _conn;
        public int _checkedCount = 0;
        public int _accountSum = 0;
        public int _bannedCount = 0;
        private List<TAccount> _list;


        public List<TAccount> GetAccountlist()
        {
            List<TAccount> accountlist = new List<TAccount>();

            OleDbCommand sql = _conn.CreateCommand();
            sql.CommandText = "SELECT * FROM Account";
            OleDbDataReader ret = sql.ExecuteReader();

            while (ret.Read())
            {
                TAccount account = new TAccount();
                account.id = (int)ret["ID"];
                account.code = ret["code"].ToString();
                account.host = ret["host"].ToString();
                account.refer = ret["refer"].ToString();
                account.thread = (int)ret["thread"];
                accountlist.Add(account);
            }

            ret.Close();
            return accountlist;
        }

        public bool SetAccountBanned(int id)
        {
            OleDbCommand sql = _conn.CreateCommand();
            sql.CommandText = "UPDATE Account SET `thread`=1 WHERE ID=" + id.ToString();
            return sql.ExecuteNonQuery() >= 1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _mdbPath = textBox1.Text;
            _conn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + _mdbPath);
            _conn.Open();
            _list = GetAccountlist();
            //======
            button1.Enabled = false;
            button1.Text = "正在验证...";
            label1.Text = "0 / " + _list.Count.ToString();
            progressBar1.Maximum = _list.Count;
            progressBar1.Value = 0;
            _accountSum = _list.Count;
            _checkedCount = 0;
            _bannedCount = 0;
            //======
            startWork();
            infoTimer.Start();
        }

        private void infoTimer_Tick(object sender, EventArgs e)
        {
            progressBar1.Value = _checkedCount;
            label1.Text = _checkedCount.ToString() + " / " + _accountSum.ToString();
            label4.Text = "被K的ID数量：" + _bannedCount.ToString();
            if (progressBar1.Value >= progressBar1.Maximum)
            {
                //验证完成
                button1.Enabled = true;
                button1.Text = "点击开始检查";
                label1.Text = "验证完毕";
                infoTimer.Stop();
            }
        }

        /// <summary>
        /// 开始验证
        /// </summary>
        private void startWork()
        {
            //ThreadPool.SetMaxThreads(250, 512); //最大线程250
            ThreadManager.init(200);
            ServicePointManager.DefaultConnectionLimit = 512;   //HTTP最大并发数

            foreach(TAccount acc in _list)
            {
                IdChecker checker = new IdChecker(this, acc);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(checker.run));
                ThreadManager.startOneThread(checker.run);
                //return;
            }
        }
       
    }
}
