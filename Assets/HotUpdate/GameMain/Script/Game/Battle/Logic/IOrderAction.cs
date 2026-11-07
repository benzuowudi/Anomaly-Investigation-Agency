using System;

interface IOrderAction
{
    /// <summary>
    /// 接受命令
    /// </summary>
    void OrderIssue();
    /// <summary>
    /// 执行命令
    /// </summary>
    void OrderRun(object o, Action _callback);
    /// <summary>
    /// 检查命令执行状态
    /// </summary>
    /// <returns>返回命令执行状态（成功 or 失败）</returns>
    bool OrderCheck();
    /// <summary>
    /// 命令执行结束
    /// </summary>
    void OrderFinish(bool _IsSuccess);
}