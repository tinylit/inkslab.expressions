using Inkslab.Emitters;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;

namespace Inkslab
{
    /// <summary>
    /// 模块。
    /// </summary>
    public class ModuleEmitter
    {
        /// <summary>
        ///  默认文件名称。
        /// </summary>
        public static readonly string DEFAULT_FILE_NAME = "Inkslab.Override.dll";

        /// <summary>
        ///  程序集名称。
        /// </summary>
        public static readonly string DEFAULT_ASSEMBLY_NAME = "Inkslab.Override";

        private ModuleBuilder builder;
        private readonly INamingScope namingScope;

        private readonly string moduleName;
        private readonly string assemblyPath;

        // Used to lock the module builder creation
        private readonly object moduleLocker = new object();
#if NET461_OR_GREATER
        // Specified whether the generated assemblies are intended to be saved
        private readonly bool savePhysicalAssembly;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ModuleEmitter() : this(false)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        public ModuleEmitter(string moduleName)
            : this(moduleName, string.Concat(moduleName, ".dll"))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(string moduleName, string assemblyPath)
            : this(false, new NamingScope(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="naming">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(INamingScope naming, string moduleName, string assemblyPath) : this(false, naming, moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        public ModuleEmitter(bool savePhysicalAssembly)
            : this(savePhysicalAssembly, DEFAULT_ASSEMBLY_NAME, DEFAULT_FILE_NAME)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        /// <param name="moduleName">程序集名称。</param>
        public ModuleEmitter(bool savePhysicalAssembly, string moduleName)
            : this(savePhysicalAssembly, new NamingScope(), moduleName, string.Concat(moduleName, ".dll"))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(bool savePhysicalAssembly, string moduleName, string assemblyPath)
            : this(savePhysicalAssembly, new NamingScope(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="savePhysicalAssembly">是否保存物理文件。</param>
        /// <param name="namingScope">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(bool savePhysicalAssembly, INamingScope namingScope,
                           string moduleName, string assemblyPath)
        {
            this.namingScope = namingScope ?? throw new ArgumentNullException(nameof(namingScope));

            this.savePhysicalAssembly = savePhysicalAssembly;
            this.moduleName = moduleName;
            this.assemblyPath = assemblyPath;
        }
#else
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ModuleEmitter()
            : this(DEFAULT_ASSEMBLY_NAME, DEFAULT_FILE_NAME)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        public ModuleEmitter(string moduleName)
            : this(moduleName, string.Concat(moduleName, ".dll"))
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(string moduleName, string assemblyPath)
            : this(new NamingScope(), moduleName, assemblyPath)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="namingScope">命名规则。</param>
        /// <param name="moduleName">程序集名称。</param>
        /// <param name="assemblyPath">程序集地址。</param>
        public ModuleEmitter(INamingScope namingScope,
                           string moduleName, string assemblyPath)
        {
            this.namingScope = namingScope ?? throw new ArgumentNullException(nameof(namingScope));
            this.moduleName = moduleName;
            this.assemblyPath = assemblyPath;
        }
#endif

        /// <summary>
        /// 程序集名称。
        /// </summary>
        public string AssemblyFileName
        {
            get { return Path.GetFileName(assemblyPath); }
        }

#if NET461_OR_GREATER
        /// <summary>
        /// 程序集文件地址。
        /// </summary>
        public string AssemblyDirectory
        {
            get
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                if (directory == string.Empty)
                {
                    return null;
                }
                return directory;
            }
        }
#endif
        private ModuleBuilder CreateModule()
        {
            var assemblyName = new AssemblyName
            {
                Name = this.moduleName
            };
            var moduleName = AssemblyFileName;

#if NET461_OR_GREATER
            if (savePhysicalAssembly)
            {
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                        assemblyName, AssemblyBuilderAccess.RunAndSave, AssemblyDirectory);

                return assemblyBuilder.DefineDynamicModule(moduleName, moduleName, false);
            }
            else
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

                return assemblyBuilder.DefineDynamicModule(moduleName);
            }
#else
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            return assemblyBuilder.DefineDynamicModule(moduleName);
#endif
        }

        /// <summary>
        /// 在此模块中用指定的名称为公共类型构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径，其中包括命名空间。 name 不能包含嵌入的 null。</param>
        /// <returns>具有指定名称的公共类型。</returns>
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new ClassEmitter(this, name);
        }

        /// <summary>
        /// 在给定类型名称和类型特性的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">已定义类型的属性。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name, TypeAttributes attributes)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new ClassEmitter(this, name, attributes);
        }

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">与类型关联的属性。</param>
        /// <param name="baseType">已定义类型扩展的类型。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name, TypeAttributes attributes, Type baseType)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new ClassEmitter(this, name, attributes, baseType);
        }

        /// <summary>
        /// 在给定类型名称、特性、已定义类型扩展的类型和已定义类型实现的接口的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attributes">与类型关联的特性。</param>
        /// <param name="baseType">已定义类型扩展的类型。</param>
        /// <param name="interfaces">类型实现的接口列表。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [ComVisible(true)]
        [SecuritySafeCritical]
        public ClassEmitter DefineType(string name, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return new ClassEmitter(this, name, attributes, baseType, interfaces);
        }

        private ModuleBuilder Complete()
        {
            if (builder is null)
            {
                lock (moduleLocker)
                {
                    builder ??= CreateModule();
                }
            }

            return builder;
        }

        private string GetUniqueName(string name) => namingScope.GetUniqueName(name);

        /// <summary>
        /// 开始名称范围。
        /// </summary>
        /// <returns></returns>
        public INamingScope BeginScope() => namingScope.BeginScope();

        internal static TypeBuilder DefineType(ModuleEmitter emitter, string name) => emitter.Complete().DefineType(emitter.GetUniqueName(name));
        internal static TypeBuilder DefineType(ModuleEmitter emitter, string name, TypeAttributes attributes) => emitter.Complete().DefineType(emitter.GetUniqueName(name), attributes);
        internal static TypeBuilder DefineType(ModuleEmitter emitter, string name, TypeAttributes attributes, Type baseType) => emitter.Complete().DefineType(emitter.GetUniqueName(name), attributes, baseType);
        internal static TypeBuilder DefineType(ModuleEmitter emitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces) => emitter.Complete().DefineType(emitter.GetUniqueName(name), attributes, baseType, interfaces);

#if NET461_OR_GREATER
        /// <summary>
        /// 保存程序集。
        /// </summary>
        /// <returns>返回文件地址。</returns>
        public string SaveAssembly()
        {
            if (builder is null)
            {
                throw new InvalidOperationException("未生成弱命名程序集。");
            }

            if (!savePhysicalAssembly)
            {
                throw new NotSupportedException("未设置保存为物理文件的支持!");
            }

            var assemblyBuilder = (AssemblyBuilder)builder.Assembly;
            var assemblyFileName = AssemblyFileName;
            var assemblyFilePath = builder.FullyQualifiedName;

            if (File.Exists(assemblyFilePath))
            {
                File.Delete(assemblyFilePath);
            }

            assemblyBuilder.Save(assemblyFileName);

            return assemblyFilePath;
        }
#endif
    }
}
