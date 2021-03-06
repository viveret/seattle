using System.Collections.Generic;

namespace Estuary.Core
{
    public class OrderedCollectionPageObject: CollectionPageObject
    {
        public OrderedCollectionPageObject(string type = null) : base(type ?? "OrderedCollectionPage")
        {
        }

        public List<BaseObject> orderedItems { get; set; }

        public int startIndex { get; set; }
    }
}