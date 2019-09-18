using System;

namespace Omu.ProDinner.Infra
{
    public interface ICacheManager
    {
        void RemoveGroup(Type group);
        void Remove(string key);
        T Get<T>(string key, Func<T> getFunc, Type group = null);
        void ChangeAction<T>(ChangeType chtype);
    }
}