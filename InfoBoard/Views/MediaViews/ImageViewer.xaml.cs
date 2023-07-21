using InfoBoard.Models;

namespace InfoBoard.Views.MediaViews;

public partial class ImageViewer : ContentPage, IQueryAttributable
{
	public ImageViewer()
	{
		InitializeComponent();
	}

    void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> message)
    {
        var infoMessage = message["ImageMedia"] as Media;
        BindingContext = infoMessage;
    }
}