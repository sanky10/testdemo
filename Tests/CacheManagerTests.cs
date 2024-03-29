using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Omu.ProDinner.Core.Model;
using Omu.ProDinner.Infra;

namespace Omu.ProDinner.Tests
{
    public class CacheManagerTests
    {
        [Test]
        public void GetOrAdd()
        {
            var strg = new MyCacheStorage();
            var cache = new CacheManager(strg);
            
            Action act = () =>
            {
                var res = cache.Get("a1", () => "v1");

                Assert.AreEqual("v1", res);
            };

            Parallel.ForEach(Enumerable.Range(0, 1000), i =>
            {
               act(); 
            });

            Assert.AreEqual(1, strg.Data.Count);

            object val;
            strg.Data.TryGetValue("a1", out val);

            Assert.AreEqual("v1", val);
        }

        [Test]
        public void GetOrAddGroup()
        {
            var strg = new MyCacheStorage();
            var cache = new CacheManager(strg);
            
            Action act = () =>
            {
                var res = cache.Get("a1", () => "v1", typeof(Meal));
                var res2 = cache.Get("a2", () => "v2", typeof(Meal));
                var res3 = cache.Get("a3", () => "v3", typeof(Meal));

                Assert.AreEqual("v1", res);
                Assert.AreEqual("v2", res2);
                Assert.AreEqual("v3", res3);
            };

            Parallel.ForEach(Enumerable.Range(0, 1000), i =>
            {
                Console.WriteLine(i);
                if (i < 900 && i % 3 == 0)
                {
                    cache.RemoveGroup(typeof(Meal));
                }
                else
                {
                    act();
                }
            });

            Console.WriteLine("end parallel exec");

            cache.RemoveGroup(typeof(Meal));
            Assert.AreEqual(0, strg.Data.Count);
        }

        [Test]
        public void RemoveGroupOnChangeAction()
        {
            var strg = new MyCacheStorage();
            var cache = new CacheManager(strg);

            cache.Get("a1", () => "v1", typeof(Meal));
            cache.Get("a2", () => "v2", typeof(Dinner));

            var res21 = cache.Get("a2", () => "v3", typeof(Dinner));
            Assert.AreEqual("v2", res21);

            cache.ChangeAction<Meal>(ChangeType.Edit);

            var res3 = cache.Get("a2", () => "v3", typeof(Dinner));

            Assert.AreEqual("v3", res3);
        }

        public class MyCacheStorage : ICacheStorage
        {
            public ConcurrentDictionary<string, object> Data = new ConcurrentDictionary<string, object>();

            public void Insert(string key, object value)
            {
                Data.AddOrUpdate(key, value, (k, ov) => value);
            }

            public void Remove(string key)
            {
                object val;
                Data.TryRemove(key, out val);
            }

            public object Get(string key)
            {
                object val;
                Data.TryGetValue(key, out val);
                return val;
            }
        }
    }
}