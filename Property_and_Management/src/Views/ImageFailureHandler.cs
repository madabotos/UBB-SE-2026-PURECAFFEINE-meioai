using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Property_and_Management.Src.Views
{
    /// <summary>
    /// Shared <c>ImageFailed</c> fallback logic for the five pages that render
    /// game thumbnails. Previously each page duplicated the same ~30 lines.
    ///
    /// Resolution order:
    /// 1. If the failing image is already the default placeholder, do nothing
    ///    (prevents infinite recursion when the asset itself cannot be loaded).
    /// 2. Try the page's own <c>DefaultGameImage</c> resource.
    /// 3. Fall back to <see cref="Application.Current"/>'s resource dictionary.
    /// 4. Last-resort: construct a fresh BitmapImage pointing at the assets URI.
    /// </summary>
    internal static class ImageFailureHandler
    {
        private const string DefaultGameImageKey = "DefaultGameImage";
        private const string DefaultGameImageAssetUri = "ms-appx:///Assets/default-game-placeholder.jpg";
        private const string DefaultGameImageAssetSuffix = "/Assets/default-game-placeholder.jpg";

        public static void HandleFailure(Image failedImage, ResourceDictionary pageResources)
        {
            if (failedImage == null)
            {
                return;
            }

            if (failedImage.Source is BitmapImage current &&
                current.UriSource != null &&
                current.UriSource.AbsoluteUri.EndsWith(DefaultGameImageAssetSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (pageResources != null &&
                pageResources.TryGetValue(DefaultGameImageKey, out var localResource) &&
                localResource is BitmapImage localImage)
            {
                failedImage.Source = localImage;
                return;
            }

            if (Application.Current.Resources.TryGetValue(DefaultGameImageKey, out var appResource) &&
                appResource is BitmapImage appImage)
            {
                failedImage.Source = appImage;
                return;
            }

            failedImage.Source = new BitmapImage(new Uri(DefaultGameImageAssetUri));
        }
    }
}
