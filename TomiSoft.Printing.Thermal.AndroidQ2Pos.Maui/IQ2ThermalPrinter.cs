namespace TomiSoft.Printing.Thermal.AndroidQ2Pos.Maui {
    public interface IQ2ThermalPrinter {
        event EventHandler<Q2PrinterStatus> PrinterStatusChanged;
        bool IsConnected { get; }
        Task<bool> InitializePrinterAsync();
        Task<bool> SendEscPosCommandsAsync(byte[] commands);
        Task<Q2PrinterStatus> GetPrinterStatusAsync();
    }
}
