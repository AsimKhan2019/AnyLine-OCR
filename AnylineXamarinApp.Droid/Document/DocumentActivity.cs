using System;
using Android.App;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AT.Nineyards.Anyline.Models;
using AT.Nineyards.Anyline.Modules.Document;

namespace AnylineXamarinApp.Document
{
    [Activity(Label = "Scan Documents", MainLauncher = false, Icon = "@drawable/ic_launcher", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
    public class DocumentActivity : Activity, IDocumentResultListener
    {
        public static string TAG = typeof(DocumentActivity).Name;

        private DocumentScanView _scanView;
        private ProgressDialog _progressDialog;
        private ImageView _imageViewResult;
        private ImageView _imageViewFull;
        private Toast _toast;
        
        protected override void OnCreate(Bundle bundle)
        {
            Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);

            base.OnCreate(bundle);
            SetContentView(Resource.Layout.DocumentActivity);

            _scanView = FindViewById<DocumentScanView>(Resource.Id.document_scan_view);
            _imageViewResult = FindViewById<ImageView>(Resource.Id.image_result);
            _imageViewFull = FindViewById<ImageView>(Resource.Id.full_image);

            _scanView.SetConfigFromAsset("DocumentConfig.json");

            _scanView.InitAnyline(MainActivity.LicenseKey, this);

            _scanView.CameraOpened += (s, e) =>
            {
                Log.Debug(TAG, "Camera opened successfully. Frame resolution " + e.Width + " x " + e.Height);
                _scanView.StartScanning();
            };

            _scanView.CameraError += (s, e) => { Log.Error(TAG, "OnCameraError: " + e.Event.Message); };

        }

        void IDocumentResultListener.OnResult(AnylineImage transformedImage, AnylineImage fullFrame)
        {
            _toast = Toast.MakeText(BaseContext, "Document scanned successfully.", ToastLength.Short);
            _toast.Show();

            _progressDialog.Dismiss();

            var resImg = transformedImage.Clone();

            _imageViewFull.SetImageBitmap(null);
            _imageViewResult.SetImageBitmap(resImg.Bitmap);

            transformedImage.Release();
            fullFrame.Release();
        }
        
        bool IDocumentResultListener.OnDocumentOutlineDetected(System.Collections.Generic.IList<Android.Graphics.PointF> rect, bool anglesValid) { return false; }

        // handle an error while processing the full picture here - the preview will be restarted automatically
        void IDocumentResultListener.OnPictureProcessingFailure(DocumentScanView.DocumentError error)
        {
            
            String text = "Error scanning full document. ";
            String e = error.ToString();

            if (e.Equals(DocumentScanView.DocumentError.DocumentNotSharp.ToString()))
                text += "Document is not sharp. Please hold the camera steadily and ensure the document is in focus.";
            if (e.Equals(DocumentScanView.DocumentError.DocumentSkewTooHigh.ToString()))
                text += "Document is skewed. Place the camera directly above the document.";
            if (e.Equals(DocumentScanView.DocumentError.DocumentOutlineNotFound.ToString()))
                text += "Could not detect document outline.";
            if (e.Equals(DocumentScanView.DocumentError.GlareDetected.ToString()))
                text += "Please remove the glare.";
            if (e.Equals(DocumentScanView.DocumentError.ImageTooDark.ToString()))
                text += "The image is too dark. Please ensure there is enough light.";
            if (e.Equals(DocumentScanView.DocumentError.Unknown.ToString()))
                text += "Unknown Failure.";

            _toast = Toast.MakeText(this, text, ToastLength.Long);
            _toast.Show();

            if(_progressDialog != null && _progressDialog.IsShowing)
            {
                _progressDialog.Dismiss();
            }

            _imageViewFull.SetImageBitmap(null);
        }

        // this is called on any error while processing the document image
        // Note: this is called every time an error occurs in a run, so that might be quite often
        // An error message should only be presented to the user after some time
        void IDocumentResultListener.OnPreviewProcessingFailure(DocumentScanView.DocumentError error)
        {
            if (_toast != null)
                _toast.Cancel();

            var e = error.ToString();
            string text = "";

            if (e.Equals(DocumentScanView.DocumentError.DocumentNotSharp.ToString()))
                text += "Document is not sharp. Please hold the camera steadily and ensure the document is in focus.";
            if (e.Equals(DocumentScanView.DocumentError.DocumentSkewTooHigh.ToString()))
                text += "Document is skewed. Place the camera directly above the document.";
            if (e.Equals(DocumentScanView.DocumentError.DocumentOutlineNotFound.ToString()))
                text += "Could not detect document outline.";
            if (e.Equals(DocumentScanView.DocumentError.GlareDetected.ToString()))
                text += "Please remove the glare.";
            if (e.Equals(DocumentScanView.DocumentError.ImageTooDark.ToString()))
                text += "The image is too dark. Please ensure there is enough light.";
            if (e.Equals(DocumentScanView.DocumentError.Unknown.ToString()))
                text += "Unknown Failure.";

            if (text != "")
            {
                _toast = Toast.MakeText(this, text, ToastLength.Short);
                _toast.Show();
            }
        }

        // this is called after the preview of the document is completed, and a full picture will be processed automatically
        void IDocumentResultListener.OnPreviewProcessingSuccess(AnylineImage anylineImage)
        {
            if (_toast != null)
                _toast.Cancel();

            _imageViewFull.SetImageBitmap(anylineImage.Bitmap);

            _toast = Toast.MakeText(this, "Scanning full document. Please hold still.", ToastLength.Long);
            _toast.Show();
        }

        // This is called if the image could not be captured from the camera (most probably because of an OutOfMemoryError)
        void IDocumentResultListener.OnTakePictureError(Java.Lang.Throwable error)
        {
            Console.WriteLine(error.Message);
            GC.Collect();
        }

        // this is called after the image has been captured from the camera and is about to be processed
        void IDocumentResultListener.OnTakePictureSuccess()
        {
            _progressDialog = ProgressDialog.Show(this, "Processing", "Processing the picture. Please wait.", true);

            if (_toast != null)
                _toast.Cancel();

        }

        protected override void OnResume()
        {
            base.OnResume();
            _scanView.StartScanning();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _scanView.CancelScanning();
            _scanView.ReleaseCameraInBackground();
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            Finish();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // explicitly free memory to avoid leaks
            GC.Collect(GC.MaxGeneration);
        }
    }

}