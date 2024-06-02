namespace Void.EXStremio.Web {
    public class WebServer {
        public Task RunAsync() {
            return Task.Run(() => {
                Program.Main(new string[0]);
            });
        }
    }
}