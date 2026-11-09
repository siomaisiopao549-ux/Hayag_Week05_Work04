namespace ProductFormApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new DashboardForm());   // Dashboard is the main screen
    }
}
