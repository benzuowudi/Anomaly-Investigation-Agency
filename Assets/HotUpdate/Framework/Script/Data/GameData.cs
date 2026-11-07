namespace Framework.Script.Data
{
    public class GameData
    {
        /// <summary>
        /// 生成数据实例时执行一次
        /// </summary>
        /// <param name="o">传入的数据</param>
        protected internal virtual void Init()
        {

        }

        /// <summary>
        /// 数据更新时执行一次
        /// </summary>
        /// <param name="o">传入的数据</param>
        protected internal virtual void Update(object o)
        {

        }

        /// <summary>
        /// 销毁实例时执行一次
        /// </summary>
        protected internal virtual void Destroy()
        {

        }
    }
}