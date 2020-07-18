using System.Collections.Generic;
using PinkUmbrella.Models;
using Poncho.Models;
using Poncho.Models.Public;

namespace PinkUmbrella.ViewModels.Shop
{
    public class IndexViewModel : BaseViewModel
    {
        public Dictionary<int, List<ShopModel>> ShopsByCategory { get; set; }
        
        public PaginatedModel<UsedTagModel> Categories { get; set; }
        
        public List<ShopModel> ShopsList { get; set; }
    }
}