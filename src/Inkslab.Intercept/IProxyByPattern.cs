﻿using Microsoft.Extensions.DependencyInjection;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 代理方式。
    /// </summary>
    public interface IProxyByPattern
    {
        /// <summary>
        /// 刷新服务配置。
        /// </summary>
        /// <returns></returns>
        ServiceDescriptor Ref();
    }
}
