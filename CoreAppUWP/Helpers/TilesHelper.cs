using CoreAppUWP.Common;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CoreAppUWP.Helpers
{
    public static class TilesHelper
    {
        public static void UpdateTile() => CreateTile().GetXmlDocument().UpdateTitle();

        private static void UpdateTitle(this XmlDocument xmlDocument)
        {
            TileUpdater tileUpdater = TileUpdateManager.CreateTileUpdaterForApplication();
            tileUpdater.Clear();
            TileNotification tileNotification = new(xmlDocument);
            tileUpdater.Update(tileNotification);
        }

        private static XmlDocument GetXmlDocument(this TileContent tileContent)
        {
            XmlDocument xmlDocument = tileContent.GetXml();
            int i = 1;
            xmlDocument.GetElementsByTagName("binding")
                       .FirstOrDefault(x => x.Attributes?.GetNamedItem("template")?.InnerText == "TileWide").ChildNodes
                       .Where(x => x.NodeName == "text" && x.Attributes?.GetNamedItem("hint-style")?.InnerText == "captionSubtle")
                       .OfType<XmlElement>()
                       .Take(3)
                       .ToArray()
                       .ForEach(x => x.SetAttribute("id", $"{i++}"));
            return xmlDocument;
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
