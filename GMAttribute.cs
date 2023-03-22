using System;


namespace GameManagerField
{

    //GM指令特性
    public class GMAttribute : Attribute
    {
        //指令描述
        public string Desc { get; set; }

        //默认描述
        public GMAttribute()
        {
            this.Desc = "GM指令，建议在特性中补充用途";

        }
        public GMAttribute(string discription)
        {
            this.Desc = discription;
        }

    }



}
