﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if !__MACOS__
using Plugin.Media;
#endif
using Xamarin.Forms;
#if __ANDROID__
using Plugin.CurrentActivity;
#endif

#if __MACOS__
using AppKit;
using CoreGraphics;
#endif

namespace Emgu.TF.XamarinForms
{
    public partial class ButtonTextImagePage : ContentPage
    {

        public ButtonTextImagePage()
        {
            InitializeComponent();
        }

        public virtual async void LoadImages(String[] imageNames, String[] labels = null)
        {

#if (__MACOS__) //Xamarin Mac
            //use default images
            InvokeOnImagesLoaded(imageNames);
#else
	
#if __ANDROID__ || __IOS__
            await CrossMedia.Current.Initialize();
#endif
            String[] mats = new String[imageNames.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                String pickImgString = "Use Image from";
                if (labels != null && labels.Length > i)
                    pickImgString = labels[i];
                bool haveCameraOption;
                bool havePickImgOption;
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    //CrossMedia is not implemented on Windows.
                    haveCameraOption = false;
                    havePickImgOption = true; //We will provide our implementation of pick image option
                }
                else
                {
                    haveCameraOption =
                        (CrossMedia.Current.IsCameraAvailable && CrossMedia.Current.IsTakePhotoSupported);
                    havePickImgOption =
                        CrossMedia.Current.IsPickVideoSupported;
                }

                String action;
                if (haveCameraOption & havePickImgOption)
                {
                    action = await DisplayActionSheet(pickImgString, "Cancel", null, "Default", "Photo Library",
                        "Camera");
					if (action == null) //user clicked outside of action sheet
                        return;
                }
                else if (havePickImgOption)
                {
                    action = await DisplayActionSheet(pickImgString, "Cancel", null, "Default", "Photo Library");
					if (action == null) //user clicked outside of action sheet
                        return;
                }
                else
                {
                    action = "Default";
                }


                if (action.Equals("Default"))
                {
#if __ANDROID__
                    FileInfo fi = Emgu.TF.Util.AndroidFileAsset.WritePermanantFileAsset(CrossCurrentActivity.Current.Activity, imageNames[i], "tmp",
                        Emgu.TF.Util.AndroidFileAsset.OverwriteMethod.AlwaysOverwrite);

                    mats[i] = fi.FullName;

#else
	                if (!File.Exists(imageNames[i]))
                        throw new FileNotFoundException(String.Format("File {0} do not exist.", imageNames[i]));
                    mats[i] = imageNames[i];
            
#endif

                }
                else if (action.Equals("Photo Library"))
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        // our implementation of pick image
#if !(__ANDROID__ || __IOS__ || __MACOS__)
                        using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
                        {
                            dialog.Multiselect = false;
                            dialog.Title = "Select an Image File";
                            dialog.Filter = "Image | *.jpg;*.jpeg;*.png;*.bmp;*.gif | All Files | *";
                            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                            {
                                mats[i] = dialog.FileName;
                            }
                            else
                            {
                                return; 
                            }
                        }
#endif
                    }
                    else
                    {
                        var photoResult = await CrossMedia.Current.PickPhotoAsync();
                        if (photoResult == null) //canceled
                            return;
                        mats[i] = photoResult.Path;
                    }
                }
                else if (action.Equals("Camera"))
                {
                    var mediaOptions = new Plugin.Media.Abstractions.StoreCameraMediaOptions
                    {
                        Directory = "Emgu",
                        Name = $"{DateTime.UtcNow}.jpg"
                    };
                    var takePhotoResult = await CrossMedia.Current.TakePhotoAsync(mediaOptions);
                    if (takePhotoResult == null) //cancelled
                        return;
                    mats[i] = takePhotoResult.Path;
                }

                //Handle user cancel
                if (action == null)
                    return;
            }
            InvokeOnImagesLoaded(mats);
#endif
        }

        public void InvokeOnImagesLoaded(string[] images)
        {
            if (OnImagesLoaded != null)
                OnImagesLoaded(this, images);
        }

        public event EventHandler<string[]> OnImagesLoaded;

        public void SetImage(String fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new Exception(String.Format("File '{0}' do not exist.", fileName));
            }

            Xamarin.Forms.Device.BeginInvokeOnMainThread(
               () =>
               {
                   var imageSource = new FileImageSource();
                   imageSource.File = fileName;
                   this.DisplayImage.Source = imageSource;
#if __MACOS__
                   NSImage image = new NSImage(fileName);
                   this.DisplayImage.WidthRequest = image.Size.Width;
                   this.DisplayImage.HeightRequest = image.Size.Height;
#endif
               });
        }

        public void SetImage(byte[] image = null, int widthRequest = -1, int heightRequest = -1)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(
               () =>
               {
                   if (image == null)
                   {
                       this.DisplayImage.Source = null;
                       return;
                   }

                   this.DisplayImage.Source = ImageSource.FromStream(() => new MemoryStream(image));

                   if (widthRequest > 0)
                       this.DisplayImage.WidthRequest = widthRequest;
                   if (heightRequest > 0)
                       this.DisplayImage.HeightRequest = heightRequest;
               });
#if __IOS__
                    //Xamarin Form's Image class do not seems to re-render after Soure is change
                    //forcing focus seems to force a re-rendering
                    this.DisplayImage.Focus();
#endif
        }

        public Xamarin.Forms.Label GetLabel()
        {
            //return null;
            return this.MessageLabel;
        }

        public void SetMessage(String message)
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(
                () =>
            {
                this.MessageLabel.Text = message;
                this.MessageLabel.LineBreakMode = LineBreakMode.WordWrap;
                this.MessageLabel.WidthRequest = this.Width;
            }
            );
        }

        public Xamarin.Forms.Button GetButton()
        {
            //return null;
            return this.TopButton;
        }

        public Image GetImage()
        {
            //return null;
            return this.DisplayImage;
        }
    }
}
