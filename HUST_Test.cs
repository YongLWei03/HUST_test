﻿using HUST_Com;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;

namespace HUST_Test
{
    public partial class HUST_Test : Form
    {
        private string cfgFile = Application.StartupPath + "\\HUST_Config.xml", outPath = "";
        private DataSet cfgDS = new DataSet();
        private HUST_Univ.UniChart chart;       // 表单输出结果
        private List<HUST_Univ.UniChart> charts = new List<HUST_Univ.UniChart>(); // 表单输出结果集
        public string PosTableName = null; 

        public HUST_Test()
        { InitializeComponent(); }

        private void HUST_Test_Load(object sender, EventArgs e)
        {
            try
            {
                if (!HUSTLog.IsInitialized)   // 初始化HUST_Log.xml文件
                { HUSTLog.InitializeLog(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "HUST_Test_Log.xml"), "HUST_Test-ErrorLog"); }

                if (!HUST_TestFormInitialization()) this.Close();
            }
            catch (Exception ex)
            {
                HUSTLog.WriteLog(ex);
                this.Close();
            }
        }

        private void btnSheet_Click(object sender, EventArgs e) // 显示输出运行模拟结果表单
        {
            try
            {
                // 生成表单输出结果集List<UniChart> charts
                if (!GetListUniCharts("表单", cmbSheet.Text.Trim())) return;
                // 下面添加 <表单输出程序（输出结果图表存储路径：outPath；输出数据集：List<UniChart> charts）>
                /*DataTable[] dt = new DataTable[charts.Count];//需要将dataset数据集转化为datatable来处理数据
                int i = 0;//定义datatable索引初始值
                foreach (HUST_Univ.UniChart uc in charts)
                {
                    string[] titleName = uc.title.Split(' ');
                    if (!titleName[0].Contains(cmbSheet.SelectedItem.ToString())) continue;
                    dt[i] = uc.chart.Tables[0];
                    //dt[i].TableName = titleName[0];
                    string allStr = uc.title + "*" + uc.remark + "*" + uc.unit + "*" + uc.page;//将每张表的标题、备注、单位等信息传入到datatable里面供调用
                    dt[i].TableName = allStr;
                    i++;
                }*/
                HUST_OutPut.TableView tableview = new HUST_OutPut.TableView();
                tableview.Text = "输出表单数据结构";
                tableview.newTab(charts);
                tableview.Owner = this;
                tableview.StartPosition = FormStartPosition.CenterScreen;
                tableview.parentForm = this;
                tableview.Show();
            }
            catch (Exception ex)
            { HUSTLog.WriteLog(ex); }
        }

        private void btnPos_Click(object sender, EventArgs e)   // 显示输出电站工作位置图
        {
            try
            {
                // 生成电站工作位置输出结果集List<UniChart> charts
                if (!GetListUniCharts("位置", cmbPos.Text.Trim())) return;
                // 下面添加电站工作位置输出程序（输出结果图表存储路径：outPath；输出数据集：List<UniChart> charts）
                int tableCount = charts[0].chart.Tables.Count;
                DataTable[] dt = new DataTable[tableCount];//需要将dataset数据集转化为datatable来处理数据
                string Allstr = charts[0].title + "*" + charts[0].remark + "*" + charts[0].unit + "*" + charts[0].page;
                for (int i = 0; i < tableCount; i++)
                {
                    string[] titleName = charts[0].title.Split(' ');
                    if (!titleName[0].Contains(cmbPos.SelectedItem.ToString())) continue;
                    dt[i] = charts[0].chart.Tables[i];
                    dt[i].TableName = dt[i].TableName+"*"+Allstr;
                   // Console.WriteLine("dt[" + i + "].TableName:" + dt[i].TableName);
                }
                if (dt.Length != 3)
                {
                    return;
                }

                HUST_OutPut.FigureView figureView = new HUST_OutPut.FigureView(true, dt);
                figureView.Text = "输出电站工作位置图";
                figureView.newTab(Allstr);
                //figureView.Owner = this;
                figureView.StartPosition = FormStartPosition.CenterScreen;
                figureView.Show();
            }
            catch (Exception ex)
            { HUSTLog.WriteLog(ex); }
        }

        private void btnCurve_Click(object sender, EventArgs e) // 显示输出电站发电曲线图
        {
            try
            {
                // 生成电站发电曲线输出结果集List<UniChart> charts
                if (!GetListUniCharts("曲线", cmbCurve.Text.Trim())) return;
                //foreach (UniChart cht in charts)  // 调试
                //{
                //  foreach (DataTable tbl in cht.chart.Tables)
                //  { txtPath.Text += "  " + tbl.Rows[4][2].ToString(); }
                //}

                // 下面添加电站发电曲线输出程序（输出结果图表存储路径：outPath；输出数据集：List<UniChart> charts）
            }
            catch (Exception ex)
            { HUSTLog.WriteLog(ex); }
        }

        private void button1_Click(object sender, EventArgs e)  // 退出
        { this.Close(); }

        #region 函数集

        private bool GetListUniCharts(string flag, string ttl)  // 生成图表输出数据集
        {
            try
            {
                charts.Clear();
                DataRow[] rows = cfgDS.Tables["TEST"].Select("Flag = '" + flag + "'");
                foreach (DataRow row in rows)
                {
                    chart = new HUST_Univ.UniChart();
                    if (flag == "表单")
                    {
                        if (row["Title"].ToString().Trim().Substring(0, row["Title"].ToString().Trim().IndexOf("-")) != ttl) continue;
                    }
                    else
                    {
                        if (row["Title"].ToString().Trim().Substring(0, row["Title"].ToString().Trim().IndexOf(" ")) != ttl) continue;
                    }

                    chart.title = row["Title"].ToString().Trim();
                    string file = outPath + chart.title.Substring(0, row["Title"].ToString().Trim().IndexOf(" ")).Trim() + ".xml";
                    if (!File.Exists(file)) continue;
                    chart.chart = new DataSet();
                    chart.chart.ReadXml(file);
                    chart.remark = row["Remark"].ToString().Trim();
                    chart.unit = row["Unit"].ToString().Trim();
                    chart.page = Convert.ToInt32(row["Page"]);
                    charts.Add(chart);
                }
                return true;
            }
            catch (Exception ex)
            {
                HUSTLog.WriteLog(ex);
                return false;
            }
        }

        private void btnPath_Click(object sender, EventArgs e)  // 改变图表文件目录，更新并保存cfgDS
        {
            try
            {
                string[] str = cfgDS.Tables["TEST"].Rows[1]["Title"].ToString().Split(' ');//空格分割字符串
                string openFile = txtPath + str[0] + ".xml";
                FolderBrowserDialog dlg = new FolderBrowserDialog();
                dlg.Description = "请选择数据所在的文件夹";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    outPath = dlg.SelectedPath + "\\";
                    txtPath.Text = outPath;
                    cfgDS.Tables["TEST"].Rows[0]["Title"] = outPath.Trim();
                    // 保存HUST_Config.xml
                    FileStream fil0 = new FileStream(cfgFile, FileMode.Create);
                    StreamWriter write = new StreamWriter(fil0);
                    cfgDS.WriteXml(write, XmlWriteMode.WriteSchema);
                    write.Close();
                    fil0.Close();
                }
                dlg.Dispose();
            }
            catch (Exception ex)
            { HUSTLog.WriteLog(ex); }
        }

        private bool HUST_TestFormInitialization()
        {
            try
            {
                #region 打开cfgDS文件

                if (!File.Exists(cfgFile))
                {
                    return false;
                }

                cfgDS.ReadXml(cfgFile);

                if (!cfgDS.Tables.Contains("TEST"))
                {
                    return false;
                }

                #endregion 打开cfgDS文件

                #region 窗体选项初始化

                outPath = cfgDS.Tables["TEST"].Rows[0]["Title"].ToString().Trim();
                txtPath.Text = outPath;
                // 模拟结果表单选项初始化
                DataRow[] rows = cfgDS.Tables["TEST"].Select("Flag = '表单'");
                foreach (DataRow row in rows)
                {
                    string str = row["Title"].ToString().Trim().Substring(0, row["Title"].ToString().Trim().IndexOf("-"));
                    if (!cmbSheet.Items.Contains(str))
                    {
                        cmbSheet.Items.Add(str);
                    }
                }
                if (cmbSheet.Items.Count > 0) cmbSheet.Text = cmbSheet.Items[0].ToString().Trim();
                // 工作位置选项初始化说明
                rows = cfgDS.Tables["TEST"].Select("Flag = '位置'");
                foreach (DataRow row in rows)
                {
                    string[] s = row["Title"].ToString().Split(' ');
                    string str = s[0];
                    if (!cmbPos.Items.Contains(str))
                    {
                        cmbPos.Items.Add(str);
                    }
                }
                if (cmbPos.Items.Count > 0) cmbPos.Text = cmbPos.Items[0].ToString().Trim();
                // 发电曲线选项初始化
                rows = cfgDS.Tables["TEST"].Select("Flag = '曲线'");
                foreach (DataRow row in rows)
                {
                    string[] s = row["Title"].ToString().Split(' ');
                    string str = s[0];
                    if (!cmbCurve.Items.Contains(str))
                    {
                        cmbCurve.Items.Add(str);
                    }
                }
                if (cmbCurve.Items.Count > 0) cmbCurve.Text = cmbCurve.Items[0].ToString().Trim();

                #endregion 窗体选项初始化

                return true;
            }
            catch (Exception ex)
            {
                HUSTLog.WriteLog(ex);
                return false;
            }
        }

        #endregion 函数集
    }
}