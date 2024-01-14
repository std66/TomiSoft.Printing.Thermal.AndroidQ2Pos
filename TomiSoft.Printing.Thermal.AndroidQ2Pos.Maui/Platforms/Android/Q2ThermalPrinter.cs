using Android.Content;
using Android.OS;
using Com.Iposprinter.Iposprinterservice;
using System;
using System.Diagnostics;

namespace TomiSoft.Printing.Thermal.AndroidQ2Pos.Maui.Platforms.Android {
    public class Q2ThermalPrinter : Java.Lang.Object, IServiceConnection, IQ2ThermalPrinter {
        private IPosPrinterService? printerService;

        public static event EventHandler<IQ2ThermalPrinter>? ServiceConnected;
        public event EventHandler<Q2PrinterStatus>? PrinterStatusChanged;

        public void OnServiceConnected(ComponentName? name, IBinder? service) {
            if (service == null)
                return;

            printerService = IPosPrinterServiceStub.AsInterface(service);

            if (printerService != null)
                ServiceConnected?.Invoke(null, this);
        }

        public void OnServiceDisconnected(ComponentName? name) {
            printerService = null;
        }

        public bool IsConnected => printerService != null;

        private IPosPrinterService Printer {
            get {
                if (!IsConnected) {
                    throw new InvalidOperationException($"The printer service is currently not connected. Please check if {nameof(InitializeBinding)} has succeeded.");
                }

                return printerService!;
            }
        }

        private Q2ThermalPrinter() { }

        public static bool InitializeBinding(Context context) {
            Intent i = new();
            i.SetPackage("com.iposprinter.iposprinterservice");
            i.SetAction("com.iposprinter.iposprinterservice.IPosPrintService");

            IServiceConnection serviceConnection = new Q2ThermalPrinter();
            return context.BindService(i, serviceConnection, Bind.AutoCreate);
        }

        public async Task<bool> InitializePrinterAsync() {
            DebugLog("Initializing printer. Checking (and waiting) if printer is busy...");
            await DelayUntilQ2PrinterStatusIs(Q2PrinterStatus.Busy);
            DebugLog("Printer is ready to initialize");

            var t = new TaskCompletionSource<bool>();

            PrinterCallback callback = new();
            callback.RunResult += (o, e) => t.TrySetResult(e);

            Printer.PrinterInit(callback);

            return await t.Task;
        }

        public async Task<bool> SendEscPosCommandsAsync(byte[] commands) {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            Q2PrinterStatus Q2PrinterStatus = await GetPrinterStatusAsync();
            if (Q2PrinterStatus != Q2PrinterStatus.Ready) {
                if (Q2PrinterStatus == Q2PrinterStatus.PrintingHeadOverheat) {
                    DebugLog("Printer head is overheated. Waiting until printer status changes...");
                    await DelayUntilQ2PrinterStatusIs(Q2PrinterStatus.PrintingHeadOverheat);
                }
                else {
                    DebugLog($"Failed to print due to printer status '{Q2PrinterStatus}'");
                    return false;
                }
            }

            //Pro tip: DO NOT call PrinterPerformPrint when SendUserCMDData is used! It will cause
            //a sporadic issue, the printer "forgetting" the first few instructions.
            //PrinterPerformPrint is necessary only when other functions from the AIDL are being used,
            //since they only cache print data (and returns "CACHE PRINTDATA  DATA OK!").

            //var t = new TaskCompletionSource<bool>();

            //PrinterCallback callback = new();
            //callback.ReturnString += (o, e) => t.TrySetResult(e == "NO ERROR");

            //Printer.PrinterPerformPrint(0, callback);

            //await t.Task;

            var t = new TaskCompletionSource<bool>();

            PrinterCallback callback = new();
            callback.ReturnString += (o, e) => t.TrySetResult(e == "UserCMDData is Paesed OK!");

            Printer.SendUserCMDData(commands, callback);

            await t.Task;

            DebugLog("Waiting until status is ready...");
            Q2PrinterStatus = await DelayUntilQ2PrinterStatusIs(Q2PrinterStatus.Ready);

            DebugLog($"Status has changed to '{Q2PrinterStatus}'");

            if (Q2PrinterStatus != Q2PrinterStatus.Busy) {
                return false;
            }

            DebugLog("Waiting until printer is busy...");
            Q2PrinterStatus = await DelayUntilQ2PrinterStatusIs(Q2PrinterStatus.Busy);

            DebugLog($"Final status is '{Q2PrinterStatus}'");
            return Q2PrinterStatus is Q2PrinterStatus.Ready;
        }

        public async Task<Q2PrinterStatus> GetPrinterStatusAsync() {
            return await Task.Run(Printer.GetPrinterStatus) switch {
                0 => Q2PrinterStatus.Ready,
                1 => Q2PrinterStatus.OutOfPaper,
                2 => Q2PrinterStatus.PrintingHeadOverheat,
                3 => Q2PrinterStatus.MotorOverheat,
                4 => Q2PrinterStatus.Busy,
                _ => Q2PrinterStatus.Unknown,
            };
        }

        private async Task<Q2PrinterStatus> DelayUntilQ2PrinterStatusIs(Q2PrinterStatus Q2PrinterStatus) {
            Q2PrinterStatus currentStatus = Q2PrinterStatus.Unknown;

            do {
                var newStatus = await GetPrinterStatusAsync();

                if (newStatus != currentStatus) {
                    PrinterStatusChanged?.Invoke(this, newStatus);
                    currentStatus = newStatus;
                }

                await Task.Delay(100);
            } while (currentStatus == Q2PrinterStatus || currentStatus == Q2PrinterStatus.PrintingHeadOverheat);

            return currentStatus;
        }

        [Conditional("DEBUG")]
        public void DebugLog(string message) {
            System.Diagnostics.Debug.WriteLine($"[{nameof(Q2ThermalPrinter)}] {message}");
        }
    }
}
