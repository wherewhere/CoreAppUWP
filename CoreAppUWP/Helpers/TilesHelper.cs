using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Runtime.InteropServices;
using Windows.UI.Notifications;

namespace CoreAppUWP.Helpers
{
    public static class TilesHelper
    {
        public static void UpdateTile() => CreateTile().UpdateTitle();

        private static void UpdateTitle(this TileContent tileContent)
        {
            TileUpdater tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            TileNotification tileNotification = new(tileContent.GetXml());
            tileUpdater.Update(tileNotification);
        }

        private static TileContent CreateTile()
        {
            return new TileContent
            {
                Visual = new TileVisual
                {
                    Branding = TileBranding.NameAndLogo,

                    TileMedium = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text = "Platform",
                                    HintStyle = AdaptiveTextStyle.Caption,
                                },

                                new AdaptiveText
                                {
                                    Text = Environment.Version.ToString(),
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },

                                new AdaptiveText
                                {
                                    Text = Environment.OSVersion.Version.ToString(),
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },

                                new AdaptiveText
                                {
                                    Text = RuntimeInformation.ProcessArchitecture.ToString(),
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text = "Platform Information",
                                    HintStyle = AdaptiveTextStyle.Caption,
                                },

                                new AdaptiveText
                                {
                                    Text = RuntimeInformation.FrameworkDescription,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },

                                new AdaptiveText
                                {
                                    Text = RuntimeInformation.OSDescription,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },

                                new AdaptiveText
                                {
                                    Text = $"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                }
                            }
                        }
                    },

                    TileLarge = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text = "Platform Information",
                                    HintStyle = AdaptiveTextStyle.Caption,
                                },

                                new AdaptiveText
                                {
                                    Text = RuntimeInformation.FrameworkDescription,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },

                                new AdaptiveText
                                {
                                    Text = RuntimeInformation.OSDescription,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                },

                                new AdaptiveText
                                {
                                    Text = $"ProcessArchitecture: {RuntimeInformation.ProcessArchitecture}",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                    HintWrap = true
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
