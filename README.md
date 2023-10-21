![Inkslab](inkslab-mini.jpg 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/inkslab.expressions.svg)
![language](https://img.shields.io/github/languages/top/tinylit/inkslab.expressions.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/inkslab.expressions.svg)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab.expressions)](../../issues)

### “Inkslab.Expressions”是什么？

Inkslab.Expressions 是一套基于原生Emit指令封装的类型生成器，封装了类似于Expression的表达式语法，简单易用。

### 如何安装？
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [Inkslab.Expressions](https://www.nuget.org/packages/inkslab.expressions/) from the package manager console: 

```
PM> Install-Package Inkslab.Expressions
```

NuGet 包
--------

| Package | NuGet | Downloads | Jane Says <kbd>Markdown</kbd> |
| ------- | ----- | --------- | --------- |
| Inkslab.Expressions | [![Inkslab.Expressions](https://img.shields.io/nuget/v/Inkslab.Expressions.svg)](https://www.nuget.org/packages/Inkslab.Expressions/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Expressions) | Core universal design. |
| Inkslab.Intercept | [![DeltaExpression.AOP](https://img.shields.io/nuget/v/Inkslab.Intercept.svg)](https://www.nuget.org/packages/Inkslab.Intercept/) | ![Nuget](https://img.shields.io/nuget/dt/Inkslab.Intercept) | AOP framework based on method return types. |

### 如何使用？

* 实现“InterceptAttribute”拦截器属性。

* 使用拦截器“UseIntercept”。
```C#
public class Startup {

	public void ConfigureServices(IServiceCollection services) {
		// 在此语句之前注入的服务实现，会自动生成拦截类。
		services.UseIntercept();
	}
}
```