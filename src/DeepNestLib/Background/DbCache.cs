using DeepNestLib.NoFitPolygon;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib.Background
{
    public class DbCache(WindowUnk w)
    {
        public WindowUnk window = w;

        public bool Has(DbCacheKey obj)
        {
            NfpCacheKey key = new NfpCacheKey(obj); // Create the new struct key
            return window.nfpCache.ContainsKey(key);
        }

        internal void Insert(DbCacheKey obj, bool inner = false)
        {
            NfpCacheKey key = new NfpCacheKey(obj); // Create the new struct key
            List<NFP> value = NestingService.CloneNfp(obj.Nfp, inner).ToList();
            window.nfpCache.TryAdd(key, value);
        }

        public NFP[] Find(DbCacheKey obj, bool inner = false)
        {
            NfpCacheKey key = new NfpCacheKey(obj); // Create the new struct key
            if (window.nfpCache.TryGetValue(key, out List<NFP> cachedNfp))
            {
                return NestingService.CloneNfp(cachedNfp.ToArray(), inner);
            }
            return null;
        }
    }
}
