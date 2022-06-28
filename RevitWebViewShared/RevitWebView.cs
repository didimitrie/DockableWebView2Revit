using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace RevitWebView
{
#if RVT2023
  public class RevitWebView : IExternalApplication
  {
    UIControlledApplication App { get; set; }

    BrowserPage Page { get; set; }

    public static Guid PanelId { get { return new Guid("3E545790-5D3B-475D-9280-81F4F0640C66"); } }

    public Result OnStartup(UIControlledApplication application)
    {
      App = application;

      application.CreateRibbonTab("WebView");

      var panel = application.CreateRibbonPanel("WebView", "WebView");
      var path = typeof(RevitWebView).Assembly.Location;
      var button = panel.AddItem(new PushButtonData("Show WebView", "WebView", path, typeof(RevitWebViewCommand).FullName)) as PushButton;

      Page = new BrowserPage();
      var panelId = new DockablePaneId(PanelId);
      App.RegisterDockablePane(panelId, "WebView", Page as IDockablePaneProvider);
      App.ViewActivating += new EventHandler<ViewActivatingEventArgs>(Page.ViewActivationHandler);
      App.ViewActivated += new EventHandler<ViewActivatedEventArgs>(Page.ViewActivatedHandler);
      App.DockableFrameVisibilityChanged += new EventHandler<DockableFrameVisibilityChangedEventArgs>(Page.DockableFrameVisibilityHandler);

      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
      return Result.Succeeded;
    }
  }

  public class BrowserPage : IDockablePaneProvider, IFrameworkElementCreator
  {
    private WebView2Page DockPage { get; set; }
    private Uri Url = new Uri("https://speckle.xyz");
    private bool IsInitiated { get; set; } = false;
    private bool IsVisible { get; set; } = false;
    private Document RevitDoc { get; set; }

    public FrameworkElement CreateFrameworkElement()
    {
      DockPage = new WebView2Page();
      return DockPage;
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
      data.FrameworkElementCreator = this as IFrameworkElementCreator;
      data.InitialState = new DockablePaneState();
      data.InitialState.MinimumWidth = 300;
      data.VisibleByDefault = true;
      data.EditorInteraction = new EditorInteraction(EditorInteractionType.KeepAlive);
      data.InitialState.DockPosition = DockPosition.Tabbed;
      data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser;
    }

    // Close the current webView when opening a new document 
    public void ViewActivationHandler(object sender, ViewActivatingEventArgs e)
    {
      RegisterDocument(e.NewActiveView.Document);
      if (e.NewActiveView.Document == null || e.CurrentActiveView?.Document?.Title != e.NewActiveView.Document.Title)
      {
        SaveUrlAndCloseWebview();
      }
    }

    // Register to the DocumentClosing event of the current document
    private void RegisterDocument(Document document)
    {
      try
      {
        RevitDoc.DocumentClosing -= new EventHandler<DocumentClosingEventArgs>(D_DClosing);
      }
      catch (Exception) { }
      RevitDoc = document;
      RevitDoc.DocumentClosing += new EventHandler<DocumentClosingEventArgs>(D_DClosing);
    }

    // Create a new webView and navigate to the current url
    public void ViewActivatedHandler(object sender, ViewActivatedEventArgs e)
    {
      if (e.PreviousActiveView?.Document.Title != e.CurrentActiveView?.Document.Title)
      {
        DockPage.myWebView.Source = Url;
      }
    }

    // Save the current url and kill the webView
    public void SaveUrlAndCloseWebview()
    {
      Url = DockPage.myWebView.Source;
      DockPage.myWebView.Dispose();
    }

    // If we are about to close down the very last document, kill the webView
    internal void D_DClosing(object sender, DocumentClosingEventArgs e)
    {
      if (e.Document.Application.Documents.Size == 1)
      {
        RevitDoc.DocumentClosing -= new EventHandler<DocumentClosingEventArgs>(D_DClosing);
        RevitDoc = null;
        IsInitiated = false;
        SaveUrlAndCloseWebview();
      }
    }

    // Navigate for the first time (delay until dockable pane is on screen)
    internal void DockableFrameVisibilityHandler(object sender, DockableFrameVisibilityChangedEventArgs e)
    {
      IsVisible = e.DockableFrameShown;
      if (IsVisible && DockPage != null && !IsInitiated)
      {
        DockPage.myWebView.Source = Url;
        IsInitiated = true;
      }
    }
  }

  [Transaction(TransactionMode.Manual)]
  public class RevitWebViewCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      commandData.Application.GetDockablePane(new DockablePaneId(RevitWebView.PanelId)).Show();
      return Result.Succeeded;
    }
  }
#endif

#if RVT2020 || RVT2021
  public class RevitWebView : IExternalApplication
  {
    UIControlledApplication App { get; set; }

    public static BrowserWindow Window { get; set; }

    public Result OnStartup(UIControlledApplication application)
    {
      App = application;

      application.CreateRibbonTab("WebView");
      
      var panel = application.CreateRibbonPanel("WebView", "WebView");
      var path = typeof(RevitWebView).Assembly.Location;
      var button = panel.AddItem(new PushButtonData("Show WebView", "WebView", path, typeof(RevitWebViewCommand).FullName)) as PushButton;
      
      Window = new BrowserWindow();

      return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {

      return Result.Succeeded;
    }
  }

  public class BrowserWindow
  {
    WebView2Window Window;

    public BrowserWindow()
    {

    }

    public void CreateOrShowWindow()
    {
      if(Window == null)
      {
        Window = new WebView2Window();
        Window.Closing += Window_Closing;
      }

      Window.Show();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Window.Hide();
      e.Cancel = true;
    }
  }

  [Transaction(TransactionMode.Manual)]
  public class RevitWebViewCommand : IExternalCommand
  {
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      //commandData.Application.
      RevitWebView.Window.CreateOrShowWindow();
      //commandData.Application;
      return Result.Succeeded;
    }
  }

#endif
}