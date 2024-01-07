using Com.Iposprinter.Iposprinterservice;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TomiSoft.Printing.Thermal.AndroidQ2Pos.Maui.Platforms.Android {
    internal class PrinterCallback : IPosPrinterCallbackStub {
        private readonly string operationName;

        public event EventHandler<string>? ReturnString;
        public event EventHandler<bool>? RunResult;

        public PrinterCallback([CallerMemberName] string operationName = "") {
            this.operationName = operationName;
        }

        public override void OnReturnString(string result) {
#if DEBUG
            Debug.WriteLine($"{nameof(PrinterCallback)}.{nameof(OnReturnString)} (operation = '{operationName}'): {result}");
#endif
            ReturnString?.Invoke(this, result);
        }

        public override void OnRunResult(bool isSuccess) {
#if DEBUG
            Debug.WriteLine($"{nameof(PrinterCallback)}.{nameof(OnRunResult)} (operation = '{operationName}'): {isSuccess}");
#endif

            RunResult?.Invoke(this, isSuccess);
        }
    }
}
