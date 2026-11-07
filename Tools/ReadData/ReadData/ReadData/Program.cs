using System;
using System.Data;
using System.IO;
using System.Text;
using System.Data.OleDb;
using System.Xml;
using System.Collections.Generic;

namespace ReadData
{
    class Program
    {
        private int m_FileNum = 0;
        public static void Main(string[] args)
        {
            Program program = new Program();

            DirectoryInfo folder = new DirectoryInfo(@"D:\unityproject_x\DataTable");

            foreach (FileInfo file in folder.GetFiles("*.xlsx"))
            {
                if (file.Name.StartsWith("~"))
                {
                    continue;
                }
                string kName = file.Name.Split(".")[0];
                Console.WriteLine(kName);
                program.GetExcelData(@"D:\unityproject_x\DataTable", @"D:\unityproject_x\Assets\GameMain\Data", kName);
            }
        }

        public static int GetFilesCount(DirectoryInfo dirInfo)
        {

            int totalFile = 0;
            //totalFile += dirInfo.GetFiles().Length;//获取全部文件
            totalFile += dirInfo.GetFiles("*.xlsx").Length;//获取某种格式
            foreach (System.IO.DirectoryInfo subdir in dirInfo.GetDirectories())
            {
                totalFile += GetFilesCount(subdir);
            }
            return totalFile;
        }

        /// <summary>
        /// 读取excel文件内容,只能读取xlsx类型的文件
        /// </summary>
        /// <param name="_filePath">表格文件路径</param>
        /// <param name="_fileName">表格文件名称</param>
        public void GetExcelData(string _filePath, string _outPath, string _fileName)
        {
            try
            {
                string kFilePath = _filePath + @"\" + _fileName + ".xlsx";
                string connectinString = string.Format("Provider=Microsoft.ACE.OLEDB.{0}.0;Persist Security Info=False;" +
                                                       "Extended Properties=\"Excel {1}.0;HDR={2};IMEX=1;\";" + "data source={3};",
                                                       12, 12, (true ? "Yes" : "NO"), kFilePath);
                //建立 链接  创建到数据源的对象
                OleDbConnection connection = new OleDbConnection(connectinString);
                //打开链接
                connection.Open();

                //返回Excel的架构，包括各个sheet表的名称,类型，创建时间和修改时间等　
                DataTable dtSheetName = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "Table" });
                //包含excel中表名的字符串数组
                string[] strTableNames = new string[dtSheetName.Rows.Count];
                for (int k = 0; k < dtSheetName.Rows.Count; k++)
                {
                    strTableNames[k] = dtSheetName.Rows[k]["TABLE_NAME"].ToString();
                }

                //string sql = $"select * from [{sheet}$]"; //sql 语法  是一个查询命令,该句报错,修改成如下
                string sql = "select * from [" + strTableNames[0] + "]";

                OleDbDataAdapter adapter = new OleDbDataAdapter(sql, connection);

                DataSet dataSet = new DataSet();//用来放数据 用来存放DataTable
                adapter.Fill(dataSet);//表示把查询 结果datatable 放到(填充)dataset 里面
                connection.Close();//释放链接资源  

                //取得数据
                DataTableCollection tableCollection = dataSet.Tables; //获取当前集合中所有 表格
                DataTable table = tableCollection[0];//因为只往dataset里面放置 了一张表格 ，所有这里取得索引为0的表格就是我们刚刚查询到的表格
                //取得表格中的数据
                //取得table中所有的行
                DataRowCollection dataTableRow = table.Rows;//返回了一个行的集合

                string kRowData = "";
                for (int i = 1; i < dataTableRow.Count; i++)
                {
                    int temp_i = i;
                    kRowData = kRowData + "     <" + _fileName;
                    for (int j = 0; j < dataTableRow[0].ItemArray.Length; j++)
                    {
                        int temp_j = j;
                        kRowData = kRowData + " " + dataTableRow[0].ItemArray[temp_j] + " = " + '"' + dataTableRow[temp_i].ItemArray[temp_j] + '"';
                    }
                    kRowData = kRowData + " /" + ">\n";
                }
                string kXMLData = string.Format("<data>\n{0}</data>", kRowData);
                Console.WriteLine(kXMLData);

                CreatData(_outPath, _fileName, kXMLData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("读取Excel内容出错!\r\n" + ex);//自定义的日志输出
            }
        }

        public void CreatData(string _outPath, string _fileName, string _data)
        {
            string kData = _outPath + @"\" + _fileName + ".xml";
            File.WriteAllText(kData, _data);
            Console.WriteLine("xml生成完成");
        }
    }
}
