﻿

#pragma checksum "C:\Users\Martijn\Documents\Voor\VoorPDMW\Visual Studio 2013\Projects\Followshows\Followshows\ShowPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "E999D9C1F4BFAD70EC6A7A20663CDF71"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Followshows
{
    partial class ShowPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 64 "..\..\ShowPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).DoubleTapped += this.Item_Tapped;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 66 "..\..\ShowPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Image)(target)).ImageFailed += this.Image_ImageFailed;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 21 "..\..\ShowPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Image)(target)).ImageFailed += this.Image_ImageFailed;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 24 "..\..\ShowPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).Tapped += this.Tapped_ShowFullText;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 96 "..\..\ShowPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).DoubleTapped += this.Item_Tapped;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 98 "..\..\ShowPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Image)(target)).ImageFailed += this.Image_ImageFailed;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


