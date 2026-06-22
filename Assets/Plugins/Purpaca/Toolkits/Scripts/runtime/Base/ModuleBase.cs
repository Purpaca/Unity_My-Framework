using System;
using System.Collections.Generic;

namespace Purpaca.Base
{
    public abstract class ModuleBase
    {
        private static List<ModuleBase> modules;
        protected static List<ModuleBase> Modules 
        {
            get 
            {
                modules = modules == null? new List<ModuleBase>() : modules;
                return modules;
            }
        }
    }


    //TODO:完成类的编写

    /// <summary>
    /// 
    /// </summary>
    [Obsolete("不知道怎么设计，此类型禁止使用！",true)]
    public abstract class ModuleBase<T> : IDisposable where T : ModuleBase<T>
    {
        private static volatile T instance;
        private static bool initialized => instance != null;
        private static object locker = new object();

        protected ModuleBase()
        {
            if (initialized)
            {
                throw new InvalidOperationException($"\"{typeof(T).FullName}\" is a singleton type and the unique instance already exists. Unable to create another instance of this type");
            }

            instance = this as T;
        }

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            instance = Activator.CreateInstance(typeof(T), true) as T;
                        }

                        return instance;
                    }
                }

                return instance;
            }
        }

        public virtual void OnInit() { }
        public virtual void OnRelease() { }

        public static void Release() 
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}