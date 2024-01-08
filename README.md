# TomiSoft.Printing.Thermal.AndroidQ2Pos
.NET thermal printer support library for touchscreen Android Q2 POS terminals.

## Supported devices and platforms
Tested with an Android Q2 device with 1 GiB RAM, running Android 6.0 / API 23, targeting .NET 8.
Available for Android only, even though the IQ2ThermalPrinter interface is available for all platforms.

## Where is the NuGet package
I didn't set up CI/CD pipeline for this repo. Maybe I will in the future, maybe not. Pull in this project
into your solution.

## Setting up
Initialize the binding in your MAUI application, in file Platforms/Android/MainActivity.cs

```csharp
public class MainActivity : MauiAppCompatActivity {
  protected override void OnCreate(Bundle? savedInstanceState) {
    //...
    Q2ThermalPrinter.ServiceConnected += (o, e) => DependencyService.RegisterSingleton<IQ2ThermalPrinter>(e);
    bool bindSuccessful = Q2ThermalPrinter.InitializeBinding(this);
    //...
  }
}
```

## Usage
The printer works by sending ESC/POS commands. Use a library like https://github.com/igorocampos/ESCPOS

```csharp
//Create ESC/POS package to send
List<byte> result = new();
result.AddRange(ESCPOS.Commands.InitializePrinter);
//TODO: Create commands to whatever you want to print
//Pro tip: Sending QR code ESC/POS command will cause the printer to print it twice. This may be the case with other
//         barcode formats as well. Generate it as bitmap and send that to the printer to fix this issue.

//It is recommended to use 6-8 line feeds at the end with default font size for a good cutting point.
for (int i = 0; i < 8; i++)
  result.AddRange(ESCPOS.Commands.LineFeed);

//Send data to the printer
IQ2ThermalPrinter printer = DependencyService.Get<IQ2ThermalPrinter>();
if (printer is null) {
  //Q2ThermalPrinter.InitializeBinding has failed.
  return;
}

if (!await printer.InitializePrinterAsync()) {
  //Failed to initialize the printer
  return;
}

//You can subscribe to an event that reports printer status changes during printing.
printer.PrinterStatusChanged += (o, status) => {
  //TODO: inform the user of status changes.

  //Recommendation: Only inform from Q2PrinterStatus.PrintingHeadOverheat, Q2PrinterStatus.Ready and Q2PrinterStatus.Busy. Other statuses will cause the
  //                printing to fail. You can check that reason later in the code.
};

if (!await printer.SendEscPosCommandsAsync(result.ToArray())) {
  //Printing has failed. Check printer status. You may not expect Q2PrinterStatus.PrintingHeadOverheat
  //since the printing will automatically resume. This library handles this case (at least, it should).
  Q2PrinterStatus status = await printer.GetPrinterStatusAsync();

  //Out of paper and other statuses are reported here. In case of Q2PrinterStatus.MotorOverheat you need to reinitialize the printer
  //with printer.InitializePrinterAsync according to the docs (not tested functionality, never occurred to me).
  //I also suggest reinitialization on each print.
}
```
