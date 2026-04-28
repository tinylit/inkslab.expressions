using System.Collections.Concurrent;

namespace Inkslab
{
    /// <summary>
    /// 名称提供者。线程安全。
    /// </summary>
    public class NamingScope : INamingScope
    {
        // 使用 ConcurrentDictionary 保证多线程并发调用 GetUniqueName 时
        // 不会发生 Dictionary 内部状态损坏或唯一性丢失。
        private readonly ConcurrentDictionary<string, int> names = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// 子命名范围。
        /// </summary>
        /// <returns></returns>
        public INamingScope BeginScope() => new NamingScope();

        /// <summary>
        /// 获取范围内唯一的名称。
        /// </summary>
        /// <param name="displayName">显示名称。</param>
        /// <returns></returns>
        public string GetUniqueName(string displayName)
        {
            // AddOrUpdate 在并发下保证原子性：首次返回 0（即原始名称），
            // 后续每次返回单调递增的后缀编号，永不重复。
            int counter = names.AddOrUpdate(displayName, 0, (_, current) => current + 1);

            if (counter == 0)
            {
                return displayName;
            }

            return displayName + "_" + counter.ToString();
        }
    }
}
