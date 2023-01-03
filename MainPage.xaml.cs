using System.Diagnostics;
using MauiMvvm.ViewModel;

namespace MauiMvvm;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = new MainViewModel();
    }
}

