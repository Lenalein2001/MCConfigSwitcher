using CommunityToolkit.Mvvm.ComponentModel;

namespace MCConfigSwitcher.Models;

public class IpEntry : ObservableObject
{
    private string _name = string.Empty;
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    private string _address = string.Empty;
    public string Address { get => _address; set => SetProperty(ref _address, value); }
}
