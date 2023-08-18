![Delta](delta.png 'Logo')

![GitHub](https://img.shields.io/github/license/tinylit/DeltaExpression.svg)
![language](https://img.shields.io/github/languages/top/tinylit/DeltaExpression.svg)
![codeSize](https://img.shields.io/github/languages/code-size/tinylit/DeltaExpression.svg)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/DeltaExpression)](../../issues)

### “Delta”是什么？

Delta 是一套基于原生Emit指令封装的类型生成器，封装了类似于Expression的表达式语法，简单易用。

### 如何安装？
First, [install NuGet](http://docs.nuget.org/docs/start-here/installing-nuget). Then, install [Inkslab](https://www.nuget.org/packages/inkslab/) from the package manager console: 

```
PM> Install-Package DeltaExpression
```

NuGet 包
--------

| Package | NuGet | Downloads | Jane Says <kbd>Markdown</kbd> |
| ------- | ----- | --------- | --------- |
| DeltaExpression | [![DeltaExpression](https://img.shields.io/nuget/v/DeltaExpression.svg)](https://www.nuget.org/packages/DeltaExpression/) | ![Nuget](https://img.shields.io/nuget/dt/DeltaExpression) | Core universal design. |
| DeltaExpression.AOP | [![DeltaExpression.AOP](https://img.shields.io/nuget/v/DeltaExpression.AOP.svg)](https://www.nuget.org/packages/inkslab.map/) | ![Nuget](https://img.shields.io/nuget/dt/DeltaExpression.AOP) | AOP framework based on method return types. |

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