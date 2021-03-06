﻿using DevComponents.DotNetBar;
using HUST_Aux;
using HUST_Com;
using ProS_Assm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace HUST_OutPut
{
    //生成电站工作位置图类
    public partial class FigureView : DevComponents.DotNetBar.Office2007Form
    {
        //public Form parentForm; //不再使用这个参数，不需要对父窗口进行操作了
        private progress myprogress;

        private List<Brush> fillBrushes = null;
        private List<string> brushDescriptions = null;
        private List<int> flags = null;
        private bool defaultUnit = true;
        private int COLUMNSCOUNT = 24;
        private MyPictureBox myPictureBox;
        private DataTable dt_maps, dt_ldcs, dt_styl;
        private DataTable[] dts_myDatas;
        private String[] colorAndHatchStyle;
        private Dictionary<string, MyDrawer> myDrawers;
     
        public void progressB()
        {
            this.myprogress = new progress();
            myprogress.Start();
            myprogress.ShowDialog();
        }

        public FigureView(bool unitFlg, DataTable[] d)
        {
            defaultUnit = unitFlg;
            Preprocessing(d);
            InitializeComponent();
        }

        #region 预处理部分

        /* 目的： 将MAPS,LDCS,STYL和对应的DataTable对应起来
         *
         * **/

        private void Preprocessing(DataTable[] dts)
        {
            dts_myDatas = dts;
            // 绑定数据源
            foreach (DataTable d in dts_myDatas)
            {
                string[] tables = d.TableName.Split('*');

                if (tables[0] == "MAPS")
                {
                    dt_maps = d.Copy();
                    continue;
                }
                else if (tables[0] == "LDCS")
                {
                    dt_ldcs = d.Copy();
                    continue;
                }
                else if (tables[0] == "STYL")
                {
                    dt_styl = d.Copy();

                    continue;
                }
                
            }

            // 得到每个行对应的刷子
            styleDataAnalysis();
        }

        #endregion 预处理部分

        public void newTab(string str)
        {

            string[] name = str.Split(' ');
            TabItem tp = this.tabControl1.CreateTab(name[0]);
            TabControlPanel tcp = new TabControlPanel();
            tcp.Visible = false;
            tcp.TabItem = tp;
            tcp.AutoScroll = true;
            tcp.Dock = DockStyle.Fill;

            tcp.Resize += new System.EventHandler(this.pictureBox_Resize);

            myPictureBox = new MyPictureBox();
            tcp.Controls.Add(myPictureBox);

            myPictureBox.AutoScrollOffset = new Point(100, 100);
            tcp.ScrollControlIntoView(myPictureBox);

            //属性设置
            myPictureBox.LevelLines = dt_ldcs;
            myPictureBox.genPos = dt_maps;
            myPictureBox.pageSettings = new System.Drawing.Printing.PageSettings();
            PageSettings pageSettings = myPictureBox.pageSettings;
            myPictureBox.Width = (int)(pageSettings.PaperSize.Width / 100.0 * 96);
            myPictureBox.Height = (int)(pageSettings.PaperSize.Height / 100.0 * 96);
            int xDraw = (int)(pageSettings.Margins.Left / 254.0 * 96);
            int yDraw = (int)(pageSettings.Margins.Top / 254.0 * 96);
            int widthDraw = (int)(pageSettings.PaperSize.Width / 100.0 * 96) -
                (int)((pageSettings.Margins.Left + pageSettings.Margins.Right) / 254.0 * 96);
            int heightDraw = (int)(pageSettings.PaperSize.Height / 100.0 * 96) -
                (int)((pageSettings.Margins.Top + pageSettings.Margins.Bottom) / 254.0 * 96);
            myPictureBox.drawArea = new Rectangle(xDraw, yDraw, widthDraw, heightDraw);
            myPictureBox.logoPos = new Rectangle(myPictureBox.drawArea.Left + (int)(myPictureBox.drawArea.Width * 0.5)
                , myPictureBox.drawArea.Bottom - (int)(myPictureBox.drawArea.Height * 0.4)
                , 300
                , (myPictureBox.LogoItems.Count + 1) / 2 * 50);

            foreach (DataColumn col in myPictureBox.genPos.Columns)
            {
                string[] colNames = col.ColumnName.Split(new char[] { '.' });
                col.ColumnName = colNames[colNames.Length - 1];
            }

            myPictureBox.ContextMenuStrip = contextMenuStrip1;

            //事件处理
            myPictureBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseWheel);
            myPictureBox.MouseLeave += new System.EventHandler(this.pictureBox_MouseLeave);
            myPictureBox.MouseEnter += new System.EventHandler(this.pictureBox_MouseEnter);
            myPictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseMove);
            myPictureBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseDown);
            myPictureBox.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseUp);
            myPictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox_Paint);
            myPictureBox.Resize += new System.EventHandler(this.pictureBox_Resize);
            //pictureBox.Visible = false;

            this.tabControl1.Controls.Add(tcp);
            tp.AttachedControl = tcp;
        }

        //暂时不再使用这个函数，也就是不需要关闭窗口后显示父窗口了
        //private void TableView_FormClosed(object sender, FormClosedEventArgs e)
        //{
        //    if (this.parentForm != null)
        //    {
        //        this.parentForm.Visible = true;
        //    }
        //}

        private double GenerateMaxValue(DataTable dt)
        {
            double max = 0.0;

            for (int i = 1; i < dt.Columns.Count; i++)
                if (max < Convert.ToDouble(dt.Rows[0][i]))
                    max = Convert.ToDouble(dt.Rows[0][i]);

            for (int i = 1; i < dt.Columns.Count; i++)
                if (max < Convert.ToDouble(dt.Rows[dt.Rows.Count - 1][i]))
                    max = Convert.ToDouble(dt.Rows[dt.Rows.Count - 1][i]);
            //有时数据会远小于200，加此偏移会影响数据显示效果故注释 孙凯 2016.1.20
            max += 200;
            int tmp = (int)Math.Floor(max / 100);
            return tmp * 100;
        }
        //产生flag21与Yx比较的点集
        private Point[] GeneratePoints(MyPictureBox picture, DataRow[] row,int compareNumber )
        {
            //DataRow row = picture.LevelLines.Rows[rowIndex];
            Point[] points = new Point[COLUMNSCOUNT * 2];//横轴上一天有48个点需要显示
            double max = GenerateMaxValue(picture.LevelLines);
            int h = (int)Math.Floor(max);
            double step = h / (picture.drawArea.Height * 0.9 / 10.2 * 10);
            for (int i = 1; i <= COLUMNSCOUNT; i++)
            {
                double val = Convert.ToDouble(row[0][i]);
                val = (val > compareNumber) ? compareNumber : val;
                if (step == 0)
                    points[i * 2 - 2].Y = 0;
                else
                    points[i * 2 - 2].Y = (int)(val / step);

                points[i * 2 - 1].Y = points[2 * i - 2].Y;
            }
            return points;
        }
        //产生flag55—0的点集
        private Point[] GeneratePoints(MyPictureBox picture, int rowIndex)
        {
            DataRow row = picture.LevelLines.Rows[rowIndex];
            Point[] points = new Point[COLUMNSCOUNT * 2];//横轴上一天有48个点需要显示
            double max = GenerateMaxValue(picture.LevelLines);
            int h = (int)Math.Floor(max);
            double step = h / (picture.drawArea.Height * 0.9 / 10.2 * 10); //step表示画布和坐标的比例
            for (int i = 1; i <= COLUMNSCOUNT; i++)
            {
                double val = Convert.ToDouble(row[i]);
               
                if (step == 0)
                    points[i * 2 - 2].Y = 0;
                else
                    points[i * 2 - 2].Y = (int)(val / step);
                points[i * 2 - 1].Y = points[2 * i - 2].Y;
            }
           // for(int i =0;i<48;i++)
           //{
           //   Console.WriteLine("points["+i+"].Y:"+points[i].Y*step);
           //}
            return points;
        }

        #region 貌似无用代码
        private string GenerateGenIDs(List<int> types)
        {
            DataView dv = UniVars.InDS.Tables["系统表"].DefaultView;
            dv.RowFilter = "节点类型 >=300 and 节点类型<400";
            string result = "(";
            for (int i = 0; i < dv.Count; i++)
            {
                if (!selectGenIDs.Contains(i) && types.Contains(Convert.ToInt32(dv[i]["节点类型"])))
                    result += i + ",";
            }
            if (result.Length > 1)
                result = result.Substring(0, result.Length - 1);
            else
                result += "-1";
            result += ")";
            return result;
        }

        private List<double> GenerateIntervals(DataView dv)
        {
            List<double> result = new List<double>();
            double up = -10000, low = -1;

            for (int i = 0; i < dv.Count; i++)
                if (Convert.ToDouble(dv[i]["Yn"]) - 10 <= up)
                    up = Convert.ToDouble(dv[i]["Yx"]);
                else
                {
                    result.Add(up);
                    low = Convert.ToDouble(dv[i]["Yn"]);
                    up = Convert.ToDouble(dv[i]["Yx"]);
                    result.Add(low);
                }
            result.Add(up);
            result.RemoveAt(0);
            return result;
        }

        private void DrawIntervals(List<double> intervals, MyPictureBox picture, Graphics g, Brush brush)
        {
            DataRow row = null;
            int variableItemCount = GetBrushCountByType(0);
            int commonItemCount = GetBrushCountByType(1);
            //获取电力不足曲线对应的行
            if (commonItemCount == picture.LevelLines.Rows.Count - 1)
                row = picture.LevelLines.Rows[1];
            else
                row = picture.LevelLines.Rows[variableItemCount + 1];
            string tmp = row[0].ToString();
            double max = GenerateMaxValue(picture.LevelLines);
            int h = (int)Math.Floor(max);

            double ystep = h / (picture.drawArea.Height * 0.9 / 10.2 * 10);
            int xstep = (int)(picture.drawArea.Width * 0.87 / 24);

            if (ystep == 0)
                return;
            Point zeroPoint = new Point(picture.drawArea.Left + (int)(picture.drawArea.Width * 0.1), picture.drawArea.Top + (int)(picture.drawArea.Height * 0.9));

            for (int i = 0; i < intervals.Count / 2; i++)
            {
                List<Point> polygen = new List<Point>();
                for (int j = 1; j < COLUMNSCOUNT; j++)
                {
                    Point firstPoint = new Point(zeroPoint.X, zeroPoint.Y);
                    firstPoint.X += (int)((j - 1) * xstep);
                    firstPoint.Y -= (int)(intervals[2 * i] / ystep);
                    polygen.Add(firstPoint);

                    while (j < COLUMNSCOUNT)
                    {
                        double top = Convert.ToDouble(row[j]);
                        if (intervals[2 * i] > top)
                        {
                            break;
                        }
                        else
                        {
                            Point topLeft = new Point(zeroPoint.X, zeroPoint.Y);
                            topLeft.X += (int)((j - 1) * xstep);
                            if (intervals[2 * i + 1] < top)
                            {
                                topLeft.Y -= (int)(intervals[2 * i + 1] / ystep);
                            }
                            else
                            {
                                topLeft.Y -= (int)(top / ystep);
                            }
                            polygen.Add(topLeft);
                            polygen.Add(new Point(topLeft.X + xstep, topLeft.Y));
                            j++;
                        }
                    }
                    Point lastPoint = new Point(zeroPoint.X, zeroPoint.Y);
                    lastPoint.X += (int)((j - 1) * xstep);
                    lastPoint.Y -= (int)(intervals[2 * i] / ystep);
                    polygen.Add(lastPoint);

                    if (polygen.Count >= 4)
                        g.FillPolygon(brush, polygen.ToArray());
                    polygen.Clear();
                }
            }
        }

        private void DrawGenAreas(MyPictureBox picture, Graphics g)
        {
            DataView dv = new DataView();
            dv.Table = picture.genPos;
            dv.Sort = "Yn ASC";

            int brushIndex = 0;

            //水电
            //dv.RowFilter = "gID in " + GenerateGenIDs(new List<int>() { 307 });
            List<double> intervals = GenerateIntervals(dv);
            DrawIntervals(intervals, picture, g, fillBrushes[brushIndex]);
            if (intervals.Count > 0)
            {
                AddLogoItemWithCheck(picture, brushIndex);
            }
            brushIndex++;

            //核电
            dv.RowFilter = "gID in " + GenerateGenIDs(new List<int>() { 306 });
            intervals = GenerateIntervals(dv);
            DrawIntervals(intervals, picture, g, fillBrushes[brushIndex]);
            if (intervals.Count > 0)
            {
                AddLogoItemWithCheck(picture, brushIndex);
            }
            brushIndex++;

            //火电
            //dv.RowFilter = "gID in " + GenerateGenIDs(new List<int>() { 300, 301, 302 });
            intervals = GenerateIntervals(dv);
            DrawIntervals(intervals, picture, g, fillBrushes[brushIndex]);
            if (intervals.Count > 0)
            {
                AddLogoItemWithCheck(picture, brushIndex);
            }
            brushIndex++;

            //新能源
            /*
            dv.RowFilter = "gID in " + GenerateGenIDs(new List<int>() { 309, 310 });
            intervals = GenerateIntervals(dv);
            DrawIntervals(intervals, picture, g, fillBrushes[brushIndex]);
            if (intervals.Count > 0)
            {
                AddLogoItemWithCheck(picture, brushIndex);

                LogoItem item=new LogoItem();
                item.brush = fillBrushes[brushIndex];
                item.description = brushDescriptions[brushIndex];
                             item.priority = priorities[brushIndex];
                picture.LogoItems.Add(item);

                if (!bDelete)
                    bDelete = CheckToOverlapped(picture, intervals[intervals.Count - 1]);
            }*/
            brushIndex++;

            //抽蓄
            // dv.RowFilter = "gID in " + GenerateGenIDs(new List<int>() { 308 });
            intervals = GenerateIntervals(dv);
            DrawIntervals(intervals, picture, g, fillBrushes[brushIndex]);
            if (intervals.Count > 0)
            {
                AddLogoItemWithCheck(picture, brushIndex);
            }
            brushIndex++;
        }

        private List<int> GenerateSelectGenIDs()
        {
            List<int> indexes = new List<int>();

            return indexes;
        }

        private List<int> selectGenIDs = null;
        #endregion
 
        #region 此处可能修改以作图例
        private void DrawSelectGens(MyPictureBox picture, Graphics g)
        {
            Console.WriteLine("selectGenIDs.Count:"+ selectGenIDs.Count);
            for (int i = 0; i < selectGenIDs.Count; i++)
            {
                DataView dv = new DataView();
                dv.Table = picture.genPos;
                dv.Sort = "Yn ASC";

                dv.RowFilter = "gID =" + selectGenIDs[i];
                List<double> intervals = GenerateIntervals(dv);

                int fixedItemCount = 0;
                fixedItemCount += GetBrushCountByType(0);
                fixedItemCount += GetBrushCountByType(1);

                DrawIntervals(intervals, picture, g, fillBrushes[i + fixedItemCount]);
                if (intervals.Count > 0)
                {
                    LogoItem item = new LogoItem();
                    item.brush = fillBrushes[i + fixedItemCount];
                    item.name = brushDescriptions[i + fixedItemCount];
                    item.priority = flags[fixedItemCount];
                    picture.LogoItems.Add(item);
                }
            }
        }
        #endregion

        //画坐标轴
        private void DrawCoordinates(MyPictureBox picture, Graphics g)
        {
            Pen coordinatePen = new Pen(Brushes.Black, 3.5F);

            Point zeroPoint = new Point(picture.drawArea.Left + (int)(picture.drawArea.Width * 0.1), picture.drawArea.Top + (int)(picture.drawArea.Height * 0.9));
            //Console.WriteLine("zeroPoint:" + zeroPoint);
            Point xAxisEnd = new Point(picture.drawArea.Right, zeroPoint.Y);
            Point yAxisEnd = new Point(zeroPoint.X, picture.drawArea.Top);

            g.DrawLine(coordinatePen, zeroPoint, xAxisEnd);
            g.DrawLine(coordinatePen, zeroPoint, yAxisEnd);

            coordinatePen.Dispose();

            Font drawFont = new Font("宋体", picture.smallFontSize);
            Font drawFont1 = new Font("宋体", picture.smallestFontSize);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            Pen drawAxisPen = new Pen(Brushes.Black, 1.5F);
            Pen drawLinePen = new Pen(Brushes.Black, 3F);
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;

            //绘制X轴
            int step = (int)(picture.drawArea.Width * 0.87 / COLUMNSCOUNT);//原为0.87
            for (int i = 0; i <= COLUMNSCOUNT; i++)
            {
                g.DrawLine(drawAxisPen, zeroPoint.X + (int)(i * step), zeroPoint.Y, zeroPoint.X + (int)(i * step), zeroPoint.Y - 5);//X轴上的辅助标尺线长度减半——2018.7.4
                if(i%4==0)
                {
                    if(i!= COLUMNSCOUNT)
                    {
                        int m = i / 2;
                        g.DrawString(m.ToString(), drawFont, drawBrush, zeroPoint.X + (int)(i * step), zeroPoint.Y + 3, stringFormat);
                    }
                    
                }
            }
            g.DrawLine(drawLinePen, zeroPoint.X + (int)(COLUMNSCOUNT * step), zeroPoint.Y, zeroPoint.X + (int)(COLUMNSCOUNT * step), zeroPoint.Y - 10);

            Point[] arrow = new Point[]{
                new Point(xAxisEnd.X-10,xAxisEnd.Y-5),
                new Point(xAxisEnd.X-10,xAxisEnd.Y+5)
            };
            g.DrawLine(drawLinePen, xAxisEnd, arrow[0]);
            g.DrawLine(drawLinePen, xAxisEnd, arrow[1]);
            //X轴下的“时”放在箭头附近——2018.7.4
            g.DrawString("时", drawFont, drawBrush, zeroPoint.X + (int)(COLUMNSCOUNT * step)+16, zeroPoint.Y + 3);

            //绘制Y轴
            double ystep = picture.drawArea.Height * 0.9 / 10.2;
            double max = 0;

            max = GenerateMaxValue(picture.LevelLines);
            if (!defaultUnit)
                max = max / 10;

            int unit = (int)max / 10;
            for (int i = 1; i < 11; i++)
            {
                g.DrawLine(drawAxisPen, zeroPoint.X, zeroPoint.Y - (int)(i * ystep), zeroPoint.X - 5, zeroPoint.Y - (int)(i * ystep));
                int chars = (unit * i).ToString().Trim().Length;
                double delta = 0;
                if (chars >= 4)
                    delta = 2;

                int strWidth = (int)((unit * i).ToString().Trim().Length * (drawFont.SizeInPoints - delta));
                Rectangle rect = new Rectangle(
                    zeroPoint.X - strWidth, zeroPoint.Y - (int)(i * ystep) - drawFont.Height / 2, strWidth, drawFont.Height);
                g.DrawString((unit * i).ToString(), drawFont, drawBrush, rect, stringFormat);
            }
            arrow = new Point[]{
                new Point(yAxisEnd.X-5,yAxisEnd.Y+10),
                new Point(yAxisEnd.X+5,yAxisEnd.Y+10)
            };
            g.DrawLine(drawLinePen, yAxisEnd, arrow[0]);
            g.DrawLine(drawLinePen, yAxisEnd, arrow[1]);

            if (defaultUnit)
                g.DrawString("MW", drawFont, drawBrush, zeroPoint.X - 2 * drawFont.SizeInPoints, yAxisEnd.Y - drawFont.Height / 2);
            else
                g.DrawString("万kW", drawFont, drawBrush, zeroPoint.X - 4 * drawFont.SizeInPoints, yAxisEnd.Y - drawFont.Height / 2);

            foreach (TabItem ti in tabControl1.Tabs)
                if (ti.IsSelected)
                {
                    Font nameFont = new Font("宋体", picture.largeFontSize, FontStyle.Bold);
                    //string[] arrStr = "tes tte st test".Split(new char[] { ' ' });//ti.Tooltip.Split(new string[] { "\n" }, StringSplitOptions.None);
                    string[] xname = dt_ldcs.TableName.Split('*');
                    string name = xname[1];   //图名称
                    //if (arrStr[2].Trim() != "")
                        //name += "(" + arrStr[2] + ")";
                    g.DrawString(name, nameFont, drawBrush,
                        picture.drawArea.Left + (int)(picture.drawArea.Width * 0.1) + (int)(picture.drawArea.Width * 0.9 - 3 * nameFont.SizeInPoints) / 2
                        , picture.drawArea.Top + (int)(picture.drawArea.Height * 0.93), stringFormat);
                    nameFont.Dispose();
                }

            drawFont.Dispose();
            drawLinePen.Dispose();
            drawBrush.Dispose();
            drawAxisPen.Dispose();
        }

        /* 获取不同类型画刷的个数，其中
         * type=0:VariableItems
         * type=1:CommonItems
         */

        private int GetBrushCountByType(int type)
        {
            int result = 0;
            foreach (MyDrawer drawer in myDrawers.Values)
            {
                if (int.Parse(drawer.Mark) == type)
                {
                    result += 1;
                }
            }
            return result;
        }

        private void styleDataAnalysis()
        {
            DataRow[] rows = dt_styl.Select("Flag < 9999");

            myDrawers = new Dictionary<string, MyDrawer>();

            foreach (DataRow dr in rows)
            {
                String ARGB = dr["ARGB"].ToString();
                String Hatch = dr["Hatch"].ToString();
                String Name = dr["Name"].ToString();
                String Flag = dr["Flag"].ToString();
                String Mark = dr["Mark"].ToString();
                myDrawers.Add(Flag, new MyDrawer(Flag, ARGB, Hatch, Name, Mark));
            }

            DataRow[] dr1 = dt_styl.Select("Name = '日负荷'");
            string st = dr1[0]["ARGB"].ToString();
            COLUMNSCOUNT = int.Parse(st);
        }

        private List<Brush> GenerateFillBrushed()
        {
            List<Brush> brushes = new List<Brush>();
            brushDescriptions = new List<string>();
            flags = new List<int>();

            int recordLength = 5;
            colorAndHatchStyle = new string[dt_styl.Select("Flag < 9999").Length];
            int count = colorAndHatchStyle.Length / recordLength;
            for (int i = 0; i < count; i++)
            {
                #region 背景色

                if (colorAndHatchStyle[recordLength * i].Equals(""))
                {
                    continue;
                }
                string[] argbStr = colorAndHatchStyle[recordLength * i].Split(new char[] { ' ' });
                //存rg的4个字段
                int[] argb = new int[4];
                for (int index = 0; index < 4; index++)
                    argb[index] = Convert.ToInt32(argbStr[index]);
                // 得到对应颜色
                Color backColor = Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);

                #endregion 背景色

                HatchStyle hatchStyle = HatchStyle.Min;
                if (colorAndHatchStyle[recordLength * i + 1] != "")
                {
                    hatchStyle = (HatchStyle)Enum.Parse(typeof(HatchStyle), colorAndHatchStyle[recordLength * i + 1]);
                    brushes.Add(new HatchBrush(hatchStyle, Color.Black, backColor));
                }
                else
                {
                    brushes.Add(new SolidBrush(backColor));
                }

                brushDescriptions.Add(colorAndHatchStyle[recordLength * i + 2]);
                flags.Add(Convert.ToInt32(colorAndHatchStyle[recordLength * i + 3]));
            }
            return brushes;
        }

        #region  画矩形并填充风格和色彩(包括21号线的比较和0号虚线)
        private void DrawLevelLinesAndFill(MyPictureBox picture, Graphics g)
        {
            Pen thinPen = new Pen(Brushes.Black, 1.5F);
            Pen fatPen = new Pen(Brushes.Black, 2.0F);

            // 初始化点集
            Point zeroPoint = new Point(picture.drawArea.Left + (int)(picture.drawArea.Width * 0.1), picture.drawArea.Top + (int)(picture.drawArea.Height * 0.9));
            int step = (int)(picture.drawArea.Width * 0.87 / COLUMNSCOUNT);
           // Console.WriteLine("zeroPoint.Y:" + zeroPoint.Y);
            // List<Point[]> lineList = new List<Point[]>();
            Point[] bottomLine = new Point[COLUMNSCOUNT * 2];
            for (int i = 0; i < COLUMNSCOUNT; i++)
            {
                bottomLine[2 * i].X = zeroPoint.X + (int)(i * step);
                bottomLine[2 * i + 1].X = zeroPoint.X + (int)((i + 1) * step);
                bottomLine[2 * i].Y = bottomLine[2 * i + 1].Y = zeroPoint.Y;
            }
            // lineList.Add(bottomLine);

            // 将数据按照flag 排序，从大到小
            DataTable dtq = picture.LevelLines.Copy();
            DataView dv = dtq.DefaultView;
            dv.Sort = "FLAG desc";
            picture.LevelLines = dv.ToTable();

            // TestData.TestDataTable(picture.LevelLines);  // 测试数据

            # region 画填充矩形和特效线0
            for (int i = 0; i < picture.LevelLines.Rows.Count; i++)
            {
                string flag = picture.LevelLines.Rows[i][COLUMNSCOUNT + 1].ToString();

                Point[] points = GeneratePoints(picture, i);
                if (points != null)
                {
                    for (int j = 0; j < COLUMNSCOUNT; j++)
                    {
                        points[2 * j].X = zeroPoint.X + (int)(j * step);
                        points[2 * j + 1].X = zeroPoint.X + (int)((j + 1) * step);
                        points[2 * j].Y = (int)(zeroPoint.Y - points[2 * j].Y);
                        points[2 * j + 1].Y = (int)(zeroPoint.Y - points[2 * j + 1].Y);
                    }

                }
                if (!flag.Equals("0"))
                {
                    for (int idx = 0; idx < points.Length - 1; idx += 2)
                    {
                        Rectangle rec = new Rectangle(points[idx].X, points[idx].Y, bottomLine[idx + 1].X - points[idx].X, zeroPoint.Y - points[idx].Y);
                        g.FillRectangle(myDrawers[flag].brush, rec);
                    }
                    Pen drawPen = null;
                    drawPen = thinPen;
                    g.DrawLines(drawPen, points);
                    g.DrawLine(drawPen, points[points.Length虚线封闭 - 1], new Point(bottomLine[COLUMNSCOUNT * 2 - 1].X, zeroPoint.Y));
                }
                 else
                {   //虚线封闭——2018.7.4
                    Point[] extrapoint = new Point[2];
                    extrapoint[0].X = points[95].X;
                    extrapoint[0].Y = points[95].Y;
                    extrapoint[1].X = points[95].X;
                    extrapoint[1].Y = zeroPoint.Y;
                    //O号虚线的粗细thinPen,原来为fatPen——2018.7.4
                    DashStyle ds = thinPen.DashStyle;
                    thinPen.DashStyle = DashStyle.Dot;
                    g.DrawLines(thinPen, points);
                    g.DrawLines(thinPen, extrapoint);
                    thinPen.DashStyle = ds;

                }
            }
            #endregion
            
            #region 画线21和map线
            if(picture.genPos.Rows.Count != 0)
            {
                DataRow[] row = dt_ldcs.Select("Flag='21'");
                DataTable dtq1 = picture.genPos.Copy();
                DataView dv1 = dtq1.DefaultView;
                dv1.Sort = "Yx desc";
                picture.genPos = dv1.ToTable();
                for (int i = 0; i < picture.genPos.Rows.Count; i++)
                {
                    string flag = picture.genPos.Rows[i][0].ToString();
                    int compareNumber = int.Parse(picture.genPos.Rows[i]["Yx"].ToString());
                    Point[] points = GeneratePoints(picture, row,compareNumber);
                    //Console.WriteLine("flag:" + flag);
                    if (points != null)
                    {
                        for (int j = 0; j < COLUMNSCOUNT; j++)
                        {
                            points[2 * j].X = zeroPoint.X + (int)(j * step);
                            points[2 * j + 1].X = zeroPoint.X + (int)((j + 1) * step);
                            points[2 * j].Y = (int)(zeroPoint.Y - points[2 * j].Y);
                            points[2 * j + 1].Y = (int)(zeroPoint.Y - points[2 * j + 1].Y);
                        }
                    }

                    for (int idx = 0; idx < points.Length - 1; idx += 2)
                    {
                        Rectangle rec = new Rectangle(points[idx].X, points[idx].Y, bottomLine[idx + 1].X - points[idx].X, zeroPoint.Y - points[idx].Y);
                        g.FillRectangle(myDrawers[flag].brush, rec);
                    }
                    Pen drawPen = null;
                    drawPen = thinPen;
                    g.DrawLines(drawPen, points);
                    g.DrawLine(drawPen, points[points.Length - 1], new Point(bottomLine[COLUMNSCOUNT * 2 - 1].X, zeroPoint.Y));

                }
                //Console.WriteLine("6666666");
            }

            #endregion
            
            thinPen.Dispose();
            fatPen.Dispose();
        }
        #endregion

        private void AddLogoItemWithCheck(MyPictureBox picture, int brushIndex)
        {
            bool alreadyIn = false;
            foreach (LogoItem item in picture.LogoItems)
                if (item.name == brushDescriptions[brushIndex])
                    alreadyIn = true;
            if (!alreadyIn)
            {
                LogoItem item = new LogoItem();
                item.brush = fillBrushes[brushIndex];
                item.name = brushDescriptions[brushIndex];
                item.priority = flags[brushIndex];
                picture.LogoItems.Add(item);
            }
        }
        /*
        private void DrawSpecialLines(MyPictureBox picture, Graphics g)
        {
            Pen thinPen = new Pen(Brushes.Black, 2.0F);
            Pen fatPen = new Pen(Brushes.Black, 1.5F);

            Point zeroPoint = new Point(picture.drawArea.Left + (int)(picture.drawArea.Width * 0.1), picture.drawArea.Top + (int)(picture.drawArea.Height * 0.9));
            int step = (int)(picture.drawArea.Width * 0.87 / 24);

            List<Point[]> lineList = new List<Point[]>();
            Point[] bottomLine = new Point[48];
            for (int i = 0; i < 24; i++)
            {
                bottomLine[2 * i].X = zeroPoint.X + (int)(i * step);
                bottomLine[2 * i + 1].X = zeroPoint.X + (int)((i + 1) * step);
                bottomLine[2 * i].Y = bottomLine[2 * i + 1].Y = zeroPoint.Y;
            }
            lineList.Add(bottomLine);
            for (int j = 1; j < picture.LevelLines.Rows.Count; j++)
            {
                Point[] points = GeneratePoints(picture, j);
                if (points != null)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        points[2 * i].X = zeroPoint.X + (int)(i * step);
                        points[2 * i + 1].X = zeroPoint.X + (int)((i + 1) * step);
                        points[2 * i].Y = (int)(zeroPoint.Y - points[2 * i].Y);
                        points[2 * i + 1].Y = (int)(zeroPoint.Y - points[2 * i + 1].Y);
                    }
                    lineList.Add(points);
                }
            }

            for (int i = lineList.Count - 1; i > 0; i--)
            {
                Point[] upLine = lineList[i];
                Pen drawPen = null;
                if (picture.LevelLines.Rows[i - 1][0].ToString() == "23")
                {
                    drawPen = fatPen;
                    g.DrawLines(drawPen, upLine);
                    g.DrawLine(drawPen, upLine[47], new Point(upLine[47].X, zeroPoint.Y));
                }
            }

            Pen dashPen = new Pen(Color.Black, 1.0f);
            dashPen.DashStyle = DashStyle.Dash;
            {
                Point[] points = GeneratePoints(picture, 0);
                if (points != null)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        points[2 * i].X = zeroPoint.X + (int)(i * step);
                        points[2 * i + 1].X = zeroPoint.X + (int)((i + 1) * step);
                        points[2 * i].Y = (int)(zeroPoint.Y - points[2 * i].Y);
                        points[2 * i + 1].Y = (int)(zeroPoint.Y - points[2 * i + 1].Y);
                    }
                    g.DrawLines(dashPen, points);
                    g.DrawLine(dashPen, points[47], new Point(points[47].X, zeroPoint.Y)); ;
                }
            }

            thinPen.Dispose();
            fatPen.Dispose();
            dashPen.Dispose();
        }
        */
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            MyPictureBox picture = sender as MyPictureBox;

            try
            {
                Graphics g = e.Graphics; ;
                g.Clear(Color.White);

                picture.LogoItems.Clear();

                DrawLevelLinesAndFill(picture, g);
                AddLogoItem(picture);
               /* 
                if (picture.genPos.Rows.Count > 0)
                {
                  DrawGenAreas(picture, g);
                    DrawSelectGens(picture, g);
                }
               */
                DrawCoordinates(picture, g);

                //DrawSpecialLines(picture, g);

                DrawLogo(picture, g);

                //picture.drawed = true;
                ////// picture.Image = memImage;
            }
            catch (Exception ex)
            {
                ex.WriteLog();
                MessageBox.Show(ex.Message);
            }
        }
        private void AddLogoItem(MyPictureBox picture)
        {
            List<string> flagisshow = new List<string>();
            for (int i = 1; i < 9; i += 2)
            {
                if (Convert.ToInt16(dt_styl.Rows[i][dt_styl.Columns.Count - 1]) == 1 || Convert.ToInt16(dt_styl.Rows[i + 1][dt_styl.Columns.Count - 1]) == 1)
                {
                    flagisshow.Add(dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString());
                    flagisshow.Add(dt_styl.Rows[i + 1][dt_styl.Columns.Count - 2].ToString());
                    picture.LogoItems.Add(new LogoItem { brush = myDrawers[dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString()].brush, name = dt_styl.Rows[i][0].ToString(), priority = 1, secondBrush = myDrawers[dt_styl.Rows[i + 1][dt_styl.Columns.Count - 2].ToString()].brush, secondName = dt_styl.Rows[i + 1][0].ToString() });
                }
            }
            if (Convert.ToInt16(dt_styl.Rows[9][dt_styl.Columns.Count - 1]) == 1 || Convert.ToInt16(dt_styl.Rows[10][dt_styl.Columns.Count - 1]) == 1 || Convert.ToInt16(dt_styl.Rows[11][dt_styl.Columns.Count - 1]) == 1)
            {
                flagisshow.Add(dt_styl.Rows[9][dt_styl.Columns.Count - 2].ToString());
                flagisshow.Add(dt_styl.Rows[10][dt_styl.Columns.Count - 2].ToString());
                flagisshow.Add(dt_styl.Rows[11][dt_styl.Columns.Count - 2].ToString());
                picture.LogoItems.Add(new LogoItem { brush = myDrawers[dt_styl.Rows[9][dt_styl.Columns.Count - 2].ToString()].brush, name = dt_styl.Rows[9][0].ToString(), priority = 1, secondBrush = myDrawers[dt_styl.Rows[10][dt_styl.Columns.Count - 2].ToString()].brush, secondName = dt_styl.Rows[10][0].ToString() });
            }
            for (int i = 12; i < 16; i += 2)
            {
                if (Convert.ToInt16(dt_styl.Rows[i][dt_styl.Columns.Count - 1]) == 1 || Convert.ToInt16(dt_styl.Rows[i + 1][dt_styl.Columns.Count - 1]) == 1)
                {
                    flagisshow.Add(dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString());
                    flagisshow.Add(dt_styl.Rows[i + 1][dt_styl.Columns.Count - 2].ToString());
                    picture.LogoItems.Add(new LogoItem { brush = myDrawers[dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString()].brush, name = dt_styl.Rows[i][0].ToString(), priority = 1, secondBrush = myDrawers[dt_styl.Rows[i + 1][dt_styl.Columns.Count - 2].ToString()].brush, secondName = dt_styl.Rows[i + 1][0].ToString() });
                }
            }
            for (int i = 20; i < 25; i++)
            {
                if (Convert.ToInt16(dt_styl.Rows[i][dt_styl.Columns.Count - 1]) == 1&& !flagisshow.Contains(dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString()))
                {
                    flagisshow.Add(dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString());
                    picture.LogoItems.Add(new LogoItem { brush = myDrawers[dt_styl.Rows[i][dt_styl.Columns.Count - 2].ToString()].brush, name = dt_styl.Rows[i][0].ToString(), priority = 1, secondBrush = null, secondName = null });
                }
            }
            if (picture.genPos != null && picture.genPos.Rows.Count != 0)
            {
                for (int i = 0; i < picture.genPos.Rows.Count; i++)
                {
                    if (!flagisshow.Contains(picture.genPos.Rows[i][0].ToString()))
                    {
                        flagisshow.Add(picture.genPos.Rows[i][0].ToString());
                        picture.LogoItems.Add(new LogoItem { brush = myDrawers[picture.genPos.Rows[i][0].ToString()].brush, name = dt_styl.Select("Flag=" + picture.genPos.Rows[i][0].ToString())[0][0].ToString(), priority = 1, secondBrush = null, secondName = null });
                    }
                    
                }
            }
        }

        private string WrapLogoString(string originalStr)
        {
            int j = 0;
            string result = "";
            for (int i = 0; i < originalStr.Length; i++)
            {
                result += originalStr[i];
                j++;
                if (j == 4)
                {
                    result += "\n";
                    j = 0;
                }
            }
            return result;
        }

        private void SortLogo(MyPictureBox picture)
        {
            //Console.WriteLine(" picture.LogoItems.Count1:" + picture.LogoItems.Count);
            for (int i = 0; i < picture.LogoItems.Count; i++)
            {
                int minPriority = 10000;
                int index = -1;
                for (int j = i; j < picture.LogoItems.Count; j++)
                    if (picture.LogoItems[j].priority < minPriority)
                    {
                        minPriority = picture.LogoItems[j].priority;
                        index = j;
                    }
                LogoItem item = new LogoItem();
                item.brush = picture.LogoItems[index].brush;
                item.name = picture.LogoItems[index].name;
                item.priority = picture.LogoItems[index].priority;
                picture.LogoItems.RemoveAt(index);
                picture.LogoItems.Insert(i, item); ;
            }
        }

        private void DrawLogo(MyPictureBox picture, Graphics g)
        {
            //SortLogo(picture);
            LogoItem newItem = new LogoItem();
            newItem.priority = 0;
            newItem.brush = new SolidBrush(Color.SkyBlue);
            newItem.name = "原始负荷";
            picture.LogoItems.Insert(0, newItem);
            SolidBrush backBrush = new SolidBrush(Color.White);

            Font drawFont = new Font("宋体", picture.smallFontSize);
            SolidBrush drawBrush = new SolidBrush(Color.Black);
            Pen framePen = new Pen(Color.Black, 1.0f);
            Pen dashPen = new Pen(Color.Black, 1.0f);
            dashPen.DashStyle = DashStyle.Dash;

            int vacant = 10;
            Font titleFont = new Font("宋体", picture.largeFontSize, FontStyle.Bold);
            picture.logoPos = new Rectangle(picture.logoPos.Left
                , picture.logoPos.Top
                , picture.logoWidth
                , (picture.LogoItems.Count + 1) / 2 * (drawFont.Height * 2 + vacant) + vacant + titleFont.Height + vacant + 3);

            g.FillRectangle(backBrush, picture.logoPos);
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;

            int itemWidth = picture.logoPos.Width / 2;

            g.DrawString("图例", titleFont, drawBrush, picture.logoPos.Left + picture.logoPos.Width / 2, picture.logoPos.Top + vacant, stringFormat);
            Pen pen = new Pen(Color.Black);
            g.DrawLine(pen, picture.logoPos.Left, picture.logoPos.Top + vacant + titleFont.Height + 3,
                picture.logoPos.Right, picture.logoPos.Top + vacant + titleFont.Height + 3);
            Point startPoint = new Point(picture.logoPos.Left, picture.logoPos.Top + vacant + titleFont.Height + 3);
            //Console.WriteLine(" picture.LogoItems.Count:" +picture.LogoItems.Count);
            for (int i = 0; i < picture.LogoItems.Count; i++)
            {
                Point point = new Point(startPoint.X + 5 + (i % 2) * itemWidth,
                    startPoint.Y + (i / 2) * (drawFont.Height * 2 + vacant) + vacant);
                if (i > 0)
                {
                    if (picture.LogoItems[i].secondBrush != null)
                    {
                        g.FillRectangle(picture.LogoItems[i].secondBrush, point.X, point.Y + drawFont.Height, 40, drawFont.Height);
                        g.FillRectangle(picture.LogoItems[i].brush, point.X, point.Y, 40, drawFont.Height);
                    }
                    else
                    {
                        g.FillRectangle(picture.LogoItems[i].brush, point.X, point.Y, 40, drawFont.Height * 2);
                    }
                    g.DrawString(WrapLogoString(picture.LogoItems[i].name), drawFont, drawBrush, point.X + 45, point.Y);
                    if (picture.LogoItems[i].secondName != null)
                    {
                        g.DrawString(WrapLogoString(picture.LogoItems[i].secondName), drawFont, drawBrush, point.X + 45, point.Y + drawFont.Height);
                    }
                }
                else
                {
                    g.FillRectangle(picture.LogoItems[i].brush, point.X, point.Y + drawFont.Height, 40, drawFont.Height);
                    g.DrawLine(framePen, point.X, point.Y + drawFont.Height, point.X + 40, point.Y + drawFont.Height);

                    PointF[] points = new PointF[]
                    {
                        new PointF(point.X, point.Y + (float)drawFont.Height*3.0f/2),
                        new PointF(point.X + 20, point.Y + (float)drawFont.Height*3.0f/2),
                        new PointF(point.X + 20, point.Y+(float)drawFont.Height/2.0f),
                        new PointF(point.X + 40, point.Y+(float)drawFont.Height/2.0f)
                    };
                    g.DrawLines(dashPen, points);
                    g.DrawString(WrapLogoString(picture.LogoItems[i].name), drawFont, drawBrush, point.X + 45, point.Y);
                }
                g.DrawRectangle(framePen, point.X, point.Y, 40, drawFont.Height * 2);
            }

            stringFormat.Dispose();
            pen.Dispose();
            framePen.Dispose();
            drawBrush.Dispose();
            titleFont.Dispose();
            drawFont.Dispose();
        }

        private void pictureBox_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (ctrl)
            {
                MyPictureBox picture = sender as MyPictureBox;
                Panel panel = picture.Parent as Panel;
                panel.AutoScroll = false;
                if (e.Delta < 0)
                {
                    shrinkPicture(picture, panel);
                    panel.AutoScroll = true;
                }
                else
                {
                    amplifyPicture(picture, panel);
                }
            }
            else
            {
                Point mousePoint = new Point(e.X, e.Y);
                Panel panel = (sender as PictureBox).Parent as Panel;
                mousePoint.Offset(this.Location.X, this.Location.Y);
                if (panel.RectangleToScreen(panel.DisplayRectangle).Contains(mousePoint))
                {
                    panel.AutoScrollPosition = new Point(0, panel.VerticalScroll.Value - e.Delta);
                }
            }
        }

        private static void amplifyPicture(MyPictureBox picture, Panel panel)
        {
            if (picture.Width * 1.1 <= 1400 && picture.Height * 1.1 <= 2400)
            {
                picture.Size = new Size((int)(picture.Width * 1.1), (int)(picture.Height * 1.1));
                panel.AutoScroll = true;
                picture.smallFontSize = (float)(picture.smallFontSize * 1.1);
                picture.largeFontSize = (float)(picture.largeFontSize * 1.1);
                picture.logoWidth = (int)(picture.logoWidth * 1.1);
                picture.logoPos = new Rectangle((int)(picture.logoPos.Left * 1.1)
                    , (int)(picture.logoPos.Top * 1.1)
                    , picture.logoPos.Width
                    , picture.logoPos.Height); ;
                picture.drawArea = new Rectangle((int)(picture.drawArea.Left * 1.1),
                      (int)(picture.drawArea.Top * 1.1),
                      (int)(picture.drawArea.Width * 1.1),
                      (int)(picture.drawArea.Height * 1.1
                      ));
                picture.Invalidate();
            }
        }

        private static void shrinkPicture(MyPictureBox picture, Panel panel)
        {
            if (picture.Width * 0.9 > 300 && picture.Height * 0.9 > 500)
            {
                picture.Size = new Size((int)(picture.Width * 0.9), (int)(picture.Height * 0.9));
                panel.AutoScroll = true;
                picture.smallFontSize = (float)(picture.smallFontSize * 0.9);
                picture.largeFontSize = (float)(picture.largeFontSize * 0.9);
                picture.logoWidth = (int)(picture.logoWidth * 0.9);
                picture.logoPos = new Rectangle((int)(picture.logoPos.Left * 0.9)
                    , (int)(picture.logoPos.Top * 0.9)
                    , picture.logoPos.Width
                    , picture.logoPos.Height);
                picture.drawArea = new Rectangle((int)(picture.drawArea.Left * 0.9),
                    (int)(picture.drawArea.Top * 0.9),
                    (int)(picture.drawArea.Width * 0.9),
                    (int)(picture.drawArea.Height * 0.9));
                picture.Invalidate();
            }
        }

        private void pictureBox_MouseEnter(object sender, System.EventArgs e)
        {
            //((sender as PictureBox).Parent as Panel).Focus();
            (sender as PictureBox).Focus();
        }

        private void pictureBox_MouseLeave(object sender, System.EventArgs e)
        {
            // this.Focus();
        }

        //指示是否正在进行图例拖动
        private bool isDragPic = false;

        private void pictureBox_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                MyPictureBox picture = sender as MyPictureBox;
                Point mousePoint = new Point(e.X, e.Y);
                if (isDragPic == true) //2013-9-22 刘水兵：当鼠标拖离图例的时候，应该也是要进行移动的.所以 用一个变量指示 是否正在拖动
                {//if(picture.logoPos.Countains(mousePoint))
                    Point topleft = new Point(picture.logoPos.Left, picture.logoPos.Top);
                    picture.logoPos = new Rectangle(
                        picture.logoPos.Left + e.X - picture.previousPos.X,
                        picture.logoPos.Top + e.Y - picture.previousPos.Y,
                        picture.logoPos.Width,
                        picture.logoPos.Height);
                    picture.previousPos = mousePoint;
                    picture.Invalidate(new Rectangle(topleft.X, topleft.Y,
                        picture.Width + e.X - picture.previousPos.X, picture.logoPos.Height + e.Y - picture.previousPos.Y));
                }
            }
        }

        private void pictureBox_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MyPictureBox picture = sender as MyPictureBox;
            picture.previousPos = new Point(e.X, e.Y);

            //指示开始图例拖动
            if (e.Button == MouseButtons.Left && picture.logoPos.Contains(new Point(e.X, e.Y)))
                this.isDragPic = true;
        }

        private void pictureBox_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MyPictureBox picture = sender as MyPictureBox;

            //指示结束图例拖动
            if (e.Button == MouseButtons.Left)
                this.isDragPic = false;
        }

        private bool ctrl = false;

        private void tabControl1_KeyDown(object sender, KeyEventArgs e)
        {
            ctrl = e.Control;
        }

        private void tabControl1_KeyUp(object sender, KeyEventArgs e)
        {
            ctrl = e.Control;
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            Control picture;
            Control parent;
            if (sender is PictureBox || sender is Panel)
            {
                if (sender is PictureBox)
                {
                    picture = sender as Control;
                    parent = picture.Parent;
                }
                else
                {
                    parent = sender as Control;
                    picture = parent.Controls[0];
                }
                int x = picture.Width < parent.Width ? (parent.Width - picture.Width) / 2 : 0;
                int y = picture.Height < parent.Height ? (parent.Height - picture.Height) / 2 : 0;
                picture.Location = new Point(x, y);
            }
        }

        [DllImport("gdi32.dll")]

        #region 貌似无用代码
        private static extern bool BitBlt(
        IntPtr hdcDest, // handle to destination DC
        int nXDest, // x-coord of destination upper-left corner
        int nYDest, // y-coord of destination upper-left corner
        int nWidth, // width of destination rectangle
        int nHeight, // height of destination rectangle
        IntPtr hdcSrc, // handle to source DC
        int nXSrc, // x-coordinate of source upper-left corner
        int nYSrc, // y-coordinate of source upper-left corner
        System.Int32 dwRop // raster operation code
        );

        private const Int32 SRCCOPY = 0xCC0020;
        #endregion

        #region contextMenuStrip1插件部分
        private void SavePicture(PictureBox picture, string filename)
        {
            /*
            Graphics graphic = picture.CreateGraphics();
            Size s = picture.Size;
            memImage = new Bitmap(s.Width, s.Height, graphic);
            Graphics memGraphic = Graphics.FromImage(memImage);
            IntPtr dc1 = graphic.GetHdc();
            IntPtr dc2 = memGraphic.GetHdc();
            BitBlt(dc2, 0, 0, picture.Width,
            picture.Height, dc1, 0, 0, SRCCOPY);
            graphic.ReleaseHdc(dc1);
            memGraphic.ReleaseHdc(dc2);
            */
            MyPictureBox picBox = tabControl1.SelectedPanel.Controls[0] as MyPictureBox;
            MyPictureBox imagePic = CreatePictureFromSource(picBox);

            Bitmap memImage = new Bitmap(imagePic.Width, imagePic.Height);

            Graphics g = Graphics.FromImage(memImage);
            imagePic.LogoItems.Clear();

            g.Clear(Color.White);
            DrawLevelLinesAndFill(imagePic, g);
            /*
            if (imagePic.genPos.Rows.Count > 0)
            {
                DrawGenAreas(imagePic, g);
                DrawSelectGens(imagePic, g);
            }
            */
            DrawCoordinates(imagePic, g);

            //DrawSpecialLines(picBox, g);

            DrawLogo(imagePic, g);

            String picPath = "";
            if (filename == "")
                picPath = Application.StartupPath + "\\ListView.bmp";

            memImage.Save(filename);

            g.Dispose();
            memImage.Dispose();
        }

        private void 保存图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dlgSavePic.ShowDialog() == DialogResult.OK)
            {
                //开启进度条
                Thread thdSub = new Thread(new ThreadStart(this.progressB));
                thdSub.Start();
                Thread.Sleep(100);

                PictureBox picture = ((sender as ToolStripMenuItem).Owner as ContextMenuStrip).SourceControl as PictureBox;
                SavePicture(picture, dlgSavePic.FileName);

                //关闭进度条
                this.myprogress.isOver = true;

                MessageBox.Show("保存图片成功！");
            }
        }

        private void FigureView_Load(object sender, EventArgs e)
        {
        }

        private void 页面设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MyPictureBox picture = ((sender as ToolStripMenuItem).Owner as ContextMenuStrip).SourceControl as MyPictureBox;

            PageSetupDialog setupDialog = new PageSetupDialog();
            setupDialog.PageSettings = picture.pageSettings;
            setupDialog.PrinterSettings =
                new System.Drawing.Printing.PrinterSettings();
            if (setupDialog.ShowDialog() == DialogResult.OK)
            {
                PageSettings pageSettings = setupDialog.PageSettings;
                picture.pageSettings = pageSettings;
                int xDraw = (int)(pageSettings.Margins.Left / 254.0 * 96);
                int yDraw = (int)(pageSettings.Margins.Top / 254.0 * 96);
                int widthDraw = (int)(pageSettings.PaperSize.Width / 100.0 * 96) -
                    (int)((pageSettings.Margins.Left + pageSettings.Margins.Right) / 254.0 * 96);
                int heightDraw = (int)(pageSettings.PaperSize.Height / 100.0 * 96) -
                    (int)((pageSettings.Margins.Top + pageSettings.Margins.Bottom) / 254.0 * 96);
                picture.drawArea = new Rectangle(xDraw, yDraw, widthDraw, heightDraw);
                picture.Invalidate();
            }
            setupDialog.Dispose();
        }

        private void Print_Click(object sender, EventArgs e)
        {
            MyPictureBox picture = ((sender as ToolStripMenuItem).Owner as ContextMenuStrip).SourceControl as MyPictureBox;

            PrintDocument pd = new PrintDocument();
            //设置页面
            pd.DefaultPageSettings = picture.pageSettings;
            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            pd.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);

            // Allow the user to choose the page range he or she would
            // like to print.
            printDialog1.AllowSomePages = true;

            // Show the help button.
            printDialog1.ShowHelp = true;

            // Set the Document property to the PrintDocument for
            // which the PrintPage Event has been handled. To display the
            // dialog, either this property or the PrinterSettings property
            // must be set
            printDialog1.Document = pd;

            DialogResult result = printDialog1.ShowDialog();

            // If the result is OK then print the document.
            if (result == DialogResult.OK && printDialog1.Document != null)
            {
                pd.Print();
            }
            pd.Dispose();
            printDialog1.Document = null;
        }

        private MyPictureBox CreatePictureFromSource(MyPictureBox source)
        {
            MyPictureBox picture = new MyPictureBox();
            picture.LevelLines = source.LevelLines;
            picture.genPos = source.genPos;
            picture.pageSettings = source.pageSettings;
            picture.LogoItems = source.LogoItems;

            //设置图片大小和图例位置
            PageSettings pageSettings = picture.pageSettings;
            picture.Width = (int)(pageSettings.PaperSize.Width / 100.0 * 96);
            picture.Height = (int)(pageSettings.PaperSize.Height / 100.0 * 96);
            int xDraw = (int)(pageSettings.Margins.Left / 254.0 * 96);
            int yDraw = (int)(pageSettings.Margins.Top / 254.0 * 96);
            int widthDraw = (int)(pageSettings.PaperSize.Width / 100.0 * 96) -
                (int)((pageSettings.Margins.Left + pageSettings.Margins.Right) / 254.0 * 96);
            int heightDraw = (int)(pageSettings.PaperSize.Height / 100.0 * 96) -
                (int)((pageSettings.Margins.Top + pageSettings.Margins.Bottom) / 254.0 * 96);
            picture.drawArea = new Rectangle(xDraw, yDraw, widthDraw, heightDraw);
            picture.logoPos = new Rectangle((int)(picture.Width * source.logoPos.Left / 1.0 / source.Width)
                , (int)(picture.Height * source.logoPos.Top / 1.0 / source.Height)
                , 300
                , (picture.LogoItems.Count + 1) / 2 * 50);
            return picture;
        }

        //打印事件处理
        private void pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            //读取图片
            MyPictureBox picBox = tabControl1.SelectedPanel.Controls[0] as MyPictureBox;
            MyPictureBox imagePic = CreatePictureFromSource(picBox);
            //Image temp = picBox.Image;
            int x = e.MarginBounds.X;
            int y = e.MarginBounds.Y;
            //int width = temp.Width;
            //int height = temp.Height;
            //Rectangle destRect = new Rectangle(x, y, width, height);
            Graphics g = e.Graphics;
            imagePic.LogoItems.Clear();

            DrawLevelLinesAndFill(imagePic, g);
            /*
            if (imagePic.genPos.Rows.Count > 0)
            {
                DrawGenAreas(imagePic, g);
                DrawSelectGens(imagePic, g);
            }
            */
            DrawCoordinates(imagePic, g);

            //DrawSpecialLines(picBox, g);

            DrawLogo(imagePic, g);

            //e.Graphics.DrawImage(temp, destRect, 0, 0, temp.Width, temp.Height, System.Drawing.GraphicsUnit.Pixel);
        }

        private void tabControl1_Click(object sender, EventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void 放大图片Ctrl鼠标左键向上滚轮ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            amplifyPicture(myPictureBox, myPictureBox.Parent as Panel);
        }

        private void 缩小图片Ctrl鼠标左键向下滚轮ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shrinkPicture(myPictureBox, myPictureBox.Parent as Panel);
        }
    }
    #endregion


    public struct LogoItem
    {
        public Brush brush;
        //因为一些图标合并到一起所以需要保存两个Brush 添加by孙凯 2016.1.19
        public Brush secondBrush;
        public string name;
        public string secondName;
        public int priority;
    }

    public class MyPictureBox : PictureBox
    {
        public DataTable LevelLines { get; set; }
        public DataTable genPos { get; set; }
        public Rectangle logoPos { get; set; }
        public List<LogoItem> LogoItems = new List<LogoItem>();
        public Point previousPos { get; set; }
        public float largeFontSize = 12;
        public float smallFontSize = 10.5F;
        public float smallestFontSize = 8.5F;
        public System.Drawing.Printing.PageSettings pageSettings { get; set; }
        public Rectangle drawArea { get; set; }
        public int logoWidth = 300;
        public bool drawed = false;
        public int maxRectangleY = 10000;
    }

    public class MyDrawer
    {
        public bool isFillingExist = true;
        public String Flag;
        public String Name;
        public String Mark;
        public Brush brush;

        public MyDrawer(String Flag, String ARGB, String Hatch, String Name, String Mark)
        {
            if (ARGB == "")
            {
                isFillingExist = false;
            }
            else
            {
                string[] argbStr = ARGB.Split(new char[] { ' ' });
                //存rg的4个字段
                int[] argb = new int[4];
                for (int index = 0; index < 4; index++)
                    argb[index] = Convert.ToInt32(argbStr[index]);
                // 得到对应颜色
                Color backColor = Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
                HatchStyle hatchStyle = HatchStyle.Min;
                hatchStyle = (HatchStyle)Enum.Parse(typeof(HatchStyle), Hatch);
                brush = new HatchBrush(hatchStyle, Color.Black, backColor);
            }
            this.Flag = Flag;
            this.Name = Name;
            this.Mark = Mark;
        }
    }
}