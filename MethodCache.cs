using Shared;
using SIDAI.Shared.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIDAI.Shared
{
    public delegate object GenericDelegate(object[] arguments);
    public enum CachedDelegateStatus { running, returned }
    abstract class MethodCache
    {
        FilesVars.Vars FSCache = new FilesVars.Vars("", false, "MethodCache.calls.");
        

        Dictionary<string, CacheCallInfo> cache = new Dictionary<string, CacheCallInfo>();

        public MethodCache() {
            Thread th = new Thread(delegate ()
            {
                //get all caches from FSCache
                string[] childs = FSCache.getChilds("");

                //delete all 
                foreach (var c in childs)
                    FSCache.del(c);
            });

            th.Start();
        }

        protected void clearCallCache(string callId, bool usesContainsFunction = false)
        {
            lock (cache)
            {
                if (!usesContainsFunction)
                {
                    if (cache.ContainsKey(callId))
                    {
                        string callIdName = (string)(cache[callId].tags["callId"]);
                        FSCache.del(callIdName);
                        cache.Remove(callId);
                    }
                }
                else
                {
                    for (int cont = cache.Count-1; cont >= 0; cont--)
                    {
                        if (cache.ElementAt(cont).Key.ToLower().Contains(callId.ToLower()))
                        {
                            string callIdName = (string)(cache.ElementAt(cont).Value.tags["callId"]);
                            FSCache.del(callIdName);
                            cache.Remove(cache.ElementAt(cont).Key);
                        }
                    }
                }
            }
        }

        public delegate bool BoolAction<T>(T param);
        protected void clearCallCache(string callId, BoolAction<string> verifyCallback)
        {
            lock (cache)
            {
                
                for (int cont = cache.Count - 1; cont >= 0; cont--)
                {
                    if (verifyCallback(cache.ElementAt(cont).Key))
                    {
                        string callIdName = (string)(cache.ElementAt(cont).Value.tags["callId"]);
                        FSCache.del(callIdName);
                        cache.Remove(cache.ElementAt(cont).Key);

                    }
                }
            }
        }



        protected T callDelegateWithCache<T>(GenericDelegate theDelegate, string callId, int expireTimeInSeconds, params object[] arguments)
        {
            CacheCallInfo callInfoInCache = null;
            //check if the cache contains a expirated delegate in the cache, or if the cache not contains a delegate with same callId
            if (!cache.ContainsKey(callId))
            {

                //insert the new delegate to cache
                CacheCallInfo temp = new CacheCallInfo(expireTimeInSeconds, delegate (CacheCallInfo sender)
                {
                    //delete from cache
                    string callIdName = (string)(sender.tags["callId"]);
                    lock(cache)
                    {
                        if (cache.ContainsKey(callIdName))
                            cache.Remove(callIdName);
                    }
                    FSCache.del(callIdName);
                })
                {
                    theDelegate = theDelegate,
                    arguments = arguments,
                    status = CachedDelegateStatus.running,
                    tags = new Dictionary<string, object> { { "callId", callId } }
                };

                lock (cache) {
                    cache[callId] = temp;
                }

                    
                //call the delegate (int another thread)
                Thread th = new Thread(delegate (object cacheInfo)
                {
                    object result = ((CacheCallInfo)cacheInfo).theDelegate(((CacheCallInfo)cacheInfo).arguments);
                    //store result to files
                    string resultAsString = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(result);
                    //string resultAsString = ReflectionSerializer.SerializeObject(result, new JsonSerializer()).serialize();
                    FSCache.set((string)((CacheCallInfo)cacheInfo).tags["callId"], resultAsString);
                        
                    ((CacheCallInfo)cacheInfo).status = CachedDelegateStatus.returned;

                });
                th.Start(temp);
            }
            callInfoInCache = cache[callId];
            //wait delegate response
            int sum = 0;
            while (callInfoInCache.status == CachedDelegateStatus.running)
            { 
                sum++;
                if (sum % 10 == 0)
                {
                    sum = 0;
                    Thread.Sleep(1);
                }
            }


            //take the data from FSCahce
            string FSCacheVarName = ((string)callInfoInCache.tags["callId"]);
            string json = FSCache.get(FSCacheVarName, "{}").AsString;
            T ret = (new System.Web.Script.Serialization.JavaScriptSerializer()).Deserialize<T>(json);
            //T ret = (T)ReflectionSerializer.DeSerializeObject(json, new JsonSerializer());

            return ret;
        }
    }

    public class CacheCallInfo{
        public delegate void OnExpire(CacheCallInfo sender);
        public GenericDelegate theDelegate;
        public object[] arguments;
        public CachedDelegateStatus status = CachedDelegateStatus.running;

        public Dictionary<string, object> tags = new Dictionary<string, object>();
        
        public CacheCallInfo(int expiresTimeoutSeconds, OnExpire onExpire)
        {
            Timer tm = null;
            tm = new Timer(delegate (object state)
            {
                onExpire(this);
                tm.Dispose();
            }, null, expiresTimeoutSeconds * 1000, 0);

        }


    }
}
