using DeepNestLib.NoFitPolygon;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DeepNestLib.Background
{
    public class WindowUnk
    {
        public WindowUnk()
        {
            db = new DbCache(this);
        }

        public ConcurrentDictionary<NfpCacheKey, List<NFP>> nfpCache = new();
        public DbCache db;
    }
}
