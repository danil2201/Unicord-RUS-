﻿
#if false
#define HAS_WEBVIEW_2
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Unicord.Universal.Services;

#if HAS_WEBVIEW_2
using Microsoft.Web.WebView2.Core;
#endif

#pragma warning disable CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace Unicord.Universal.Controls
{
    /// <summary>
    /// Provides a lightweight wrapper around WebView or WebView2, whichever is available.
    /// </summary>
    public class UniversalWebView : Control
    {
        private bool _isLoaded;
        private Border _root;
        private Border _fullscreenBorder;
        private WebView _webView;

#if HAS_WEBVIEW_2
        private WebView2 _webView2;
        private static Lazy<bool> IsWebView2Available = new Lazy<bool>(() =>
        {
            try { _ = CoreWebView2Environment.GetAvailableBrowserVersionString(); return true; } catch { return false; }
        });
#endif

        public Uri Source { get => (Uri)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(UniversalWebView), new PropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UniversalWebView view)
                return;

#if HAS_WEBVIEW_2
            if (view._webView2 != null)
                view._webView2.Source = (Uri)e.NewValue;
#endif
            if (view._webView != null)
                view._webView.Source = (Uri)e.NewValue;
        }

        public string Title { get => (string)GetValue(TitleProperty); private set => SetValue(TitleProperty, value); }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(UniversalWebView), new PropertyMetadata(""));

        public UniversalWebView()
        {
            DefaultStyleKey = typeof(UniversalWebView);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (_isLoaded) return;
            _isLoaded = true;

            _root = this.FindChild<Border>("PART_Root");
            _fullscreenBorder = Window.Current.Content.FindChild<Border>("FullscreenBorder");

#if HAS_WEBVIEW_2
            if (IsWebView2Available.Value)
            {
                _webView2 = new WebView2 { Source = Source };
                _webView2.CoreWebView2Initialized += OnCoreWebView2Initialized;

                _root.Child = _webView2;
                return;
            }
#endif

            var executionMode = WebViewExecutionMode.SeparateThread;
            if (ApiInformation.IsEnumNamedValuePresent("Windows.UI.Xaml.Controls.WebViewExecutionMode", "SeparateProcess"))
                executionMode = WebViewExecutionMode.SeparateProcess;

            _webView = new WebView(executionMode) { Source = Source };
            _webView.ContainsFullScreenElementChanged += OnWebViewFullScreenElementChanged;

            _root.Child = _webView;
        }

        private void OnWebViewFullScreenElementChanged(WebView sender, object args)
        {
            var service = FullscreenService.GetForCurrentView();
            if (sender.ContainsFullScreenElement)
                service.EnterFullscreen(_webView, _root);
            else
                service.LeaveFullscreen(_webView, _root);
        }

#if HAS_WEBVIEW_2
        private void OnCoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            sender.CoreWebView2.DocumentTitleChanged += OnWebView2DocumentTitleChanged;
            sender.CoreWebView2.ContainsFullScreenElementChanged += OnWebView2FullScreenElementChanged;
        }

        private void OnWebView2DocumentTitleChanged(CoreWebView2 sender, object args)
        {
            this.Title = sender.DocumentTitle;
        }

        private void OnWebView2FullScreenElementChanged(CoreWebView2 sender, object args)
        {
            //var service = FullscreenService.GetForCurrentView();
            //if (sender.ContainsFullScreenElement)
            //    service.EnterFullscreen(_webView2, _root);
            //else
            //    service.LeaveFullscreen(_webView2, _root);

            if (sender.ContainsFullScreenElement)
            {
                _root.Child = null;
                _fullscreenBorder.Child = _webView2;
                _fullscreenBorder.Visibility = Visibility.Visible;
            }
            else
            {
                _fullscreenBorder.Child = null;
                _fullscreenBorder.Visibility = Visibility.Collapsed;
                _root.Child = _webView2;
            }
        }
#endif
    }
}

#pragma warning restore CS8305 // Type is for evaluation purposes only and is subject to change or removal in future updates.