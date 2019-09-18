using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Omu.ProDinner.Core.Model;

namespace Omu.ProDinner.Infra
{
    public class CacheManager : ICacheManager
    {
        private readonly ICacheStorage repo;

        private static readonly ConcurrentDictionary<Type, Tuple<object, HashSet<string>>> groups
            = new ConcurrentDictionary<Type, Tuple<object, HashSet<string>>>();

        private static readonly IDictionary<Type, Type[]> typedeps;

        static CacheManager()
        {
            typedeps = new Dictionary<Type, Type[]>
            {
                {typeof(Meal), new[] {typeof(Dinner)}},
                {typeof(Chef), new[] {typeof(Dinner)}},
                {typeof(Country), new[] {typeof(Dinner), typeof(Chef)}}
            };
        }

        public CacheManager(ICacheStorage repo)
        {
            this.repo = repo;
        }

        public void RemoveGroup(Type group)
        {
            Tuple<object, HashSet<string>> tuple;

            if (groups.ContainsKey(group) && groups.TryGetValue(group, out tuple))
            {
                lock (tuple.Item1)
                {
                    foreach (var key in tuple.Item2)
                    {
                        repo.Remove(key);
                    }
                }
            }
        }

        public void Remove(string key)
        {
            repo.Remove(key);
        }

        public T Get<T>(string key, Func<T> getFunc, Type group = null)
        {
            var val = repo.Get(key);

            if (val != null)
            {
                return (T)val;
            }

            if (group != null)
            {
                groups.AddOrUpdate(
                    group,
                    g => new Tuple<object, HashSet<string>>(new object(), new HashSet<string> { key }),
                    (gname, tuple) =>
                    {
                        lock (tuple.Item1)
                        {
                            tuple.Item2.Add(key);
                        }

                        return tuple;
                    });
            }

            val = getFunc();

            repo.Insert(key, val);

            return (T)val;
        }

        public void ChangeAction<T>(ChangeType chtype)
        {
            var mytype = typeof(T);

            RemoveGroup(mytype);

            if (typedeps.ContainsKey(mytype))
            {
                var deps = typedeps[mytype];

                if (chtype == ChangeType.Edit && deps != null)
                {
                    foreach (var dep in deps)
                    {
                        RemoveGroup(dep);
                    }
                }
            }
        }
    }
}