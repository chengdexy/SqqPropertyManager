using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace SqqPropertyManager
{
    public partial class Form1 : Form
    {
        public static SqlConnection cnn = new SqlConnection("Data Source=bds-001.hichina.com;Initial Catalog=bds0010431_db;User Id=bds0010431;Password=sfvhj2569; ");
        public static SqlCommand cmd;

        public static int curDeptID = 0;
        public static int curFuncID = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Dictionary<int, string> dicDept = new Dictionary<int, string>();
            cnn.Open();
            string sql = "select * from dept";
            cmd = new SqlCommand(sql, cnn);
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                dicDept.Add(Convert.ToInt32(reader["id"]), reader["deptname"].ToString());
            }
            BindingSource bsDept = new BindingSource();
            bsDept.DataSource = dicDept;
            comboBox1.DataSource = bsDept;
            comboBox1.DisplayMember = "value";
            comboBox1.ValueMember = "key";
            reader.Close();

            int[] parentIds;
            sql = "select distinct parentid from functions";
            cmd.CommandText = sql;
            reader = cmd.ExecuteReader();
            List<int> parentList = new List<int>();
            while (reader.Read())
            {
                parentList.Add(Convert.ToInt32(reader["parentid"]));
            }
            parentIds = parentList.ToArray<int>();
            reader.Close();

            Dictionary<int, string> dicFunc = new Dictionary<int, string>();
            sql = "select * from functions";
            cmd = new SqlCommand(sql, cnn);
            reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (!parentIds.Contains(Convert.ToInt32(reader["id"])))
                {
                    dicFunc.Add(Convert.ToInt32(reader["id"]), reader["funname"].ToString());
                }
            }
            BindingSource bsFunc = new BindingSource();
            bsFunc.DataSource = dicFunc;
            comboBox2.DataSource = bsFunc;
            comboBox2.DisplayMember = "value";
            comboBox2.ValueMember = "key";

            reader.Close();
            cmd.Dispose();
            cnn.Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAdd.Enabled = false;
            btnDel.Enabled = false;

            try
            {
                curDeptID = Convert.ToInt32(((KeyValuePair<int, string>)comboBox1.SelectedValue).Key);
            }
            catch
            {
                curDeptID = Convert.ToInt32(comboBox1.SelectedValue);
            }

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnAdd.Enabled = false;
            btnDel.Enabled = false;

            try
            {
                curFuncID = Convert.ToInt32(((KeyValuePair<int, string>)comboBox2.SelectedValue).Key);
            }
            catch
            {
                curFuncID = Convert.ToInt32(comboBox2.SelectedValue);
            }

        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (curDeptID != 0 && curFuncID != 0)
            {
                cnn.Open();
                string sql = $"select funids from dept where id={curDeptID}";
                cmd = new SqlCommand(sql, cnn);
                string funids = cmd.ExecuteScalar().ToString();
                string[] ids = funids.Split(',');
                if (ids.Contains<string>(curFuncID.ToString()))
                {
                    btnAdd.Enabled = false;
                    btnDel.Enabled = true;
                }
                else
                {
                    btnAdd.Enabled = true;
                    btnDel.Enabled = false;
                }
                cnn.Close();
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            cnn.Open();
            AddNewFunc(curFuncID);
            cnn.Close();
            MessageBox.Show($"已添加权限:{curFuncID }");
        }

        private static void AddNewFunc(int id)
        {
            //先检查此节点是否有父节点
            //如果有父节点,且父节点的父节点不为0,则先加入其父节点


            string sql2 = $"select * from functions where id={id}";
            cmd.CommandText = sql2;
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                int parentid = Convert.ToInt32(reader["parentid"]);
                reader.Close();
                if (parentid != 0)
                {
                    AddNewFunc(parentid);
                }

            }

            string sql = $"select funids from dept where id={curDeptID}";
            cmd.CommandText = sql;
            string[] ids = cmd.ExecuteScalar().ToString().Split(',');
            List<string> lst = ids.ToList<string>();
            lst.Add(id.ToString());
            ids = lst.ToArray<string>();
            string finalString = string.Join(",", ids);

            sql = $"update dept set funids='{finalString }' where id={curDeptID}";
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            cnn.Open();
            DelFun(curFuncID);
            cnn.Close();
            MessageBox.Show($"已删除权限:{curFuncID }");
        }

        private static void DelFun(int id)
        {
            //查看其父id
            int parentid = checkParent(id);
            //删除此项
            string sql = $"select funids from dept where id={curDeptID}";
            cmd.CommandText = sql;
            string[] ids = cmd.ExecuteScalar().ToString().Split(',');
            if (ids.Contains<string>(id.ToString()))
            {
                ids = ids.Where(x => x != id.ToString()).ToArray();
            }
            string finalString = string.Join(",", ids);

            sql = $"update dept set funids='{finalString }' where id={curDeptID}";
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            //检差是否还有同父的项
            bool hasBrother = false;
            if (parentid != 0)
            {
                foreach (string item in ids)
                {
                    int theID = Convert.ToInt32(item);
                    int theParentID = checkParent(theID);
                    if (theParentID == parentid)
                    {
                        //有
                        hasBrother = true;
                    }
                }
            }
            if (!hasBrother && parentid != 0)
            {
                //如果没兄弟节点
                DelFun(parentid);
            }
        }

        private static int checkParent(int id)
        {
            string sql2 = $"select parentid from functions where id={id}";
            cmd.CommandText = sql2;
            int parentid = Convert.ToInt32(cmd.ExecuteScalar());
            return parentid;
        }
    }
}
