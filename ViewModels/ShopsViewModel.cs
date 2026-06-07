using JAA.Models;

namespace JAA.ViewModels
{
    public class ShopsViewModel
    {
        public List<RepairShop> Shops { get; set; } = new();
        public string? SelectedCity { get; set; }
        public List<string> Cities { get; set; } = new();
    }
}
