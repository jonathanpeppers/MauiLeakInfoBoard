﻿

using InfoBoard.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using QRCoder;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.Win32;
using System.Windows.Input;
using System.Diagnostics.Metrics;
using InfoBoard.Models;
using System.Reflection;

namespace InfoBoard.ViewModel
{
    public sealed class RegisterDeviceViewModel : INotifyPropertyChanged
    {
        private static readonly RegisterDeviceViewModel instance = new();
        static RegisterDeviceViewModel()
        {
        }
       
        public static RegisterDeviceViewModel Instance {
            get {
                return instance;
            }
        }   
        public event PropertyChangedEventHandler PropertyChanged;
        System.Timers.Timer aTimer = new System.Timers.Timer();

        private string _registerKeyLabel;
        private string _qrImageButton;
        private string _status;
        private int counter;

        public Command OnQRImageButtonClickedCommand { get; set; }
        private RegisterDeviceViewModel() 
        {
            counter = 0;
            //Initial Code Generation t
            generateQrCode();

            OnQRImageButtonClickedCommand = new Command(
               execute: async () =>
               {
                   generateQrCode();
                   startRegistration();
                   // Navigate to the specified URL in the system browser.
                   await Launcher.Default.OpenAsync($"https://guzelboard.com/index.php?action=devices&temporary_code={Constants.TEMPORARY_CODE}"); 

               });
            //Set timer to call to register with new code
            //startTimedRegisterationEvent();
        }

        public void startRegistration()
        {
            counter++;
            Task.Run(() => registerDeviceViaServer()).Wait();          
        }

        public void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string RegisterationKey {
            get => _registerKeyLabel;
            set {
                if (_registerKeyLabel == value)
                    return;
                _registerKeyLabel = value;
                OnPropertyChanged();
            }
        }
      
        public string QRImageButton {
            get => _qrImageButton;
            set {
                if (_qrImageButton == value)
                    return;
                _qrImageButton = value;
                OnPropertyChanged();
            }
        }
        public string Status {
            get => _status ;
            set {
                if (_status == value)
                    return;
                _status = value;
                OnPropertyChanged();
            }
        }

        private void startTimedRegisterationEvent()
        {            
            //aTimer.Elapsed += updateQrCodeImageAndRegisterDevice;
            aTimer.Elapsed += async (sender, e) => await registerDeviceViaServer();
         
            aTimer.Start();
            aTimer.Interval = 180 * 1000;      // This should be like 15 seconds or more      
          
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            _status = "Timed Registration Event Created";
            OnPropertyChanged();

        }
        private void generateQrCode() 
        { 
            //Reset the temporary code and handshake URL
            Constants.resetTemporaryCodeAndHandshakeURL();

            _registerKeyLabel = "Temporary Code:" + Constants.TEMPORARY_CODE;            

            //Give full path to API with QR Code 
            //string qrCodeContent = Constants.HANDSHAKE_URL + Constants.TEMPORARY_CODE;
            createQrCrCodeImage(Constants.HANDSHAKE_URL);
            _qrImageButton = Path.Combine(Constants.MEDIA_DIRECTORY_PATH, Constants.QR_IMAGE_NAME_4_TEMP_CODE);

            _status = "New QR Code Generated";
            OnPropertyChanged(nameof(RegisterationKey));
            OnPropertyChanged(nameof(QRImageButton));            
            OnPropertyChanged(nameof(Status));
            
            // Navigate to the specified URL in the system browser.
            // await Launcher.Default.OpenAsync(Constants.HANDSHAKE_URL);            
        }

        private async Task<string> registerDeviceViaServer()
        {
            if (!UtilityServices.isInternetAvailable())
            {             
                _status = "No Internet Connection";
                OnPropertyChanged(nameof(Status));
                return _status;
            }

            _status = "Registering Device";
           
            //Register Device
            RegisterDevice register = new RegisterDevice();            
            RegisterationResult registrationResult = await (register.attemptToRegister());
            
            if (registrationResult.error == null)
            {
                DeviceSettingsService service = DeviceSettingsService.Instance;
                DeviceSettings deviceSettings=  service.readSettingsFromLocalJSON();
                _status = $"Already registered device. \nDevice ID: {deviceSettings.device_key}";
                aTimer.Stop(); // Timer needs to be stopped after successful registration               
            }
            else
            {                
                _status = $"Registration Failed. \nError: {registrationResult.error} \nAttempt {counter}";                
            }
            OnPropertyChanged(nameof(Status));
            return _status;
        }


        private void createQrCrCodeImage(string content)
        {
            var image = generateImage(content, (qr) => qr.GetGraphic(10) as Image<Rgba32>);
            saveImageToFile(Constants.MEDIA_DIRECTORY_PATH, Constants.QR_IMAGE_NAME_4_TEMP_CODE, image);

        }
        private Image<Rgba32> generateImage(string content, Func<QRCode, Image<Rgba32>> getGraphic)
        {
            QRCodeGenerator gen = new QRCodeGenerator();
            QRCodeData data = gen.CreateQrCode(content, QRCodeGenerator.ECCLevel.H);
            return getGraphic(new QRCode(data));
        }

        private void saveImageToFile(string path, string imageName, Image<Rgba32> image)
        {
            if (String.IsNullOrEmpty(path))
                return;

            image.Save(Path.Combine(path, imageName));
        }
    }
}