using System;

namespace MessageStructure
{
    public class PipeMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public int Id;

        /// <summary>
        /// 配置文本
        /// </summary>
        public string Configure;

        /// <summary>
        /// 输入信息
        /// </summary>
        public string Input;


        /// <summary>
        /// 输出信息
        /// </summary>
        public string Output;
    }
}
