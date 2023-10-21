namespace Delta.Intercept
{
    /// <summary>
    /// 调用方法。
    /// </summary>
    public interface IInvocation
    {
        /// <summary>
        /// 执行方法。
        /// </summary>
        /// <param name="parameters">参数。</param>
        /// <returns>返回方法结果。</returns>
        object Invoke(object[] parameters);
    }
}
