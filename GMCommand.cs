using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace MoleMole.CaseTest
{
        [GM("自定义")]
        public static void MyGM()
        {
            /*
            
            Do What the fxxk you want and could.
            
            */
        }

#if UNITY_EDITOR
        [MenuItem("Tools/GM/在桌面生成GM指令文档 %g")]
        public static void OutPutGMInfoFile()
        {
            List<GMInfomation> GMInfo = GMUtils.GetGMMethodsInfo();
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GM指令文档.html";
            StreamWriter sw = new StreamWriter(filePath, false, Encoding.UTF8);
            sw.Write(@"<html>" + sw.NewLine + @"<head>" + sw.NewLine);
            sw.Write(@"<title>GM指令集</title>" + sw.NewLine + @"</head>" + sw.NewLine);
            sw.Write(@"<body>");
            sw.Write(@"<table width='100%' cellpadding='8' style='margin-top:5px' cellspacing='2' border='1' rules='all'>" + sw.NewLine);
            sw.Write(@"<caption ><b><big>GM指令集</big></b></caption>");
            sw.Write(@"<tr>
                       <th>指令名</th>
                       <th>指令用途</th>
                       <th>是否需要参数</th>
                       <th>参数列表</th>         
                       </tr>" + sw.NewLine);
            foreach (GMInfomation info in GMInfo)
            {
                sw.Write(@"<tr>
                       <th>" + info.Name + @"</th>
                       <th>" + info.GMUsage + @"</th>");
                if (info.Paras.Length > 0)
                {
                    sw.Write(@"<th>是</th><th>");
                    foreach (ParameterInfo p in info.Paras)
                    {
                        sw.Write("【\"" + p.Name + "\"");  // + "..");
                        if (p.HasDefaultValue)
                        {
                            sw.Write(" = " + p.DefaultValue + "】");
                        }
                        else
                        {
                            sw.Write("】");
                        }
                    }
                    sw.Write("</th></tr>" + sw.NewLine);
                }
                else
                {
                    sw.WriteLine(@"<th>否</th>
                       <th>无</th>
                       </tr>");
                }
            }
            sw.Flush();
            sw.Close();
            Process.Start(filePath);
        }
#endif
}
