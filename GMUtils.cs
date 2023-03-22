using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace GameManagerField
{

    public struct GMInfomation
    {
        public string Name;
        public ParameterInfo[] Paras;
        public string GMUsage;
    }



    public static class GMUtils
    {

        //存放一般GM指令名称和存在重载的GM指令名称的列表
        static List<string> MethodsNameList = new List<string>();
        static List<string> OverLoadMethodsNameList = new List<string>();

        //GetAllGmMethods用的临时列表
        static List<MethodInfo> GMMethodInfos = new List<MethodInfo>();

        //定义字典，存放一般GM指令以及存在重载的GM指令内容
        static Dictionary<string, MethodInfo> GmMethodsDictionary = new Dictionary<string, MethodInfo>();
        static Dictionary<GMInfomation, MethodInfo> GmOverLoadMethodsDictionary = new Dictionary<GMInfomation, MethodInfo>();




        /// <summary>
        /// 初始化，获得所有GM指令的方法和方法名
        /// </summary>
        static GMUtils()
        {
            GetAllGmMethods();
        }


        /// <summary>
        /// 获得所有GM函数的相关信息
        /// </summary>
        /// <returns></returns>
        public static List<GMInfomation> GetGMMethodsInfo()
        {
            List<GMInfomation> GMInfos = new List<GMInfomation>();
            Type t = typeof(GmCommand);
            //获得GmCommand类下所有方法
            MethodInfo[] GmMethods = t.GetMethods();
            //遍历方法
            foreach (MethodInfo m in GmMethods)
            {
                //判断对应方法是否有GM特性，有特性就放入字典里
                if (Attribute.IsDefined(m, typeof(GMAttribute)))
                {
                    List<ParameterInfo> paranames = new List<ParameterInfo>();
                    GMInfomation gminfo = new GMInfomation();
                    gminfo.Name = m.Name;
                    ParameterInfo[] tmpParaArray = m.GetParameters();
                    foreach (ParameterInfo p in tmpParaArray)
                    {
                        paranames.Add(p);
                    }
                    gminfo.Paras = paranames.ToArray();
                    foreach (Attribute a in m.GetCustomAttributes())
                    {
                        if (a is GMAttribute)
                        {
                            GMAttribute ga = (GMAttribute)a;
                            if (null != ga)
                            {
                                gminfo.GMUsage = ga.Desc;
                            }
                        }
                    }
                    GMInfos.Add(gminfo);
                }

            }
            return GMInfos;
        }

       
        /// <summary>
        /// 获取所有可执行的GM指令函数和函数名
        /// </summary>
        private static void GetAllGmMethods()
        {
            try
            {
                //保底清空
                MethodsNameList.Clear();
                OverLoadMethodsNameList.Clear();
                GMMethodInfos.Clear();


                GmMethodsDictionary.Clear();
                GmOverLoadMethodsDictionary.Clear();

                //GM指令希望能全部写在Gmtools下的GmCommand类中，保证代码整洁
                Type t = typeof(GmCommand);

                //获得GmCommand类下所有方法
                MethodInfo[] GmMethods = t.GetMethods();

                //存参用
                string tmpname;

                //遍历方法
                foreach (MethodInfo m in GmMethods)
                {
                    //判断对应方法是否有GM特性，所有带GM特性的方法存列表，并且把是否存在重载的函数名放入不同的列表
                    if (Attribute.IsDefined(m, typeof(GMAttribute)))
                    {
                        tmpname = m.Name.ToLower();
                        if (MethodsNameList.Contains(tmpname) && !OverLoadMethodsNameList.Contains(tmpname))
                        {
                            MethodsNameList.Remove(tmpname);
                            OverLoadMethodsNameList.Add(tmpname);
                        }
                        else if (OverLoadMethodsNameList.Contains(tmpname))
                        {
                            if (MethodsNameList.Contains(tmpname))
                                MethodsNameList.Remove(tmpname);
                        }
                        else
                        {
                            MethodsNameList.Add(tmpname);
                        }

                        //所有方法都放入临时列表
                        GMMethodInfos.Add(m);
                    }
                }
                foreach (MethodInfo m in GMMethodInfos)
                {
                    tmpname = m.Name.ToLower();
                    GMInfomation m4d;

                    //存入一般GM指令信息
                    if (MethodsNameList.Contains(tmpname))
                        GmMethodsDictionary.Add(tmpname, m);

                    //存入存在重载的GM指令信息
                    else if (OverLoadMethodsNameList.Contains(tmpname))
                    {
                        m4d.Name = tmpname;
                        m4d.Paras = m.GetParameters();
                        m4d.GMUsage = null;
                        GmOverLoadMethodsDictionary.Add(m4d, m);
                    }
                }

                //Clear
                GMMethodInfos.Clear();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 将SRDebugger输入栏获得的指令信息格式化
        /// </summary>
        /// <param name="command">完整的指令输入值</param>
        /// <returns>将指令分割后每一段指令的字符串信息</returns>
        public static string[] GetCommandStrings(string command)
        {
            try
            {
                //格式化，不论输入的是什么内容全部小写
                command = command.ToLower();
                //正则匹配输入的字符串
                Match match;
                string patern = @"^\s*\w+\s*(-?(\w+\.)?\w+\s*)*$"; //(-? (\w +\.)?\w)*
                Regex r = new Regex(patern);
                match = r.Match(command);

                //此时的command已经是正则匹配后的内容
                command = match.Value;

                //做一个判断，如果此时匹配不到结果，确认输入的内容完全错误
                //p.s 服务端指令格式更自由，所以有些异常情况需要照顾一下，此时也返回一个结果给服务端指令服务
                if (command.Length <= 0)
                {
                    SuperDebug.LogWarning("指令输入转化异常，会以服务端格式发送一遍完整的指令");
                    return new string[] { command };
                }


                //把输入的多个空格全部简化为一个空格
                patern = @"\s+";
                r = new Regex(patern);
                command = r.Replace(command, " ");

                //尾部可能还会有空格残留，用trim干掉
                command = command.Trim();

                //定义一个字符串数组
                string[] result;

                //GM指令固定格式：指令名 + 参数1 + 参数2 ....+ 参数n，以单个空格做分割
                //以空格分割输入的内容，获得指令和参数
                result = command.Split(' ');

                //如果指令不带任何参数，重新赋值，此时数组只有指令本身
                if (result.Length <= 1)
                    result = new string[1] { command };

                string s1 = "";
                foreach (string s in result)
                {
                    s1 += s;
                }
                return result;

            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// 函数参数均为可选参数时，获取对应方法的所有参数默认值
        /// </summary>
        /// <param name="m">当前方法</param>
        /// <returns></returns>
        private static object[] GetDefaultParas(MethodInfo m)
        {
            List<object> obj = new List<object>();
            foreach (ParameterInfo p in m.GetParameters())
            {
                obj.Add(p.DefaultValue);
            }
            return obj.ToArray();
        }

        /// <summary>
        /// 获取指令的所有参数
        /// 其实就是把数组第一个元素剔除
        /// </summary>
        /// <param name="s">完整指令</param>
        /// <returns></returns>
        private static string[] GetMethodParams(string[] s)
        {
            List<string> list = new List<string>();
            for (int i = 1; i < s.Length; i++)
            {
                list.Add(s[i]);
            }
            return list.ToArray();
        }


        /// <summary>
        /// 构成一个对应GM指令参数类型的数组
        /// 有优化空间，暂时先这么做
        /// </summary>
        /// <param name="m">传入的方法</param>
        /// <param name="s">参数字符串数组</param>
        /// <returns>用于invoke的object数组</returns>
        private static object[] SetCorrectParams(MethodInfo m, string[] s)
        {

            try
            {
                ParameterInfo[] p = m.GetParameters();
                List<object> obj = new List<object>();
                if (p.Length == s.Length)
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        Type t = p[i].ParameterType;
                        if (t != typeof(string))
                        {
                            if (t == typeof(int))
                            {
                                obj.Add(Int32.Parse(s[i]));
                            }
                            else if (t == typeof(uint))
                            {
                                obj.Add(UInt32.Parse(s[i]));
                            }
                            else if (t == typeof(double))
                            {
                                obj.Add(Double.Parse(s[i]));
                            }
                            else if (t == typeof(float))
                            {
                                obj.Add(Single.Parse(s[i]));
                            }
                            else if (t == typeof(bool))
                            {
                                obj.Add(Boolean.Parse(s[i]));
                            }
                            else
                            {
                                SuperDebug.Log("Unexpectable Situation...May Cause Command Error...");
                            }
                        }
                        else
                        {
                            obj.Add(s[i]);
                        }

                    }
                }
                else
                {
                    SuperDebug.LogError("参数数量不匹配");
                }
                return obj.ToArray();
            }
            catch (Exception e)
            {
                throw e;
            }
        }



        /// <summary>
        /// 执行指令操作
        /// </summary>
        /// <param name="s">完整指令</param>
        public static void TryGMCommand(string s)
        {
            try
            {
                //解析完整的指令
                string[] command = GetCommandStrings(s);
                if (MethodsNameList.Contains(command[0]))
                {
                    MethodInfo m = GmMethodsDictionary[command[0]];
                    //判断对应函数是否存在传参，需要传参的指令特殊处理
                    if (m.GetParameters().Length > 0)
                    {
                        //判断函数的传参是否均有默认值，是：可以选择不输入任何参数执行；否：一定要输入正确的参数数量和格式才能执行
                        bool isAllDefault = true;
                        foreach (ParameterInfo p in m.GetParameters())
                        {
                            if (!p.HasDefaultValue)
                            {
                                isAllDefault = false;
                                break;
                            }

                        }
                        //函数参数均为可选参数时
                        if (isAllDefault)
                        {
                            if (command.Length == 1)
                            {
                                //使用默认值执行GM
                                object[] obj = GetDefaultParas(m);
                                m.Invoke(null, obj);
                                SuperDebug.Log("Method Invoke, Method name : " + m.Name);
                                SuperDebug.Log("Use All Default Parameters");
                            }
                            else if (command.Length - 1 == m.GetParameters().Length)
                            {
                                string[] parameters = GetMethodParams(command);
                                object[] obj = SetCorrectParams(m, parameters);
                                m.Invoke(null, obj);


                                SuperDebug.Log("Method Invoke, Method name : " + m.Name);
                            }
                            else
                            {
                                SuperDebug.Log("指令对应的参数数量输入不正确！错误指令： " + s);
                            }
                        }

                        //其他情况，必须匹配完整的指令和参数个数
                        else
                        {
                            if (command.Length - 1 == m.GetParameters().Length)
                            {
                                string[] parameters = GetMethodParams(command);
                                object[] obj = SetCorrectParams(m, parameters);
                                m.Invoke(null, obj);


                                SuperDebug.Log("Method Invoke, Method name : " + m.Name);
                            }
                            else
                            {
                                SuperDebug.LogError("指令对应的参数数量输入不正确！错误指令： " + s);
                            }
                        }
                    }
                    //无需传参的指令直接invoke
                    else
                    {
                        if (command.Length - 1 == m.GetParameters().Length)
                        {
                            m.Invoke(null, null);

                            SuperDebug.Log("Method Invoke, Method name : " + m.Name);
                        }
                        else
                        {
                            SuperDebug.LogError("对应指令无需传参！错误指令：" + s);
                        }

                    }
                }
                //重载函数处理
                //存在重载的函数不处理可选参数情况，一定要满足参数对应形式
                else if (OverLoadMethodsNameList.Contains(command[0]))
                {
                    foreach (var item in GmOverLoadMethodsDictionary)
                    {
                        if (item.Key.Name == command[0])
                        {
                            if (item.Key.Paras.Length == command.Length - 1)
                            {
                                if (item.Key.Paras.Length > 0)
                                {
                                    string[] parameters = GetMethodParams(command);
                                    object[] obj = SetCorrectParams(item.Value, parameters);
                                    item.Value.Invoke(null, obj);


                                    SuperDebug.Log("Method Invoke, Method name : " + item.Value.Name);
                                    break;
                                }
                                else
                                {
                                    item.Value.Invoke(null, null);
                                    SuperDebug.Log("Method Invoke, Method name : " + item.Value.Name);
                                    break;
                                }
                            }
                            else
                            {
                                SuperDebug.Log(
                                    "Search For Invoking Another OverLoad Method, If Nothing Invoke You Probably Insert Wrong Number " +
                                    "of Parameters, " +
                                    "Method name : " + command[0]);
                            }
                        }
                        else
                        {
                            //暂无提示必要
                            //SuperDebug.LogError("Command Processing Error By Error GMUtils Function, Method name : " + command[0]);
                        }
                    }

                }
                //匹配不到任何指令后提示，尝试直接走一次服务端指令流程
                else
                {
                    SuperDebug.LogWarning("GMCommand中没有定义对应的客户端GM指令，尝试走服务端指令；若仍未生效，请检查指令格式");
                    SuperDebug.LogWarning("指令可能不存在或格式异常，指令： " + s);
                }

            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
